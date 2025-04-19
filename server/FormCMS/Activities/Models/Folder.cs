namespace FormCMS.Activities.Models;

public record Folder(
    string UserId,
    string Name,
    string Description,
    long? id
);