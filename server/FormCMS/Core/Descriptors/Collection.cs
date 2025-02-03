namespace FormCMS.Core.Descriptors;

public record Collection(LoadedEntity SourceEntity, LoadedEntity TargetEntity, LoadedAttribute LinkAttribute);

public static class CollectionHelper
{
    public static SqlKata.Query List(
        this Collection c,
        IEnumerable<ValidFilter> filters,
        ValidSort[] sorts,
        ValidPagination? pagination,
        ValidSpan? span,
        IEnumerable<LoadedAttribute> attrs,
        IEnumerable<ValidValue> parentsIds,
        PublicationStatus? status
    )
    {
        var query = c.TargetEntity.GetCommonListQuery(filters, sorts, pagination,span, attrs,status);
        query.WhereIn(c.LinkAttribute.Field, parentsIds.GetValues());
        return query;
    }

    public static SqlKata.Query Count(this Collection c, IEnumerable<ValidFilter> filters, IEnumerable<ValidValue> parentIds)
    {
        var query = c.TargetEntity.GetCommonCountQuery(filters);
        query.WhereIn(c.LinkAttribute.Field, parentIds.GetValues());
        return query;
    }
}