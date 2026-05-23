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
                    null,
                    x.Key,
                    x.Key,
                    null,
                    null,
                    null,
                    x.Value,
                    0,
                    (double)x.Value / totalMs,
                    0))
                .ToList();
        }

        public static IReadOnlyList<UsageSummaryRow> FromForegroundUsage(
            IReadOnlyList<ForegroundUsageSummary> summaries,
            bool showDateInTimestamps = false)
        {
            var totalMs = summaries.Sum(x => x.ActiveUsageMs);
            if (totalMs <= 0)
                return [];

            return summaries
                .OrderByDescending(x => x.ActiveUsageMs)
                .Select(x => new UsageSummaryRow(
                    x.AppId,
                    x.AppName,
                    x.ProcessName,
                    x.ExecutablePath,
                    x.PrimaryCategoryId,
                    x.CategoryName,
                    x.ActiveUsageMs,
                    x.IdleRecordedMs,
                    (double)x.ActiveUsageMs / totalMs,
                    x.SwitchCount,
                    null,
                    x.FirstStartedAt,
                    x.LastObservedAt,
                    showDateInTimestamps))
                .ToList();
        }
    }
}
