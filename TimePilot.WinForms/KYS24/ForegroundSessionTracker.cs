namespace TimePilot.WinForms.KYS24
{
    internal sealed class ForegroundSessionTracker
    {
        private readonly TimePilotStorage storage;
        private string? currentProcessName;
        private long? currentSessionId;

        public ForegroundSessionTracker(TimePilotStorage storage)
        {
            this.storage = storage;
        }

        public void Track(string? processName, bool isIdle, DateTimeOffset observedAt)
        {
            if (isIdle || string.IsNullOrWhiteSpace(processName))
            {
                EndCurrentSession(observedAt);
                return;
            }

            if (string.Equals(currentProcessName, processName, StringComparison.OrdinalIgnoreCase))
                return;

            EndCurrentSession(observedAt);

            currentProcessName = processName;
            currentSessionId = storage.StartForegroundSession(processName, observedAt);
        }

        public void EndCurrentSession(DateTimeOffset endedAt)
        {
            if (currentSessionId is not { } sessionId)
                return;

            storage.EndForegroundSession(sessionId, endedAt);
            currentSessionId = null;
            currentProcessName = null;
        }
    }
}
