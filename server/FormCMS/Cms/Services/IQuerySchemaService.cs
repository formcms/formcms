using FormCMS.Core.Descriptors;
using GraphQLParser.AST;
using Schema = FormCMS.Core.Descriptors.Schema;

namespace FormCMS.Cms.Services;

public interface IQuerySchemaService
{
    Task<LoadedQuery> ByGraphQlRequest(Query query, GraphQLField[] fields);
    Task<LoadedQuery> ByNameAndCache(string name, PublicationStatus? status, CancellationToken token);
    Task Delete(Schema schema, CancellationToken token);
    Task SaveQuery(Query query,  PublicationStatus?status, CancellationToken ct = default);
    string GraphQlClientUrl();

}