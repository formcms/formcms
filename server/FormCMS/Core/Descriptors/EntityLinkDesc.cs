namespace FormCMS.Core.Descriptors;

public record CollectiveQueryArgs(ValidFilter[] Filters, ValidSort[] Sorts,ValidPagination? Pagination,ValidSpan? Span);
public record EntityLinkDesc(
    LoadedAttribute SourceAttribute,
    LoadedEntity TargetEntity,
    LoadedAttribute TargetAttribute,
    bool IsCollective,
    Func<IEnumerable<GraphAttribute> , ValidValue[] , CollectiveQueryArgs? , PublicationStatus? , SqlKata.Query> GetQuery);