namespace TimePilot.WinForms.KYS24.Analytics
{
    internal sealed record DailyAnalyticsSnapshot(
        DailyAnalyticsRange Range,
        IReadOnlyList<DailyAnalyticsDay> Days);
}
