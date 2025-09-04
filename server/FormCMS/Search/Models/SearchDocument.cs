using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.DataModels;
using Humanizer;

namespace FormCMS.Search.Models;

public record SearchDocument(
    string EntityName,
    long RecordId,
    
    string Url = "",
    string Image = "",
    
    string Title = "",
    string Subtitle = "",
    string Content = "",
    
    DateTime? PublishedAt = null,
    long?Id = 0
);

public static class SearchConstant
{
    public const string TableName = "__search";
}

public static class SearchDocumentHelper
{
    public static readonly string[] UniqKeyFields = [
        nameof(SearchDocument.EntityName).Camelize(),
        nameof(SearchDocument.RecordId).Camelize(),
    ];

    public static readonly string[] FtsFields = [
        nameof(SearchDocument.Title).Camelize(),
        nameof(SearchDocument.Subtitle).Camelize(),
        nameof(SearchDocument.Content).Camelize()
    ];

    
    public static readonly Column[] Columns =
    [
        ColumnHelper.CreateCamelColumn<SearchDocument>(x => x.Id!, ColumnType.Id),
        ColumnHelper.CreateCamelColumn<SearchDocument, string>(x => x.EntityName),
        ColumnHelper.CreateCamelColumn<SearchDocument, long>(x => x.RecordId),
        
        ColumnHelper.CreateCamelColumn<SearchDocument, string>(x => x.Title),
        ColumnHelper.CreateCamelColumn<SearchDocument, string>(x => x.Subtitle),
        ColumnHelper.CreateCamelColumn<SearchDocument, string>(x => x.Content),
        
        ColumnHelper.CreateCamelColumn<SearchDocument, string>(x => x.Url),
        ColumnHelper.CreateCamelColumn<SearchDocument, string>(x => x.Image),
        
        DefaultAttributeNames.PublishedAt.CreateCamelColumn(ColumnType.Datetime),
        DefaultColumnNames.CreatedAt.CreateCamelColumn(ColumnType.CreatedTime),
        DefaultColumnNames.UpdatedAt.CreateCamelColumn(ColumnType.UpdatedTime),
    ];
    
    
}