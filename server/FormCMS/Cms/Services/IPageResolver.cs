using FormCMS.Core.Descriptors;

namespace FormCMS.Cms.Services;

public interface IPageResolver
{
    Task<Schema> GetPage(string path, CancellationToken ct);
    Task<Schema> GetPage(string name, bool matchPrefix, PublicationStatus? status, CancellationToken ct);
}