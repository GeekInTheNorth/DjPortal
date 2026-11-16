using DjPortalApi.Features.Import.Models;

namespace DjPortalApi.Features.Import;

public interface ICerocClient
{
    /// <summary>
    /// Reads the Ceroc events listing for the configured postcode and radius and returns the Freestyle events.
    /// </summary>
    Task<IList<CerocEventListing>> GetFreestyleEventsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a Ceroc event page and returns its main text plus the start and end times.
    /// </summary>
    Task<CerocEventPage> GetEventPageAsync(Uri eventUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the URL is an https link to the Ceroc website, the only site this client will fetch.
    /// </summary>
    bool IsCerocUrl(Uri url);
}
