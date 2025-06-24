using System.Text.Json;
using FormCMS.Core.HookFactory;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Infrastructure.FileStore;
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
        registry.AssetPostAdd.RegisterDynamic("*", async (AssetPostAddArgs args, IStringMessageProducer producer) =>
        {
            if (args.Asset.Type.Contains("video/"))
            {
                var msg = JsonSerializer.Serialize(new FFMpegMessage(args.Asset.Name, args.Asset.Path, "m3u8",false));
                await producer.Produce(VideoTopics.Rdy4FfMpeg, msg);
            }

            return args;
        });
        registry.AssetPostDelete.RegisterDynamic("*", async (AssetPostDeleteArgs args,  IStringMessageProducer producer) =>
        {
            if (args.Asset.Type.Contains("video/"))
            {
                var msg = JsonSerializer.Serialize(new FFMpegMessage(args.Asset.Name, args.Asset.Path, "m3u8",true));
                await producer.Produce(VideoTopics.Rdy4FfMpeg, msg);
            }
            return args;
        });
        return app;
    }
}