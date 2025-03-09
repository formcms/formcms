namespace FormCMS.Infrastructure.ImageUtil;

public record ResizeOptions(int MaxWidth, int Quality);
public interface IResizer
{
    IFormFile CompressImage(IFormFile inputFile);
}