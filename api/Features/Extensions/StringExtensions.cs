namespace DjPortalApi.Features.Extensions;

public static class StringExtensions
{
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