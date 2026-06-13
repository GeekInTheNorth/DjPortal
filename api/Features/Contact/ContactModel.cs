using System.Text.Json.Serialization;

namespace DjPortalApi.Features.Contact;

public sealed class ContactModel
{
    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Subject { get; set; }

    public string? Message { get; set; }

    /// <summary>
    /// Honeypot field. Hidden from real users; only bots populate it.
    /// </summary>
    public string? Company { get; set; }

    /// <summary>
    /// Milliseconds between the form rendering and submission. Submissions faster
    /// than a human could realistically manage are treated as automated.
    /// </summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int ElapsedMs { get; set; }
}
