namespace FormCMS.Cms.Models;

public static class AssetTopics
{
    public const string AssetUpdate = "AssetUpdate";
    public const string ConvertVideo = "ConvertVideo";
}

public record ConvertVideoMessage(string AssetName, string Path, string TargetFormat, bool IsDelete, string? TargetPath = null, string? UserId = null);
public record AssetUpdateMessage(
    string TargetFormat,
    string OriginalPath,    // existing asset path (used as DB key)
    string? NewUrl,         // public URL of the converted/new file
    string? NewPath,        // storage path of the new file
    string? NewName,        // file name
    string? NewType,        // MIME type
    long? NewSize,          // file size in bytes
    int Progress,           // 100 = done
    bool IsNewAsset,         // true = add a new row, false = update existing row
    string? UserId = null    // pass back the user id to recover context
);