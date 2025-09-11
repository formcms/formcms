namespace FormCMS.Search.Services;

public interface ISearchService
{
    Task<Record[]> Search(string query, int offset, int limit);
}