using System.Text.Json;
using FormCMS.Core.HookFactory;
using FormCMS.Infrastructure.EventStreaming;
using FormCMS.Video.Models;

namespace FormCMS.Video.Builders;

public static class HookRegistryExtensions
{
    public static void RegisterVideoMessageProducerPlugIn(this HookRegistry registry)
    {
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
    }
}