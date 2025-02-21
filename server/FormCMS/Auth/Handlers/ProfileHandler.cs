using FormCMS.Auth.Services;
using FormCMS.Cms.Services;
using FormCMS.Utils.ResultExt;

namespace FormCMS.Auth.Handlers;

public static class ProfileHandler
{
    public static void MapProfileHandlers(this RouteGroupBuilder app)
    {
        app.MapPost("/password", async (
            IProfileService svc, ProfileDto dto
        ) => await svc.ChangePassword(dto));

        app.MapGet("/info", (
            IProfileService svc
        ) =>
        {
            var info = svc.GetInfo();
            return info is not null ? Results.Ok(info) : Results.Unauthorized();
        });
    }
}