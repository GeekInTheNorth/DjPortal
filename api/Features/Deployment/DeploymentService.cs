using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DjPortalApi.Features.Deployment;

public sealed class DeploymentService : IDeploymentService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DeploymentService> _logger;

    public DeploymentService(HttpClient httpClient, IConfiguration configuration, ILogger<DeploymentService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<DeploymentTriggerResult> TriggerRebuild()
    {
        // Read configuration
        var gitHubToken = _configuration.GetValue<string>("GitHubToken");
        var gitHubOwner = _configuration.GetValue<string>("GitHubOwner");
        var gitHubRepo = _configuration.GetValue<string>("GitHubRepo");
        var gitHubWorkflowFileName = _configuration.GetValue<string>("GitHubWorkflowFileName");

        // Validate configuration
        if (string.IsNullOrEmpty(gitHubToken) || string.IsNullOrEmpty(gitHubOwner) ||
            string.IsNullOrEmpty(gitHubRepo) || string.IsNullOrEmpty(gitHubWorkflowFileName))
        {
            _logger.LogError("GitHub configuration is missing. Please configure GitHubToken, GitHubOwner, GitHubRepo, and GitHubWorkflowFileName.");
            return new DeploymentTriggerResult
            {
                Success = false,
                Message = "GitHub configuration is not properly set up.",
                StatusCode = 500
            };
        }

        try
        {
            // Build GitHub API URL
            var url = $"https://api.github.com/repos/{gitHubOwner}/{gitHubRepo}/actions/workflows/{gitHubWorkflowFileName}/dispatches";

            // Create request
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            // Set headers
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", gitHubToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
            request.Headers.UserAgent.ParseAdd("DjPortal-Rebuild-Trigger");

            // Set body
            var body = new { @ref = "main" };
            var jsonContent = JsonSerializer.Serialize(body);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending workflow dispatch request to GitHub: {Url}", url);

            // Send request
            var response = await _httpClient.SendAsync(request);

            // Log response status
            _logger.LogInformation("GitHub API response status: {StatusCode}", response.StatusCode);

            // Handle response
            if (response.IsSuccessStatusCode)
            {
                return new DeploymentTriggerResult
                {
                    Success = true,
                    Message = "Rebuild triggered successfully.",
                    StatusCode = (int)response.StatusCode
                };
            }

            // Handle error responses
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("GitHub API request failed. Status: {StatusCode}, Response: {Response}",
                response.StatusCode, errorContent);

            var errorMessage = (int)response.StatusCode switch
            {
                401 => "GitHub authentication failed. Please check the GitHub token configuration.",
                404 => "Workflow not found. Please verify the repository and workflow file name.",
                422 => "Workflow does not support manual triggering. Please add 'workflow_dispatch' to the workflow file.",
                429 => "GitHub API rate limit exceeded. Please try again later.",
                _ => $"GitHub API request failed with status code {response.StatusCode}."
            };

            return new DeploymentTriggerResult
            {
                Success = false,
                Message = errorMessage,
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while triggering rebuild");
            return new DeploymentTriggerResult
            {
                Success = false,
                Message = "An unexpected error occurred while triggering the rebuild.",
                StatusCode = 500
            };
        }
    }
}
