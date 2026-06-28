using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Timeline
{
    internal static class TimelineSystemEventFilterMatcher
    {
        public static bool Matches(string eventType, TimelineSystemEventFilter filter)
        {
            var normalized = eventType.ToLowerInvariant();
            return filter switch
            {
                TimelineSystemEventFilter.Lock => normalized is "lock" or "unlock" or "logon" or "logoff",
                TimelineSystemEventFilter.Power => normalized is "suspend" or "resume" or "power-status-change" or "power-mode",
                TimelineSystemEventFilter.Shutdown => normalized is "system-shutdown" or "recording-end-estimate",
                TimelineSystemEventFilter.TimePilot => normalized is "timepilot-start" or "timepilot-exit" or "windows-boot-estimate" or "recording-end-estimate",
                _ => true
            };
        }
    }
}
