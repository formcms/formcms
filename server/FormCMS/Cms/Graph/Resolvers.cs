using FormCMS.Cms.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.GraphTypeConverter;
using FormCMS.Utils.ResultExt;
using GraphQL;
using GraphQL.Resolvers;
using GraphQLParser.AST;

namespace FormCMS.Cms.Graph;

public record GraphQlRequestDto(Query Query, GraphQLField[] Fields, StrArgs Args);

public static class Resolvers
{
    public static readonly IFieldResolver ValueResolver = new FuncFieldResolver<object>(context => 
        context.Source is Record record ? record[context.FieldDefinition.Name] : null);

    public static IFieldResolver GetSingleResolver(IQueryService queryService, string entityName)
    {
        return new FuncFieldResolver<Record>(async context =>
        {
            var dto = GetRequestDto(context, entityName);
            return await ResultException.Try(()=>queryService.SingleWithAction(dto));
        });
    }

    public static IFieldResolver GetListResolver(IQueryService queryService, string entityName)
    {
        return new FuncFieldResolver<Record[]>(async context =>
        {
            var dto = GetRequestDto(context, entityName);
            return await ResultException.Try(() => queryService.ListWithAction(dto));
        });
    }

    private static GraphQlRequestDto GetRequestDto(IResolveFieldContext context, string entityName)
    {
        var queryName = context.ExecutionContext.Operation.Name is null
            ? ""
            : context.ExecutionContext.Operation.Name.StringValue;

        IArgument[] args = context.FieldAst.Arguments
            ?.Select(x => new GraphArgument(x))
            .ToArray<IArgument>() ?? [];
        var res = QueryHelper.ParseArguments(args);
        if (res.IsFailed)
        {
            throw new ResultException(string.Join(";", res.Errors.Select(x=>x.Message)));
        }
        var (sorts,filters,pagination,omitAssetDetails,distinct) = res.Value;
        
        var query = new Query(
            Name: queryName, 
            EntityName: entityName, context.Document.Source.ToString(), 
            IdeUrl: "",
            Pagination: pagination,
            Filters: [..filters], 
            Sorts: [..sorts], 
            ReqVariables: [..context.Variables.GetRequiredNames()],
            OmitAssetDetails: omitAssetDetails,
            Distinct: distinct
        );
        
        return new GraphQlRequestDto(query, 
            context.FieldAst.SelectionSet?.Selections.OfType<GraphQLField>().ToArray()??[],
            context.Variables.ToPairArray());
    }
}