using FormCMS.Core.Descriptors;

namespace FormCMS.Cms.Services;

public interface IContentTagService
{
    Task<ContentTag[]> GetContentTags(LoadedEntity entity, string[] ids, CancellationToken ct);
}