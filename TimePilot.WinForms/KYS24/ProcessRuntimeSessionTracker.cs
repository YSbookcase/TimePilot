namespace TimePilot.WinForms.KYS24
{
    internal sealed class ProcessRuntimeSessionTracker
    {
        private readonly TimePilotStorage storage;
        private readonly Dictionary<int, TrackedProcessSession> currentSessions = new();

        public ProcessRuntimeSessionTracker(TimePilotStorage storage)
        {
            this.storage = storage;
        }

        public bool Track(
            IReadOnlyList<RunningProcessSnapshot> processes,
            ProcessRuntimeTrackingScope trackingScope,
            DateTimeOffset observedAt)
        {
            var observedProcessIds = new HashSet<int>();
            var starts = new List<ProcessRuntimeSessionStart>();
            var updates = new List<ProcessRuntimeSessionUpdate>();
            var endedSessionIds = new List<long>();

            foreach (var process in processes)
            {
                if (string.IsNullOrWhiteSpace(process.App.ProcessName))
                    continue;

                observedProcessIds.Add(process.ProcessId);

                if (currentSessions.TryGetValue(process.ProcessId, out var session)
                    && string.Equals(session.ProcessName, process.App.ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    updates.Add(new ProcessRuntimeSessionUpdate(
                        session.SessionId,
                        process.App,
                        trackingScope,
                        process.HasMainWindow,
                        process.IsCurrentSessionProcess));
                    continue;
                }

                if (session is not null)
                {
                    endedSessionIds.Add(session.SessionId);
                }

                starts.Add(new ProcessRuntimeSessionStart(
                    process.ProcessId,
                    process.App,
                    trackingScope,
                    process.HasMainWindow,
                    process.IsCurrentSessionProcess));
            }

            var endedProcessIds = currentSessions.Keys
                .Where(processId => !observedProcessIds.Contains(processId))
                .ToList();

            foreach (var processId in endedProcessIds)
            {
                endedSessionIds.Add(currentSessions[processId].SessionId);
            }

            var startResults = storage.ApplyProcessRuntimeSessionChanges(
                starts,
                updates,
                endedSessionIds,
                observedAt);

            foreach (var processId in endedProcessIds)
            {
                currentSessions.Remove(processId);
            }

            foreach (var startResult in startResults)
            {
                currentSessions[startResult.ProcessId] = new TrackedProcessSession(
                    startResult.SessionId,
                    startResult.ProcessName);
            }

            return starts.Count > 0 || endedSessionIds.Count > 0;
        }

        public void EndCurrentSessions(DateTimeOffset endedAt)
        {
            foreach (var session in currentSessions.Values)
            {
                storage.EndProcessRuntimeSession(session.SessionId, endedAt);
            }

            currentSessions.Clear();
        }

        private sealed record TrackedProcessSession(long SessionId, string ProcessName);
    }
}
