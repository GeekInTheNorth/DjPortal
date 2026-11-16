using System.Globalization;
using System.Text.RegularExpressions;
using DjPortalApi.Features.Import.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace DjPortalApi.Features.Import;

public sealed partial class CerocClient(
    IHttpClientFactory httpClientFactory,
    ILogger<CerocClient> logger) : ICerocClient
{
    public const string HttpClientName = "Ceroc";

    private const string CerocHost = "www.ceroc.com";

    private const int MaxPageTextLength = 6000;

    private static readonly Uri BaseUri = new($"https://{CerocHost}/");

    private static readonly string[] DateFormats = ["dd/MM/yyyy", "d/M/yyyy"];

    private static readonly string[] TimeFormats = ["h:mmtt", "hh:mmtt", "H:mm", "HH:mm"];

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"Event start:\s*(?<date>\d{1,2}/\d{1,2}/\d{4})(?:\s+(?<time>\d{1,2}:\d{2}\s*[ap]m))?", RegexOptions.IgnoreCase)]
    private static partial Regex EventStart();

    [GeneratedRegex(@"Event end:\s*(?<date>\d{1,2}/\d{1,2}/\d{4})(?:\s+(?<time>\d{1,2}:\d{2}\s*[ap]m))?", RegexOptions.IgnoreCase)]
    private static partial Regex EventEnd();

    public bool IsCerocUrl(Uri url)
    {
        return url.IsAbsoluteUri
            && url.Scheme == Uri.UriSchemeHttps
            && (url.Host.Equals(CerocHost, StringComparison.OrdinalIgnoreCase)
                || url.Host.Equals("ceroc.com", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IList<CerocEventListing>> GetFreestyleEventsAsync(CancellationToken cancellationToken = default)
    {
        var listingUrl = new Uri(BaseUri, $"/27/41/events?filter=&radius=25&postcode=Leeds#search_events");
        var html = await GetHtmlAsync(listingUrl, cancellationToken);

        var document = new HtmlDocument();
        document.LoadHtml(html);

        var table = document.DocumentNode.SelectSingleNode("//table[contains(concat(' ', normalize-space(@class), ' '), ' event_list ')]")
            ?? throw new InvalidOperationException("The Ceroc events listing table was not found. The page layout may have changed.");

        var listings = new List<CerocEventListing>();
        foreach (var row in table.SelectNodes(".//tr[td]") ?? Enumerable.Empty<HtmlNode>())
        {
            var cells = row.SelectNodes("./td");
            if (cells is null || cells.Count < 6)
            {
                continue;
            }

            if (!CleanText(cells[1].InnerText).Equals("Freestyle", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var link = cells[0].SelectSingleNode(".//a[@href]");
            var href = link is null ? null : HtmlEntity.DeEntitize(link.GetAttributeValue("href", string.Empty));
            if (string.IsNullOrWhiteSpace(href) || !Uri.TryCreate(BaseUri, href, out var eventUrl) || !IsCerocUrl(eventUrl))
            {
                logger.LogWarning("Skipped a Ceroc listing row with an unusable link: {Href}", href);
                continue;
            }

            // The date cell reads like "22/11/2026 Sun".
            var dateText = CleanText(cells[5].InnerText).Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!DateOnly.TryParseExact(dateText, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                logger.LogWarning("Skipped a Ceroc listing row with an unreadable date: {DateText}", dateText);
                continue;
            }

            listings.Add(new CerocEventListing
            {
                Name = CleanText(link!.InnerText),
                Location = CleanText(cells[2].InnerText),
                Postcode = CleanText(cells[3].InnerText).ToUpperInvariant(),
                Date = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Url = eventUrl.ToString()
            });
        }

        return listings;
    }

    public async Task<CerocEventPage> GetEventPageAsync(Uri eventUrl, CancellationToken cancellationToken = default)
    {
        if (!IsCerocUrl(eventUrl))
        {
            throw new ArgumentException("Only Ceroc event pages can be read.", nameof(eventUrl));
        }

        var html = await GetHtmlAsync(eventUrl, cancellationToken);

        var document = new HtmlDocument();
        document.LoadHtml(html);

        var main = document.DocumentNode.SelectSingleNode("//main")
            ?? document.DocumentNode.SelectSingleNode("//body")
            ?? document.DocumentNode;

        foreach (var node in main.SelectNodes(".//script|.//style|.//noscript")?.ToList() ?? [])
        {
            node.Remove();
        }

        // One line per text node, so text either side of a <br> or between headings never runs together.
        var lines = main.DescendantsAndSelf()
            .Where(node => node.NodeType == HtmlNodeType.Text)
            .Select(node => CleanText(node.InnerText))
            .Where(line => line.Length > 0);

        var text = string.Join('\n', lines);
        if (text.Length > MaxPageTextLength)
        {
            text = text[..MaxPageTextLength];
        }

        var start = EventStart().Match(text);
        var end = EventEnd().Match(text);

        return new CerocEventPage
        {
            Text = text,
            StartDate = start.Success ? ParseDate(start.Groups["date"].Value) : null,
            StartTime = start.Success ? ParseTime(start.Groups["time"].Value) : null,
            EndTime = end.Success ? ParseTime(end.Groups["time"].Value) : null
        };
    }

    private async Task<string> GetHtmlAsync(Uri url, CancellationToken cancellationToken)
    {
        // The client doesn't follow redirects, so ask for the canonical host rather than be bounced to it.
        if (!url.Host.Equals(CerocHost, StringComparison.OrdinalIgnoreCase))
        {
            url = new UriBuilder(url) { Host = CerocHost }.Uri;
        }

        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static string CleanText(string? text)
    {
        return string.IsNullOrEmpty(text)
            ? string.Empty
            : Whitespace().Replace(HtmlEntity.DeEntitize(text), " ").Trim();
    }

    private static DateOnly? ParseDate(string value)
    {
        return DateOnly.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    private static TimeOnly? ParseTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // "3:00pm" and "3:00 PM" both become "3:00PM".
        var normalised = Whitespace().Replace(value, string.Empty).ToUpperInvariant();
        return TimeOnly.TryParseExact(normalised, TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            ? time
            : null;
    }
}
