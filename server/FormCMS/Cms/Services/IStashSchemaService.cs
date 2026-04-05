
namespace FormCMS.Cms.Services;

public interface IStashSchemaService
{
    Task<string[]> GetQueries(CancellationToken ct ); 
}
