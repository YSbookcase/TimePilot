using System.Globalization;

namespace TimePilot.WinForms.KYS24
{
    internal sealed record DailyUsageTrendRow(
        DateTime Date,
        long ActiveUsageMs,
        string TopAppName,
        long TopAppUsageMs)
    {
        public string DateText => Date.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture);

        public string ActiveUsageTimeText => FormatDuration(ActiveUsageMs);

        public string TopAppUsageTimeText => TopAppUsageMs > 0 ? FormatDuration(TopAppUsageMs) : "";

        private static string FormatDuration(long durationMs)
        {
            var span = TimeSpan.FromMilliseconds(durationMs);
            return string.Format(
                CultureInfo.CurrentCulture,
                "{0:D2}:{1:D2}:{2:D2}",
                (int)span.TotalHours,
                span.Minutes,
                span.Seconds);
        }
    }
}
