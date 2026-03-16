using FormCMS.Cms.Models;

namespace FormCMS.Video.Workers;

public interface IConversionStrategy
{
    bool CanHandle(ConvertVideoMessage message);
    Task ExecuteAsync(ConvertVideoMessage message, CancellationToken ct);
}
