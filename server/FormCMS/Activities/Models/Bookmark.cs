using FormCMS.Core.Assets;
using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.RecordExt;
using Humanizer;
using Column = FormCMS.Utils.DataModels.Column;
using Query = SqlKata.Query;

namespace FormCMS.Activities.Models;

public record Bookmark (  
     string EntityName,
     long RecordId,
     string UserId,
     long? FolderId = null ,
     long? Id = null,
     string Title ="", 
     string Url="", 
     string Image="", 
     string Subtitle="", 
     DateTime PublishedAt=default,
     DateTime UpdatedAt = default
);

public static class Bookmarks
{
     internal const string TableName = "__bookmarks";
     public static readonly Column[] Columns =
     [
          ColumnHelper.CreateCamelColumn<Bookmark>(x => x.Id!, ColumnType.Id),
          ColumnHelper.CreateCamelColumn<Bookmark, string>(x => x.EntityName),
          ColumnHelper.CreateCamelColumn<Bookmark, long>(x => x.RecordId),
          ColumnHelper.CreateCamelColumn<Bookmark, string>(x => x.UserId),
          ColumnHelper.CreateCamelColumn<Bookmark, long?>(x => x.FolderId),

          ColumnHelper.CreateCamelColumn<Bookmark, string>(x => x.Title),
          ColumnHelper.CreateCamelColumn<Bookmark, string>(x => x.Url),
          ColumnHelper.CreateCamelColumn<Bookmark, string>(x => x.Subtitle),
          ColumnHelper.CreateCamelColumn<Bookmark, string>(x => x.Image),
          
          DefaultAttributeNames.PublishedAt.CreateCamelColumn(ColumnType.Datetime),
          DefaultColumnNames.Deleted.CreateCamelColumn(ColumnType.Boolean),
          DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
          DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime),
     ];

     public static readonly string[] KeyFields =
     [
          nameof(Bookmark.EntityName).Camelize(),
          nameof(Bookmark.RecordId).Camelize(),
          nameof(Bookmark.UserId).Camelize(),
          nameof(Bookmark.FolderId).Camelize(),
     ];
     
     public static Bookmark LoadMetaData(this Bookmark bookmark, Entity entity, Record record)
     {

          bookmark = bookmark with { Url = entity.PageUrl + bookmark.RecordId };
          if (record.ByJsonPath<string>(entity.BookmarkTitleField, out var title))
          {
               bookmark = bookmark with { Title = Trim(title)};
          }

          if (record.ByJsonPath<Asset>(entity.BookmarkImageField, out var asset))
          {
               bookmark = bookmark with { Image = Trim(asset?.Url) };
          }

          if (record.ByJsonPath<string>(entity.BookmarkSubtitleField, out var subtitle))
          {
               bookmark = bookmark with { Subtitle = Trim(subtitle)};
          }

          if (record.ByJsonPath<DateTime>(entity.BookmarkPublishTimeField, out var publishTime))
          {
               bookmark = bookmark with { PublishedAt = publishTime };
          }

          return bookmark;
        
          string Trim(string? s) => s?.Length > 255 ? s[..255] : s??"";
     }

     public static Record ToInsertRecord(
          this Bookmark bookmark
     ) => RecordExtensions.FormObject(
          bookmark,
          whiteList:
          [
               nameof(Bookmark.EntityName),
               nameof(Bookmark.RecordId),
               nameof(Bookmark.UserId),
               nameof(Bookmark.FolderId),
               nameof(Bookmark.PublishedAt),
               nameof(Bookmark.Title),
               nameof(Bookmark.Url),
               nameof(Bookmark.Image),
               nameof(Bookmark.Subtitle),
          ]
     );

     public static Query Delete(string userId, string entityName, long recordId, long?folderId)
          => new Query(TableName)
               .Where(nameof(Bookmark.UserId).Camelize(), userId)
               .Where(nameof(Bookmark.EntityName).Camelize(), entityName)
               .Where(nameof(Bookmark.RecordId).Camelize(), recordId)
               .Where(nameof(Bookmark.FolderId).Camelize(), folderId)
               .AsUpdate([DefaultColumnNames.Deleted.Camelize()], [true]);
     
     public static Query FolderIdByUserIdRecordId(
          string userId, string entityName, long recordId
     ) => new Query(TableName)
          .Where(nameof(Bookmark.UserId).Camelize(), userId)
          .Where(nameof(Bookmark.EntityName).Camelize(), entityName)
          .Where(nameof(Bookmark.RecordId).Camelize(), recordId)
          .Where(nameof(DefaultColumnNames.Deleted).Camelize(),false)
          .Select(nameof(Bookmark.FolderId).Camelize());
}