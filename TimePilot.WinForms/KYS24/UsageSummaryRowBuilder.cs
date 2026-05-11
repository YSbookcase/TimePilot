namespace TimePilot.WinForms.KYS24
{
    internal static class UsageSummaryRowBuilder
    {
        public static IReadOnlyList<UsageSummaryRow> FromTotals(IReadOnlyDictionary<string, long> totalsMs)
        {
            var totalMs = totalsMs.Values.Sum();
            if (totalMs <= 0)
                return [];

            return totalsMs
                .OrderByDescending(x => x.Value)
                .Select(x => new UsageSummaryRow(
                    x.Key,
                    x.Value,
                    (double)x.Value / totalMs))
                .ToList();
        }

        public static IReadOnlyList<UsageSummaryRow> FromForegroundUsage(IReadOnlyList<ForegroundUsageSummary> summaries)
        {
            var totalMs = summaries.Sum(x => x.ActiveUsageMs);
            if (totalMs <= 0)
                return [];

            return summaries
                .OrderByDescending(x => x.ActiveUsageMs)
                .Select(x => new UsageSummaryRow(
                    x.AppName,
                    x.ActiveUsageMs,
                    (double)x.ActiveUsageMs / totalMs,
                    x.FirstStartedAt,
                    x.LastObservedAt))
                .ToList();
        }
    }
}
