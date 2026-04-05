namespace FormCMS.Core.Descriptors;

public record ContentTag(
    Record Data,
    string EntityName ,
    string RecordId,
    string Title = "",
    string Url = "",
    string Image = "",
    string Subtitle = "",
    string Content = "",
    DateTime? PublishedAt = null
);