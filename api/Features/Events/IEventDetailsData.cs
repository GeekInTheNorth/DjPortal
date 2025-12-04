namespace DjPortalApi.Features.Events;

public interface IEventDetailsData
{
    string? Name { get; set; }

    string? Description { get; set; }

    string? LocationName { get; set; }

    string? LocationAddress { get; set; }

    DateTime Date { get; set; }

    string? Times { get; set; }

    string? FacebookEventId { get; set; }

    string? Tags { get; set; }

    bool IsRequestable { get; set; }

    bool GenerateSchemaData { get; set; }

    bool IsCancelled { get; set; }
}