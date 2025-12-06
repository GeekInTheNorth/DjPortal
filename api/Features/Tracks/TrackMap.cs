using CsvHelper.Configuration;

namespace DjPortalApi.Features.Tracks;
    
public class TrackMap : CsvClassMap<Track>
{
    public TrackMap()
    {
        Map(m => m.Id).Ignore();
        Map(m => m.Title).Name(nameof(Track.Title));
        Map(m => m.Artist).Name(nameof(Track.Artist));
        Map(m => m.Album).Name(nameof(Track.Album));
        Map(m => m.Time).Name(nameof(Track.Time));
        Map(m => m.BPM).Name(nameof(Track.BPM));
        Map(m => m.Key).Name(nameof(Track.Key));
        Map(m => m.Summary).Ignore();
    }
}