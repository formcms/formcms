namespace FormCMS.Infrastructure.FileStore;

public static class Util
{
    public static string GetContentType(string extension)
    {
        return extension.ToLower() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".txt" => "text/plain",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream" // Default for unknown types
        };
    }
}