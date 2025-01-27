using FluentResults;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.KateQueryExt;
using Humanizer;

namespace FormCMS.CoreKit.RelationDbQuery;

public static class KateQueryExt
{
    public static void ApplyJoin(this SqlKata.Query query, IEnumerable<AttributeVector> vectors, bool onlyPublished )
    {
        var root = AttributeTreeNode.Parse(vectors);
        bool hasCollection = false;
        Bfs(root, "");
        if (hasCollection)
        {
            query.Distinct();
        }

        void Bfs(AttributeTreeNode node, string prefix)
        {
            var nextPrefix = prefix;

            //root doesn't have attribute
            if (node.Attribute is not null)
            {
                nextPrefix = prefix == ""
                    ? node.Attribute.Field
                    : AttributeVectorConstants.Separator + node.Attribute.Field;

                var desc = node.Attribute.GetEntityLinkDesc().Value;
                if (desc.IsCollective) hasCollection = true;

                _ = node.Attribute.DataType switch
                {
                    DataType.Junction => ApplyJunctionJoin(query, node.Attribute.Junction!, prefix, nextPrefix, onlyPublished),
                    DataType.Lookup or DataType.Collection => ApplyJoin(query, desc, prefix, nextPrefix, onlyPublished),
                    _ => query
                };
            }

            foreach (var sub in node.Children)
            {
                Bfs(sub, nextPrefix);
            }
        }
    }

    private static SqlKata.Query ApplyJoin(SqlKata.Query query, EntityLinkDesc desc, string prefix, string nextPrefix, bool onlyPublished)
    {
        query.LeftJoin($"{desc.TargetEntity.TableName} as {nextPrefix}",
                desc.SourceAttribute.AddTableModifier(prefix),
                desc.TargetAttribute.AddTableModifier(nextPrefix))
            .Where(desc.TargetEntity.DeletedAttribute.AddTableModifier(nextPrefix), false);
        if (onlyPublished)
        {
            query = query.WhereCamelEnum(desc.TargetEntity.PublicationStatusAttribute.AddTableModifier(nextPrefix), PublicationStatus.Published);
        }
        return query;
    }

    private static SqlKata.Query ApplyJunctionJoin(SqlKata.Query query, Junction junction, 
        string prefix, string nextPrefix, bool onlyPublished)
    {
        var crossAlias = $"{nextPrefix}_{junction.JunctionEntity.TableName}";
        query
            .LeftJoin($"{junction.JunctionEntity.TableName} as {crossAlias}",
                junction.SourceEntity.PrimaryKeyAttribute.AddTableModifier(prefix),
                junction.SourceAttribute.AddTableModifier(crossAlias))
            .LeftJoin($"{junction.TargetEntity.TableName} as {nextPrefix}",
                junction.TargetAttribute.AddTableModifier(crossAlias),
                junction.TargetEntity.PrimaryKeyAttribute.AddTableModifier(nextPrefix))
            .Where(junction.JunctionEntity.DeletedAttribute.AddTableModifier(crossAlias), false)
            .Where(junction.TargetEntity.DeletedAttribute.AddTableModifier(nextPrefix), false);
        if (onlyPublished)
        {
            query = query
                .WhereCamelEnum(junction.TargetEntity.PublicationStatusAttribute.AddTableModifier(nextPrefix), PublicationStatus.Published);
        }
        return query;
    }


    public static void ApplyPagination(this SqlKata.Query query, ValidPagination pagination)
    {
        query.Offset(pagination.Offset).Limit(pagination.Limit);
    }
    public static void ApplySorts(this SqlKata.Query query, IEnumerable<ValidSort> sorts)
    {
        foreach (var sort in sorts)
        {
            var vector = sort.Vector;
            if (sort.Order == SortOrder.Desc)
            {
                query.OrderByDesc(vector.Attribute.AddTableModifier(vector.TableAlias));
            }
            else
            {
                query.OrderBy(vector.Attribute.AddTableModifier(vector.TableAlias));
            }
        }
    }

    public static Result ApplyFilters(this SqlKata.Query query, IEnumerable<ValidFilter> filters)
    {
        var result = Result.Ok();
        foreach (var filter in filters)
        {
            var filedName = filter.Vector.Attribute.AddTableModifier(filter.Vector.TableAlias);
            query.Where(q =>
            {
                foreach (var c in filter.Constraints)
                {
                    var ret = filter.MatchType == MatchTypes.MatchAny
                        ? q.ApplyOrConstraint(filedName, c.Match, c.Values.GetValues())
                        : q.ApplyAndConstraint(filedName, c.Match, c.Values.GetValues());
                    if (ret.IsFailed)
                    {
                        result.WithErrors(ret.Errors);
                        break;
                    }
                }

                return q;
            });
        }

        return result;
    }

