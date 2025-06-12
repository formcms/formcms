using System.Text.Json;
using FormCMS.Comments.Services;
using FormCMS.Core.HookFactory;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Video.Workers;

namespace FormCMS.Video.Builders;

public class VideoBuilder
{
    public static IServiceCollection AddWorker(
        IServiceCollection services, int ffmpegDelay )
    {
        services.AddSingleton(new FFMepgConversionDelayOptions(ffmpegDelay));
        services.AddHostedService<FFMpegWorker>();    
        return services;
    }
}