namespace DjPortalApi.Features.Events;

public class EventDetailTag(string name, string colour)
{
    public string Name { get; } = name;

    public string Colour { get; } = colour;
}