using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Mvc;

namespace FormCMS.Builders;

public static class Endpoints
{
    public static async Task MapConfigEndpoints(this WebApplication app)
    {
        app.MapGet("/api/system/is-ready",
            async ([FromServices] ISystemSetupService setupService, CancellationToken ct) =>
            {
                var (databaseReady, hasMasterPassword, hasUser) = await setupService.GetSystemStatus(ct);
                return new { DatabaseReady = databaseReady, HasMasterPassword = hasMasterPassword, HasUser = hasUser };
            });

        app.MapPost("/api/system/config",
            ([FromServices] ISystemSetupService setupService,
             [FromBody] MasterPasswordRequest request) =>
            {
                var config = setupService.GetConfig(request.MasterPassword);
                return config is not null ? Results.Ok(config) : Results.NotFound();
            });

        app.MapPut("/api/system/config/database", async (
            [FromServices] ISystemSetupService setupService,
            [FromBody] DatabaseConfigRequest request
        ) =>
        {
            await setupService.UpdateDatabaseConfig(request.DatabaseProvider, request.ConnectionString, request.MasterPassword);
            return Results.Ok();
        });

        app.MapPut("/api/system/config/master-password", async (
            [FromServices] ISystemSetupService setupService,
            [FromBody] MasterPasswordRequest request
        ) =>
        {
            await setupService.UpdateMasterPassword(request.MasterPassword, request.OldMasterPassword);
            return Results.Ok();
        });

        app.MapPost("/api/system/setup-super-admin", async (
            [FromBody] SuperAdminRequest request,
            [FromServices] ISystemSetupService setupService,
            CancellationToken ct) =>
        {
            await setupService.SetupSuperAdmin(request, ct);
            return Results.Ok();
        });

        app.MapPost("/api/system/add-spa", async (
            [FromServices] ISystemSetupService setupService,
            [FromForm] IFormFile file,
            [FromForm] string path,
            [FromForm] string dir
        ) =>
        {
            await setupService.AddSpa(file, path, dir);
            return Results.Ok();
        }).DisableAntiforgery();

        app.MapGet("/api/system/spas", ([FromServices] ISystemSetupService setupService) => setupService.GetSpas());

        app.MapDelete("/api/system/spas", async (
            [FromServices] ISystemSetupService setupService,
            [FromQuery] string path
        ) =>
        {
            await setupService.DeleteSpa(path);
            return Results.Ok();
        });
    }

    public static void MapSpas(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var spaService = scope.ServiceProvider.GetRequiredService<ISpaService>();
        spaService.MapSpas(app);
    }
}