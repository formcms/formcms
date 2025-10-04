using System.Collections.Immutable;
using System.Text.Json;
using System.Text.RegularExpressions;
using DynamicExpresso;

using FormCMS.CoreKit.RelationDbQuery;
using FormCMS.Utils.ResultExt;
using FluentResults;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.jsonElementExt;
using GraphQL.Client.Abstractions.Utilities;

namespace FormCMS.Core.Descriptors;

public record Entity(
    ImmutableArray<Attribute> Attributes,
    string Name,
    string DisplayName,
    string TableName,

    string LabelAttributeName,
    string PrimaryKey,

    int DefaultPageSize = EntityConstants.DefaultPageSize,
    PublicationStatus DefaultPublicationStatus = PublicationStatus.Published,
    
    string PageUrl = "",
    string TagsQuery = "",
    string TagsQueryParam = "",
    string TitleTagField = "",
    string SubtitleTagField ="",
    string ContentTagField ="",
    string ImageTagField="",
    string PublishTimeTagField =""
);

public record LoadedEntity(
    ImmutableArray<LoadedAttribute> Attributes,
    LoadedAttribute PrimaryKeyAttribute,
    LoadedAttribute LabelAttribute,
    LoadedAttribute DeletedAttribute,
    LoadedAttribute PublicationStatusAttribute,
    LoadedAttribute UpdatedAtAttribute,
    string Name,
    string DisplayName, //needed by admin panel
    string TableName,

    string PrimaryKey,
    string LabelAttributeName,
    int DefaultPageSize,
    PublicationStatus DefaultPublicationStatus,
    string PageUrl,
    string TagsQuery,
    string TagsQueryParam ,
    string TitleTagField ,
    string SubtitleTagField,
    string ContentTagField,
    string ImageTagField,
    string PublishTimeTagField
); 

public static class EntityConstants
{
    public const int DefaultPageSize = 50;
}

public static class EntityHelper
{
    public static Entity AddLookUp(this Entity entity, string lookup)
    {
        if (string.IsNullOrWhiteSpace(lookup)) return entity;
        var attr = new Attribute
        (
            Field: lookup,
            Options: lookup,
            Header: lookup,
            InList: true,
            InDetail: true,
            DataType: DataType.Lookup,
            DisplayType: DisplayType.Lookup
        );
        return entity with{Attributes = entity.Attributes.Add(attr)};
    }

    public static Entity AddJunction(this Entity entity, string junction)
    {
        if (string.IsNullOrWhiteSpace(junction)) return entity;
       var attr = new Attribute
        (
            Field: junction,
            Options: junction,
            Header: junction,
            DataType: DataType.Junction,
            DisplayType: DisplayType.Picklist,
            InDetail: true
        );
        return entity with { Attributes = entity.Attributes.Add(attr) };
    }

    public static Entity AddCollection(this Entity entity, string collection, string linkAttribute)
    {
        if (string.IsNullOrWhiteSpace(collection)) return entity;
        var attr = new Attribute
        (
            Field: collection,
            Options: $"{collection}.{linkAttribute}",
            Header: collection,
            DataType: DataType.Collection,
            DisplayType: DisplayType.EditTable,
            InDetail: true
        );
        return entity with { Attributes = entity.Attributes.Add(attr) };
    }

    public static Entity CreateSimpleEntity( string entityName, string field, bool needPublish)
    {
        var attr = new List<Attribute>([
            new Attribute
            (
                Field: field,
                Header: field
            )
        ]);

        var entity = new Entity
        (
            Name: entityName,
            TableName: entityName,
            DisplayName: entityName,
            DefaultPageSize: EntityConstants.DefaultPageSize,
            PrimaryKey: "id",
            LabelAttributeName: field,
            DefaultPublicationStatus: needPublish ?PublicationStatus.Draft:PublicationStatus.Published,
            Attributes: [..attr]
        );
        return entity;
    } 
    
