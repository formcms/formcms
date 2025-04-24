using FormCMS.AuditLogging.Models;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.RecordExt;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using SqlKata;
using Column = FormCMS.Utils.DataModels.Column;

namespace FormCMS.Activities.Models;

public record BookmarkFolder(
    string UserId,
    string Name,
    string Description,
    DateTime UpdatedAt = default,
    long Id = 0,
    bool Selected = false
);

public static class BookmarkFolders
{
    public const string TableName = "__bookmark_folders";

    public static readonly Column[] Columns =
    [
        ColumnHelper.CreateCamelColumn<BookmarkFolder>(x => x.Id, ColumnType.Id),
        ColumnHelper.CreateCamelColumn<BookmarkFolder, string>(x => x.UserId),
        ColumnHelper.CreateCamelColumn<BookmarkFolder, string>(x => x.Name),
        ColumnHelper.CreateCamelColumn<BookmarkFolder, string>(x => x.Description),

        DefaultColumnNames.Deleted.CreateCamelColumn(ColumnType.Boolean),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime),
    ];

    public static Query All(string userId)
    {
        return new Query(TableName)
            .Where(nameof(BookmarkFolder.UserId).Camelize(), userId)
            .Where(nameof(DefaultColumnNames.Deleted).Camelize(),false)
            .OrderBy(nameof(BookmarkFolder.Name).Camelize())
            .Select(
                nameof(BookmarkFolder.Id).Camelize(),
                nameof(BookmarkFolder.Name).Camelize(),
                nameof(BookmarkFolder.Description).Camelize()
            );
    }

    public static Record ToRecord(this BookmarkFolder folder)
        => RecordExtensions.FormObject(folder, whiteList:
        [
            nameof(BookmarkFolder.Id),
            nameof(BookmarkFolder.UserId),
            nameof(BookmarkFolder.Name),
            nameof(BookmarkFolder.Description),
        ]);

    //although folderId is global uniq, add userId to prevent forge identity
    public static Query Delete(string userId, long id) 
        => new Query(TableName)
            .Where(nameof(BookmarkFolder.UserId).Camelize(), userId)
            .Where(nameof(BookmarkFolder.Id).Camelize(), id)
            .AsUpdate([DefaultColumnNames.Deleted.Camelize()], [true]);

    public static Query Insert(this BookmarkFolder folder) 
        => new Query(TableName).AsInsert(RecordExtensions.FormObject(
            folder, whiteList:
            [
                nameof(BookmarkFolder.UserId),
                nameof(BookmarkFolder.Name),
                nameof(BookmarkFolder.Description),
            ]
        ),true);

    public static Query Update(this BookmarkFolder folder)
        => new Query(TableName)
            .Where(nameof(BookmarkFolder.Id).Camelize(), folder.Id)
            .Where(nameof(BookmarkFolder.UserId).Camelize(), folder.UserId)
            .AsUpdate(
                [nameof(BookmarkFolder.Name).Camelize(), nameof(BookmarkFolder.Description).Camelize()],
                [folder.Name, folder.Description]
            );

}