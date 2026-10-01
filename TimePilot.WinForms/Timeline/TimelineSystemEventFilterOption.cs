using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Timeline
{
    internal sealed record TimelineSystemEventFilterOption(string Label, TimelineSystemEventFilter Value)
    {
        public override string ToString() => Label;

        public static IReadOnlyList<TimelineSystemEventFilterOption> GetOptions()
        {
            var isEnglish = UiText.CurrentLanguage == UiLanguage.English;
            return
            [
                new(isEnglish ? "All events" : "전체 이벤트", TimelineSystemEventFilter.All),
                new(isEnglish ? "Lock/logon" : "잠금/로그온", TimelineSystemEventFilter.Lock),
                new(isEnglish ? "Sleep/resume" : "절전/복귀", TimelineSystemEventFilter.Power),
                new(isEnglish ? "Shutdown" : "종료/재시작", TimelineSystemEventFilter.Shutdown),
                new(isEnglish ? "TimePilot" : "TimePilot", TimelineSystemEventFilter.TimePilot)
            ];
        }
    }
}
