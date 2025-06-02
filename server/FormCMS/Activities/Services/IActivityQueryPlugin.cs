using FormCMS.Core.Descriptors;

namespace FormCMS.Activities.Services;

public interface IActivityQueryPlugin
{
    Task LoadCounts(LoadedEntity entity, IEnumerable<ExtendedGraphAttribute> attributes, IEnumerable<Record> records, CancellationToken ct);
}