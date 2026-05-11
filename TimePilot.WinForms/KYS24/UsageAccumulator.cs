namespace TimePilot.WinForms.KYS24
{
    internal sealed class UsageAccumulator
    {
        private readonly Dictionary<string, long> totalsMs = new(StringComparer.OrdinalIgnoreCase);

        public void AddSample(string? processName, int intervalMs, bool isIdle)
        {
            if (isIdle || string.IsNullOrEmpty(processName))
                return;
            totalsMs.TryGetValue(processName, out var ms);
            totalsMs[processName] = ms + intervalMs;
        }

        public IReadOnlyDictionary<string, long> SnapshotTotalsMs()
        {
            return new Dictionary<string, long>(totalsMs, StringComparer.OrdinalIgnoreCase);
        }
    }
}
