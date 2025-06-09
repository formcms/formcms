using System.Collections.Immutable;
using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;

namespace FormCMS.Core.HookFactory;

public record QueryPreListArgs(
    LoadedQuery Query,
    ImmutableArray<ValidFilter> Filters,
    ImmutableArray<ValidSort> Sorts,
    ValidSpan Span,
    ValidPagination Pagination,
    Record[]? OutRecords = null
) : BaseArgs(Query.Name);

public record QueryPostListArgs(
    LoadedQuery Query,
    ValidSpan Span,
    ValidPagination Pagination,
    Record[] RefRecords 
) : BaseArgs(Query.Name);

public record QueryPreSingleArgs(
    LoadedQuery Query,
    Record? OutRecord = null
) : BaseArgs(Query.Name);

public record QueryPostSingleArgs(
    LoadedQuery Query,
    Record RefRecord
) : BaseArgs(Query.Name);

public record QueryPartialArgs(
    LoadedQuery Query,
    GraphNode Node,
    Span Span,
    long SourceId,
    Record[]? OutRecords  = null 
):BaseArgs(Query.Name);

public record BuildInQueryArgs(
    string Name,
    Span Span,
    Pagination Pagination,
    StrArgs Args,
    Record[]? OutRecords = null
) : BaseArgs(Name);