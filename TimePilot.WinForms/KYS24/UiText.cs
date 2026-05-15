namespace TimePilot.WinForms.KYS24
{
    internal static class UiText
    {
        public const string AppName = "TimePilot";

        public static class Common
        {
            public const string Yes = "예";
            public const string No = "아니오";
            public const string Ok = "확인";
            public const string Save = "저장";
            public const string Cancel = "취소";
        }

        public static class Main
        {
            public const string FileMenu = "파일";
            public const string ExportCsv = "CSV 내보내기...";
            public const string Exit = "종료";
            public const string SettingsMenu = "설정";
            public const string Preferences = "환경 설정...";
            public const string HelpMenu = "도움말";
            public const string About = "정보";
            public const string SummaryTab = "요약";
            public const string DetailTab = "상세";
            public const string TimelineTab = "타임라인";
            public const string Period = "기간";
            public const string Date = "날짜";
            public const string Calendar = "달력";
            public const string Today = "오늘";
            public const string NotChecked = "확인 전";
            public const string CurrentTrackingScopeOnly = "현재 추적 범위만";
            public const string RunningOnly = "실행 중만";
            public const string App = "앱";
            public const string Start = "시작";
            public const string End = "종료";
            public const string Duration = "시간";
            public const string Status = "상태";
            public const string Type = "유형";
            public const string Pid = "PID";
            public const string FirstStartedAt = "첫 시작";
            public const string FirstObservedAt = "첫 감지";
            public const string LastObservedAt = "마지막 감지";
            public const string ActiveUsageTime = "활성 사용 시간";
            public const string ActiveRatio = "활성 비중";
            public const string SwitchCount = "전환 횟수";
            public const string TotalActiveUsageTime = "총 활성 사용 시간";
            public const string TopApp = "가장 많이 사용한 앱";
            public const string TopAppTime = "주요 앱 시간";
            public const string Runtime = "실행 시간";
            public const string ActualUsageRatio = "실사용 비율";
            public const string RuntimeSegmentCount = "실행 구간";
            public const string ObservationBasis = "관측 기준";
            public const string HasData = "기록 있음";
            public const string NoData = "기록 없음";
            public const string Active = "활성";
            public const string Idle = "유휴";
            public const string Untracked = "미실행";
            public const string TimePilotUntracked = "TimePilot 미실행";
            public const string Running = "실행 중";
            public const string Ended = "종료";
            public const string OutsideTrackingScope = "추적 범위 밖";
            public const string NoForegroundApp = "(없음)";
            public const string PerformancePrefix = "성능: ";
            public const string UsageRatioTooltip = "선택 기간 전체 활성 사용 시간 중 이 앱이 차지한 비율입니다.";
            public const string RuntimeLastObservedTooltip = "백그라운드 앱 추적이 실제로 마지막 관측한 시각입니다.";
            public const string RuntimeDurationTooltip = "실행 중인 앱은 현재 시각 기준으로 계속 증가하고, 종료된 앱은 마지막 관측 시각까지 계산합니다.";
            public const string RuntimeActualUsageRatioTooltip = "실행 시간 중 실제 foreground 활성 사용 시간이 차지한 비율입니다.";
            public const string RuntimeSegmentCountTooltip = "앱이 이어서 실행된 것으로 관측된 구간 수입니다.";
            public const string RuntimeStatusTooltip = "현재 설정 기준으로 실행 중, 종료, 추적 범위 밖 상태를 표시합니다.";
            public const string RecordedDateCalendarTooltip = "기록이 있는 날짜는 달력에서 굵게 표시됩니다.";
            public const string OpenWindow = "창 열기";
            public const string UsageDataCleared = "사용 기록을 삭제했습니다.";
            public const string StartupPromptTitle = "TimePilot 자동 시작";
            public const string StartupPromptMessage = "Windows 시작 시 TimePilot을 자동으로 실행할까요?\n\n나중에 환경 설정에서 언제든 변경할 수 있습니다.";
            public const string SafeModeTitle = "TimePilot 안전모드";
            public const string SafeModeMessage = "이전 실행에서 위험한 백그라운드 앱 추적 설정으로 짧은 시간 안에 비정상 종료가 반복된 것으로 보입니다.\n\n안전모드로 백그라운드 앱 추적만 자동으로 껐습니다. 현재 사용 중인 앱 기록과 유휴 감지는 계속 동작합니다.\n\n다시 사용하려면 설정 > 환경 설정에서 백그라운드 앱 추적을 켜주세요.";
            public const string SafeModeBalloonMessage = "반복 비정상 종료를 피하기 위해 백그라운드 앱 추적을 자동으로 껐습니다.";
        }

        public static class Preferences
        {
            public const string Title = "환경 설정";
            public const string IdleThresholdLabel = "유휴 판단 대기시간";
            public const string MinuteUnit = "분";
            public const string SecondUnit = "초";
            public const string StartWithWindows = "Windows 시작 시 자동 실행";
            public const string PerformanceDiagnostics = "성능 진단 표시";
            public const string ProcessRuntimeGroup = "백그라운드 앱 추적";
            public const string ProcessRuntimeTracking = "실행 중 앱 세션 추적";
            public const string ProcessRuntimeScope = "추적 범위";
            public const string ProcessRuntimeInterval = "확인 주기";
            public const string ProcessRuntimeWarning = "짧은 확인 주기는 CPU 사용량, 배터리 소모, 저장 데이터 증가를 유발할 수 있습니다.";
            public const string ProcessRuntimeDangerWarning = "위험 설정입니다. 반복 비정상 종료가 감지되면 다음 실행에서 백그라운드 앱 추적이 자동으로 꺼질 수 있습니다.";
            public const string DataManagementGroup = "데이터 관리";
            public const string DataManagementDescription = "기록과 설정 저장 위치를 관리합니다.";
            public const string OpenDataFolder = "폴더 열기";
            public const string ClearUsageData = "기록 삭제";
            public const string ClearUsageDataPending = "삭제 예정";
            public const string DataFolderOpenTitle = "데이터 폴더 열기";
            public const string ClearUsageDataTitle = "사용 기록 삭제";
            public const string ClearUsageDataMessage = "저장을 누르면 앱 사용 기록과 타임라인 기록이 삭제됩니다.\n\n환경 설정과 Windows 시작 시 자동 실행 설정은 유지됩니다.";
            public const string AdvancedTrackingTitle = "고급 추적 설정";
            public const string WindowedAppsScope = "창이 있는 앱만";
            public const string UserProcessesScope = "모든 사용자 프로세스";
            public const string AllProcessesScope = "모든 프로세스";
            public const string Custom = "사용자 지정";
            public const string AllProcessesRiskMessage = "모든 프로세스를 10초 이하 주기로 추적하면 환경에 따라 TimePilot이 멈추거나 비정상 종료될 수 있습니다.\n\n같은 설정으로 짧은 실행 후 비정상 종료가 반복되면 다음 실행에서 백그라운드 앱 추적이 자동으로 꺼집니다.\n\n이 위험 설정을 저장하시겠습니까?";
            public const string UserProcessesRiskMessage = "모든 사용자 프로세스를 5초 이하 주기로 추적하면 CPU 사용량과 저장 데이터가 크게 증가할 수 있습니다.\n\n같은 설정으로 짧은 실행 후 비정상 종료가 반복되면 다음 실행에서 백그라운드 앱 추적이 자동으로 꺼집니다.\n\n이 위험 설정을 저장하시겠습니까?";
            public const string AnyScopeRiskMessage = "3초 이하 확인 주기는 추적 범위와 관계없이 시스템 부하와 저장 데이터 증가를 유발할 수 있습니다.\n\n같은 설정으로 짧은 실행 후 비정상 종료가 반복되면 다음 실행에서 백그라운드 앱 추적이 자동으로 꺼집니다.\n\n이 위험 설정을 저장하시겠습니까?";

            public static string DataFolderOpenFailed(string message)
            {
                return $"데이터 폴더를 열 수 없습니다.\n\n{message}";
            }
        }

        public static class DateStatus
        {
            public const string HasData = "기록 있음";
            public const string NoData = "기록 없음";
            public const string NotChecked = "확인 전";
        }

        public static class SummaryPeriod
        {
            public const string SpecificDate = "특정 날짜";

            public static string Today(string date)
            {
                return $"오늘 ({date})";
            }

            public static string Yesterday(string date)
            {
                return $"어제 ({date})";
            }

            public static string ThisWeek(string weekStart)
            {
                return $"이번 주({weekStart}~오늘)";
            }

            public static string LastWeek(string weekStart, string weekEnd)
            {
                return $"지난 주({weekStart}~{weekEnd})";
            }

            public static string ThisMonth()
            {
                return "이번 달(1일~오늘)";
            }

            public static string LastMonth(string monthStart, string monthEnd)
            {
                return $"지난 달({monthStart}~{monthEnd})";
            }

            public static string ThisYear(int year)
            {
                return $"올해({year}.01~오늘)";
            }

            public static string LastYear(int year)
            {
                return $"작년({year})";
            }
        }

        public static class Timeline
        {
            public const string InProgress = "진행 중";
        }

        public static class RuntimeCoverage
        {
            public static string Coverage(double ratio)
            {
                return string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    "오늘 0시~현재 기록 커버리지 {0:P1}",
                    ratio);
            }

            public static string Tracked(string duration)
            {
                return $"기록 {duration}";
            }

            public static string Missing(string duration)
            {
                return $"미기록 {duration}(원인 미확정)";
            }

            public static string LongestMissing(string duration)
            {
                return $"최장 미기록 {duration}";
            }

            public static string BootBeforeTimePilot(string duration)
            {
                return $"부팅 후 미실행 {duration}";
            }
        }
    }
}
