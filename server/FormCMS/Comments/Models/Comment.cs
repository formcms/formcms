using FormCMS.Core.Descriptors;
using FormCMS.Core.Identities;
using FormCMS.CoreKit.RelationDbQuery;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.RecordExt;
using Humanizer;
using Attribute = FormCMS.Core.Descriptors.Attribute;
using Column = FormCMS.Utils.DataModels.Column;
using Query = SqlKata.Query;

namespace FormCMS.Comments.Models;

public record Comment(
    string EntityName,
    long RecordId,
    string User,
    string Content,
    long Id = 0,
    long? Parent = null,
    string? Mention = null,
    DateTime CreatedAt = default,
    DateTime UpdatedAt = default
);

public static class CommentHelper
{
    public  const int MaxCommentCount = 20;
    public const string CommentsField = "comments";
    public const string CommentRepliesQuery = "commentReplies";

    public static readonly Column[] Columns = [
        ColumnHelper.CreateCamelColumn<Comment>(x => x.Id, ColumnType.Id),
        ColumnHelper.CreateCamelColumn<Comment, string>(x => x.EntityName),
        ColumnHelper.CreateCamelColumn<Comment, long>(x => x.RecordId),
        ColumnHelper.CreateCamelColumn<Comment, string>(x => x.User),
        ColumnHelper.CreateCamelColumn<Comment>(x => x.Content, ColumnType.Text),
        ColumnHelper.CreateCamelColumn<Comment>(x => x.Parent!, ColumnType.Int),
        ColumnHelper.CreateCamelColumn<Comment>(x => x.Mention!, ColumnType.String),
        DefaultColumnNames.Deleted.CreateCamelColumn(ColumnType.Boolean),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime),
    ];


    public static readonly Entity Entity = new (
        Attributes: [
            new Attribute(nameof(Comment.Id).Camelize(),DataType:DataType.Int),
            new Attribute(nameof(Comment.EntityName).Camelize()),
            new Attribute(nameof(Comment.RecordId).Camelize(),DataType:DataType.Int),
            new Attribute(nameof(Comment.User).Camelize(),DataType: DataType.Lookup, Options:PublicUserInfos.Entity.Name),
            new Attribute(nameof(Comment.Content).Camelize()),
            new Attribute(nameof(Comment.CreatedAt).Camelize(),DataType: DataType.Datetime),
            new Attribute(nameof(Comment.UpdatedAt).Camelize(),DataType:DataType.Datetime),
            new Attribute(DefaultAttributeNames.PublicationStatus.Camelize()),
        ],
        
        Name: nameof(Comment).Camelize(),
        DisplayName: "",
        TableName: "__comments",
        LabelAttributeName: nameof(Comment.Content).Camelize(),
        PrimaryKey: nameof(Comment.Id).Camelize()
    );
    
    public static readonly LoadedEntity LoadedEntity = Entity.ToLoadedEntity();
    
    private static string[] Fields =
    [
        nameof(Comment.Id).Camelize(),
        nameof(Comment.EntityName).Camelize(),
        nameof(Comment.RecordId).Camelize(),
        nameof(Comment.User).Camelize(),
        nameof(Comment.Content).Camelize(),
        nameof(Comment.Parent).Camelize(),
        nameof(Comment.Mention).Camelize(),
        nameof(Comment.CreatedAt).Camelize(),
        nameof(Comment.UpdatedAt).Camelize(),
    ];
    public static Query List(long parent,Sort[] sorts, ValidSpan span, ValidPagination pg)
    {
        var query = new Query(Entity.TableName)
            .Where(nameof(Comment.Parent).Camelize(), parent)
            .Where(nameof(DefaultColumnNames.Deleted).Camelize(), false)
            .Select(Fields)
            .Offset(pg.Offset)
            .Limit(pg.Limit);
        
        if (span.Span.IsEmpty())
        {
            query.ApplySorts(sorts);
        }
        else
        {
            query.ApplySpanFilter(span,sorts,s=>s.Field,s=>s.Field);
            query.ApplySorts(SpanHelper.IsForward(span.Span) ? sorts : sorts.ReverseOrder());
        }
        return query;
    }

    public static Query List(string entityName, long recordId, Sort[] sorts, ValidSpan span, ValidPagination pg)
    {
        var query = new Query(Entity.TableName)
            .Where(nameof(Comment.EntityName).Camelize(), entityName)
            .Where(nameof(Comment.RecordId).Camelize(), recordId)
            .Where(nameof(Comment.Parent).Camelize(), null)
            .Where(nameof(DefaultColumnNames.Deleted).Camelize(), false)
            .Select(Fields)
            .Offset(pg.Offset)
            .Limit(pg.Limit);
        
        if (span.Span.IsEmpty())
        {
            query.ApplySorts(sorts);
        }
        else
        {
            query.ApplySpanFilter(span,sorts,s=>s.Field,s=>s.Field);
            query.ApplySorts(SpanHelper.IsForward(span.Span) ? sorts : sorts.ReverseOrder());
        }
        return query;
    }

    public static Query Single(long id)
    {
        return new Query(Entity.TableName)
            .Where(nameof(Comment.Id).Camelize(), id)
            .Where(nameof(DefaultColumnNames.Deleted).Camelize(), false)
            .Select(Fields);
    }
    
    public static Query Insert(this Comment comment)
        => new Query(Entity.TableName).AsInsert(
            RecordExtensions.FormObject(
                comment, whiteList:
                [
                    nameof(Comment.EntityName),
                    nameof(Comment.RecordId),
                    nameof(Comment.User),
                    nameof(Comment.Content),
                    nameof(Comment.Parent),
                    nameof(Comment.Mention)
                ]
            ),true);
    
    public static Query Update(this Comment comment)
        => new Query(Entity.TableName)
            .Where(nameof(comment.Id).Camelize(), comment.Id)
            .Where(nameof(comment.User).Camelize(), comment.User)
            .AsUpdate(
                [nameof(Comment.Content).Camelize()],
                [comment.Content]
            );

    public static Query Delete(string userId, long id)
    => new Query(Entity.TableName)
        .Where(nameof(Comment.User).Camelize(), userId)
        .Where(nameof(Comment.Id).Camelize(), id)
        .AsUpdate([DefaultColumnNames.Deleted.Camelize()], [true]);
    
}