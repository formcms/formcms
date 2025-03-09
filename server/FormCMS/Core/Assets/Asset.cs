using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.RecordExt;
using Humanizer;
using SqlKata;
using Column = FormCMS.Utils.DataModels.Column;

namespace FormCMS.Core.Assets;

public record Asset(
    string Path, // unique name, yyyy-MM date + ulid
    string Url,
    string Name, // original name, for search
    string Title, // default as name, for link title, picture caption
    long Size,
    string Type,
    Record Metadata,
    string CreatedBy,
    DateTime CreatedAt = default,
    DateTime UpdatedAt = default,
    long Id = 0,
    int LinkCount = 0, //calculated field, omit from attribute and columns
    AssetLink[]? Links = null //calculated field, omit from attribute and columns
);

public static class Assets
{
    public const string TableName = "__assets";
    private const int DefaultPageSize = 48;

    public static readonly XEntity Entity = XEntityExtensions.CreateEntity<Asset>(
        nameof(Asset.Type),
        defaultPageSize: DefaultPageSize,
        attributes:
        [
            XAttrExtensions.CreateAttr<Asset, string>(x => x.Path, displayType: DisplayType.Image, isDefault:true),
            XAttrExtensions.CreateAttr<Asset, long>(x => x.Id, isDefault:true),
            XAttrExtensions.CreateAttr<Asset, string>(x => x.Url,inList:false, isDefault:true),
            XAttrExtensions.CreateAttr<Asset, string>(x => x.Name,isDefault:true),
            XAttrExtensions.CreateAttr<Asset, string>(x => x.Title),
            XAttrExtensions.CreateAttr<Asset, long>(x => x.Size, isDefault:true),
            XAttrExtensions.CreateAttr<Asset, string>(x => x.Type, isDefault:true),
            XAttrExtensions.CreateAttr<Asset, object>(x => x.Metadata, inList:false, displayType:DisplayType.Dictionary),
            XAttrExtensions.CreateAttr<Asset, string>(x => x.CreatedBy, isDefault:true),
            XAttrExtensions.CreateAttr<Asset, DateTime>(x => x.CreatedAt, isDefault:true),
            XAttrExtensions.CreateAttr<Asset, DateTime>(x => x.UpdatedAt, isDefault:true),
        ]);

    public static readonly XEntity EntityWithLinkCount = 
        Entity with
        {
            Attributes = [
                ..Entity.Attributes,
                XAttrExtensions.CreateAttr<Asset, int>(x => x.LinkCount, isDefault:true),
            ]
        };

    public static readonly Column[] Columns =
    [
        ColumnHelper.CreateCamelColumn<Asset>(x => x.Id, ColumnType.Id),
        ColumnHelper.CreateCamelColumn<Asset, string>(x => x.Url),
        ColumnHelper.CreateCamelColumn<Asset, string>(x => x.Path),
        ColumnHelper.CreateCamelColumn<Asset, string>(x => x.Name),
        ColumnHelper.CreateCamelColumn<Asset, string>(x => x.Title),
        ColumnHelper.CreateCamelColumn<Asset, long>(x => x.Size),
        ColumnHelper.CreateCamelColumn<Asset, string>(x => x.Type),
        ColumnHelper.CreateCamelColumn<Asset, string>(x => x.CreatedBy),
        
        ColumnHelper.CreateCamelColumn<Asset>(x => x.Metadata, ColumnType.Text),

        DefaultColumnNames.Deleted.CreateCamelColumn(ColumnType.Boolean),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime),
    ];

    public static Record[] ToInsertRecords(this IEnumerable<Asset> assets)
    {
        return assets.Select(x => RecordExtensions.FormObject(x,
            whiteList:
            [
                nameof(Asset.Path),
                nameof(Asset.Url),
                nameof(Asset.Name),
                nameof(Asset.Title),
                nameof(Asset.Size),
                nameof(Asset.Type),
                nameof(Asset.CreatedBy),
            ])).ToArray();
    }

    public static Query UpdateMetaData(this Asset asset)
    {
        var record = RecordExtensions.FormObject(asset, 
            whiteList: [nameof(Asset.Title), nameof(Asset.Metadata)]);
        return new Query(TableName)
            .Where(nameof(Asset.Id).Camelize(), asset.Id)
            .AsUpdate(record);
    }

    public static Query Single(long id)
        => new Query(TableName)
            .Where(DefaultColumnNames.Deleted.Camelize(), false)
            .Where(nameof(Asset.Id).Camelize(),id)
            .Select(Entity.Attributes.Select(x => x.Field));
    public static Query List(int?offset = null, int? limit = null)
    {
        var q = new Query(TableName)
            .Where(DefaultColumnNames.Deleted.Camelize(), false)
            .Select(
                Entity.Attributes
                    .Where(x => x.InList || x.Field == nameof(Asset.Id).Camelize())
                    .Select(x => x.Field)
            );
        if (offset > 0) q.Offset(offset.Value);
        q.Limit(limit ?? DefaultPageSize);
        return q;
    }

    public static bool IsValidPath(string path)=> 
        !string.IsNullOrWhiteSpace(path) 
        && !path.StartsWith("http"); // not external link

    public static Query UpdateFile(long id, string fileName, long size, string contentType) =>
        new Query(TableName)
            .Where(nameof(Asset.Id).Camelize(), id)
            .AsUpdate(
                [
                    nameof(Asset.Name).Camelize(),
                    nameof(Asset.Size).Camelize(),
                    nameof(Asset.Type).Camelize()
                ],
                [
                    fileName,
                    size,
                    contentType
                ]
            );

    public static Query GetAssetIDsByPaths(IEnumerable<string> paths)
        => new Query(TableName)
            .Select( nameof(Asset.Id).Camelize(), nameof(Asset.Path).Camelize())
            .Where(DefaultColumnNames.Deleted.Camelize(), false)
            .WhereIn(nameof(Asset.Path).Camelize(), paths);

    public static Query Count() => new Query(TableName)
        .Where(DefaultColumnNames.Deleted.Camelize(), false);

    public static Query Deleted(long id) =>
        new Query(TableName)
            .Where(nameof(Asset.Id).Camelize(), id)
            .AsUpdate([DefaultColumnNames.Deleted.Camelize()], [true]);

}
