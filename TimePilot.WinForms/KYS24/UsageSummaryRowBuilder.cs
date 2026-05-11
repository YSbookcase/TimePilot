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
    }
}
