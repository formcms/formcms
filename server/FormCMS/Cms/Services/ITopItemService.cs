namespace FormCMS.Activities.Services;

public interface ITopItemService
{
    Task<Record[]> GetTopItems(string entityName, int offset,int limit, CancellationToken ct);
}