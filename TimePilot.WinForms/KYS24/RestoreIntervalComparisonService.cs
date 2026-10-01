namespace TimePilot.WinForms.KYS24
{
    internal sealed record RestoreAnalysisInterval(
        DateTimeOffset StartedAt,
        DateTimeOffset EndedAt,
        string? MatchKey = null);

    internal sealed record RestoreIntervalComparisonResult(
        IReadOnlyList<RestoreAnalysisInterval> ImportableIntervals,
        IReadOnlyList<RestoreAnalysisInterval> ExcludedByOverlapIntervals)
    {
        public int ImportableCount => ImportableIntervals.Count;

        public int ExcludedByOverlapCount => ExcludedByOverlapIntervals.Count;
    }

    internal static class RestoreIntervalComparisonService
    {
        public static RestoreIntervalComparisonResult Compare(
            IReadOnlyList<RestoreAnalysisInterval> baselineIntervals,
            IReadOnlyList<RestoreAnalysisInterval> candidateIntervals)
        {
            var allBaseline = Normalize(baselineIntervals);
            var keylessBaseline = allBaseline
                .Where(interval => interval.MatchKey is null)
                .ToList();
            var keyedBaseline = allBaseline
                .Where(interval => interval.MatchKey is not null)
                .GroupBy(interval => interval.MatchKey!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList(),
                    StringComparer.OrdinalIgnoreCase);
            var candidates = Normalize(candidateIntervals);
            var importable = new List<RestoreAnalysisInterval>();
            var excluded = new List<RestoreAnalysisInterval>();
            var allBaselineIndex = 0;
            var keylessBaselineIndex = 0;
            var keyedBaselineIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var candidate in candidates)
            {
                var overlaps = candidate.MatchKey is { } matchKey
                    ? HasKeyedOverlap(
                        keyedBaseline,
                        keyedBaselineIndexes,
                        keylessBaseline,
                        ref keylessBaselineIndex,
                        matchKey,
                        candidate)
                    : HasOverlap(allBaseline, ref allBaselineIndex, candidate);

                if (overlaps)
                    excluded.Add(candidate);
                else
                    importable.Add(candidate);
            }

            return new RestoreIntervalComparisonResult(importable, excluded);
        }

        private static bool HasKeyedOverlap(
            IReadOnlyDictionary<string, List<RestoreAnalysisInterval>> keyedBaseline,
            Dictionary<string, int> keyedBaselineIndexes,
            IReadOnlyList<RestoreAnalysisInterval> keylessBaseline,
            ref int keylessBaselineIndex,
            string matchKey,
            RestoreAnalysisInterval candidate)
        {
            var hasOverlap = false;
            if (keyedBaseline.TryGetValue(matchKey, out var matchingBaseline))
            {
                keyedBaselineIndexes.TryGetValue(matchKey, out var matchingBaselineIndex);
                hasOverlap = HasOverlap(matchingBaseline, ref matchingBaselineIndex, candidate);
                keyedBaselineIndexes[matchKey] = matchingBaselineIndex;
            }

            return hasOverlap || HasOverlap(keylessBaseline, ref keylessBaselineIndex, candidate);
        }

        private static bool HasOverlap(
            IReadOnlyList<RestoreAnalysisInterval> baseline,
            ref int baselineStartIndex,
            RestoreAnalysisInterval candidate)
        {
            while (baselineStartIndex < baseline.Count
                && baseline[baselineStartIndex].EndedAt <= candidate.StartedAt)
            {
                baselineStartIndex++;
            }

            for (var index = baselineStartIndex; index < baseline.Count; index++)
            {
                var currentBaseline = baseline[index];
                if (currentBaseline.StartedAt >= candidate.EndedAt)
                    break;

                if (Overlaps(currentBaseline, candidate))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<RestoreAnalysisInterval> Normalize(IReadOnlyList<RestoreAnalysisInterval> intervals)
        {
            return intervals
                .Where(interval => interval.StartedAt < interval.EndedAt)
                .OrderBy(interval => interval.StartedAt)
                .ThenBy(interval => interval.EndedAt)
                .ToList();
        }

        private static bool Overlaps(RestoreAnalysisInterval baseline, RestoreAnalysisInterval candidate)
        {
            return candidate.StartedAt < baseline.EndedAt
                && candidate.EndedAt > baseline.StartedAt;
        }
    }
}
