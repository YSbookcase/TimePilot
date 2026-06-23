using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Details
{
    internal static class RuntimeSegmentHelpContentBuilder
    {
        public static string GetObservationFilterLabelText()
        {
            return UiText.CurrentLanguage == UiLanguage.English ? "Basis" : "관측 기준";
        }

        public static string GetResetTooltip()
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? "Return the selected app runtime chart to the full-day view."
                : "선택 앱 실행 구간 그래프를 하루 전체 보기로 되돌립니다.";
        }

        public static string GetHelpTitle()
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? "Selected App Runtime Chart Help"
                : "선택 앱 실행 구간 도움말";
        }

        public static string GetHelpMessage()
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? "Selected app runtime chart controls\n\n"
                    + "- Drag the chart: zoom into the selected time range.\n"
                    + "- Ctrl+wheel: zoom around the pointer.\n"
                    + "- Shift+wheel: pan left/right in the zoomed view.\n"
                    + "- Left / Right keys: pan left/right after clicking the chart.\n"
                    + "- Esc or Full: return to the full-day view.\n"
                    + "- Selecting a runtime segment row highlights that segment on the chart.\n\n"
                    + "Overlapping or dense segments may look close together. Zoom into the range when you need to inspect the exact position."
                : "선택 앱 실행 구간 그래프 조작\n\n"
                    + "- 그래프 드래그: 선택한 시간 범위로 확대합니다.\n"
                    + "- Ctrl+휠: 마우스 위치를 기준으로 확대/축소합니다.\n"
                    + "- Shift+휠: 확대한 보기에서 좌우로 이동합니다.\n"
                    + "- 왼쪽/오른쪽 방향키: 그래프를 클릭한 뒤 좌우로 이동합니다.\n"
                    + "- Esc 또는 전체: 하루 전체 보기로 되돌립니다.\n"
                    + "- 실행 구간 목록의 행을 선택하면 해당 구간이 그래프에서 강조됩니다.\n\n"
                    + "구간이 많이 겹치거나 촘촘한 앱은 한눈에 구분하기 어려울 수 있습니다. 정확한 위치를 보려면 해당 범위를 확대해서 확인하세요.";
        }
    }
}
