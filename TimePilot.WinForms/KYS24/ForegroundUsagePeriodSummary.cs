namespace TimePilot.WinForms.KYS24
{
    internal sealed record ForegroundUsagePeriodSummary(
        IReadOnlyList<ForegroundUsageSummary> ForegroundUsage,
        IReadOnlyList<DailyUsageTrendRow> DailyUsageTrendRows);
}
