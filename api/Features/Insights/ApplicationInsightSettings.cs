namespace DjPortalApi.Features.Insights;

public sealed class ApplicationInsightSettings
{
    public string ConnectionString { get; set; } = string.Empty;

    public string AppId { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}