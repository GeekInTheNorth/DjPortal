using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace DjPortalApi.Features.Common;

public static class AzureOpenAiChatClientFactory
{
    /// <summary>
    /// Builds a chat client from the AzureOpenAiEndpoint, AzureOpenAiApiKey and AzureOpenAiDeployment settings.
    /// Returns null when any of them is missing, so callers can fall back gracefully.
    /// </summary>
    public static ChatClient? Create(IConfiguration configuration)
    {
        var endpoint = configuration.GetValue<string>("AzureOpenAiEndpoint");
        var apiKey = configuration.GetValue<string>("AzureOpenAiApiKey");
        var deployment = configuration.GetValue<string>("AzureOpenAiDeployment");

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(deployment)
            || !Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
        {
            return null;
        }

        // Accept either the base resource endpoint or a Foundry project endpoint
        // (…/api/projects/…) — AzureOpenAIClient needs just scheme + host.
        var baseUri = new Uri(endpointUri, "/");
        var client = new AzureOpenAIClient(baseUri, new AzureKeyCredential(apiKey));
        return client.GetChatClient(deployment);
    }
}
