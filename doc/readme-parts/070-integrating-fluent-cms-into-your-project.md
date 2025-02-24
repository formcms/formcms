

---
## Integrating it into Your Project

<details>
<summary>
Follow these steps to integrate FormCMS into your project using a NuGet package.
</summary>

You can reference the code from https://github.com/FormCMS/FormCMS/tree/main/examples

1. **Create a New ASP.NET Core Web Application**.

2. **Add the NuGet Package**:
   To add FormCMS, run the following command:  
   ```
   dotnet add package FormCMS
   ```

3. **Modify `Program.cs`**:
   Add the following line before `builder.Build()` to configure the database connection (use your actual connection string):  
   ```
   builder.AddSqliteCms("Data Source=cms.db");
   var app = builder.Build();
   ```
   Currently,  FormCMS supports `AddSqliteCms`, `AddSqlServerCms`, and `AddPostgresCms`.

4. **Initialize FormCMS**:
   Add this line after `builder.Build()` to initialize the CMS:  
   ```
   await app.UseCmsAsync();
   ```  
   This will bootstrap the router and initialize the FormCMS schema table.

</details>