    public static LoadedEntity ToLoadedEntity(this Entity entity)
    {
        var attributes = entity.Attributes.Select(x => x.ToLoaded(entity.TableName)).ToArray();
        var primaryKey = attributes.First(x=>x.Field == entity.PrimaryKey);
        var labelAttribute = attributes.First(x => x.Field == entity.LabelAttributeName);
        var publicationStatusAttribute  = attributes.First(x=>x.Field == DefaultAttributeNames.PublicationStatus.Camelize());
        
        var deletedAttribute = DefaultColumnNames.Deleted.CreateLoadedAttribute(entity.TableName, DataType.Int, DisplayType.Number);
        var updatedAtAttribute =  attributes.First(x=>x.Field == DefaultColumnNames.UpdatedAt.Camelize());
        
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
            ContentTagField:entity.ContentTagField,
            PageUrl:entity.PageUrl,
            TagsQuery:entity.TagsQuery,
            TagsQueryParam:entity.TagsQueryParam,
            TitleTagField:entity.TitleTagField,
            SubtitleTagField:entity.SubtitleTagField,
            ImageTagField:entity.ImageTagField,
            PublishTimeTagField:entity.PublishTimeTagField
        );
    }
    public static string[] GetAssets(this LoadedEntity entity, Record record)
    {
        var paths = new List<string>();
        foreach (var attribute in entity.Attributes.Where(x => x.DisplayType.IsAsset()))
        {
            if (record.TryGetValue(attribute.Field, out var value) 
                && value is string stringValue )
            {
                paths.AddRange(stringValue.Split(","));
            }
        }
        return paths.ToArray();
    }
    public static SqlKata.Query PublishedAt(this LoadedEntity entity, long recordId)
    {
        return entity.Basic().Where(entity.PrimaryKey, recordId)
            .Select(DefaultAttributeNames.PublishedAt.Camelize());
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
            query.Where(e.PublicationStatusAttribute.AddTableModifier(), publicationStatus.Value.Camelize());
        }

        query.ApplyJoin([..filters.Select(x => x.Vector), ..sorts.Select(x => x.Vector)], publicationStatus);
        var result = query.ApplyFilters(filters);
        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }

        query.ApplyValidSorts(sorts);
        return query;
    }

    public static SqlKata.Query ByIdsQuery(
        this LoadedEntity e,
        IEnumerable<string> fields,
        IEnumerable<ValidValue> ids,
        PublicationStatus? publicationStatus
    )
    {
        var query = e.Basic().WhereIn(e.PrimaryKeyAttribute.AddTableModifier(), ids.GetValues()).Select(fields);
        if (publicationStatus.HasValue)
        {
            query.Where(e.PublicationStatusAttribute.AddTableModifier(), publicationStatus.Value.Camelize());
        }

        return query;
    }

    //why not use listQuery? allQuery doesn't default limitation
    public static SqlKata.Query AllQueryForTree(
        this LoadedEntity e,
        IEnumerable<LoadedAttribute> attributes
    ) => e.Basic().Select(attributes.Select(x => x.Field));
    
    public static SqlKata.Query ListQuery(
        this LoadedEntity e,
        ValidFilter[] filters, 
        ValidSort[] sorts, 
        ValidPagination? pagination, 
        ValidSpan? span, 
        IEnumerable<LoadedAttribute> attributes,PublicationStatus? publicationStatus
        )
    {
        var query = e.GetCommonListQuery(filters,sorts,pagination,span,attributes,publicationStatus);
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
            q.Where(e.PublicationStatusAttribute.AddTableModifier(), publicationStatus.Value.Camelize());
        }

        q.ApplyFilters(filters);
        q.ApplyValidSorts(SpanHelper.IsForward(span?.Span) ? sorts : sorts.ReverseOrder());
        q.ApplySpanFilter(span, sorts, x=>x.Vector.Attribute.AddTableModifier(x.Vector.TableAlias),x=>x.Vector.FullPath);
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
        
        item[DefaultAttributeNames.PublicationStatus.Camelize()] = e.DefaultPublicationStatus.Camelize();
        if (e.DefaultPublicationStatus == PublicationStatus.Published)
        {
            item[DefaultAttributeNames.PublishedAt.Camelize()] = DateTime.UtcNow;
        }

        return new SqlKata.Query(e.TableName).AsInsert(item, true);
    }

    public static Result<SqlKata.Query> SavePublicationStatus(this LoadedEntity e, object id, Record record)
    {
        if (!record.TryGetValue(DefaultAttributeNames.PublicationStatus.Camelize(), out var value)
            || value is not string str || !Enum.TryParse<PublicationStatus>(str,true, out var status))
        {
            return Result.Fail("Cannot save publication status, unknown status");
        }

        var updatingRecord = new Dictionary<string, object>
        {
            [DefaultAttributeNames.PublicationStatus.Camelize()] = status.Camelize()
        };

        if (status is PublicationStatus.Published or PublicationStatus.Scheduled)
        {
            if (!record.TryGetValue(DefaultAttributeNames.PublishedAt.Camelize(), out var dateTime))
            {
                return Result.Fail("Cannot save publication status, invalidate publish time");
            }
            updatingRecord[DefaultAttributeNames.PublishedAt.Camelize()] = dateTime;
        }
        
        return new SqlKata.Query(e.TableName)
            .Where(e.PrimaryKey, id)
            .AsUpdate(updatingRecord);
    }

    public static SqlKata.Query PublishAllScheduled(this Entity e)
        => new SqlKata.Query(e.TableName)
            .Where(DefaultAttributeNames.PublicationStatus.Camelize(), PublicationStatus.Scheduled.Camelize())
            .Where(DefaultAttributeNames.PublishedAt.Camelize(), "<", DateTime.UtcNow)
            .AsUpdate([DefaultAttributeNames.PublicationStatus.Camelize()], [PublicationStatus.Published.Camelize()]);
    
    public static Result<SqlKata.Query> UpdateQuery(this LoadedEntity e, long id, Record item)
    {
        if (!item.Remove(DefaultColumnNames.UpdatedAt.Camelize(), out var updatedAt))
        {
            return Result.Fail($"Failed to get updatedAt value with field [{e.UpdatedAtAttribute.Field.ToCamelCase()}]");
        }

        var ret = new SqlKata.Query(e.TableName)
            .Where(e.PrimaryKey, id)
            .Where(DefaultColumnNames.UpdatedAt.Camelize(), updatedAt!)
            .Where(DefaultColumnNames.Deleted.Camelize(), false)
            .AsUpdate(item.Keys, item.Values);
        item[e.PrimaryKey] = id;
        return ret;
    }

    public static Result<SqlKata.Query> DeleteQuery(this LoadedEntity e,long id,Record item)
    {
        if (!item.Remove(DefaultColumnNames.UpdatedAt.Camelize(), out var updatedAt))
        {
            return Result.Fail(
                $"Failed to get updatedAt value with field [{e.UpdatedAtAttribute.Field.ToCamelCase()}]");
        }

        return new SqlKata.Query(e.TableName)
            .Where(e.PrimaryKey, id)
            .Where(DefaultColumnNames.UpdatedAt.Camelize(), updatedAt!)
            .AsUpdate([DefaultColumnNames.Deleted.Camelize()], [true]);
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
        foreach (var localAttribute in e.Attributes.Where(x=>x.DataType.IsLocal() && !string.IsNullOrWhiteSpace(x.Validation)))
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

    public static Result<Record> Parse (this LoadedEntity entity, JsonElement element)
    {
        var rec = element.ToDictionary();
        foreach (var attribute in entity.Attributes)
        {
            if (!rec.TryGetValue(attribute.Field, out var value)) continue;
            var dataType = attribute.DataType is DataType.Lookup ? attribute.Lookup!.TargetEntity.PrimaryKeyAttribute.DataType: attribute.DataType;
            
            if (attribute.Lookup is not null && value is Record record)
            {
                 value = record[attribute.Lookup.TargetEntity.PrimaryKeyAttribute.Field];
            }
            var (_,fail, obj,errors) = Converter.DisplayObjToDbObj(dataType, attribute.DisplayType, value);
            if (fail)
            {
                return Result.Fail(errors);
            }
            rec[attribute.Field] = obj;
        }
        return rec;
    }

    internal static SqlKata.Query GetCommonCountQuery(this LoadedEntity e, IEnumerable<ValidFilter> filters)
    {
        var query = e.Basic();
        query.ApplyFilters(filters);
        return query;
    }
}