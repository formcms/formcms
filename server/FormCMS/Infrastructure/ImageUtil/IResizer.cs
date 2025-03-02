namespace FormCMS.Infrastructure.ImageUtil;

public interface IResizer
{
    void Compress(Stream inputStream, Stream outputStream );
    bool IsImage(IFormFile file);
}