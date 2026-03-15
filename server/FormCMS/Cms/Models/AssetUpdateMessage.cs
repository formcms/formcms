namespace FormCMS.Cms.Models;

/// <summary>
/// Generic message for updating or adding an asset from any plugin.
/// The plugin fills in what it knows; the handler applies it.
/// </summary>
public record AssetUpdateMessage(
    string OriginalPath,    // existing asset path (used as DB key)
    string? NewUrl,         // public URL of the converted/new file
    string? NewPath,        // storage path of the new file
    string? NewName,        // file name
    string? NewType,        // MIME type
    long? NewSize,          // file size in bytes
    int Progress,           // 100 = done
    bool IsNewAsset         // true = add a new row, false = update existing row
);
