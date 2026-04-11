using System.Text.Json;
using System.Text.Json.Serialization;
using FormCMS;
using FormCMS.Auth.Models;
using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.MonoApp;
using System.IO; // Added for Path.Combine and File.ReadAllText
using System;
using FormCMS.Auth.Services;
using FormCMS.Core.Identities; // Added for AppContext.BaseDirectory

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var dataPath = builder.Configuration.GetValue<string>("FORMCMS_DATA_PATH")??"";
var sizeStr = builder.Configuration.GetValue<string>("FORMCMS_MAX_REQUEST_SIZE");
builder.SetMaxRequestBody(sizeStr);

var settings = builder.AddMonoApp(dataPath);
var app = builder.Build();
app.UseMonoCors();
app.MapReverseProxy();
await app.MapConfigEndpoints();
app.MapCorsEndpoints();

if (!string.IsNullOrWhiteSpace(settings?.ConnectionString)   && await app.EnsureDbCreatedAsync())
{
    await using var scope = app.Services.CreateAsyncScope();
    var entitiesFilePath = Path.Combine(AppContext.BaseDirectory, "entities.json");
    if (File.Exists(entitiesFilePath))
    {
        var entitySchemaService = scope.ServiceProvider.GetRequiredService<IEntitySchemaService>();
        var jsonContent = await File.ReadAllTextAsync(entitiesFilePath);
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
        var schemas = JsonSerializer.Deserialize<Schema[]>(jsonContent, jsonSerializerOptions) ?? Array.Empty<Schema>();

        foreach (var schema in schemas)
        {
            var existing =
                await entitySchemaService.LoadEntity(schema.Name, PublicationStatus.Published, CancellationToken.None);
            if (!existing.IsSuccess)
            {
                await entitySchemaService.SaveTableDefine(schema, true, CancellationToken.None);
            }
        }
        
        var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();
        await accountService.EnsureRoleAccess(new RoleAccess(
            Name:Roles.User,
            RestrictedReadonlyEntities:[],
            RestrictedReadWriteEntities:schemas.Select(x=>x.Name).ToArray(),
            ReadWriteEntities:[],
            ReadonlyEntities:[]
            ));
    }

    await app.UseCmsAsync();
    app.MapSpas();
}


app.Run();
