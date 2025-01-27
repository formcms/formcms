using System.Text.Json;
using FormCMS.Utils.HttpClientExt;
using FormCMS.Utils.jsonElementExt;
using FormCMS.Utils.ResultExt;
using FluentResults;
using FormCMS.DataLink.Types;
using FormCMS.Infrastructure.DocumentDbDao;
using FormCMS.Infrastructure.EventStreaming;

namespace FormCMS.DataLink.Workers;

public sealed class SyncWorker(
    IStringMessageConsumer consumer,
    IDocumentDbDao dao,
    ILogger<SyncWorker> logger,
    ApiLinks[] links) : BackgroundService
{
    private readonly Dictionary<string, ApiLinks> _dict = links.ToDictionary(x => x.Entity, x => x);

    private readonly HttpClient _httpClient = new();
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await consumer.Subscribe(async s =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<RecordMessage>(s);
                if (message is null)
                {
                    logger.LogWarning("Could not deserialize message");
                    return;
                }

                if (!_dict.TryGetValue(message.EntityName, out var apiLinks))
                {
                    logger.LogWarning("entity [{message.EntityName}] is not in Feed Dictionary, ignore the message",
                        message.EntityName);
                }

                switch (message.Operation)
                {
                    case Operations.Create:
                    case Operations.Update:
                        if (!(await FetchSaveSingle(apiLinks!, message.Id)).Try(out var err))
                        {
                            logger.LogWarning("failed to fetch and save single item, err ={err}", err);
                        }
                        break;
                    case Operations.Delete:
                        await dao.Delete(apiLinks!.Collection, message.Id);
                        break;
                    default:
                        logger.LogWarning("unknown operation {message.Operation}, ignore the message",
                            message.Operation);
                        break;
                }

                logger.LogInformation(
                    "consumed message successfully, entity={message.EntityName}, operation={message.Operation}, id = {message.Id}",
                    message.EntityName, message.Operation, message.Id);
            }
            catch (Exception e)
            {
                logger.LogError("Fail to handler message, err= {error}", e.Message);
            }

        }, ct);
    }

    private async Task<Result> FetchSaveSingle(ApiLinks links, string id )
    {
        if (!(await _httpClient.GetResult<JsonElement>($"{links.Api}/single?{links.PrimaryKey}={id}"))
            .Try(out var s, out var e))
        {
            return Result.Fail(e).WithError("Failed to fetch single data");
        } 
        return await Result.Try(() => dao.Upsert(links.Collection, links.PrimaryKey, s.ToDictionary()));
    }
}