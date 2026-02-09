using Microsoft.Extensions.FileProviders;

namespace FormCMS.Builders;

public interface ISpaService
{
    void MapSpas(IApplicationBuilder app);
}

public class SpaService(IWebHostEnvironment env) : ISpaService
{
    public void MapSpas(IApplicationBuilder app)
    {
        var settings = SettingsStore.Load();
        if (settings?.Spas is null)
        {
            return;
        }

        var systemSettings = app.ApplicationServices.GetRequiredService<SystemSettings>();
        systemSettings.KnownPaths = [..systemSettings.KnownPaths, ..settings.Spas.Select(s => s.Path)];
        var knownPaths = systemSettings.KnownPaths.Select(p => p.StartsWith("/") ? p : "/" + p).ToList();

        foreach (var spa in settings.Spas)
        {
            var path = spa.Path.StartsWith("/") ? spa.Path : "/" + spa.Path;
            var dir = Path.Combine(env.WebRootPath, spa.Dir);
            if (!Directory.Exists(dir))
            {
                continue;
            }
            
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(dir),
                RequestPath = path == "/" ? "" : path
            });

            if (path == "/")
            {
                app.MapWhen(context => 
                        !knownPaths.Any(kp => context.Request.Path.StartsWithSegments(kp)),
                    subApp =>
                    {
                        subApp.UseRouting();
                        subApp.UseEndpoints(endpoints =>
                        {
                            endpoints.MapFallbackToFile("index.html");
                            endpoints.MapFallbackToFile("{*path:nonfile}", "index.html");
                        });
                    });
            }
            else
            {
                app.MapWhen(context => context.Request.Path.StartsWithSegments(path),
                    subApp =>
                    {
                        subApp.UseRouting();
                        subApp.UseEndpoints(endpoints =>
                        {
                            var indexPath = $"{path.TrimEnd('/')}/index.html";
                            endpoints.MapFallbackToFile(path, indexPath);
                            endpoints.MapFallbackToFile($"{path.TrimEnd('/')}/{{*path:nonfile}}", indexPath);
                        });
                    });
            }
        }
    }
}