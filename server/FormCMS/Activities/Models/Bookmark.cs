using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;

namespace FormCMS.Activities.Models;

public record Bookmark (  
     string EntityName,
     long RecordId,
     string UserId,
     long FolderId = 0,
     long? Id = null,
     string Title ="", 
     string Url="", 
     string Image="", 
     string Subtitle="", 
     DateTime? PublishTime=null
);

public static class Bookmarks
{
     public const string TableName = "__bookmarks";
     public static readonly Column[] Columns =
     [
          ColumnHelper.CreateCamelColumn<Bookmark>(x => x.Id!, ColumnType.Id),
          ColumnHelper.CreateCamelColumn<Bookmark, string>(x => x.EntityName),
          ColumnHelper.CreateCamelColumn<Bookmark, long>(x => x.RecordId),
          ColumnHelper.CreateCamelColumn<Bookmark, string>(x => x.UserId),
          ColumnHelper.CreateCamelColumn<Bookmark, long>(x => x.FolderId),

          DefaultColumnNames.Deleted.CreateCamelColumn(ColumnType.Boolean),
          DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
          DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime),
     ]; 
}