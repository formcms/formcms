using NUlid;

namespace FormCMS.Infrastructure.FileStore;

public static class FileUtils
{
    private static void EnsureFolder(string? destDir)
    {
        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }
    }
    
    public static void EnsureParentFolder(string destPath)
    {
        var destDir = Path.GetDirectoryName(destPath);
        EnsureFolder(destDir);
    }
    
    public static string GetFilePath(string fileName)
    {
        var dir = DateTime.Now.ToString("/yyyy-MM");
        var file = string.Concat(Ulid.NewUlid().ToString().AsSpan(6, 20), Path.GetExtension(fileName));
        return Path.Join(dir, file);
    }
}