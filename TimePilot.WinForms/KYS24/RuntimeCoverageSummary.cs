using System.Globalization;

namespace TimePilot.WinForms.KYS24
{
    internal sealed record RuntimeCoverageSummary(
        long WindowsRuntimeMs,
        long RecordableRuntimeMs,
        long TrackedRuntimeMs,
        long MissingRuntimeMs,
        long LongestMissingRuntimeMs,
        long SleepExcludedMs,
        long LockExcludedMs,
        long? BootBeforeTimePilotMs)
    {
        public double CoverageRatio => RecordableRuntimeMs <= 0
            ? 0
            : Math.Min(1, (double)TrackedRuntimeMs / RecordableRuntimeMs);

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
                    UiText.RuntimeCoverage.Coverage(CoverageRatio),
                    UiText.RuntimeCoverage.Recordable(FormatDuration(RecordableRuntimeMs)),
                    UiText.RuntimeCoverage.Tracked(FormatDuration(TrackedRuntimeMs)),
                    UiText.RuntimeCoverage.Missing(FormatDuration(MissingRuntimeMs)),
                    UiText.RuntimeCoverage.LongestMissing(FormatDuration(LongestMissingRuntimeMs))
                };

                if (SleepExcludedMs > 0)
                    parts.Add(UiText.RuntimeCoverage.SleepExcluded(FormatDuration(SleepExcludedMs)));

                if (LockExcludedMs > 0)
                    parts.Add(UiText.RuntimeCoverage.LockExcluded(FormatDuration(LockExcludedMs)));

                if (BootBeforeTimePilotMs is { } bootMs && bootMs > 0)
                    parts.Add(UiText.RuntimeCoverage.BootBeforeTimePilot(FormatDuration(bootMs)));

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
