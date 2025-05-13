using FormCMS.Activities.Services;

namespace FormCMS.Cms.Services;

public class DummyTopItemService:ITopItemService
{
    public Task<Record[]> GetTopItems(string entityName, int offset, int limit, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}