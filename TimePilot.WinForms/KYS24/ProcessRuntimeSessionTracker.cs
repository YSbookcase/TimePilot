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

        public void Track(IReadOnlyList<RunningProcessSnapshot> processes, DateTimeOffset observedAt)
        {
            var observedProcessIds = new HashSet<int>();

            foreach (var process in processes)
            {
                if (string.IsNullOrWhiteSpace(process.App.ProcessName))
                    continue;

                observedProcessIds.Add(process.ProcessId);

                if (currentSessions.TryGetValue(process.ProcessId, out var session)
                    && string.Equals(session.ProcessName, process.App.ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    storage.UpdateProcessRuntimeSession(session.SessionId, process.App, observedAt);
                    continue;
                }

                if (session is not null)
                {
                    storage.EndProcessRuntimeSession(session.SessionId, observedAt);
                }

                var sessionId = storage.StartProcessRuntimeSession(process.App, process.ProcessId, observedAt);
                currentSessions[process.ProcessId] = new TrackedProcessSession(sessionId, process.App.ProcessName);
            }

            var endedProcessIds = currentSessions.Keys
                .Where(processId => !observedProcessIds.Contains(processId))
                .ToList();

            foreach (var processId in endedProcessIds)
            {
                storage.EndProcessRuntimeSession(currentSessions[processId].SessionId, observedAt);
                currentSessions.Remove(processId);
            }
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
