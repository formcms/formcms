using FluentCMS.Utils.Dao;
using SqlKata;

namespace FluentCMS.Models.Queries;
using Record = IDictionary<string,object>;

public class Entity
{
    public Entity(){}
    public void SetAttributes(ColumnDefinition[] cols )
    {
        Attributes = cols.Select(x => new Attribute(x)).ToArray();
    }

    public string EntityName { get; set; } = "";
    public string TableName { get; set; } = "";
    public string Title { get; set; } = "";
    public string PrimaryKey { get; set; } = "";
    public string TitleAttribute { get; set; } = "";
    
    public int DefaultPageSize { get; set; } = 20;
    //must be public to expose to json parser
    public Attribute[] Attributes { get; set; } = [];

    public Attribute[] AttributesForLookup()
    {
        return Attributes.Where(x => x.Field == PrimaryKey || x.Field == TitleAttribute).ToArray();
    }

    public enum InListOrDetail
    {
        InList,
        InDetail,
    }

    public string Fullname(string fieldName)
    {
        return TableName + "." + fieldName;
    }
    public Attribute[] GetAttributes(DisplayType? type, InListOrDetail? listOrDetail)
    {
        IEnumerable<Attribute> ret = Attributes.Where(x =>
            type is not null ? x.Type == type : x.Type != DisplayType.crosstable && x.Type != DisplayType.subtable);
        if (listOrDetail is not null)
        {
            ret = ret.Where(x => listOrDetail == InListOrDetail.InList ? x.InList : x.InDetail);
        }
        return ret.ToArray();
    }

    public Func<string,object>? GetDatabaseTypeCaster(string field)
    {
        var attr = Attributes.FirstOrDefault(x => x.Field == field && x.InDetail);
        if (attr is null)
        {
            return null;
        }
        return attr.CastToDatabaseType;
    }

    public Attribute KeyAttribute()
    {
        return Attributes.First(x => x.Field == PrimaryKey);
    }

    public Query? One(string key, Attribute[]attributes)
    {
        var id = KeyAttribute().CastToDatabaseType(key);
        return Basic().Where(PrimaryKey, id).Select(attributes.Select(x=>x.FullName()));
    }
    public Query? List(Pagination? pagination, Sorts? sorts,  Filters? filters, Attribute[]? attributes)
    {
        if (attributes is null)
        {
            return null;
        }
        var query = Basic().Select(attributes.Select(x=>x.FullName()));
        sorts?.Apply(this, query);
        filters?.Apply(this, query);
        return query;
    }
    public Query Many(object[]ids, Attribute[] attributes)
    {
        var lstFields = attributes.Select(x => x.Field);
        return Basic().Select(lstFields.ToArray()).WhereIn(PrimaryKey,ids);
    }
   
    public Query Insert(Record item)
    {
        return new Query(TableName).AsInsert(item, true);
    }

    public Query? Update(Record item)
    {
        return item.TryGetValue(PrimaryKey, out object val)
            ? new Query(TableName).Where(PrimaryKey, val).AsUpdate(item.Keys, item.Values)
            : null;
    }

    public Query? Delete(Record item)
    {
        return item.TryGetValue(PrimaryKey, out object key)
            ? new Query(TableName).Where(PrimaryKey, key).AsUpdate(["deleted"], [true])
            : null;
    }
    
    public Query Basic()
    {
        return new Query(TableName).Where(TableName + ".deleted", false);
    }
}