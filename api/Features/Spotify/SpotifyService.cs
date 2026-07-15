using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace DjPortalApi.Features.Spotify;

public partial class SpotifyService : ISpotifyService
{
    private readonly HttpClient _httpClient;

    private readonly SpotifySettings _spotifySettings;

    private string? _accessToken;

    private DateTime _tokenExpiration;

    [GeneratedRegex(@"open\.spotify\.com/track/([a-zA-Z0-9]+)")]
    private static partial Regex SpotifyRegex();

    public SpotifyService(HttpClient httpClient, IConfiguration _configuration)
    {
        _httpClient = httpClient;
        _spotifySettings = new SpotifySettings
        {
            ClientId = _configuration.GetValue<string>("SpotifyClientId"),
            ClientSecret = _configuration.GetValue<string>("SpotifyClientSecret")
        };
    }

    private async Task Authenticate()
    {
        if (!_spotifySettings.IsConfigured || !string.IsNullOrEmpty(_accessToken) && _tokenExpiration > DateTime.UtcNow)
        {
            return;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
        var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_spotifySettings.ClientId}:{_spotifySettings.ClientSecret}"));
        
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);

        // Use FormUrlEncodedContent for the body
        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _accessToken = null;
            _tokenExpiration = DateTime.MinValue;
            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<SpotifyTokenResponse>(responseBody);

        if (tokenResponse != null)
        {
            _accessToken = tokenResponse.AccessToken;
            _tokenExpiration = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        }
    }

    public async Task<SpotifyTrack?> GetTrack(string trackId)
    {
        if (!_spotifySettings.IsConfigured || string.IsNullOrWhiteSpace(trackId))
        {
            return null;
        }

        await Authenticate();
        if (string.IsNullOrEmpty(_accessToken))
        {
            return null;
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.spotify.com/v1/tracks/{trackId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var responseStream = await response.Content.ReadAsStreamAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        return await JsonSerializer.DeserializeAsync<SpotifyTrack>(responseStream, options);
    }

    public bool TryGetSpotifyId(string? url, [NotNullWhen(true)] out string? id)
    {
        id = null;
        if (string.IsNullOrWhiteSpace(url)) return false;
        var match = SpotifyRegex().Match(url);
        if (match.Success)
        {
            id = match.Groups[1].Value;
            return true;
        }
        return false;
    }

    private class SpotifyTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
