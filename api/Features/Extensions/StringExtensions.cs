using System.Diagnostics.CodeAnalysis;

namespace DjPortalApi.Features.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Treats the value as a link only when the whole trimmed string is an absolute http(s) URL, so
    /// free text that merely mentions a URL is left alone. The scheme check matters: the URL is rendered
    /// as an href, and allowing javascript: or data: here would hand us a stored XSS.
    /// </summary>
    public static bool TryGetLink(this string? value, [NotNullWhen(true)] out string? url, [NotNullWhen(true)] out string? domain)
    {
        url = null;
        domain = null;

        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed) || !Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        url = trimmed;
        domain = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : uri.Host;
        return true;
    }

    public static string Obfuscate(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return $"{value.Trim()[0]}******";
    }

    public static IList<string> SplitByComma(this string? value)
    {
        return value?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)?.ToList() ?? new List<string>(0);
    }
}