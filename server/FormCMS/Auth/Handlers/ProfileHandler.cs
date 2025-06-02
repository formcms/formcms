using FormCMS.Auth.Services;
using FormCMS.Cms.Services;

namespace FormCMS.Auth.Handlers;

public static class ProfileHandler
{
    public sealed record ChangePasswordReq(string OldPassword, string Password);

    public static void MapProfileHandlers(this RouteGroupBuilder app)
    {
        app.MapPost("/password", async (
            IProfileService svc, 
            ChangePasswordReq request
        ) => await svc.ChangePassword(request.OldPassword, request.Password));

        app.MapPost("/avatar", async (
            IProfileService svc,
            HttpContext context,
            CancellationToken ct
        ) => await svc.UploadAvatar(context.Request.Form.Files.First(),ct));
    }
}