namespace FormCMS.Cms.Models;

public static class AssetTopics
{
    public const string AssetUpdate = "AssetUpdate";
    public const string ConvertVideo = "ConvertVideo";
}

public static class ConvertVideoFormats
{
    public const string Mp3 = "mp3";
    public const string M4a = "m4a";
    public const string M3u8 = "m3u8";

    public static readonly Dictionary<string, string> MimeTypes = new()
    {
        { Mp3, "audio/mp3" },
        { M4a, "audio/m4a" },
        { M3u8, "application/vnd.apple.mpegurl" }
    };
}

public record ConvertVideoMessage( string Path, string TargetPath );

public record AssetUpdateMessage( string NewPath, int Progress );
