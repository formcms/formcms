using System.Collections.Immutable;
using System.Text.Json;
using System.Text.RegularExpressions;
using DynamicExpresso;

using FormCMS.CoreKit.RelationDbQuery;
using FormCMS.Utils.ResultExt;
using FluentResults;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.KateQueryExt;
using FormCMS.Utils.RecordExt;
using GraphQL.Client.Abstractions.Utilities;

namespace FormCMS.Core.Descriptors;

public record Entity(
    ImmutableArray<Attribute> Attributes,
    string Name ,
    string DisplayName ,
    string TableName ,
    
    string LabelAttributeName ,
    string PrimaryKey ,
    
    int DefaultPageSize = EntityConstants.DefaultPageSize,
    PublicationStatus DefaultPublicationStatus = PublicationStatus.Published,
    string PreviewUrl = ""
);

public record LoadedEntity(
    ImmutableArray<LoadedAttribute> Attributes,
    LoadedAttribute PrimaryKeyAttribute,
    LoadedAttribute LabelAttribute,
    LoadedAttribute DeletedAttribute,
    LoadedAttribute PublicationStatusAttribute,
    LoadedAttribute UpdatedAtAttribute,
    string Name,
    string DisplayName , //needed by admin panel
    string TableName,
    
    string PrimaryKey,
    string LabelAttributeName, 
    int DefaultPageSize,
    PublicationStatus DefaultPublicationStatus ,
    string PreviewUrl
    );

public static class EntityConstants
{
    public const int DefaultPageSize = 50;
}

public static class EntityHelper
{
    public static LoadedEntity ToLoadedEntity(this Entity entity)
    {
        var attributes = entity.Attributes.Select(x => x.ToLoaded(entity.TableName)).ToArray();
        var primaryKey = attributes.First(x=>x.Field == entity.PrimaryKey);
        var labelAttribute = attributes.First(x => x.Field == entity.LabelAttributeName);
        var publicationStatusAttribute  = attributes.First(x=>x.Field == DefaultAttributeNames.PublicationStatus.Camelize());
        
        var deletedAttribute = DefaultAttributeNames.Deleted.CreateLoadedAttribute(entity.TableName, DataType.Int, DisplayType.Number);
        var updatedAtAttribute =  attributes.First(x=>x.Field == DefaultAttributeNames.UpdatedAt.Camelize());
        
        return new LoadedEntity(
            [..attributes],
            PrimaryKeyAttribute:primaryKey,
            LabelAttribute: labelAttribute,
            DeletedAttribute:deletedAttribute,
            Name:entity.Name,
            TableName: entity.TableName,
            PrimaryKey:entity.PrimaryKey,
            DisplayName:entity.DisplayName,
            LabelAttributeName:entity.LabelAttributeName,
            DefaultPageSize:entity.DefaultPageSize,
            DefaultPublicationStatus:entity.DefaultPublicationStatus,
            UpdatedAtAttribute:updatedAtAttribute,
            PublicationStatusAttribute:publicationStatusAttribute,
            PreviewUrl:entity.PreviewUrl
        );
    }

    public static Result<SqlKata.Query> SingleQuery(
        this LoadedEntity e,
        ValidFilter[] filters,
        ValidSort[] sorts,
        IEnumerable<LoadedAttribute> attributes,
        PublicationStatus? publicationStatus 
    )
    {
        var query = e.Basic().Select(attributes.Select(x => x.AddTableModifier()));
        if (publicationStatus.HasValue)
        {
            query.WhereCamelEnum(e.PublicationStatusAttribute.AddTableModifier(), publicationStatus.Value);
        }

        query.ApplyJoin([..filters.Select(x => x.Vector), ..sorts.Select(x => x.Vector)], publicationStatus);
        var result = query.ApplyFilters(filters);
        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }

