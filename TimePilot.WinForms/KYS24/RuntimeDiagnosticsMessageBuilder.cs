using System.Globalization;

namespace TimePilot.WinForms.KYS24
{
    internal static class RuntimeDiagnosticsMessageBuilder
    {
        public static string BuildMessage(
            IReadOnlyList<AppRuntimeSessionDiagnostic> sessions,
            IReadOnlyList<SystemEventDiagnostic> systemEvents)
        {
            if (sessions.Count == 0)
            {
                if (systemEvents.Count == 0)
                    return UiText.Main.RuntimeDiagnosticsNoHistory;

                var eventOnlyLines = new List<string>
                {
                    UiText.Main.RuntimeDiagnosticsNoHistory,
                    string.Empty
                };
                AddSystemEventDiagnostics(eventOnlyLines, systemEvents);
                return string.Join(Environment.NewLine, eventOnlyLines);
            }

            var lastSession = sessions[0];
            var unexpectedCount = sessions.Count(x => IsShutdownReason(x, "unexpected"));
            var lines = new List<string>
            {
                UiText.Main.RuntimeDiagnosticsLastRun,
                UiText.Main.RuntimeDiagnosticsStartedAt(FormatDateTime(lastSession.StartedAt)),
                UiText.Main.RuntimeDiagnosticsEndedAt(FormatDateTime(lastSession.EndedAt)),
                UiText.Main.RuntimeDiagnosticsDuration(FormatDuration(lastSession)),
                UiText.Main.RuntimeDiagnosticsShutdownReason(GetShutdownReasonText(lastSession.ShutdownReason)),
                UiText.Main.RuntimeDiagnosticsRecentUnexpectedCount(unexpectedCount, sessions.Count),
                string.Empty,
                UiText.Main.RuntimeDiagnosticsHistory
            };

            foreach (var session in sessions.Take(5))
            {
                lines.Add(UiText.Main.RuntimeDiagnosticsHistoryItem(
                    FormatDateTime(session.StartedAt),
                    FormatDateTime(session.EndedAt),
                    GetShutdownReasonText(session.ShutdownReason),
                    FormatDuration(session)));
            }

            AddSystemEventDiagnostics(lines, systemEvents);

            lines.AddRange(new[]
            {
                string.Empty,
                UiText.Main.RuntimeDiagnosticsNote
            });

            return string.Join(Environment.NewLine, lines);
        }

        public static string GetShutdownReasonText(string? reason)
        {
            return reason?.ToLowerInvariant() switch
            {
                "normal" => UiText.Main.ShutdownReasonNormal,
                "unexpected" => UiText.Main.ShutdownReasonUnexpected,
                "system-shutdown" => UiText.Main.ShutdownReasonSystemShutdown,
                "clear-data" => UiText.Main.ShutdownReasonClearData,
                "running" => UiText.Main.ShutdownReasonRunning,
                _ => UiText.Main.ShutdownReasonUnknown
            };
        }

        public static string GetSystemEventTypeText(string eventType)
        {
            return eventType.ToLowerInvariant() switch
            {
                "lock" => UiText.Main.SystemEventLock,
                "unlock" => UiText.Main.SystemEventUnlock,
                "logon" => UiText.Main.SystemEventLogon,
                "logoff" => UiText.Main.SystemEventLogoff,
                "suspend" => UiText.Main.SystemEventSuspend,
                "resume" => UiText.Main.SystemEventResume,
                "timepilot-start" => UiText.Main.SystemEventTimePilotStart,
                "timepilot-exit" => UiText.Main.SystemEventTimePilotExit,
                "system-shutdown" => UiText.Main.ShutdownReasonSystemShutdown,
                "windows-boot-estimate" => UiText.CurrentLanguage == UiLanguage.English ? "Windows startup estimate" : "Windows 시작 추정",
                "recording-end-estimate" => UiText.CurrentLanguage == UiLanguage.English ? "Recording end estimate" : "기록 종료 추정",
                _ => eventType
            };
        }

        public static string FormatDateTime(DateTimeOffset? timestamp)
        {
            return timestamp?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture)
                ?? "-";
        }

        public static string FormatDuration(AppRuntimeSessionDiagnostic session)
        {
            var durationMs = session.DurationMs;
            if (durationMs is null && session.EndedAt is { } endedAt)
                durationMs = Math.Max(0, (long)(endedAt - session.StartedAt).TotalMilliseconds);

            return durationMs is null ? "-" : FormatDuration(durationMs.Value);
        }

        public static string FormatDuration(long durationMs)
        {
            var duration = TimeSpan.FromMilliseconds(Math.Max(0, durationMs));
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";

            return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        }

        private static void AddSystemEventDiagnostics(
            List<string> lines,
            IReadOnlyList<SystemEventDiagnostic> systemEvents)
        {
            if (systemEvents.Count == 0)
                return;

            lines.Add(string.Empty);
            lines.Add(UiText.Main.RuntimeDiagnosticsSystemEvents);
            foreach (var systemEvent in systemEvents)
            {
                lines.Add(UiText.Main.RuntimeDiagnosticsSystemEventItem(
                    FormatDateTime(systemEvent.OccurredAt),
                    GetSystemEventTypeText(systemEvent.EventType),
                    systemEvent.Details ?? "-"));
            }
        }

        private static bool IsShutdownReason(AppRuntimeSessionDiagnostic session, string reason)
        {
            return string.Equals(session.ShutdownReason, reason, StringComparison.OrdinalIgnoreCase);
        }
    }
}
