namespace TimePilot.WinForms.KYS24
{
    internal sealed class IdleSessionTracker
    {
        private readonly TimePilotStorage storage;
        private long? currentSessionId;

        public IdleSessionTracker(TimePilotStorage storage)
        {
            this.storage = storage;
        }

        public void Track(bool isIdle, AppMetadata? foregroundApp, int thresholdMs, DateTimeOffset observedAt)
        {
            if (isIdle)
            {
                if (currentSessionId is null)
                {
                    currentSessionId = storage.StartIdleSession(observedAt, thresholdMs, foregroundApp);
                }

                return;
            }

            EndCurrentSession(observedAt);
        }

        public void EndCurrentSession(DateTimeOffset endedAt)
        {
            if (currentSessionId is not { } sessionId)
                return;

            storage.EndIdleSession(sessionId, endedAt);
            currentSessionId = null;
        }
    }
}
