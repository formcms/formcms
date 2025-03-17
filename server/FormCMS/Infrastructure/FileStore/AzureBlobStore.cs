using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.StaticFiles;

namespace FormCMS.Infrastructure.FileStore;

public record AzureBlobStoreOptions(string ConnectionString, string ContainerName, string UrlPrefix);

public class AzureBlobStore(AzureBlobStoreOptions options) : IFileStore
{
    private readonly BlobContainerClient _containerClient = new(options.ConnectionString, options.ContainerName);
    private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    public async Task Upload(IEnumerable<(string, IFormFile)> files, CancellationToken ct)
    {
        await _containerClient.CreateIfNotExistsAsync(cancellationToken: ct);

        foreach (var (fileName, file) in files)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);
            await using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                }
            }, cancellationToken: ct);
        }
    }

    public async Task Upload(string localPath, string path, CancellationToken ct)
    {
        await _containerClient.CreateIfNotExistsAsync(cancellationToken: ct);

        var blobClient = _containerClient.GetBlobClient(path);
        await using var fileStream = File.OpenRead(localPath);
        await blobClient.UploadAsync(fileStream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = GetContentTypeFromExtension(path)
            }
        }, cancellationToken: ct);
    }


    public async Task<FileMetadata?> GetMetadata(string filePath, CancellationToken ct)
    {
        var blobClient = _containerClient.GetBlobClient(filePath);
        var properties = await blobClient.GetPropertiesAsync(cancellationToken: ct);
        return properties is null
            ? null
            : new FileMetadata(properties.Value.ContentLength, properties.Value.ContentType);
    }

    public string GetUrl(string file)
    {
        return Path.Join(options.UrlPrefix, file);
    }

    public async Task Download(string path, string localPath, CancellationToken ct)
    {
        var blobClient = _containerClient.GetBlobClient(path);
        var downloadInfo = await blobClient.DownloadAsync(ct);

        var destinationDir = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        await using var fileStream = File.OpenWrite(localPath);
        await downloadInfo.Value.Content.CopyToAsync(fileStream, ct);
    }

    public async Task Del(string file, CancellationToken ct)
    {
        var blobClient = _containerClient.GetBlobClient(file);
        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
    }


    private string GetContentTypeFromExtension(string filePath)
    {
        return _contentTypeProvider.TryGetContentType(filePath, out var contentType)
            ? contentType
            : "application/octet-stream";
    }
}