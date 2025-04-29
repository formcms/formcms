using FluentResults;

namespace FormCMS.Cms.Services;

public interface IPageService
{
    Task<string> Get(string name, StrArgs args, CancellationToken ct );
    Task<string> GetDetail(string name, string param, StrArgs args, CancellationToken ct );
    Task<string> GetPart(string partStr, CancellationToken ct);
    Task<long> GetPageId(string path, CancellationToken ct);
}