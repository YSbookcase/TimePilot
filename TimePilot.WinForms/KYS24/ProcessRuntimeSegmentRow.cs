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

        public string EndedAtText => EndedAt?.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture) ?? "진행 중";

        public string DurationText => FormatDuration(DurationMs);

        public bool IsRunning => EndedAt is null;

        public string StatusText => EndedAt is null ? "실행 중" : "종료";

        public string ObservationTypeText
        {
            get
            {
                if (HasMainWindow)
                    return "창 있음";

                return IsCurrentSessionProcess ? "사용자 프로세스" : "전체 프로세스";
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
