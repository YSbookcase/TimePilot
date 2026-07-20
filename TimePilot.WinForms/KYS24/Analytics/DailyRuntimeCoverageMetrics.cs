namespace TimePilot.WinForms.KYS24.Analytics
{
    internal sealed record DailyRuntimeCoverageMetrics(
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

        public static DailyRuntimeCoverageMetrics FromSummary(RuntimeCoverageSummary summary)
        {
            ArgumentNullException.ThrowIfNull(summary);

            return new DailyRuntimeCoverageMetrics(
                summary.WindowsRuntimeMs,
                summary.RecordableRuntimeMs,
                summary.TrackedRuntimeMs,
                summary.MissingRuntimeMs,
                summary.LongestMissingRuntimeMs,
                summary.SleepExcludedMs,
                summary.LockExcludedMs,
                summary.BootBeforeTimePilotMs);
        }
    }
}
