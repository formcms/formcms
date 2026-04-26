using Microsoft.AspNetCore.Mvc;

namespace FormCMS.MonoApp;

public static class Endpoints
{
    public static void MapConfigEndpoints(this RouteGroupBuilder app)
    {
        app.MapGet("/is-ready",
            async ([FromServices] ISystemSetupService setupService, CancellationToken ct) =>
            {
                var (databaseReady, hasSuperAdmin) = await setupService.GetSystemStatus(ct);
                return new { DatabaseReady = databaseReady, HasSuperAdmin = hasSuperAdmin };
            }).CacheOutput(x => x.NoCache());

        app.MapPost("/setup-database", async (
            [FromServices] ISystemSetupService setupService,
            [FromBody] DatabaseConfigRequest request
        ) =>
        {
            await setupService.UpdateDatabaseConfig(request.DatabaseProvider, request.ConnectionString);
            return Results.Ok();
        });

        app.MapPost("/setup-super-admin", async (
            [FromBody] SuperAdminRequest request,
            [FromServices] ISystemSetupService setupService,
            CancellationToken ct) =>
        {
            await setupService.SetupSuperAdmin(request, ct);
            return Results.Ok();
        });

        app.MapPost("/add-spa", async (
            [FromServices] ISystemSetupService setupService,
            [FromForm] IFormFile file,
            [FromForm] string path,
            [FromForm] string dir
        ) =>
        {
            await setupService.AddSpa(file, path, dir);
            return Results.Ok();
        }).DisableAntiforgery();

        app.MapGet("/spas", ([FromServices] ISystemSetupService setupService) => setupService.GetSpas());

        app.MapDelete("/spas", async (
            [FromServices] ISystemSetupService setupService,
            [FromQuery] string path
        ) =>
        {
            await setupService.DeleteSpa(path);
            return Results.Ok();
        });

        app.MapPut("/spas", async (
            [FromServices] ISystemSetupService setupService,
            [FromQuery] string oldPath,
            [FromQuery] string newPath
        ) =>
        {
            await setupService.UpdateSpaPath(oldPath, newPath);
            return Results.Ok();
        });

        app.MapGet("/api-key",
            ([FromServices] ISystemSetupService setupService) => new { ApiKey = setupService.GetApiKey() });

        app.MapPut("/api-key", async (
            [FromServices] ISystemSetupService setupService,
            [FromBody] ApiKeyRequest request
        ) =>
        {
            await setupService.UpdateApiKey(request.ApiKey);
            return Results.Ok();
        });

        app.MapGet("/download-plugins",
            ([FromServices] ISystemSetupService setupService) => setupService.GetDownloadPlugins());

        app.MapDelete("/download-plugins", async (
            [FromServices] ISystemSetupService setupService,
            [FromQuery] string fileName
        ) =>
        {
            await setupService.DeleteDownloadPlugin(fileName);
            return Results.Ok();
        });

        app.MapPost("/download-plugins", async (
            [FromServices] ISystemSetupService setupService,
            [FromForm] IFormFile file
        ) =>
        {
            await setupService.AddDownloadPlugin(file);
            return Results.Ok();
        }).DisableAntiforgery();
    }

    public static void MapSpas(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var spaService = scope.ServiceProvider.GetRequiredService<ISpaService>();
        spaService.MapSpas(app);
    }
}