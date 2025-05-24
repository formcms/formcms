using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.RecordExt;
using Humanizer;
using Column = FormCMS.Utils.DataModels.Column;
using Query = SqlKata.Query;

namespace FormCMS.Comments.Models;

public record Comment(
    string EntityName,
    long RecordId,
    string UserId,
    string Content,
    long Id = 0,
    long? ParentId = null,
    long? ReplyId = null,
    DateTime PublishedAt = default,
    DateTime UpdatedAt = default
    );

public static class CommentConstants
{
    public const string EntityName = "comments";
    public const string TableName = "__comments";
    public const int DefaultPageSize = 20;
}

public static class CommentHelper
{
    public static readonly Column[] Columns = [
        ColumnHelper.CreateCamelColumn<Comment>(x => x.Id, ColumnType.Id),
        ColumnHelper.CreateCamelColumn<Comment, string>(x => x.EntityName),
        ColumnHelper.CreateCamelColumn<Comment, long>(x => x.RecordId),
        ColumnHelper.CreateCamelColumn<Comment, string>(x => x.UserId),
        ColumnHelper.CreateCamelColumn<Comment>(x => x.Content, ColumnType.Text),
        ColumnHelper.CreateCamelColumn<Comment>(x => x.ParentId!, ColumnType.Int),
        ColumnHelper.CreateCamelColumn<Comment>(x => x.ReplyId!, ColumnType.Int),
        DefaultColumnNames.Deleted.CreateCamelColumn(ColumnType.Boolean),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime),
    ];

    public static Entity GetCommentEntity = new Entity(
        Attributes: [],
        Name: CommentConstants.EntityName,
        DisplayName: "",
        TableName: CommentConstants.TableName,
        LabelAttributeName: nameof(Comment.Content).Camelize(),
        PrimaryKey: nameof(Comment.Id)
    );
    
    private static string[] fields =
    [
        nameof(Comment.Id).Camelize(),
        nameof(Comment.EntityName).Camelize(),
        nameof(Comment.RecordId).Camelize(),
        nameof(Comment.UserId).Camelize(),
        nameof(Comment.Content).Camelize(),
        nameof(Comment.ParentId).Camelize(),
        nameof(Comment.ReplyId).Camelize(),
    ];
    
    public static Query Single(long id)
    {
        return new Query(CommentConstants.TableName)
            .Where(nameof(Comment.Id).Camelize(), id)
            .Where(nameof(DefaultColumnNames.Deleted).Camelize(), false)
            .Select(fields);
    }
    
    public static Query Insert(this Comment comment)
        => new Query(CommentConstants.TableName).AsInsert(
            RecordExtensions.FormObject(
                comment, whiteList:
                [
                    nameof(Comment.EntityName),
                    nameof(Comment.RecordId),
                    nameof(Comment.UserId),
                    nameof(Comment.Content),
                ]
            ));
    
    public static Query Update(this Comment comment)
        => new Query(CommentConstants.TableName)
            .Where(nameof(comment.Id).Camelize(), comment.Id)
            .Where(nameof(comment.UserId).Camelize(), comment.UserId)
            .AsUpdate(
                [nameof(Comment.Content).Camelize()],
                [comment.Content]
            );

    public static Query Delete(string userId, long id)
    => new Query(CommentConstants.TableName)
        .Where(nameof(Comment.UserId).Camelize(), userId)
        .Where(nameof(Comment.Id).Camelize(), id)
        .AsUpdate([DefaultColumnNames.Deleted.Camelize()], [true]);
    
}