using FormCMS.Core.Descriptors;

namespace FormCMS.Cms.Services;

public interface IEntityLinkService
{
    Task<Link[]> GetLinks(LoadedEntity entity, string[] ids, CancellationToken ct);
}