namespace DjPortalApi.Features.WebSearch;

public interface IWebSearchService
{
    Task<IList<WebSearchResult>> Search(string query, int maxResults = 5);
}
