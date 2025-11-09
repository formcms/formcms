using FormCMS.Core.HookFactory;

namespace FormCMS.Video.Builders;

public class VideoBuilder
{
    public static IServiceCollection AddVideo(IServiceCollection services)
    {
        services.AddSingleton<VideoBuilder>();
        return services;
    }

    public Task UseVideo(WebApplication app)
    {
        app.Services.GetRequiredService<HookRegistry>().RegisterVideoMessageProducerPlugIn(); 
        return Task.CompletedTask;
    }
}