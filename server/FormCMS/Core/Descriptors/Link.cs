namespace FormCMS.Core.Descriptors;

public record Link(
    string RecordId,
    string Title = "",
    string Url = "",
    string Image = "",
    string Subtitle = "",
    DateTime? PublishedAt = null
    );