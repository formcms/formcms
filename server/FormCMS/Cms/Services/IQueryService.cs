using FormCMS.Cms.Graph;
using FormCMS.Core.Descriptors;

namespace FormCMS.Cms.Services;

public interface IQueryService
{
    Task<Record[]> ListWithAction(GraphQlRequestDto dto);
    Task<Record[]> ListWithAction(string name, Span span, Pagination pagination, StrArgs args, CancellationToken ct);
    Task<Record?> SingleWithAction(GraphQlRequestDto dto);
    Task<Record?> SingleWithAction(string name, StrArgs args, CancellationToken ct);
    Task<Record[]> Partial(string name, string attr, long sourceId, Span span, int limit, StrArgs args,
        CancellationToken ct);
   
}