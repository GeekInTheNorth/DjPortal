namespace DjPortalApi.Features.Requests;

public enum CreateRequestOutcome
{
    Created,
    QuotaExceeded
}

public sealed class CreateResult
{
    public CreateRequestOutcome Outcome { get; private init; }

    public MusicRequest? Request { get; private init; }

    public string? Message { get; private init; }

    public static CreateResult Created(MusicRequest request) => new()
    {
        Outcome = CreateRequestOutcome.Created,
        Request = request
    };

    public static CreateResult QuotaExceeded(string message) => new()
    {
        Outcome = CreateRequestOutcome.QuotaExceeded,
        Message = message
    };
}
