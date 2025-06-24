namespace FormCMS.Infrastructure.FileStore;

public static class FileUtils
{
    public static void EnsureFolder(string destDir)
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
}