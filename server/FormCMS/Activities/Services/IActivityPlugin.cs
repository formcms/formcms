using FormCMS.Core.Descriptors;

namespace FormCMS.Activities.Services;

public interface IActivityPlugin
{
    Task LoadCounts(LoadedEntity entity, HashSet<string>fields, IEnumerable<Record> records, CancellationToken ct);
    Entity[] ExtendEntities(IEnumerable<Entity> entities);
}