using FormCMS.MonoApp;
using Microsoft.AspNetCore.Mvc;

namespace FormCMS.MonoApp;

public static class CorsEndpointsExtensions
{
    public static void MapCorsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/system/cors", (ISystemSetupService service) =>
        {
            var origins = service.GetCorsOrigins();
            return Results.Ok(origins);
        });

        app.MapPost("/api/system/cors", async (ISystemSetupService service, [FromBody] string[] origins) =>
        {
            await service.UpdateCorsOrigins(origins);
            return Results.Ok();
        });
    }
}
