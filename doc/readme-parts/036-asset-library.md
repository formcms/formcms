



---

## Asset Library
<details>
<summary>
The Asset Library centralizes the management of uploaded assets (e.g., images, files), supporting both local and cloud storage. It enables reuse, optimizes storage, and provides robust permissions and extensibility for various cloud providers.
</summary>
### Overview

Assets are stored in a repository, each identified by a unique `Path` (e.g., `2025-03/abc123`, where `2025-03` is a folder based on `yyyy-MM` and `abc123` is a ULID) and a fixed `Url` (e.g., `/files/2025-03/abc123` or a cloud-specific URL). Relationships to data entities are tracked via `AssetLink` records. The system supports local storage by default and integrates with cloud storage providers (e.g., Azure Cloud Storage) via the `IFileStore` interface. For images, uploads are resized to a default maximum width of 1200 pixels and a compression quality of 75, configurable in `SystemSettings`.

---

### Core Features

#### Adding Assets to Data
In forms with `image`, `file`, or `gallery` fields, users can:   
- **Upload**: Upload a new file via `IFileStore.Upload`. The system generates a unique `Path` (e.g., `2025-03/abc123`), sets a `Url` (local or cloud-based), and records metadata (`Name`, `Size`, `Type`, `CreatedBy`, `CreatedAt`). Images are resized (max width: 1200px, quality: 75) before saving to the chosen storage provider in the `2025-03` folder. A default `Title` is derived from `Name`.  
  - **Choose**: Select an existing asset from a dialog with:      
    - `Gallery View`: Thumbnails for images.    
    - `List View`: Table with `Name`, `Title`, `Size`, `CreatedAt`, and `Type`. Filterable by keyword, size range, or date range; sortable in ascending/descending order.    
    - Selection links the asset, incrementing `LinkCount` and adding an `AssetLink`.    
- **Delete**: Remove the asset reference from the entity, reducing `LinkCount`.    
- **Edit**: Update `Title` or metadata.    

#### Managing Orphan Assets   
The **Asset List Page** lists assets with `Name`, `Title`, `Size`, `Type`, `CreatedAt`, and `LinkCount`. Assets with `LinkCount` of 0 (orphans) can be deleted via `IFileStore.Del`, removing them from storage (e.g., the `2025-03` folder) and the system.   

#### Replacing Files   
On the **Asset Detail Page**, users can replace content:   
- Upload a new file via `IFileStore.Upload` to the same `Path` (e.g., `2025-03/abc123`), updating `Size`, `Type`, and `UpdatedAt`.   
- Images are resized per settings (max width: 1200px, quality: 75).   
- `Path` and `Url` remain unchanged, ensuring continuity for linked entities.   

#### Updating Metadata   
On the **Asset Detail Page**, users can modify:   
- **Title**: Change the display name (defaults to `Name`).   
- **Metadata**: Update key-value pairs (e.g., `{"AltText": "Description"}`), updating `UpdatedAt`.   

---

### Cloud Storage Integration   

The Asset Library supports cloud storage through the `IFileStore` interface, with Azure Cloud Storage as an example. Other providers (e.g., Google Cloud Storage, AWS S3) can be integrated by implementing this interface and registering it in the dependency injection container.   

#### `IFileStore` Interface   
```csharp   
namespace FormCMS.Infrastructure.FileStore;   

public record FileMetadata(long Size, string ContentType);

public interface IFileStore
{
    Task Upload(IEnumerable<(string, IFormFile)> files, CancellationToken ct);
    Task Upload(string localPath, string path, CancellationToken ct);
    Task<FileMetadata?> GetMetadata(string filePath, CancellationToken ct);
    string GetUrl(string file);
    Task Download(string path, string localPath, CancellationToken ct);
    Task Del(string file, CancellationToken ct);
}
```

#### Functionality   
- **Upload**: Stores files in the provider, using the folder structure (e.g., `2025-03/abc123`).   
- **GetMetadata**: Retrieves `Size` and `ContentType`.   
- **GetUrl**: Returns the asset’s URL (e.g., `https://<account>.blob.core.windows.net/<container>/2025-03/abc123` for Azure).   
- **Download**: Retrieves the file to a local path.   
- **Del**: Deletes the file from its folder.   

#### Extending to Other Providers   
To use Google Cloud Storage, AWS S3, or others:   
1. Implement `IFileStore` with provider-specific logic (e.g., S3’s `PutObject` for uploads to `2025-03/abc123`).   
2. Register it in dependency injection (e.g., `services.AddScoped<IFileStore, AwsS3FileStore>()`).   

#### Example: Azure Cloud Storage   
- Uploads files to Azure Blob Storage under the `2025-03` folder (e.g., `2025-03/abc123`).   
- Generates URLs like `https://<account>.blob.core.windows.net/<container>/2025-03/abc123`.   
- Supports metadata retrieval and deletion via Azure APIs.   

---

### Permissions   

A role-based system controls asset access:   
- **Super Admin**: Full control over all assets, including those in cloud folders (e.g., `2025-03`).   
- **No Permissions**: No asset interaction.   
- **Restricted Read**: Choose only self-uploaded assets.   
- **Full Read**: Choose any asset.   
- **Restricted Write**: Manage only self-uploaded assets.   
- **Full Write**: Manage all assets (except assigning).   

Permissions filter the library dialog and validate actions against ownership and storage location.   

---

### Configuration   

- **Image Compression** (`ImageCompressionOptions`):   
  - `MaxWidth`: Default 1200px.   
  - `Quality`: Default 75 (0-100).   
- **Asset URL**: Local prefix defaults to `/files` (e.g., `/files/2025-03/abc123`); cloud URLs depend on the provider (via `IFileStore.GetUrl`).   
- **Storage Provider**: Set via dependency injection (e.g., Azure, local).   

---

### Benefits   

- **Reuse**: Assets are shared across entities, reducing redundancy.   
- **Storage**: Orphan deletion, image resizing, and folder-based organization (e.g., `2025-03`) optimize usage; cloud storage scales capacity.   
- **Consistency**: Fixed `Url` ensures seamless updates.   
- **Flexibility**: Metadata, replacements, and cloud extensibility adapt to needs.   
- **Tracking**: `LinkCount` and `AssetLink` monitor usage.   
- **SEO**: `Title` as alt text enhances image discoverability.   
- **Scalability**: Cloud integration (e.g., Azure) and `IFileStore` support growing storage demands.   
</details>