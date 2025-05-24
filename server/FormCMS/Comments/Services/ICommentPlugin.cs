using FormCMS.Core.Descriptors;

namespace FormCMS.Comments.Services;

public interface ICommentPlugin
{
    Task LoadComments(LoadedEntity entity, HashSet<string>fields, IEnumerable<Record> records, CancellationToken ct);
    Entity[] ExtendEntities(IEnumerable<Entity> entities);
}