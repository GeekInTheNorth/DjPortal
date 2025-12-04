using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace DjPortalApi.Features;

/// <summary>
/// Represents the authenticated user from Azure Static Web Apps
/// </summary>
public class StaticWebAppPrincipal
{
    public string? UserId { get; set; }

    public string? UserDetails { get; set; }

    public string? IdentityProvider { get; set; }

    public List<string> UserRoles { get; set; } = new();

    public List<Claim> Claims { get; set; } = new();

    /// <summary>
    /// Checks if the user is authenticated
    /// </summary>
    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);

    /// <summary>
    /// Parses the x-ms-client-principal header from Azure Static Web Apps
    /// </summary>
    public static StaticWebAppPrincipal Parse(string? headerValue)
    {
        var principal = new StaticWebAppPrincipal();

        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return principal;
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(headerValue));
            var principalData = JsonSerializer.Deserialize<ClientPrincipalData>(decoded, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (principalData != null)
            {
                principal.UserId = principalData.UserId;
                principal.UserDetails = principalData.UserDetails;
                principal.IdentityProvider = principalData.IdentityProvider;
                principal.UserRoles = principalData.UserRoles ?? new List<string>();
                principal.Claims = principalData.Claims?.Select(c => new Claim(c.Type ?? "", c.Value ?? "")).ToList() ?? [];
            }
        }
        catch (Exception)
        {
            // If parsing fails, return empty principal (unauthenticated)
        }

        return principal;
    }

    private class ClientPrincipalData
    {
        public string? UserId { get; set; }
        public string? UserDetails { get; set; }
        public string? IdentityProvider { get; set; }
        public List<string>? UserRoles { get; set; }
        public List<ClientPrincipalClaim>? Claims { get; set; }
    }

    private class ClientPrincipalClaim
    {
        public string? Type { get; set; }
        public string? Value { get; set; }
    }
}
