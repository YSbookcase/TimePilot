using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Timeline
{
    internal static class TimelineHighlightMenuText
    {
        public static string ForSegment(UiLanguage language)
        {
            return language == UiLanguage.English
                ? "Highlight this segment"
                : "이 구간 강조";
        }

        public static string ForApp(UiLanguage language)
        {
            return language == UiLanguage.English
                ? "Highlight this app"
                : "이 앱 강조";
        }
    }
}
