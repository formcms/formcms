using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Mvc;

namespace FormCMS.Builders;

public static class Endpoints
{
    public static void MapConfigEndpoints(this WebApplication app)
    {
        app.MapGet("/api/system/is-ready",
            async ([FromServices] ISystemSetupService setupService, CancellationToken ct) =>
            {
                var (isReady, hasUser) = await setupService.GetSystemStatus(ct);
                return new { IsReady = isReady, HasUser = hasUser };
            });

        app.MapGet("/api/system/config",
            ([FromServices] ISystemSetupService setupService) => setupService.GetConfig());

        app.MapPut("/api/system/config", async (
            [FromServices] ISystemSetupService setupService,
            [FromBody] Settings settings
        ) =>
        {
            await setupService.UpdateConfig(settings);
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