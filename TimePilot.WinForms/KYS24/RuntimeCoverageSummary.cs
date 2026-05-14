using System.Globalization;

namespace TimePilot.WinForms.KYS24
{
    internal sealed record RuntimeCoverageSummary(
        long TotalWindowMs,
        long TrackedRuntimeMs,
        long MissingRuntimeMs,
        long LongestMissingRuntimeMs,
        long? BootBeforeTimePilotMs)
    {
        public double CoverageRatio => TotalWindowMs <= 0
            ? 1
            : Math.Min(1, (double)TrackedRuntimeMs / TotalWindowMs);

        public string SummaryText
        {
            get
            {
                return string.Join(" · ", SummaryParts);
            }
        }

        public IReadOnlyList<string> SummaryParts
        {
            get
            {
                var parts = new List<string>
                {
                    string.Format(CultureInfo.CurrentCulture, "오늘 0시~현재 기록 커버리지 {0:P1}", CoverageRatio),
                    $"기록 {FormatDuration(TrackedRuntimeMs)}",
                    $"미기록 {FormatDuration(MissingRuntimeMs)}(원인 미확정)",
                    $"최장 미기록 {FormatDuration(LongestMissingRuntimeMs)}"
                };

                if (BootBeforeTimePilotMs is { } bootMs && bootMs > 0)
                    parts.Add($"부팅 후 미실행 {FormatDuration(bootMs)}");

                return parts;
            }
        }

        private static string FormatDuration(long durationMs)
        {
            var span = TimeSpan.FromMilliseconds(Math.Max(0, durationMs));
            return string.Format(
                CultureInfo.CurrentCulture,
                "{0:D2}:{1:D2}:{2:D2}",
                (int)span.TotalHours,
                span.Minutes,
                span.Seconds);
        }
    }
}
