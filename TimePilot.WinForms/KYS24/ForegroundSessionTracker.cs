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

        public void Track(AppMetadata? app, bool isIdle, DateTimeOffset observedAt)
        {
            if (isIdle || app is null || string.IsNullOrWhiteSpace(app.ProcessName))
            {
                EndCurrentSession(observedAt);
                return;
            }

            if (string.Equals(currentProcessName, app.ProcessName, StringComparison.OrdinalIgnoreCase))
                return;

            EndCurrentSession(observedAt);

            currentProcessName = app.ProcessName;
            currentSessionId = storage.StartForegroundSession(app, observedAt);
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
