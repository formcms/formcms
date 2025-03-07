using SkiaSharp;

namespace FormCMS.Infrastructure.ImageUtil;

public record ResizeOptions(int MaxWidth, int Quality);

public class Resizer(ResizeOptions opts):IResizer
{
    public IFormFile CompressImage(IFormFile inputFile)
    {
        if (!IsImage(inputFile))
        {
            return inputFile;
        }

        using var inputStream = inputFile.OpenReadStream();
        var outputStream = new MemoryStream(); // no using here, because FormFile still need it, will be disposed when call copyTo

        Compress(inputStream, outputStream);
        outputStream.Position = 0;

        // Create a new IFormFile from the compressed stream
        var outputFileName = Path.ChangeExtension(inputFile.FileName, ".jpg"); // Output is JPEG
        return new FormFile(
            baseStream: outputStream,
            baseStreamOffset: 0,
            length: outputStream.Length,
            name: "file", // Form field name
            fileName: outputFileName
        )
        {
            Headers = inputFile.Headers, // Preserve original headers if needed
            ContentType = "image/jpeg"   // Set content type to JPEG
        };
    }
    
    private bool IsImage(IFormFile file)
    {
        string[] validExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".gif"];
        var ext = Path.GetExtension(file.FileName).ToLower();
        return validExtensions.Contains(ext);
    }
    
    private void Compress(Stream inputStream, Stream outputStream )
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
}