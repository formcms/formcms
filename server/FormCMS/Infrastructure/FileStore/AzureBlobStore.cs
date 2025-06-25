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

    public Task Download(string path, string localPath, CancellationToken ct)
    {
        var blobClient = _containerClient.GetBlobClient(path);
        return DownloadBlob(blobClient, localPath, ct);
    }

    private static async Task DownloadBlob(BlobClient blobClient, string localPath, CancellationToken ct)
    {
        var destinationDir = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }
        await blobClient.DownloadToAsync(localPath, ct);
    }
    
    public async Task DownloadFileWithRelated(string path, string localPath, CancellationToken ct)
    {
        await foreach (var blob in _containerClient.GetBlobsAsync(prefix: path, cancellationToken: ct))
        {
            var blobClient = _containerClient.GetBlobClient(blob.Name);

            var relativePath = blob.Name[path.Length..].TrimStart('/');
            var destinationPath = Path.Combine(localPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

            await DownloadBlob(blobClient, destinationPath, ct);
        }
    }

    public async Task Del(string file, CancellationToken ct)
    {
        var blobClient = _containerClient.GetBlobClient(file);
        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
    }
    
    public async Task DelByPrefix(string prefix, CancellationToken ct)
    {
        await foreach (var blob in _containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: ct))
        {
            var blobClient = _containerClient.GetBlobClient(blob.Name);
            await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
        }
    }


    private string GetContentTypeFromExtension(string filePath)
    {
        return _contentTypeProvider.TryGetContentType(filePath, out var contentType)
            ? contentType
            : "application/octet-stream";
    }
}