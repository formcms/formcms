namespace FormCMS.Core.Files;

public record File(
    string Path, // unique name, date + ulid
    string Name, // original name, for search
    string Title, // default as name, for link title
    int Size,
    string Type,
    Record Metadata,
    long Id = 0
);

public record FileLink(
    string EntityName,
    string EntityId,
    long FileId,
    long Id = 0
);
