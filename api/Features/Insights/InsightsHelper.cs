namespace DjPortalApi.Features.Insights;

public static class InsightsHelper
{
    public static IEnumerable<TrackSearchTerm> Deduplicate(IEnumerable<TrackSearchTerm> searchTerms)
    {
        var sortedTerms = searchTerms
            .Where(term => !string.IsNullOrWhiteSpace(term.Query))
            .OrderByDescending(term => term.Query?.Length ?? 0)
            .ThenByDescending(term => term.UniqueCount)
            .ToList();

        var deduplicatedTerms = new List<TrackSearchTerm>();
        var seenTerms = new List<string>();

        foreach (var term in sortedTerms)
        {
            if (string.IsNullOrWhiteSpace(term.Query) || seenTerms.Contains(term.Query!))
            {
                continue;
            }

            var duplicates = searchTerms.Where(x => term.Query!.StartsWith(x.Query!));
            deduplicatedTerms.Add(new TrackSearchTerm
            {
                Query = term.Query,
                Environment = term.Environment,
                UniqueCount = duplicates.Sum(x => x.UniqueCount),
                Variants = duplicates.Select(x => x.Query!).Distinct().ToList()
            });

            seenTerms.AddRange(duplicates.Select(x => x.Query!));
        }

        return deduplicatedTerms.OrderByDescending(x => x.UniqueCount).ThenBy(x => x.Query);
    }
}
