using FormCMS.Core.Descriptors;
using FormCMS.Core.Identities;
using FormCMS.CoreKit.RelationDbQuery;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.EnumExt;
using FormCMS.Utils.RecordExt;
using Humanizer;
using NUlid;
using Attribute = FormCMS.Core.Descriptors.Attribute;
using Column = FormCMS.Utils.DataModels.Column;
using Query = SqlKata.Query;

namespace FormCMS.Comments.Models;

public record Comment(
    string EntityName,
    long RecordId,
    string CreatedBy ="",
    string Content="",
    string Id = "", //use recordId +  ulid, make it easy for shard data
    string? Parent = null,
    string? Mention = null,
    DateTime PublishedAt = default,
    DateTime CreatedAt = default,
    DateTime UpdatedAt = default
);

public static class CommentHelper
{
    public const int DefaultPageSize = 20;
    public const string CommentsField = "comments";
    public const string CommentActivity = "comment";
    public const string CommentContentTagQuery = "commentContentTagQuery";

    public static readonly Column[] Columns = [
        ColumnHelper.CreateCamelColumn<Comment,string>(x => x.Id),
        ColumnHelper.CreateCamelColumn<Comment, string>(x => x.EntityName),
        ColumnHelper.CreateCamelColumn<Comment, long>(x => x.RecordId),
        ColumnHelper.CreateCamelColumn<Comment, string>(x => x.CreatedBy),
        ColumnHelper.CreateCamelColumn<Comment>(x => x.Content, ColumnType.Text),
        ColumnHelper.CreateCamelColumn<Comment>(x => x.Parent!, ColumnType.Int),
        ColumnHelper.CreateCamelColumn<Comment>(x => x.Mention!, ColumnType.String),
        DefaultColumnNames.Deleted.CreateCamelColumn(ColumnType.Boolean),
        DefaultAttributeNames.PublishedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime)
    ];


    public static readonly Entity Entity = new (
        Attributes: [
            new Attribute(nameof(Comment.Id).Camelize()),
            new Attribute(nameof(Comment.EntityName).Camelize(),DisplayType:DisplayType.Number),
            new Attribute(nameof(Comment.RecordId).Camelize(),DataType:DataType.Int,DisplayType:DisplayType.Number),
            new Attribute(nameof(Comment.Parent).Camelize(),DataType:DataType.Int,DisplayType:DisplayType.Number),
            new Attribute(nameof(Comment.CreatedBy).Camelize(),DataType: DataType.Lookup, Options:PublicUserInfos.Entity.Name),
            new Attribute(nameof(Comment.Content).Camelize()),
            new Attribute(nameof(Comment.CreatedAt).Camelize(),DataType: DataType.Datetime,DisplayType:DisplayType.LocalDatetime),
            new Attribute(nameof(Comment.UpdatedAt).Camelize(),DataType:DataType.Datetime,DisplayType:DisplayType.LocalDatetime),
            new Attribute(nameof(Comment.PublishedAt).Camelize(),DataType:DataType.Datetime,DisplayType:DisplayType.LocalDatetime),
            new Attribute(DefaultAttributeNames.PublicationStatus.Camelize())
        ],
        
        TagsQuery:CommentContentTagQuery,
        TagsQueryParam: nameof(Comment.Id).Camelize(),
        ImageTagField:"image",
        TitleTagField:"title",
        PublishTimeTagField:"publishedAt",
        SubtitleTagField:"subtitle",
        PageUrl: "url",
        Name: nameof(Comment).Camelize(),
        DisplayName: "",
        TableName: "__comments",
        LabelAttributeName: nameof(Comment.Content).Camelize(),
        PrimaryKey: nameof(Comment.Id).Camelize()
    );
    
    private static readonly string[] Fields =
    [
        nameof(Comment.Id).Camelize(),
        nameof(Comment.EntityName).Camelize(),
        nameof(Comment.RecordId).Camelize(),
        nameof(Comment.CreatedBy).Camelize(),
        nameof(Comment.Content).Camelize(),
        nameof(Comment.Parent).Camelize(),
        nameof(Comment.Mention).Camelize(),
        nameof(Comment.CreatedAt).Camelize(),
        nameof(Comment.UpdatedAt).Camelize()
    ];

    public static string GetSourceKey(this Comment comment) => $"{comment.EntityName}_{comment.RecordId}";
    public static Comment Parse(string  commentId)
    {
        var parts =  commentId.Split('_');
        return new Comment(parts[0], long.Parse(parts[1]), Id: parts[2] );
    }

    public static Comment AssignId(this Comment comment) => 
        comment with { Id = $"{comment.EntityName}_{comment.RecordId}_{Ulid.NewUlid()}"};

   
    public static Query List(ValidFilter[] filters,Sort[] sorts, ValidSpan span, ValidPagination pg)
    {
        var query = new Query(Entity.TableName)
            .Where(nameof(DefaultColumnNames.Deleted).Camelize(), false)
            .Select(Fields)
            .Offset(pg.Offset)
            .Limit(pg.Limit);

        query.ApplyFilters(filters);
        
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

    public static Query List(string entityName, long recordId, ValidSort[] sorts, ValidSpan? span, ValidPagination pg)
    {
        var query = new Query(Entity.TableName)
            .Where(nameof(Comment.EntityName).Camelize(), entityName)
            .Where(nameof(Comment.RecordId).Camelize(), recordId)
            .Where(nameof(Comment.Parent).Camelize(), null)
            .Where(nameof(DefaultColumnNames.Deleted).Camelize(), false)
            .Select(Fields)
            .Offset(pg.Offset)
            .Limit(pg.Limit);
        
        if (span is not null && span.Span.IsEmpty())
        {
            query.ApplySorts(sorts);
        }
        else
        {
            query.ApplySpanFilter(span,sorts,s=>s.Field,s=>s.Field);
            query.ApplySorts(SpanHelper.IsForward(span?.Span) ? sorts : sorts.ReverseOrder());
        }
        return query;
    }
    
    public static Query Multiple(string[] ids)
    {
        return new Query(Entity.TableName)
            .WhereIn(nameof(Comment.Id).Camelize(), ids)
            .Where(nameof(DefaultColumnNames.Deleted).Camelize(), false)
            .Select(Fields);
    }
    public static Query Single(string id)
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
                    nameof(Comment.Id),
                    nameof(Comment.EntityName),
                    nameof(Comment.RecordId),
                    nameof(Comment.CreatedBy),
                    nameof(Comment.Content),
                    nameof(Comment.Parent),
                    nameof(Comment.Mention)
                ]
            ),true);
    
    public static Query Update(this Comment comment)
        => new Query(Entity.TableName)
            .Where(nameof(comment.Id).Camelize(), comment.Id)
            .Where(nameof(comment.CreatedBy).Camelize(), comment.CreatedBy)
            .AsUpdate(
                [nameof(Comment.Content).Camelize()],
                [comment.Content]
            );

    public static Query Delete(string userId, string id)
    => new Query(Entity.TableName)
        .Where(nameof(Comment.CreatedBy).Camelize(), userId)
        .Where(nameof(Comment.Id).Camelize(), id)
        .AsUpdate([DefaultColumnNames.Deleted.Camelize()], [true]);
    
}