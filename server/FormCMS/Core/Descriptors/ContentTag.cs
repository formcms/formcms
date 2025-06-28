namespace FormCMS.Core.Descriptors;

public record ContentTag(
    string RecordId,
    string Title = "",
    string Url = "",
    string Image = "",
    string Subtitle = "",
    DateTime? PublishedAt = null
    );