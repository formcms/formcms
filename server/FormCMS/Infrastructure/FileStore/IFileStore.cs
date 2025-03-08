namespace FormCMS.Infrastructure.FileStore;

public interface IFileStore
{
    Task Upload(IEnumerable<(string,IFormFile)> files);
    Task Upload(string localPath, string path);
    Task Download(string path, string localPath);
    Task Del(string file);
    
    string GetUrl(string file);
    Task<long> GetFileSize(string file);
    Task<string> GetContentType(string file);
}