





---

## Asset Security Concern

<details>  
<summary>  
FormCMS takes measures to reduce security vulnerabilities  
</summary>  

### Large File Limitation

By default, ASP.NET Core buffers uploaded files entirely in memory, which can lead to excessive memory consumption.
FormCMS restricts individual file uploads to a default size of 10MB. This limit is configurable.

```csharp
// Set max file size to 15MB  
builder.AddSqliteCms(databaseConnectionString, settings => settings.MaxRequestBodySize = 1024 * 1024 * 15),  
```

### Chunked Uploading

For files exceeding the maximum size limit, FormCMS supports chunked uploading.
The client uploads the file in 1MB chunks.

### Custom File Storage Location

FormCMS supports both local and cloud-based file storage.
By default, uploaded files are saved to `<app>/wwwroot/files`.
However, this default setting may present some challenges:

1. It's difficult to retain uploaded files when redeploying the application.
2. Uploading large files to the system drive can exhaust disk space.

You can configure a different path for file storage as shown below:

```csharp
builder.AddSqliteCms(databaseConnectionString, settings => settings.LocalFileStoreOptions.PathPrefix = "/data/"),  
```

### File Type Restrictions and Signature Verification

FormCMS allows uploading only specific file types: `'gif'`, `'png'`, `'jpeg'`, `'jpg'`, `'zip'`, `'mp4'`, `'mpeg'`, `'mpg'`.
It verifies the binary signature of each file to prevent spoofing.
You can extend the file signature dictionary as needed.

```csharp
public Dictionary<string, byte[][]> FileSignature { get; set; } = new()  
{  
    {  
        ".gif", [  
            "GIF8"u8.ToArray()  
        ]  
    },  
    {  
        ".png", [  
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]  
        ]  
    },  
```

</details>  

---
