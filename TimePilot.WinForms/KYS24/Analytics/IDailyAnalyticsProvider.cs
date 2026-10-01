namespace TimePilot.WinForms.KYS24.Analytics
{
    internal interface IDailyAnalyticsProvider
    {
        DailyAnalyticsSnapshot GetSnapshot(
            DailyAnalyticsRange range,
            DateTimeOffset observedAt);
    }
}
