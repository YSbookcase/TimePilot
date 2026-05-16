using System.Globalization;

namespace TimePilot.WinForms.KYS24
{
    internal static class UiText
    {
        private static readonly UiTextResources Korean = UiTextResources.CreateKorean();
        private static readonly UiTextResources English = UiTextResources.CreateEnglish();
        private static UiTextResources current = Korean;

        public static UiLanguage CurrentLanguage { get; private set; } = UiLanguage.Korean;

        public static string AppName => "TimePilot";

        public static void UseLanguage(UiLanguage language)
        {
            CurrentLanguage = language;
            current = language == UiLanguage.English ? English : Korean;
            var culture = CultureInfo.GetCultureInfo(language == UiLanguage.English ? "en-US" : "ko-KR");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        public static class Common
        {
            public static string Yes => current.Common.Yes;
            public static string No => current.Common.No;
            public static string Ok => current.Common.Ok;
            public static string Save => current.Common.Save;
            public static string Cancel => current.Common.Cancel;
            public static string Apply => current.Common.Apply;
        }

        public static class Main
        {
            public static string FileMenu => current.Main.FileMenu;
            public static string ExportCsv => current.Main.ExportCsv;
            public static string Exit => current.Main.Exit;
            public static string SettingsMenu => current.Main.SettingsMenu;
            public static string Preferences => current.Main.Preferences;
            public static string HelpMenu => current.Main.HelpMenu;
            public static string About => current.Main.About;
            public static string SummaryTab => current.Main.SummaryTab;
            public static string DetailTab => current.Main.DetailTab;
            public static string TimelineTab => current.Main.TimelineTab;
            public static string Period => current.Main.Period;
            public static string Date => current.Main.Date;
            public static string Calendar => current.Main.Calendar;
            public static string Today => current.Main.Today;
            public static string NotChecked => current.Main.NotChecked;
            public static string CurrentTrackingScopeOnly => current.Main.CurrentTrackingScopeOnly;
            public static string RunningOnly => current.Main.RunningOnly;
            public static string App => current.Main.App;
            public static string Start => current.Main.Start;
            public static string End => current.Main.End;
            public static string Duration => current.Main.Duration;
            public static string Status => current.Main.Status;
            public static string Type => current.Main.Type;
            public static string Pid => current.Main.Pid;
            public static string FirstStartedAt => current.Main.FirstStartedAt;
            public static string FirstObservedAt => current.Main.FirstObservedAt;
            public static string LastObservedAt => current.Main.LastObservedAt;
            public static string ActiveUsageTime => current.Main.ActiveUsageTime;
            public static string ActiveRatio => current.Main.ActiveRatio;
            public static string SwitchCount => current.Main.SwitchCount;
            public static string TotalActiveUsageTime => current.Main.TotalActiveUsageTime;
            public static string TopApp => current.Main.TopApp;
            public static string TopAppTime => current.Main.TopAppTime;
            public static string Runtime => current.Main.Runtime;
            public static string ActualUsageRatio => current.Main.ActualUsageRatio;
            public static string RuntimeSegmentCount => current.Main.RuntimeSegmentCount;
            public static string ObservationBasis => current.Main.ObservationBasis;
            public static string WindowedApp => current.Main.WindowedApp;
            public static string UserProcess => current.Main.UserProcess;
            public static string AllProcesses => current.Main.AllProcesses;
            public static string HasData => current.Main.HasData;
            public static string NoData => current.Main.NoData;
            public static string Active => current.Main.Active;
            public static string Idle => current.Main.Idle;
            public static string Untracked => current.Main.Untracked;
            public static string TimePilotUntracked => current.Main.TimePilotUntracked;
            public static string Running => current.Main.Running;
            public static string Ended => current.Main.Ended;
            public static string OutsideTrackingScope => current.Main.OutsideTrackingScope;
            public static string NoForegroundApp => current.Main.NoForegroundApp;
            public static string ForegroundPrefix => current.Main.ForegroundPrefix;
            public static string PerformancePrefix => current.Main.PerformancePrefix;
            public static string CsvFilter => current.Main.CsvFilter;
            public static string CsvExportTitle => current.Main.CsvExportTitle;
            public static string AboutTitle => current.Main.AboutTitle;
            public static string UsageRatioTooltip => current.Main.UsageRatioTooltip;
            public static string RuntimeLastObservedTooltip => current.Main.RuntimeLastObservedTooltip;
            public static string RuntimeDurationTooltip => current.Main.RuntimeDurationTooltip;
            public static string RuntimeActualUsageRatioTooltip => current.Main.RuntimeActualUsageRatioTooltip;
            public static string RuntimeSegmentCountTooltip => current.Main.RuntimeSegmentCountTooltip;
            public static string RuntimeStatusTooltip => current.Main.RuntimeStatusTooltip;
            public static string RecordedDateCalendarTooltip => current.Main.RecordedDateCalendarTooltip;
            public static string OpenWindow => current.Main.OpenWindow;
            public static string UsageDataCleared => current.Main.UsageDataCleared;
            public static string StartupPromptTitle => current.Main.StartupPromptTitle;
            public static string StartupPromptMessage => current.Main.StartupPromptMessage;
            public static string SafeModeTitle => current.Main.SafeModeTitle;
            public static string SafeModeMessage => current.Main.SafeModeMessage;
            public static string SafeModeBalloonMessage => current.Main.SafeModeBalloonMessage;
            public static string DuplicateInstanceMessage => current.Main.DuplicateInstanceMessage;
            public static string CsvExportCompleted(int count, string? directory) =>
                current.Main.CsvExportCompleted(count, directory);
            public static string CsvExportFailed(string message) => current.Main.CsvExportFailed(message);
        }

        public static class Csv
        {
            public static string Date => current.Csv.Date;
            public static string AppName => current.Csv.AppName;
            public static string ProcessName => current.Csv.ProcessName;
            public static string ActiveUsageTime => current.Csv.ActiveUsageTime;
            public static string OverallRatio => current.Csv.OverallRatio;
            public static string SwitchCount => current.Csv.SwitchCount;
            public static string FirstStartedAt => current.Csv.FirstStartedAt;
            public static string LastObservedAt => current.Csv.LastObservedAt;
            public static string StartedAt => current.Csv.StartedAt;
            public static string EndedAt => current.Csv.EndedAt;
            public static string StartTime => current.Csv.StartTime;
            public static string EndTime => current.Csv.EndTime;
            public static string Duration => current.Csv.Duration;
            public static string Status => current.Csv.Status;
            public static string Runtime => current.Csv.Runtime;
            public static string Running => current.Csv.Running;
            public static string Ended => current.Csv.Ended;
        }

        public static class Preferences
        {
            public static string Title => current.Preferences.Title;
            public static string LanguageLabel => current.Preferences.LanguageLabel;
            public static string IdleThresholdLabel => current.Preferences.IdleThresholdLabel;
            public static string MinuteUnit => current.Preferences.MinuteUnit;
            public static string SecondUnit => current.Preferences.SecondUnit;
            public static string Minutes(int minutes) => current.Preferences.Minutes(minutes);
            public static string Seconds(int seconds) => current.Preferences.Seconds(seconds);
            public static string StartWithWindows => current.Preferences.StartWithWindows;
            public static string PerformanceDiagnostics => current.Preferences.PerformanceDiagnostics;
            public static string ProcessRuntimeGroup => current.Preferences.ProcessRuntimeGroup;
            public static string ProcessRuntimeTracking => current.Preferences.ProcessRuntimeTracking;
            public static string ProcessRuntimeScope => current.Preferences.ProcessRuntimeScope;
            public static string ProcessRuntimeInterval => current.Preferences.ProcessRuntimeInterval;
            public static string ProcessRuntimeWarning => current.Preferences.ProcessRuntimeWarning;
            public static string ProcessRuntimeDangerWarning => current.Preferences.ProcessRuntimeDangerWarning;
            public static string DataManagementGroup => current.Preferences.DataManagementGroup;
            public static string DataManagementDescription => current.Preferences.DataManagementDescription;
            public static string OpenDataFolder => current.Preferences.OpenDataFolder;
            public static string ClearUsageData => current.Preferences.ClearUsageData;
            public static string ClearUsageDataPending => current.Preferences.ClearUsageDataPending;
            public static string DataFolderOpenTitle => current.Preferences.DataFolderOpenTitle;
            public static string ClearUsageDataTitle => current.Preferences.ClearUsageDataTitle;
            public static string ClearUsageDataMessage => current.Preferences.ClearUsageDataMessage;
            public static string AdvancedTrackingTitle => current.Preferences.AdvancedTrackingTitle;
            public static string WindowedAppsScope => current.Preferences.WindowedAppsScope;
            public static string UserProcessesScope => current.Preferences.UserProcessesScope;
            public static string AllProcessesScope => current.Preferences.AllProcessesScope;
            public static string Custom => current.Preferences.Custom;
            public static string AllProcessesRiskMessage => current.Preferences.AllProcessesRiskMessage;
            public static string UserProcessesRiskMessage => current.Preferences.UserProcessesRiskMessage;
            public static string AnyScopeRiskMessage => current.Preferences.AnyScopeRiskMessage;

            public static string DataFolderOpenFailed(string message)
            {
                return current.Preferences.DataFolderOpenFailed(message);
            }
        }

        public static class DateStatus
        {
            public static string HasData => current.DateStatus.HasData;
            public static string NoData => current.DateStatus.NoData;
            public static string NotChecked => current.DateStatus.NotChecked;
        }

        public static class CalendarPicker
        {
            public static string MonthTitle(DateTime month) => current.CalendarPicker.MonthTitle(month);
            public static string MonthName(int month) => current.CalendarPicker.MonthName(month);
            public static IReadOnlyList<string> DayNames => current.CalendarPicker.DayNames;
        }

        public static class SummaryPeriod
        {
            public static string SpecificDate => current.SummaryPeriod.SpecificDate;
            public static string Today(string date) => current.SummaryPeriod.Today(date);
            public static string Yesterday(string date) => current.SummaryPeriod.Yesterday(date);
            public static string ThisWeek(string weekStart) => current.SummaryPeriod.ThisWeek(weekStart);
            public static string LastWeek(string weekStart, string weekEnd) =>
                current.SummaryPeriod.LastWeek(weekStart, weekEnd);
            public static string ThisMonth() => current.SummaryPeriod.ThisMonth();
            public static string LastMonth(string monthStart, string monthEnd) =>
                current.SummaryPeriod.LastMonth(monthStart, monthEnd);
            public static string ThisYear(int year) => current.SummaryPeriod.ThisYear(year);
            public static string LastYear(int year) => current.SummaryPeriod.LastYear(year);
        }

        public static class Timeline
        {
            public static string InProgress => current.Timeline.InProgress;
        }

        public static class RuntimeCoverage
        {
            public static string NotChecked => current.RuntimeCoverage.NotChecked;
            public static string Tooltip => current.RuntimeCoverage.Tooltip;
            public static string Coverage(double ratio) => current.RuntimeCoverage.Coverage(ratio);
            public static string Tracked(string duration) => current.RuntimeCoverage.Tracked(duration);
            public static string Missing(string duration) => current.RuntimeCoverage.Missing(duration);
            public static string LongestMissing(string duration) => current.RuntimeCoverage.LongestMissing(duration);
            public static string BootBeforeTimePilot(string duration) =>
                current.RuntimeCoverage.BootBeforeTimePilot(duration);
        }

        private sealed record UiTextResources(
            CommonText Common,
            MainText Main,
            CsvText Csv,
            PreferencesText Preferences,
            DateStatusText DateStatus,
            CalendarPickerText CalendarPicker,
            SummaryPeriodText SummaryPeriod,
            TimelineText Timeline,
            RuntimeCoverageText RuntimeCoverage)
        {
            public static UiTextResources CreateKorean()
            {
                return new UiTextResources(
                    new CommonText("예", "아니오", "확인", "저장", "취소", "적용"),
                    new MainText(
                        FileMenu: "파일",
                        ExportCsv: "CSV 내보내기...",
                        Exit: "종료",
                        SettingsMenu: "설정",
                        Preferences: "환경 설정...",
                        HelpMenu: "도움말",
                        About: "정보",
                        SummaryTab: "요약",
                        DetailTab: "상세",
                        TimelineTab: "타임라인",
                        Period: "기간",
                        Date: "날짜",
                        Calendar: "달력",
                        Today: "오늘",
                        NotChecked: "확인 전",
                        CurrentTrackingScopeOnly: "현재 추적 범위만",
                        RunningOnly: "실행 중만",
                        App: "앱",
                        Start: "시작",
                        End: "종료",
                        Duration: "시간",
                        Status: "상태",
                        Type: "유형",
                        Pid: "PID",
                        FirstStartedAt: "첫 시작",
                        FirstObservedAt: "첫 감지",
                        LastObservedAt: "마지막 감지",
                        ActiveUsageTime: "활성 사용 시간",
                        ActiveRatio: "활성 비중",
                        SwitchCount: "전환 횟수",
                        TotalActiveUsageTime: "총 활성 사용 시간",
                        TopApp: "가장 많이 사용한 앱",
                        TopAppTime: "주요 앱 시간",
                        Runtime: "실행 시간",
                        ActualUsageRatio: "실사용 비율",
                        RuntimeSegmentCount: "실행 구간",
                        ObservationBasis: "관측 기준",
                        WindowedApp: "창 있음",
                        UserProcess: "사용자 프로세스",
                        AllProcesses: "전체 프로세스",
                        HasData: "기록 있음",
                        NoData: "기록 없음",
                        Active: "활성",
                        Idle: "유휴",
                        Untracked: "미실행",
                        TimePilotUntracked: "TimePilot 미실행",
                        Running: "실행 중",
                        Ended: "종료",
                        OutsideTrackingScope: "추적 범위 밖",
                        NoForegroundApp: "(없음)",
                        ForegroundPrefix: "전경: ",
                        PerformancePrefix: "성능: ",
                        CsvFilter: "CSV 파일 (*.csv)|*.csv",
                        CsvExportTitle: "CSV 내보내기",
                        AboutTitle: "TimePilot 정보",
                        UsageRatioTooltip: "선택 기간 전체 활성 사용 시간 중 이 앱이 차지한 비율입니다.",
                        RuntimeLastObservedTooltip: "백그라운드 앱 추적이 실제로 마지막 관측한 시각입니다.",
                        RuntimeDurationTooltip: "실행 중인 앱은 현재 시각 기준으로 계속 증가하고, 종료된 앱은 마지막 관측 시각까지 계산합니다.",
                        RuntimeActualUsageRatioTooltip: "실행 시간 중 실제 foreground 활성 사용 시간이 차지한 비율입니다.",
                        RuntimeSegmentCountTooltip: "앱이 이어서 실행된 것으로 관측된 구간 수입니다.",
                        RuntimeStatusTooltip: "현재 설정 기준으로 실행 중, 종료, 추적 범위 밖 상태를 표시합니다.",
                        RecordedDateCalendarTooltip: "기록이 있는 날짜는 달력에서 굵게 표시됩니다.",
                        OpenWindow: "창 열기",
                        UsageDataCleared: "사용 기록을 삭제했습니다.",
                        StartupPromptTitle: "TimePilot 자동 시작",
                        StartupPromptMessage: "Windows 시작 시 TimePilot을 자동으로 실행할까요?\n\n나중에 환경 설정에서 언제든 변경할 수 있습니다.",
                        SafeModeTitle: "TimePilot 안전모드",
                        SafeModeMessage: "이전 실행에서 위험한 백그라운드 앱 추적 설정으로 짧은 시간 안에 비정상 종료가 반복된 것으로 보입니다.\n\n안전모드로 백그라운드 앱 추적만 자동으로 껐습니다. 현재 사용 중인 앱 기록과 유휴 감지는 계속 동작합니다.\n\n다시 사용하려면 설정 > 환경 설정에서 백그라운드 앱 추적을 켜주세요.",
                        SafeModeBalloonMessage: "반복 비정상 종료를 피하기 위해 백그라운드 앱 추적을 자동으로 껐습니다.",
                        DuplicateInstanceMessage: "TimePilot이 이미 실행 중입니다. 트레이 아이콘을 확인해 주세요.",
                        CsvExportCompleted: (count, directory) => $"CSV 파일 {count}개를 내보냈습니다.\n\n{directory}",
                        CsvExportFailed: message => $"CSV 내보내기에 실패했습니다.\n\n{message}"),
                    new CsvText(
                        Date: "날짜",
                        AppName: "앱 이름",
                        ProcessName: "프로세스 이름",
                        ActiveUsageTime: "활성 사용 시간",
                        OverallRatio: "전체 대비 비율",
                        SwitchCount: "전환 횟수",
                        FirstStartedAt: "첫 시작",
                        LastObservedAt: "마지막 감지",
                        StartedAt: "시작 일시",
                        EndedAt: "종료 일시",
                        StartTime: "시작 시간",
                        EndTime: "종료 시간",
                        Duration: "지속 시간",
                        Status: "상태",
                        Runtime: "실행 시간",
                        Running: "실행 중",
                        Ended: "종료"),
                    new PreferencesText(
                        Title: "환경 설정",
                        LanguageLabel: "표시 언어",
                        IdleThresholdLabel: "유휴 판단 대기시간",
                        MinuteUnit: "분",
                        SecondUnit: "초",
                        Minutes: minutes => $"{minutes}분",
                        Seconds: seconds => $"{seconds}초",
                        StartWithWindows: "Windows 시작 시 자동 실행",
                        PerformanceDiagnostics: "성능 진단 표시",
                        ProcessRuntimeGroup: "백그라운드 앱 추적",
                        ProcessRuntimeTracking: "실행 중 앱 세션 추적",
                        ProcessRuntimeScope: "추적 범위",
                        ProcessRuntimeInterval: "확인 주기",
                        ProcessRuntimeWarning: "짧은 확인 주기는 CPU 사용량, 배터리 소모, 저장 데이터 증가를 유발할 수 있습니다.",
                        ProcessRuntimeDangerWarning: "위험 설정입니다. 반복 비정상 종료가 감지되면 다음 실행에서 백그라운드 앱 추적이 자동으로 꺼질 수 있습니다.",
                        DataManagementGroup: "데이터 관리",
                        DataManagementDescription: "기록과 설정 저장 위치를 관리합니다.",
                        OpenDataFolder: "폴더 열기",
                        ClearUsageData: "기록 삭제",
                        ClearUsageDataPending: "삭제 예정",
                        DataFolderOpenTitle: "데이터 폴더 열기",
                        ClearUsageDataTitle: "사용 기록 삭제",
                        ClearUsageDataMessage: "저장을 누르면 앱 사용 기록과 타임라인 기록이 삭제됩니다.\n\n환경 설정과 Windows 시작 시 자동 실행 설정은 유지됩니다.",
                        AdvancedTrackingTitle: "고급 추적 설정",
                        WindowedAppsScope: "창이 있는 앱만",
                        UserProcessesScope: "모든 사용자 프로세스",
                        AllProcessesScope: "모든 프로세스",
                        Custom: "사용자 지정",
                        AllProcessesRiskMessage: "모든 프로세스를 10초 이하 주기로 추적하면 환경에 따라 TimePilot이 멈추거나 비정상 종료될 수 있습니다.\n\n같은 설정으로 짧은 실행 후 비정상 종료가 반복되면 다음 실행에서 백그라운드 앱 추적이 자동으로 꺼집니다.\n\n이 위험 설정을 저장하시겠습니까?",
                        UserProcessesRiskMessage: "모든 사용자 프로세스를 5초 이하 주기로 추적하면 CPU 사용량과 저장 데이터가 크게 증가할 수 있습니다.\n\n같은 설정으로 짧은 실행 후 비정상 종료가 반복되면 다음 실행에서 백그라운드 앱 추적이 자동으로 꺼집니다.\n\n이 위험 설정을 저장하시겠습니까?",
                        AnyScopeRiskMessage: "3초 이하 확인 주기는 추적 범위와 관계없이 시스템 부하와 저장 데이터 증가를 유발할 수 있습니다.\n\n같은 설정으로 짧은 실행 후 비정상 종료가 반복되면 다음 실행에서 백그라운드 앱 추적이 자동으로 꺼집니다.\n\n이 위험 설정을 저장하시겠습니까?",
                        DataFolderOpenFailed: message => $"데이터 폴더를 열 수 없습니다.\n\n{message}"),
                    new DateStatusText("기록 있음", "기록 없음", "확인 전"),
                    new CalendarPickerText(
                        MonthTitle: month => string.Format(CultureInfo.CurrentCulture, "{0:yyyy}년 {0:M월}", month),
                        MonthName: month => $"{month}월",
                        DayNames: ["일", "월", "화", "수", "목", "금", "토"]),
                    new SummaryPeriodText(
                        SpecificDate: "특정 날짜",
                        Today: date => $"오늘 ({date})",
                        Yesterday: date => $"어제 ({date})",
                        ThisWeek: weekStart => $"이번 주({weekStart}~오늘)",
                        LastWeek: (weekStart, weekEnd) => $"지난 주({weekStart}~{weekEnd})",
                        ThisMonth: () => "이번 달(1일~오늘)",
                        LastMonth: (monthStart, monthEnd) => $"지난 달({monthStart}~{monthEnd})",
                        ThisYear: year => $"올해({year}.01~오늘)",
                        LastYear: year => $"작년({year})"),
                    new TimelineText("진행 중"),
                    new RuntimeCoverageText(
                        NotChecked: "기록 커버리지 -",
                        Tooltip: "기록 커버리지는 오늘 0시부터 현재까지의 전체 시간 중 TimePilot이 실행되어 기록할 수 있었던 시간의 비율입니다. 컴퓨터를 실제로 사용한 시간 기준이 아닙니다.\n\n미기록 시간은 TimePilot이 실행되지 않았거나 기록할 수 없었던 구간입니다. PC 종료, Windows 절전, 앱 종료, 비정상 종료 등이 원인일 수 있으며 정확한 원인은 단정하지 않습니다.\n\n부팅 후 미실행은 Windows 시스템 시작 후 TimePilot이 처음 실행되기 전까지의 추정 시간입니다. Windows 로그인 시간이 아니라 시스템 시작 시각 기준입니다.",
                        Coverage: ratio => string.Format(CultureInfo.CurrentCulture, "오늘 0시~현재 기록 커버리지 {0:P1}", ratio),
                        Tracked: duration => $"기록 {duration}",
                        Missing: duration => $"미기록 {duration}(원인 미확정)",
                        LongestMissing: duration => $"최장 미기록 {duration}",
                        BootBeforeTimePilot: duration => $"부팅 후 미실행 {duration}"));
            }

            public static UiTextResources CreateEnglish()
            {
                return new UiTextResources(
                    new CommonText("Yes", "No", "OK", "Save", "Cancel", "Apply"),
                    new MainText(
                        FileMenu: "File",
                        ExportCsv: "Export CSV...",
                        Exit: "Exit",
                        SettingsMenu: "Settings",
                        Preferences: "Preferences...",
                        HelpMenu: "Help",
                        About: "About",
                        SummaryTab: "Summary",
                        DetailTab: "Details",
                        TimelineTab: "Timeline",
                        Period: "Period",
                        Date: "Date",
                        Calendar: "Calendar",
                        Today: "Today",
                        NotChecked: "Not checked",
                        CurrentTrackingScopeOnly: "Current scope only",
                        RunningOnly: "Running only",
                        App: "App",
                        Start: "Start",
                        End: "End",
                        Duration: "Duration",
                        Status: "Status",
                        Type: "Type",
                        Pid: "PID",
                        FirstStartedAt: "First start",
                        FirstObservedAt: "First observed",
                        LastObservedAt: "Last observed",
                        ActiveUsageTime: "Active usage time",
                        ActiveRatio: "Active ratio",
                        SwitchCount: "Switches",
                        TotalActiveUsageTime: "Total active usage",
                        TopApp: "Top app",
                        TopAppTime: "Top app time",
                        Runtime: "Runtime",
                        ActualUsageRatio: "Actual usage ratio",
                        RuntimeSegmentCount: "Runtime segments",
                        ObservationBasis: "Observation basis",
                        WindowedApp: "Windowed app",
                        UserProcess: "User process",
                        AllProcesses: "All processes",
                        HasData: "Has records",
                        NoData: "No records",
                        Active: "Active",
                        Idle: "Idle",
                        Untracked: "Not tracked",
                        TimePilotUntracked: "TimePilot not running",
                        Running: "Running",
                        Ended: "Ended",
                        OutsideTrackingScope: "Outside tracking scope",
                        NoForegroundApp: "(none)",
                        ForegroundPrefix: "Foreground: ",
                        PerformancePrefix: "Performance: ",
                        CsvFilter: "CSV files (*.csv)|*.csv",
                        CsvExportTitle: "Export CSV",
                        AboutTitle: "About TimePilot",
                        UsageRatioTooltip: "The share of this app within total active usage time for the selected period.",
                        RuntimeLastObservedTooltip: "The last time background app tracking actually observed this app.",
                        RuntimeDurationTooltip: "Running apps continue to increase from the current time; ended apps are calculated up to the last observed time.",
                        RuntimeActualUsageRatioTooltip: "The share of foreground active usage time within this app's runtime.",
                        RuntimeSegmentCountTooltip: "The number of observed continuous runtime segments for this app.",
                        RuntimeStatusTooltip: "Shows running, ended, or outside tracking scope based on the current settings.",
                        RecordedDateCalendarTooltip: "Dates with records are shown in bold on the calendar.",
                        OpenWindow: "Open window",
                        UsageDataCleared: "Usage records were deleted.",
                        StartupPromptTitle: "TimePilot startup",
                        StartupPromptMessage: "Run TimePilot automatically when Windows starts?\n\nYou can change this later in Preferences.",
                        SafeModeTitle: "TimePilot safe mode",
                        SafeModeMessage: "TimePilot appears to have closed unexpectedly multiple times in a short period while risky background app tracking settings were enabled.\n\nSafe mode automatically disabled only background app tracking. Current app usage tracking and idle detection will continue to work.\n\nTo enable it again, go to Settings > Preferences and turn on background app tracking.",
                        SafeModeBalloonMessage: "Background app tracking was automatically disabled to avoid repeated unexpected exits.",
                        DuplicateInstanceMessage: "TimePilot is already running. Check the tray icon.",
                        CsvExportCompleted: (count, directory) => $"Exported {count} CSV files.\n\n{directory}",
                        CsvExportFailed: message => $"CSV export failed.\n\n{message}"),
                    new CsvText(
                        Date: "Date",
                        AppName: "App name",
                        ProcessName: "Process name",
                        ActiveUsageTime: "Active usage time",
                        OverallRatio: "Overall ratio",
                        SwitchCount: "Switch count",
                        FirstStartedAt: "First start",
                        LastObservedAt: "Last observed",
                        StartedAt: "Started at",
                        EndedAt: "Ended at",
                        StartTime: "Start time",
                        EndTime: "End time",
                        Duration: "Duration",
                        Status: "Status",
                        Runtime: "Runtime",
                        Running: "Running",
                        Ended: "Ended"),
                    new PreferencesText(
                        Title: "Preferences",
                        LanguageLabel: "Display language",
                        IdleThresholdLabel: "Idle threshold",
                        MinuteUnit: "min",
                        SecondUnit: "sec",
                        Minutes: minutes => minutes == 1 ? "1 min" : $"{minutes} min",
                        Seconds: seconds => seconds == 1 ? "1 sec" : $"{seconds} sec",
                        StartWithWindows: "Run when Windows starts",
                        PerformanceDiagnostics: "Show performance diagnostics",
                        ProcessRuntimeGroup: "Background app tracking",
                        ProcessRuntimeTracking: "Track running app sessions",
                        ProcessRuntimeScope: "Tracking scope",
                        ProcessRuntimeInterval: "Check interval",
                        ProcessRuntimeWarning: "Short check intervals can increase CPU usage, battery drain, and stored data.",
                        ProcessRuntimeDangerWarning: "Risky setting. If repeated unexpected exits are detected, background app tracking may be disabled on the next launch.",
                        DataManagementGroup: "Data management",
                        DataManagementDescription: "Manage where records and settings are stored.",
                        OpenDataFolder: "Open folder",
                        ClearUsageData: "Delete records",
                        ClearUsageDataPending: "Pending delete",
                        DataFolderOpenTitle: "Open data folder",
                        ClearUsageDataTitle: "Delete usage records",
                        ClearUsageDataMessage: "When you click Save, app usage records and timeline records will be deleted.\n\nPreferences and Windows startup settings will be kept.",
                        AdvancedTrackingTitle: "Advanced tracking settings",
                        WindowedAppsScope: "Apps with windows only",
                        UserProcessesScope: "All user processes",
                        AllProcessesScope: "All processes",
                        Custom: "Custom",
                        AllProcessesRiskMessage: "Tracking all processes every 10 seconds or less may freeze or unexpectedly close TimePilot depending on your environment.\n\nIf repeated unexpected exits happen shortly after launch with the same setting, background app tracking will be disabled on the next launch.\n\nSave this risky setting?",
                        UserProcessesRiskMessage: "Tracking all user processes every 5 seconds or less can significantly increase CPU usage and stored data.\n\nIf repeated unexpected exits happen shortly after launch with the same setting, background app tracking will be disabled on the next launch.\n\nSave this risky setting?",
                        AnyScopeRiskMessage: "Check intervals of 3 seconds or less can increase system load and stored data regardless of tracking scope.\n\nIf repeated unexpected exits happen shortly after launch with the same setting, background app tracking will be disabled on the next launch.\n\nSave this risky setting?",
                        DataFolderOpenFailed: message => $"Could not open the data folder.\n\n{message}"),
                    new DateStatusText("Has records", "No records", "Not checked"),
                    new CalendarPickerText(
                        MonthTitle: month => month.ToString("MMMM yyyy", CultureInfo.CurrentCulture),
                        MonthName: month => CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month),
                        DayNames: ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"]),
                    new SummaryPeriodText(
                        SpecificDate: "Specific date",
                        Today: date => $"Today ({date})",
                        Yesterday: date => $"Yesterday ({date})",
                        ThisWeek: weekStart => $"This week ({weekStart}~today)",
                        LastWeek: (weekStart, weekEnd) => $"Last week ({weekStart}~{weekEnd})",
                        ThisMonth: () => "This month (1st~today)",
                        LastMonth: (monthStart, monthEnd) => $"Last month ({monthStart}~{monthEnd})",
                        ThisYear: year => $"This year ({year}.01~today)",
                        LastYear: year => $"Last year ({year})"),
                    new TimelineText("In progress"),
                    new RuntimeCoverageText(
                        NotChecked: "Record coverage -",
                        Tooltip: "Record coverage is the share of time from midnight to now when TimePilot was running and able to record. It is not based on actual PC usage time.\n\nMissing time means TimePilot was not running or could not record during that period. Possible causes include PC shutdown, Windows sleep, app exit, or unexpected exit; the exact cause is not determined.\n\nBefore TimePilot after boot is the estimated time from Windows system startup until TimePilot first started. It is based on system startup time, not Windows login time.",
                        Coverage: ratio => string.Format(CultureInfo.CurrentCulture, "Record coverage from midnight to now {0:P1}", ratio),
                        Tracked: duration => $"Recorded {duration}",
                        Missing: duration => $"Missing {duration} (cause unknown)",
                        LongestMissing: duration => $"Longest missing {duration}",
                        BootBeforeTimePilot: duration => $"Before TimePilot after boot {duration}"));
            }
        }

        private sealed record CommonText(string Yes, string No, string Ok, string Save, string Cancel, string Apply);

        private sealed record MainText(
            string FileMenu,
            string ExportCsv,
            string Exit,
            string SettingsMenu,
            string Preferences,
            string HelpMenu,
            string About,
            string SummaryTab,
            string DetailTab,
            string TimelineTab,
            string Period,
            string Date,
            string Calendar,
            string Today,
            string NotChecked,
            string CurrentTrackingScopeOnly,
            string RunningOnly,
            string App,
            string Start,
            string End,
            string Duration,
            string Status,
            string Type,
            string Pid,
            string FirstStartedAt,
            string FirstObservedAt,
            string LastObservedAt,
            string ActiveUsageTime,
            string ActiveRatio,
            string SwitchCount,
            string TotalActiveUsageTime,
            string TopApp,
            string TopAppTime,
            string Runtime,
            string ActualUsageRatio,
            string RuntimeSegmentCount,
            string ObservationBasis,
            string WindowedApp,
            string UserProcess,
            string AllProcesses,
            string HasData,
            string NoData,
            string Active,
            string Idle,
            string Untracked,
            string TimePilotUntracked,
            string Running,
            string Ended,
            string OutsideTrackingScope,
            string NoForegroundApp,
            string ForegroundPrefix,
            string PerformancePrefix,
            string CsvFilter,
            string CsvExportTitle,
            string AboutTitle,
            string UsageRatioTooltip,
            string RuntimeLastObservedTooltip,
            string RuntimeDurationTooltip,
            string RuntimeActualUsageRatioTooltip,
            string RuntimeSegmentCountTooltip,
            string RuntimeStatusTooltip,
            string RecordedDateCalendarTooltip,
            string OpenWindow,
            string UsageDataCleared,
            string StartupPromptTitle,
            string StartupPromptMessage,
            string SafeModeTitle,
            string SafeModeMessage,
            string SafeModeBalloonMessage,
            string DuplicateInstanceMessage,
            Func<int, string?, string> CsvExportCompleted,
            Func<string, string> CsvExportFailed);

        private sealed record CsvText(
            string Date,
            string AppName,
            string ProcessName,
            string ActiveUsageTime,
            string OverallRatio,
            string SwitchCount,
            string FirstStartedAt,
            string LastObservedAt,
            string StartedAt,
            string EndedAt,
            string StartTime,
            string EndTime,
            string Duration,
            string Status,
            string Runtime,
            string Running,
            string Ended);

        private sealed record PreferencesText(
            string Title,
            string LanguageLabel,
            string IdleThresholdLabel,
            string MinuteUnit,
            string SecondUnit,
            Func<int, string> Minutes,
            Func<int, string> Seconds,
            string StartWithWindows,
            string PerformanceDiagnostics,
            string ProcessRuntimeGroup,
            string ProcessRuntimeTracking,
            string ProcessRuntimeScope,
            string ProcessRuntimeInterval,
            string ProcessRuntimeWarning,
            string ProcessRuntimeDangerWarning,
            string DataManagementGroup,
            string DataManagementDescription,
            string OpenDataFolder,
            string ClearUsageData,
            string ClearUsageDataPending,
            string DataFolderOpenTitle,
            string ClearUsageDataTitle,
            string ClearUsageDataMessage,
            string AdvancedTrackingTitle,
            string WindowedAppsScope,
            string UserProcessesScope,
            string AllProcessesScope,
            string Custom,
            string AllProcessesRiskMessage,
            string UserProcessesRiskMessage,
            string AnyScopeRiskMessage,
            Func<string, string> DataFolderOpenFailed);

        private sealed record DateStatusText(string HasData, string NoData, string NotChecked);

        private sealed record CalendarPickerText(
            Func<DateTime, string> MonthTitle,
            Func<int, string> MonthName,
            IReadOnlyList<string> DayNames);

        private sealed record SummaryPeriodText(
            string SpecificDate,
            Func<string, string> Today,
            Func<string, string> Yesterday,
            Func<string, string> ThisWeek,
            Func<string, string, string> LastWeek,
            Func<string> ThisMonth,
            Func<string, string, string> LastMonth,
            Func<int, string> ThisYear,
            Func<int, string> LastYear);

        private sealed record TimelineText(string InProgress);

        private sealed record RuntimeCoverageText(
            string NotChecked,
            string Tooltip,
            Func<double, string> Coverage,
            Func<string, string> Tracked,
            Func<string, string> Missing,
            Func<string, string> LongestMissing,
            Func<string, string> BootBeforeTimePilot);
    }
}
