namespace DjPortalApi.Features.Deployment;

public interface IDeploymentService
{
    Task<DeploymentTriggerResult> TriggerRebuild();
}

public class DeploymentTriggerResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? StatusCode { get; set; }
}
