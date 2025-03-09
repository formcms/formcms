



## Asset Library
<details>
<summary>
The Asset Library tracks uploaded assets (images, files, etc.), enabling reuse across the system, optimizing storage, and providing robust management capabilities.
</summary>

The system maintains a centralized repository of assets, each represented by the `Asset` record with metadata, usage tracking, and linking capabilities. Assets are uniquely identified by a `Path` (e.g., a combination of a `yyyy-MM` date and ULID), and their relationships to other data entities are tracked via `AssetLink` records. For images, the system automatically resizes them to optimize storage space, using a default maximum width of 1200 pixels and a compression quality of 75, configurable via system settings.

### Key Features

#### Add Asset to Data
In data management forms, fields for `image`, `file`, or `gallery` inputs provide the following options: 

- **Choose File**: Upload a new file to the server. The system generates a unique `Path` (e.g., `2025-03-abc123`), assigns a fixed `Url` based on the `Path`, and records the `Name` (original filename), `Size`, `Type`, and `CreatedBy` metadata. For images, the system resizes the file to a maximum width of 1200 pixels (configurable via `ImageCompressionOptions.MaxWidth`) with a compression quality of 75 (configurable via `ImageCompressionOptions.Quality`) before saving, updating the `Size` accordingly. The `CreatedAt` timestamp is set to the upload time, and a default `Title` is derived from the `Name`. 
- **Choose Library**: Opens a dialog to select an existing asset from the library. The dialog offers: 
    - `Gallery View`: Displays assets as thumbnails (ideal for images). 
    - `List View`: Shows assets in a tabular format with details like `Name`, `Title`, `Size`, `CreatedAt`, and `Type`. Users can filter by Name (e.g., search by keyword), Size (e.g., range of bytes), and CreatedAt (e.g., date range), and order the list by these fields in ascending or descending order. 
    - Selecting an asset links it to the current data entity, incrementing its `LinkCount` and adding an `AssetLink` entry. 
- **Delete**: Removes the asset reference from the current data entity by clearing its path, potentially reducing the asset’s `LinkCount`. 

#### Delete Orphan Assets 
On the **Asset List Page**, users can view all assets with details such as `Name`, `Title`, `Size`, `Type`, `CreatedAt`, and `LinkCount` (the number of data entities referencing the asset). Assets with a `LinkCount` of 0 (orphans) can be deleted to free up storage. Deletion removes the asset’s file and its record from the system.

#### Replace File 
On the **Asset Detail Page**, users can upload a new file to replace an existing asset’s content:  
- The new file updates the asset’s `Size`, `Type`, and `UpdatedAt` timestamp.  
- For images, the system resizes the new file to a maximum width of 1200 pixels (configurable via `ImageCompressionOptions.MaxWidth`) with a compression quality of 75 (configurable via `ImageCompressionOptions.Quality`) before saving.  
- The `Path` and `Url` remain unchanged, ensuring existing references remain valid.  
- All data entities linked to the asset (via `AssetLink`) automatically use the new file content when accessing the fixed `Url`.   

#### Update Metadata 
The **Asset Detail Page** allows users to modify an asset’s metadata:  
- **Title**: Edit the `Title` field (defaults to `Name` on creation). This can serve as a display name, link title, or caption (e.g., for images in galleries).  
- **Metadata Dictionary**: Add or update key-value pairs in the `Metadata` record to store custom information (e.g., `{"AltText": "Description", "Category": "Marketing"}`).  
- Changes to metadata update the `UpdatedAt` timestamp.  

### Data Structure 
- **Asset**: Represents a stored file with fields like `Path` (unique identifier), `Url` (fixed access location), `Name`, `Title`, `Size`, `Type`, `Metadata`, `CreatedBy`, `CreatedAt`, `UpdatedAt`, `Id`, `LinkCount` (calculated), and `Links` (calculated array of `AssetLink`). 
- **AssetLink**: Tracks relationships between assets and data entities, with fields like `EntityName` (e.g., "BlogPost"), `RecordId` (entity’s ID), `AssetId`, `CreatedAt`, `UpdatedAt`, and `Id`. 

### Configuration 
- **Image Compression**: Controlled via `ImageCompressionOptions` in `SystemSettings`: 
    - `MaxWidth`: Defaults to 1200 pixels; can be overridden to adjust the maximum width of resized images. 
    - `Quality`: Defaults to 75 (on a 0-100 scale); can be adjusted to balance file size and image clarity. 
- **Asset URL**: The `Url` is prefixed with `/files` by default (configurable via `SystemSettings.AssetUrlPrefix`), ensuring a consistent access pattern (e.g., `/files/2025-03-abc123`). 

### Benefits
- **Reuse**: Assets can be shared across multiple data entities, reducing redundancy. 
- **Storage Optimization**: Orphaned assets can be removed, and configurable image resizing (e.g., max width 1200px, quality 75) minimizes storage usage. 
- **Consistency**: The fixed `Url` ensures seamless updates when replacing files, maintaining all existing links. 
- **Flexibility**: Metadata, file replacement, and adjustable compression settings allow assets to evolve without breaking references. 
- **Tracking**: `LinkCount` and `AssetLink` provide visibility into asset usage. 

</details>

