using FormCMS.Core.Descriptors;
using GraphQLParser.AST;
using Schema = FormCMS.Core.Descriptors.Schema;

namespace FormCMS.Cms.Services;

public interface IQuerySchemaService
{
    Task<LoadedQuery> ByGraphQlRequest(Query query, GraphQLField[] fields);
    Task<LoadedQuery> ByNameAndCache(string name, PublicationStatus? status, CancellationToken ct);
    Task Delete(Schema schema, CancellationToken ct);
    Task SaveQuery(Query query,  PublicationStatus?status, CancellationToken ct);
    string GraphQlClientUrl();

}