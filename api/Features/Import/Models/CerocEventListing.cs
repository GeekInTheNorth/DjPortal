namespace DjPortalApi.Features.Import.Models;

/// <summary>
/// A single row from the Ceroc events listing.
/// </summary>
public sealed class CerocEventListing
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The town, as shown in the listing.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    public string Postcode { get; set; } = string.Empty;

    /// <summary>
    /// The event date in yyyy-MM-dd format.
    /// </summary>
    public string Date { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;
}
