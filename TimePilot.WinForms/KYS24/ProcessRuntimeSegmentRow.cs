using System.Globalization;

namespace TimePilot.WinForms.KYS24
{
    internal sealed record ProcessRuntimeSegmentRow(
        DateTimeOffset StartedAt,
        DateTimeOffset? EndedAt,
        long DurationMs,
        int ProcessId,
        bool HasMainWindow,
        bool IsCurrentSessionProcess)
    {
        public string StartedAtText => StartedAt.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture);

        public string EndedAtText => EndedAt?.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture)
            ?? UiText.Timeline.InProgress;

        public string DurationText => FormatDuration(DurationMs);

        public bool IsRunning => EndedAt is null;

        public string StatusText => EndedAt is null ? UiText.Main.Running : UiText.Main.Ended;

        public string ObservationTypeText
        {
            get
            {
                if (HasMainWindow)
                    return UiText.Main.WindowedApp;

                return IsCurrentSessionProcess ? UiText.Main.UserProcess : UiText.Main.AllProcesses;
            }
        }

        private static string FormatDuration(long durationMs)
        {
            var span = TimeSpan.FromMilliseconds(durationMs);
            return string.Format(
                CultureInfo.CurrentCulture,
                "{0:D2}:{1:D2}:{2:D2}",
                (int)span.TotalHours,
                span.Minutes,
                span.Seconds);
        }
    }
}
