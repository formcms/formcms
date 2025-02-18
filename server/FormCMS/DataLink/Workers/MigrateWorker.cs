using System.Text.Json;
using FormCMS.Utils.HttpClientExt;
using FormCMS.Utils.jsonElementExt;
using FormCMS.Utils.ResultExt;
using FluentResults;
using FormCMS.Core.Descriptors;
using FormCMS.DataLink.Types;
using FormCMS.Infrastructure.DocumentDbDao;

namespace FormCMS.DataLink.Workers;


public class MigrateWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<MigrateWorker> logger,
    ApiLinks[] linksArray
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {

        const string coll = "Progress";
        while (!ct.IsCancellationRequested)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var dao = scope.ServiceProvider.GetRequiredService<IDocumentDbDao>();
            logger.LogInformation("Wakeup migrate worker...");
            var progresses = await dao.All(coll);
            foreach (var link in linksArray)
            {
                if (progresses.FirstOrDefault(x => x["collection"] is string s && s == link.Collection) is not null)
                {
                    logger.LogInformation("[{collection}] is migrated, ignoring...", link.Collection);
                    continue;
                }

                var res = await BatchSaveData(dao,link);
                if (res.IsSuccess)
                {
                    logger.LogInformation("Sync [{collection}] is finished...", link.Collection);
                    await dao.Upsert(coll, "collection", link.Collection, new { collection = link.Collection });
                }
            }
            await Task.Delay(1000 * 30, ct); // ✅ Prevents blocking
        }

        logger.LogInformation("Exiting migrate worker...");
    }

    private async Task<Result> BatchSaveData(IDocumentDbDao dao,ApiLinks links)
    {
        var res = await FetchAndSave(dao,links, "");
        while (res.IsSuccess)
        {
            var (curr, next) = res.Value;
            logger.LogInformation("succeed to download data from links={links}, cursor = {curr}", links, curr);
            if (next == "")
            {
                break;
            }

            res = await FetchAndSave(dao,links, next);
        }

        if (res.IsFailed)
        {
            var msg = string.Join(",", res.Errors.Select(x => x.Message));
            logger.LogError("Failed to execute Batch save data, links ={links},err={msg}", links, msg);
            return Result.Fail(res.Errors);
        }

        logger.LogInformation("Finished executing batch save for {links}", links);
        return Result.Ok();
    }

    private async Task<Result<(string curosr, string next)>> FetchAndSave(IDocumentDbDao dao,ApiLinks links, string cursor)
    {
        var url = links.Api + $"?last={cursor}";
        if (!(await new HttpClient().GetStringResult(url)).Try(out var s, out var err))
        {
            return Result.Fail(err).WithError("Failed to fetch data");
        }

        var elements = JsonSerializer.Deserialize<JsonElement[]>(s);
        if (elements is null || elements.Length == 0)
        {
            return (cursor, "");
        }

        var items = elements.Select(x => x.ToDictionary()).ToArray();
        var nextCursor = SpanHelper.HasNext(items) ? SpanHelper.LastCursor(items) : "";

        foreach (var item in items)
        {
            SpanHelper.RemoveCursorTags(item);
        }

        try
        {
            await dao.BatchInsert(links.Collection, items);
        }
        catch (Exception e)
        {
            return Result.Fail(e.Message);
        }

        return (cursor, nextCursor);
    }
}