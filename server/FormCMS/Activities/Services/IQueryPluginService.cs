namespace FormCMS.Activities.Services;

public interface IQueryPluginService
{
    Task LoadCounts(string entityName, string primaryKey, HashSet<string>fields, IEnumerable<Record> records, CancellationToken ct);
}