namespace FormCMS.Cms.Services;

public interface IPageService
{
    Task<string> Get(string name, StrArgs args, CancellationToken token =default);
    Task<string> GetDetail(string name, string param, StrArgs args, CancellationToken ct = default);
    Task<string> GetPart(string partStr, CancellationToken token);
}