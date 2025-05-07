using FormCMS.Utils.DisplayModels;

namespace FormCMS.Activities.Services;

public interface IActivityService
{
    Task<Record[]> GetTopVisitPages(int topN, CancellationToken ct);
    Task<Record[]> GetDailyPageVisitCount(int daysAgo, bool authed, CancellationToken ct);
    Task<Record[]> GetDailyActivityCount(int daysAgo,CancellationToken ct); 
    Task<ListResponse> List(string activityType, StrArgs args, int? offset, int? limit, CancellationToken ct);
    Task Delete(long id, CancellationToken ct = default);
}