        query.ApplySorts(sorts);
        return query;
    }

    public static SqlKata.Query ByIdsQuery(
        this LoadedEntity e,
        IEnumerable<string> fields,
        IEnumerable<ValidValue> ids,
        PublicationStatus? publicationStatus
    )
    {
        var query = e.Basic().WhereIn(e.PrimaryKey, ids.GetValues()).Select(fields);
        if (publicationStatus.HasValue)
        {
            query.WhereCamelEnum(e.PublicationStatusAttribute.AddTableModifier(), publicationStatus.Value);
        }

        return query;
    }

    public static SqlKata.Query AllQuery(this LoadedEntity e, IEnumerable<LoadedAttribute> attributes)
        => e.Basic().Select(attributes.Select(x => x.Field));
    public static SqlKata.Query ListQuery(this LoadedEntity e,ValidFilter[] filters, ValidSort[] sorts, 
        ValidPagination? pagination, ValidSpan? cursor, IEnumerable<LoadedAttribute> attributes,PublicationStatus? publicationStatus)
    {
        var query = e.GetCommonListQuery(filters,sorts,pagination,cursor,attributes,publicationStatus);
        query.ApplyJoin([..filters.Select(x => x.Vector), ..sorts.Select(x => x.Vector)], publicationStatus);
        return query;
    }

    internal static SqlKata.Query GetCommonListQuery(this LoadedEntity e,
        IEnumerable<ValidFilter> filters,
        ValidSort[] sorts,
        ValidPagination? pagination,
        ValidSpan? span,
        IEnumerable<LoadedAttribute> attributes,
        PublicationStatus? publicationStatus
    )
    {
        var q = e.Basic().Select(attributes.Select(x => x.AddTableModifier()));
        if (publicationStatus.HasValue)
        {
            q.WhereCamelEnum(e.PublicationStatusAttribute.AddTableModifier(), publicationStatus.Value);
        }

        q.ApplyFilters(filters);
        q.ApplySorts(SpanHelper.IsForward(span?.Span) ? sorts : sorts.ReverseOrder());
        q.ApplyCursor(span, sorts);
        if (pagination is not null)
        {
            q.ApplyPagination(pagination);
        }

        return q;
    }

    public static SqlKata.Query CountQuery(
        this LoadedEntity e,
        ValidFilter[] filters , 
        PublicationStatus? publicationStatus
    )
    {
        var query = e.GetCommonCountQuery(filters);
        //filter might contain lookup's target's attribute, 
        query.ApplyJoin(filters.Select(x => x.Vector), publicationStatus);
        return query;
    }

    public static SqlKata.Query Insert(this LoadedEntity e, Record item)
    {
        //omit auto generated value
        if (e.PrimaryKeyAttribute.IsDefault)
        {
            item.Remove(e.PrimaryKey);
        }
        
        item.SetCamelKeyCamelValue(DefaultAttributeNames.PublicationStatus, e.DefaultPublicationStatus);
        if (e.DefaultPublicationStatus == PublicationStatus.Published)
        {
            item.SetCamelKey(DefaultAttributeNames.PublishedAt, DateTime.Now);
        }

        return new SqlKata.Query(e.TableName).AsInsert(item, true);
    }

    public static Result<SqlKata.Query> SavePublicationStatus(this LoadedEntity e, object id, Record record)
    {
        if (!record.CamelKeyEnum<PublicationStatus>(DefaultAttributeNames.PublicationStatus, out var status))
            return Result.Fail("Cannot save publication status, unknown status");

        var updatingRecord = new Dictionary<string, object>();
        updatingRecord.SetCamelKeyCamelValue(DefaultAttributeNames.PublicationStatus, status);

        if (status is PublicationStatus.Published or PublicationStatus.Scheduled)
        {
            if (!record.CamelKeyDateTime(DefaultAttributeNames.PublishedAt, out var dateTime))
            {
                return Result.Fail("Cannot save publication status, invalidate publish time");
            }
            updatingRecord.SetCamelKey(DefaultAttributeNames.PublishedAt, dateTime);
        }
        
        return new SqlKata.Query(e.TableName)
            .Where(e.PrimaryKey, id)
            .AsUpdate(updatingRecord);
    }

    public static SqlKata.Query PublishAllScheduled(this Entity e)
    => new SqlKata.Query(e.TableName)
        .WhereCamelEnum(DefaultAttributeNames.PublicationStatus, PublicationStatus.Scheduled)
        .WhereDate(DefaultAttributeNames.PublishedAt, "<", DateTime.Now)
        .AsCamelFieldValueUpdate([DefaultAttributeNames.PublicationStatus], [PublicationStatus.Published]) ;
    
    public static Result<SqlKata.Query> UpdateQuery(this LoadedEntity e, Record item)
    {
        //to prevent SqlServer 'Cannot update identity column' error 
        if (!item.Remove(e.PrimaryKey, out var id))
        {
            return Result.Fail($"Failed to get id value with primary key [${e.PrimaryKey}]");
        }
        
        
        if (!item.RemoveCamelKey(DefaultAttributeNames.UpdatedAt, out var updatedAt))
        {
            return Result.Fail($"Failed to get updatedAt value with field [{e.UpdatedAtAttribute.Field.ToCamelCase()}]");
        }

        var ret = new SqlKata.Query(e.TableName)
            .Where(e.PrimaryKey, id)
            .Where(DefaultAttributeNames.UpdatedAt, updatedAt!)
            .Where(DefaultAttributeNames.Deleted, false)
            .AsUpdate(item.Keys, item.Values);
        item[e.PrimaryKey] = id;
        return ret;
    }

    public static Result<SqlKata.Query> DeleteQuery(this LoadedEntity e,Record item)
    {
        if (!item.TryGetValue(e.PrimaryKey, out var id))
        {
            return Result.Fail($"Failed to get id value with primary key [${e.PrimaryKey}]");
        }


        if (!item.RemoveCamelKey(DefaultAttributeNames.UpdatedAt, out var updatedAt))
        {
            return Result.Fail(
                $"Failed to get updatedAt value with field [{e.UpdatedAtAttribute.Field.ToCamelCase()}]");
        }

        return new SqlKata.Query(e.TableName)
            .Where(e.PrimaryKey, id)
            .Where(DefaultAttributeNames.UpdatedAt, updatedAt!)
            .AsCamelFieldUpdate([DefaultAttributeNames.Deleted], [true]);
    }

    public static SqlKata.Query Basic(this LoadedEntity e)
    {
        var query = new SqlKata.Query(e.TableName)
            .Where(e.DeletedAttribute.AddTableModifier(), false);
        return query;
    }

    public static Result ValidateTitleAttributes(this LoadedEntity e, Record record)
    {
        if (record.TryGetValue(e.LabelAttributeName, out var value) && value is not null)
        {
            return Result.Ok();
        }
        return Result.Fail($"Validation fail for {e.LabelAttributeName}");
    }
    
    public static Result ValidateLocalAttributes(this LoadedEntity e,Record record)
    {
        var interpreter = new Interpreter().Reference(typeof(Regex));
        var result = Result.Ok();
        foreach (var localAttribute in e.Attributes.Where(x=>x.IsLocal() && !string.IsNullOrWhiteSpace(x.Validation)))
        {
            if (!Validate(localAttribute).Try(out var err))
            {
                result.WithErrors(err);
            }
        }
        return result;
        
        Result Validate(LoadedAttribute attribute)
        {
            record.TryGetValue(attribute.Field, out var value);
            var typeOfAttribute = attribute.DataType switch
            {
                DataType.Int => typeof(int),
                DataType.Datetime => typeof(DateTime),
                _=> typeof(string)
            };

            try
            {
                var res = interpreter.Eval(attribute.Validation,
                    new Parameter(attribute.Field, typeOfAttribute, value));
                return res switch
                {
                    true => Result.Ok(),
                    "" => Result.Ok(),

                    false => Result.Fail($"Validation failed for {attribute.Header}"),
                    string errMsg => Result.Fail(errMsg),
                    _ => Result.Fail($"Validation failed for {attribute.Header}, expression should return string or bool result"),
                };
            }
            catch (Exception ex)
            {
                return Result.Fail($"validate fail for {attribute.Header}, Validate Rule is not correct, ex = {ex.Message}");
            }
        }
    }

    public static Result<Record> Parse (this LoadedEntity entity, JsonElement element, IAttributeValueResolver resolver)
    {
        Dictionary<string, object> ret = new();
        foreach (var attribute in entity.Attributes.Where(x=>x.IsLocal()))
        {
            
            if (!element.TryGetProperty(attribute.Field, out var value)) continue;
            var res = attribute.ParseJsonElement(value, resolver);
            if (res.IsFailed)
            {
                return Result.Fail(res.Errors);
            }
            ret[attribute.Field] = res.Value; 
           
        }
        return ret;
    }

    internal static SqlKata.Query GetCommonCountQuery(this LoadedEntity e, IEnumerable<ValidFilter> filters)
    {
        var query = e.Basic();
        query.ApplyFilters(filters);
        return query;
    }
}