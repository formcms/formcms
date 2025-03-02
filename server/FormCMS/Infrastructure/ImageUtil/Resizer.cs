using SkiaSharp;

namespace FormCMS.Infrastructure.ImageUtil;

public record ResizeOptions(int MaxWidth, int Quality);

public class Resizer(ResizeOptions opts):IResizer
{
    public void Compress(Stream inputStream, Stream outputStream )
    {
        using var originalBitmap = SKBitmap.Decode(inputStream);
        if (originalBitmap.Width > opts.MaxWidth)
        {
            var scaleFactor = (float)opts.MaxWidth/ originalBitmap.Width;
            var newHeight = (int)(originalBitmap.Height * scaleFactor);
            var resizedImage = originalBitmap.Resize(new SKImageInfo(opts.MaxWidth, newHeight), SKSamplingOptions.Default);
            resizedImage?.Encode(outputStream, SKEncodedImageFormat.Jpeg, opts.Quality);        
        }
        else
        {
            //inputStream isn’t necessarily rewindable after decoding
            originalBitmap.Encode(outputStream, SKEncodedImageFormat.Jpeg, opts.Quality);
        } 
    }
    
    public bool IsImage(IFormFile file)
    {
        string[] validExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".gif"];
        var ext = Path.GetExtension(file.FileName).ToLower();
        return validExtensions.Contains(ext);
    }
}