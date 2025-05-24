using System.Collections.Immutable;
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
    ImmutableArray<string> Fields,
    ImmutableArray<ValidFilter> Filters,
    ImmutableArray<ValidSort> Sorts,
    ValidSpan Span,
    ValidPagination Pagination,
    Record[] RefRecords 
) : BaseArgs(Query.Name);

public record QueryPreSingleArgs(
    LoadedQuery Query,
    ImmutableArray<ValidFilter> Filters,
    Record? OutRecord = null
) : BaseArgs(Query.Name);

public record QueryPostSingleArgs(
    LoadedQuery Query,
    ImmutableArray<ValidFilter> Filters,
    Record RefRecord
) : BaseArgs(Query.Name);

public record ExtendingEntityArgs(
    ImmutableArray<Entity> entities
) : BaseArgs("");