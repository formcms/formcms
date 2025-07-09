namespace FormCMS.Infrastructure.ImageUtil;

public static class ImageUtil
{
    public static bool IsImage(this IFormFile inputFile)
    {
        var ext = Path.GetExtension(inputFile.FileName).ToLower();
        return ext is ".jpg" or ".jpeg" or ".png" or ".bmp";
    }
}