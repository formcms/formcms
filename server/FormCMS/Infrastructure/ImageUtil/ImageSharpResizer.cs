using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace FormCMS.Infrastructure.ImageUtil;

public class ImageSharpResizer(ResizeOptions opts) : IResizer
{
    public IFormFile CompressImage(IFormFile inputFile)
    {
        if (!IsImage(inputFile))
        {
            return inputFile;
        }
        
        using var inputStream = inputFile.OpenReadStream();
        using var image = Image.Load(inputStream);
        
        if (image.Width > opts.MaxWidth)
        {
            var scaleFactor = (float)opts.MaxWidth / image.Width;
            var newHeight = (int)(image.Height * scaleFactor);
            image.Mutate(x => x.Resize(opts.MaxWidth, newHeight));
        }
        
        var outputStream = new MemoryStream(); // No 'using' to keep it open for FormFile
        image.Save(outputStream, new JpegEncoder { Quality = opts.Quality });
        outputStream.Position = 0;

        var outputFileName = Path.ChangeExtension(inputFile.FileName, ".jpg");
        return new FormFile(outputStream, 0, outputStream.Length, "file", outputFileName)
        {
            Headers = inputFile.Headers,
            ContentType = "image/jpeg"
        };
    }
    
    private bool IsImage(IFormFile file)
    {
        string[] validExtensions = [".jpg", ".jpeg", ".png", ".bmp"];
        var ext = Path.GetExtension(file.FileName).ToLower();
        return validExtensions.Contains(ext);
    }
}