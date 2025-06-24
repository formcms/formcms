namespace FormCMS.Video.Models
{
    public record FFMpegMessage(string AssetName, string Path, string TargetFormat,bool IsDelete);
}
