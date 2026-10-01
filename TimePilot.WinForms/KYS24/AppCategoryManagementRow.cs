using System.Globalization;

namespace TimePilot.WinForms.KYS24
{
    internal sealed record AppCategoryManagementRow(
        long AppId,
        string AppName,
        string AutomaticAppName,
        string? UserAlias,
        string ProcessName,
        string? ExecutablePath,
        long? PrimaryCategoryId,
        string? CategoryName,
        DateTimeOffset? LastObservedAt,
        long ActiveUsageMs,
        long RuntimeMs,
        int SwitchCount,
        int RuntimeSegmentCount,
        string? FileDescription = null,
        string? ProductName = null,
        string? CompanyName = null,
        string? CategoryDisplayName = null,
        bool HasExtractedAppIcon = false,
        bool HasForegroundActivity = false,
        bool HasMainWindow = false,
        bool IsCurrentSessionProcess = false,
        bool HasRuntimeObservation = false,
        int ObservationCount = 0,
        int ObservationPathCount = 0,
        bool HasMissingObservationPath = false,
        long? RecommendedCategoryId = null,
        string? RecommendedCategoryName = null,
        string? RecommendationReason = null,
        Image? AppIcon = null)
    {
        public string CategoryText => string.IsNullOrWhiteSpace(CategoryName)
            ? UiText.Main.Uncategorized
            : CategoryDisplayName ?? AppCategoryDisplay.GetDisplayName(CategoryName);

        public string UserAliasText => UserAlias ?? "";

        public string LastObservedAtText => LastObservedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture) ?? "";

        public string ActiveUsageTimeText => FormatDuration(ActiveUsageMs);

        public string RuntimeText => FormatDuration(RuntimeMs);

        public string SwitchCountText => SwitchCount.ToString("N0", CultureInfo.CurrentCulture);

        public string RuntimeSegmentCountText => RuntimeSegmentCount.ToString("N0", CultureInfo.CurrentCulture);

        public string FileDescriptionText => FileDescription ?? "";

        public string ProductNameText => ProductName ?? "";

        public string CompanyNameText => CompanyName ?? "";

        public string RecommendedCategoryText => RecommendedCategoryName ?? "";

        public string RecommendationReasonText => RecommendationReason ?? "";

        public bool HasRecommendation => RecommendedCategoryId is not null;

        public bool HasAppIcon => HasExtractedAppIcon;

        public bool NeedsIdentityReview =>
            ObservationPathCount > 1
            || IsGenericProcessName(ProcessName);

        public string IdentityStatusText
        {
            get
            {
                if (ObservationPathCount > 1)
                    return UiText.CurrentLanguage == UiLanguage.English ? "Multiple locations" : "여러 위치";

                if (IsGenericProcessName(ProcessName))
                    return UiText.CurrentLanguage == UiLanguage.English ? "Common name" : "흔한 이름";

                if (HasMissingObservationPath)
                    return UiText.CurrentLanguage == UiLanguage.English ? "Location unknown" : "위치 알 수 없음";

                return UiText.CurrentLanguage == UiLanguage.English ? "No issue" : "문제 없음";
            }
        }

        public string IdentityDetailText
        {
            get
            {
                var parts = new List<string>
                {
                    UiText.CurrentLanguage == UiLanguage.English
                        ? $"Observations: {ObservationCount:N0}"
                        : $"관측: {ObservationCount:N0}회",
                    UiText.CurrentLanguage == UiLanguage.English
                        ? $"Paths: {ObservationPathCount:N0}"
                        : $"경로: {ObservationPathCount:N0}개"
                };

                if (HasMissingObservationPath)
                {
                    parts.Add(UiText.CurrentLanguage == UiLanguage.English
                        ? "Some observations have no executable path. This can happen for protected Windows processes, permission-limited processes, or older records."
                        : "일부 관측에 실행 경로가 없습니다. Windows 보호 프로세스, 권한상 읽을 수 없는 프로세스, 예전 기록에서 발생할 수 있습니다.");
                }

                if (IsGenericProcessName(ProcessName))
                {
                    parts.Add(UiText.CurrentLanguage == UiLanguage.English
                        ? "The process name is generic, so it may need review."
                        : "프로세스명이 일반적이어서 확인이 필요할 수 있습니다.");
                }

                if (ObservationPathCount > 1)
                {
                    parts.Add(UiText.CurrentLanguage == UiLanguage.English
                        ? "The same app name was observed from multiple executable paths."
                        : "같은 앱 이름이 여러 실행 경로에서 관측되었습니다.");
                }

                return string.Join(Environment.NewLine, parts);
            }
        }

        public string TrackingTypeText
        {
            get
            {
                if (HasForegroundActivity || HasMainWindow)
                    return UiText.Main.WindowedApp;

                if (IsCurrentSessionProcess)
                    return UiText.Main.UserProcess;

                return HasRuntimeObservation ? UiText.Main.AllProcesses : "";
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

        private static bool IsGenericProcessName(string processName)
        {
            var normalized = Path.GetFileNameWithoutExtension(processName)
                .Trim()
                .ToLowerInvariant();
            return normalized is "setup"
                or "update"
                or "upgrade"
                or "launcher"
                or "helper"
                or "uninstall"
                or "unins000"
                or "install"
                or "installer"
                or "tmp";
        }
    }
}
