using DjPortalApi.Features.Import.Models;

namespace DjPortalApi.Features.Import;

public interface IEventImportService
{
    /// <summary>
    /// The Freestyle events currently listed on Ceroc for the configured area.
    /// </summary>
    Task<IList<CerocEventListing>> GetListingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a Ceroc event page and asks the AI for a summary and tidied details.
    /// Returns null when the AI isn't configured or doesn't produce a usable answer.
    /// </summary>
    Task<EventDraft?> CreateDraftAsync(Uri eventUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks the AI for a fresh description from the details entered in the Create Event form.
    /// Returns null when the AI isn't configured or doesn't produce a usable answer.
    /// </summary>
    Task<string?> WriteDescriptionAsync(DescribeEventModel details, CancellationToken cancellationToken = default);
}
