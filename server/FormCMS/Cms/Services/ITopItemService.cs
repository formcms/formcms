namespace FormCMS.Activities.Services;

public interface ITopItemService
{
    Task<Record[]> GetTopItems(string entityName, int topN, CancellationToken ct);
}