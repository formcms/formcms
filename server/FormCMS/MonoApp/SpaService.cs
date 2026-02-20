using Microsoft.Extensions.FileProviders;

namespace FormCMS.MonoApp;

public interface ISpaService
{
    void MapSpas(WebApplication app);
}

public class SpaService : ISpaService
{
    public void MapSpas(WebApplication app)
    {
        var monoSettings = app.Services.GetRequiredService<MonoSettings>();
        var monoRuntime = app.Services.GetRequiredService<MonoRunTime>();
        foreach (var spa in monoSettings.Spas??[])
        {
            var dir = Path.Combine(monoRuntime.AppRoot, spa.Dir);
            if (!Directory.Exists(dir))
            {
                continue;
            }
            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = spa.Path == "/" ? "" : spa.Path,
                FileProvider = new PhysicalFileProvider(dir)
            });  
            
            if (spa.Path == "/")
            {
                app.MapFallback(async context =>
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.SendFileAsync(
                        Path.Combine(dir, "index.html"));
                });
            }
            else
            {
                app.MapFallback($"{spa.Path}/{{*path:nonfile}}", async context =>
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.SendFileAsync(
                        Path.Combine(dir, "index.html"));
                });
            }
        }
    }
}