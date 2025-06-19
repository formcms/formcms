using FormCMS.Core.Descriptors;

namespace FormCMS.Cms.Services;

public interface IPageService
{
    Task<string> Get(string name, StrArgs args, string? nodeId = null, long? sourceId = null, Span? span = null,
        CancellationToken ct = default);

    Task<string> GetDetail(string name, string slug, StrArgs strArgs, string? nodeId, long? sourceId,
        Span span, CancellationToken ct);
}