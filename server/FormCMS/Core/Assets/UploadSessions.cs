using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.RecordExt;
using Humanizer;
using SqlKata;
using Column = FormCMS.Utils.DataModels.Column;

namespace FormCMS.Core.Assets;

public record UploadSession(string ClientId, string FileName, long FileSize, string Path, long? Id= null);

public static class UploadSessions
{
    public const string TableName = "_upload_sessions";
    public static readonly Column[] Columns =
    [
        ColumnHelper.CreateCamelColumn<UploadSession>(x => x.Id!, ColumnType.Id),
        ColumnHelper.CreateCamelColumn<UploadSession, string>(x => x.ClientId),
        ColumnHelper.CreateCamelColumn<UploadSession, string>(x => x.FileName),
        ColumnHelper.CreateCamelColumn<UploadSession, long>(x => x.FileSize),
        ColumnHelper.CreateCamelColumn<UploadSession, string>(x => x.Path),
       
        DefaultColumnNames.Deleted.CreateCamelColumn(ColumnType.Boolean),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime),
    ];

    public static Query Find(string clientId, string fileName, long fileSize)
    {
        return new Query(TableName)
            .Where(nameof(UploadSession.ClientId).Camelize(), clientId)
            .Where(nameof(UploadSession.FileName).Camelize(), fileName)
            .Where(nameof(UploadSession.FileSize).Camelize(), fileSize)
            .Where(nameof(DefaultColumnNames.Deleted).Camelize(),false)
            .Select(nameof(UploadSession.Path).Camelize());

    }
    
    public static Query Delete( string path) =>
        new Query(TableName)
            .Where(nameof(UploadSession.Path).Camelize(), path)
            .AsUpdate([DefaultColumnNames.Deleted.Camelize()], [true]);
    
    public static Query Insert(this UploadSession session)
    {
        var record = RecordExtensions.FormObject(session, [
            nameof(UploadSession.ClientId),
            nameof(UploadSession.FileName),
            nameof(UploadSession.FileSize),
            nameof(UploadSession.Path)
        ]);
        
        return new Query(TableName)
            .AsInsert(record, true);
    }
    
}
    