    private static Result<SqlKata.Query> ApplyAndConstraint(this SqlKata.Query query, string field, string match, object?[] values)
    {
        return match switch
        {
            Matches.StartsWith => query.WhereStarts(field, values[0]),
            Matches.Contains => query.WhereContains(field, values[0]),
            Matches.NotContains => query.WhereNotContains(field, values[0]),
            Matches.EndsWith => query.WhereEnds(field, values[0]),
            Matches.EqualsTo =>  query.Where(field, values[0]),
            Matches.NotEquals => query.WhereNot(field, values[0] ),
            Matches.NotIn => query.WhereNotIn(field, values),
            Matches.In => query.WhereIn(field, values),
            Matches.Lt => query.Where(field, "<", values[0]),
            Matches.Lte => query.Where(field, "<=", values[0]),
            Matches.Gt => query.Where(field, ">", values[0]),
            Matches.Gte => query.Where(field, ">=", values[0]),
            Matches.DateIs => query.WhereDate(field, values[0]),
            Matches.DateIsNot => query.WhereNotDate(field, values[0]),
            Matches.DateBefore => query.WhereDate(field, "<", values[0]),
            Matches.DateAfter => query.WhereDate(field, ">", values[0]),
            Matches.Between => values.Length == 2
                ? query.WhereBetween(field, values[0], values[1])
                : Result.Fail("show provide two values for between"),
            _ => Result.Fail($"Kate Query Ext: Failed to apply and constraint, Match [{match}] is not support ")
        };
    }

    private static Result<SqlKata.Query> ApplyOrConstraint(this SqlKata.Query query, string field, string match, object?[] values)
    {
        return match switch
        {
            Matches.StartsWith => query.OrWhereStarts(field, values[0]),
            Matches.Contains => query.OrWhereContains(field, values[0]),
            Matches.NotContains => query.OrWhereNotContains(field, values[0]),
            Matches.EndsWith => query.OrWhereEnds(field, values[0]),
            Matches.EqualsTo => query.OrWhere(field, values[0]),
            Matches.NotEquals => query.OrWhereNot(field, values[0]),
            Matches.NotIn => query.OrWhereNotIn(field, values),
            Matches.In => query.OrWhereIn(field, values),
            Matches.Lt => query.OrWhere(field, "<", values[0]),
            Matches.Lte => query.OrWhere(field, "<=", values[0]),
            Matches.Gt => query.OrWhere(field, ">", values[0]),
            Matches.Gte => query.OrWhere(field, ">=", values[0]),
            Matches.DateIs => query.OrWhereDate(field, values[0]),
            Matches.DateIsNot => query.OrWhereNotDate(field, values[0]),
            Matches.DateBefore => query.OrWhereDate(field, "<", values[0]),
            Matches.DateAfter => query.OrWhereDate(field, ">", values[0]),
            Matches.Between => values.Length == 2
                ? query.OrWhereBetween(field, values[0], values[1])
                : Result.Fail("show provide two values for between"),
            _ => Result.Fail($"Kate Query Ext: Failed to apply or constraint, Match [{match}] is not support ")
        };
    }

    
    public static void ApplyCursor(this SqlKata.Query? query,  ValidSpan? cursor,ValidSort[] sorts)
    {
        
        if (query is null || cursor?.EdgeItem is null)
        {
            return;
        }

        query.Where(q =>
        {
            for (var i = 0; i < sorts.Length; i++)
            {
                ApplyFilter(q, i);
                if (i < sorts.Length - 1)
                {
                    q.Or();
                }
            }
            return q;
        });
        return ;

        void ApplyFilter(SqlKata.Query q,int idx)
        {
            for (var i = 0; i < idx; i++)
            {
                ApplyEq(q, sorts[i]);
            }
            ApplyCompare(q,sorts[idx]);
        }

        void ApplyEq(SqlKata.Query q, ValidSort sort)
        {
            q.Where(sort.Vector.Attribute.AddTableModifier(sort.Vector.TableAlias),  cursor.Edge(sort.Vector.FullPath).ObjectValue);
        }

        void ApplyCompare(SqlKata.Query q, ValidSort sort)
        {
            q.Where(
                sort.Vector.Attribute.AddTableModifier(sort.Vector.TableAlias), 
                cursor.Span.GetCompareOperator(sort.Order), 
                cursor.Edge(sort.Vector.FullPath).ObjectValue);
        }
        
    }
}