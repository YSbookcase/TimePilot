namespace TimePilot.WinForms.KYS24
{
    internal static class SystemTimelineEventTextFormatter
    {
        public static string GetListTitle(DateTime date)
        {
            var dateText = date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.CurrentCulture);
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"System event list ({dateText})"
                : $"시스템 이벤트 목록 ({dateText})";
        }

        public static string GetListDescription(DateTime date)
        {
            var dateText = date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.CurrentCulture);
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"Selected date: {dateText}. Event intervals are hints for interpretation, not confirmed causes of missing records."
                : $"선택 날짜: {dateText}. 이벤트 간격은 해석 보조 정보이며, 미기록 원인을 확정하지 않습니다.";
        }

        public static string GetTimeHeaderText()
        {
            return UiText.CurrentLanguage == UiLanguage.English ? "Time" : "시각";
        }

        public static string GetPreviousIntervalHeaderText()
        {
            return UiText.CurrentLanguage == UiLanguage.English ? "Since previous" : "직전 간격";
        }

        public static string GetRelationHeaderText()
        {
            return UiText.CurrentLanguage == UiLanguage.English ? "Hint" : "해석 단서";
        }

        public static string GetDetailsHeaderText()
        {
            return UiText.CurrentLanguage == UiLanguage.English ? "Details" : "상세";
        }

        public static string GetRelationText(string eventType)
        {
            var isEnglish = UiText.CurrentLanguage == UiLanguage.English;
            return eventType.ToLowerInvariant() switch
            {
                "lock" => isEnglish ? "Lock range start" : "잠금 구간 시작",
                "unlock" or "logon" => isEnglish ? "Lock range end candidate" : "잠금 구간 종료 후보",
                "logoff" => isEnglish ? "Logoff event" : "로그오프 이벤트",
                "suspend" => isEnglish ? "Sleep range start" : "절전 구간 시작",
                "resume" => isEnglish ? "Sleep range end candidate" : "절전 구간 종료 후보",
                "system-shutdown" => isEnglish ? "Shutdown/restart event" : "종료/다시 시작 이벤트",
                "timepilot-start" => isEnglish ? "TimePilot recording start" : "TimePilot 기록 시작",
                "timepilot-exit" => isEnglish ? "TimePilot recording end" : "TimePilot 기록 종료",
                "windows-boot-estimate" => isEnglish ? "Windows startup estimate" : "Windows 시작 추정",
                "recording-end-estimate" => isEnglish ? "Recording end estimate" : "기록 종료 추정",
                _ => "-"
            };
        }

        public static string FormatDetails(SystemTimelineEvent systemEvent)
        {
            if (string.IsNullOrWhiteSpace(systemEvent.Details))
                return systemEvent.IsInferred ? GetInferredDetailsText(systemEvent.EventType) : "-";

            if (systemEvent.Details.StartsWith("TimePilotStartedAt:", StringComparison.OrdinalIgnoreCase)
                && DateTimeOffset.TryParse(systemEvent.Details["TimePilotStartedAt:".Length..], out var startedAt))
            {
                return UiText.CurrentLanguage == UiLanguage.English
                    ? $"TimePilot started at {startedAt.ToLocalTime():HH:mm:ss}"
                    : $"TimePilot 시작 {startedAt.ToLocalTime():HH:mm:ss}";
            }

            if (systemEvent.Details.StartsWith("Reason:", StringComparison.OrdinalIgnoreCase))
            {
                var reason = systemEvent.Details["Reason:".Length..];
                return UiText.CurrentLanguage == UiLanguage.English
                    ? $"Reason: {RuntimeDiagnosticsMessageBuilder.GetShutdownReasonText(reason)}"
                    : $"사유: {RuntimeDiagnosticsMessageBuilder.GetShutdownReasonText(reason)}";
            }

            return systemEvent.Details;
        }

        public static string GetInferredDetailsText(string eventType)
        {
            var isEnglish = UiText.CurrentLanguage == UiLanguage.English;
            return eventType.ToLowerInvariant() switch
            {
                "windows-boot-estimate" => isEnglish
                    ? "Estimated from Windows system startup time."
                    : "Windows 시스템 시작 시간을 기준으로 추정했습니다.",
                "recording-end-estimate" => isEnglish
                    ? "Estimated from the last TimePilot runtime record."
                    : "TimePilot의 마지막 실행 기록을 기준으로 추정했습니다.",
                _ => "-"
            };
        }
    }
}
