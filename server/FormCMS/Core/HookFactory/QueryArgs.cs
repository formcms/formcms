using System.Collections.Immutable;
using FormCMS.Core.Descriptors;

namespace FormCMS.Core.HookFactory;

public record QueryPreGetListArgs(
    LoadedQuery Query,
    ImmutableArray<ValidFilter> Filters,
    ImmutableArray<ValidSort> Sorts,
    ValidSpan Span,
    ValidPagination Pagination,
    Record[]? OutRecords = null) : BaseArgs(Query.Name);

public record QueryPreGetSingleArgs(LoadedQuery Query, ImmutableArray<ValidFilter> Filters, Record? OutRecord = null):BaseArgs(Query.Name) ;