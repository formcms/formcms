namespace FormCMS.Infrastructure.EventStreaming
{
    public record FFMpegMessage(string AssetName, string Path, string TargetFormat);
}
