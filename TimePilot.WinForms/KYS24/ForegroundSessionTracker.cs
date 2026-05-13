namespace TimePilot.WinForms.KYS24
{
    internal sealed class ForegroundSessionTracker
    {
        private readonly TimePilotStorage storage;
        private string? currentProcessName;
        private string? currentDisplayName;
        private string? currentExecutablePath;
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
            {
                if (currentSessionId is { } sessionId)
                {
                    storage.UpdateForegroundSessionObservation(sessionId, app, observedAt);
                }

                if (HasMetadataChanged(app))
                {
                    currentDisplayName = app.DisplayName;
                    currentExecutablePath = app.ExecutablePath;
                }

                return;
            }

            EndCurrentSession(observedAt);

            currentProcessName = app.ProcessName;
            currentDisplayName = app.DisplayName;
            currentExecutablePath = app.ExecutablePath;
            currentSessionId = storage.StartForegroundSession(app, observedAt);
        }

        public void EndCurrentSession(DateTimeOffset endedAt)
        {
            if (currentSessionId is not { } sessionId)
                return;

            storage.EndForegroundSession(sessionId, endedAt);
            currentSessionId = null;
            currentProcessName = null;
            currentDisplayName = null;
            currentExecutablePath = null;
        }

        private bool HasMetadataChanged(AppMetadata app)
        {
            return !string.Equals(currentDisplayName, app.DisplayName, StringComparison.Ordinal)
                || !string.Equals(currentExecutablePath, app.ExecutablePath, StringComparison.OrdinalIgnoreCase);
        }
    }
}
