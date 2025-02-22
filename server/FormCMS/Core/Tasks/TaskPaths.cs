using System.IO.Compression;
using Microsoft.Data.Sqlite;

namespace FormCMS.Core.Tasks;


public record TaskPaths(string Zip, string FullZip, string Folder, string Db);
internal static class SystemTaskExtensions
{
    public static SqliteConnection CreateConnection(this TaskPaths paths) {
        if (!File.Exists(paths.Folder))
        {
            Directory.CreateDirectory(paths.Folder);
        }
        var connectionString = $"Data Source={paths.Db}";
        var conn = new SqliteConnection(connectionString);
        conn.Open();
        return conn;
    }
    
    public static void Clean(this TaskPaths paths)
    {
        Directory.Delete(paths.Folder, true);
        File.Delete(paths.FullZip);
    }
    
    public static void ExtractTaskFile(this TaskPaths paths)
    {
        if (!Directory.Exists(paths.Folder))
        {
            Directory.CreateDirectory(paths.Folder);
        }
        // Extract the zip file to the folder
        ZipFile.ExtractToDirectory(paths.FullZip, paths.Folder);
    }

    public static void Zip(this TaskPaths paths)
    {
        ZipFile.CreateFromDirectory(paths.Folder, paths.Zip); 
    }
}