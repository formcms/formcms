namespace FormCMS.Infrastructure.ImageUtil;

public interface IResizer
{
    IFormFile CompressImage(IFormFile inputFile);
}