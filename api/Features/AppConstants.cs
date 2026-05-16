namespace DjPortalApi.Features;

public static class AppConstants
{
    public const string RequestedByCookieName = "cydjr.requestor";

    public const string TrackIndexName = "tracks";

    public const string EventIndexName = "events";

    public const string RequestsIndexName = "requests";

    public const string SynonymMapName = "ceroc-dj-synonyms";

    public const int MaxAnonymousRequestsPerEvent = 5;

    public const string MaxRequestsExceededMessage = "You may only make 5 requests per event, DJ Mark will select one to play";
}
