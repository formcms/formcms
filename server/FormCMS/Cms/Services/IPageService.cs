namespace FormCMS.Cms.Services;

public interface IPageService
{
    Task<string> Get(string name, StrArgs args, CancellationToken ct );
    Task<string> GetDetail(string name, string param, StrArgs args, CancellationToken ct );
    Task<string> GetPart(long? sourceId,string partStr, bool replace, CancellationToken ct);
}