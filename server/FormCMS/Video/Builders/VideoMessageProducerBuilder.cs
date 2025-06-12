using System.Text.Json;
using FormCMS.Core.HookFactory;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Video.Models;

namespace FormCMS.Video.Builders;

public class VideoMessageProducerBuilder
{
    public static IServiceCollection AddVideoMessageProducer(IServiceCollection services)
    {
        services.AddSingleton(new VideoMessageProducerBuilder());
        return services;
    }
    
    public WebApplication UseVideo(WebApplication app)
    {
        var registry = app.Services.GetRequiredService<HookRegistry>();
        registry.AssetPreAdd.RegisterDynamic("*", async (AssetPreAddArgs args, IStringMessageProducer producer) =>
        {
            if (args.RefAsset.Type.Contains("video/"))
            {
                var msg = JsonSerializer.Serialize(new FFMpegMessage(args.RefAsset.Name, args.RefAsset.Path, "m3u8"));
                await producer.Produce(VideoTopics.Rdy4FfMpeg, msg);
            }

            return args;
        });
        return app;
    }
}