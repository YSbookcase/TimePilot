using System.Globalization;

namespace TimePilot.WinForms.KYS24
{
    internal sealed record UsageSummaryRow(
        string AppName,
        long ActiveUsageMs,
        double UsageRatio)
    {
        public string ActiveUsageTimeText => FormatDuration(ActiveUsageMs);

        public string UsageRatioText => UsageRatio.ToString("P1", CultureInfo.CurrentCulture);

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
