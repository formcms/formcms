using FormCMS.Core.HookFactory;
using FormCMS.Search.Models;
using FormCMS.Search.Services;

namespace FormCMS.Search.Builders;

public  static class HookRegistryExtensions
{
    public static void RegisterFtsHooks(this HookRegistry hookRegistry)
    {
      
        hookRegistry.ListPlugInQueryArgs.RegisterDynamic(SearchConstants.SearchQueryName, 
            async (ListPlugInQueryArgs args, ISearchService service ) =>
            {
                if (args.Args.TryGetValue(SearchConstants.Query, out var query))
                {
                    var limit = int.Parse(args.Pagination.Limit ?? "10");
                    var offset = 0;
                    if (!string.IsNullOrEmpty(args.Span.First))
                    {
                        offset = int.Parse(args.Span.First) - limit;
                    }else if (!string.IsNullOrEmpty(args.Span.Last))
                    {
                        offset = int.Parse(args.Span.Last) + 1 ;
                    }
                    var records =await service.Search(query.ToString(), offset, limit);
                    args = args with { OutRecords = records };
                }
                return args;
            });
            
    } 
}