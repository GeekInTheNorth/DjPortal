namespace DjPortalApi.Features.Insights;

public interface IInsightsService
{
    Task<IList<TrackSearchTerm>> GetSearchTerms(int numberOfDays = 7);
}
