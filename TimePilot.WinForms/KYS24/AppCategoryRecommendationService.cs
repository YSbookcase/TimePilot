namespace TimePilot.WinForms.KYS24
{
    internal static class AppCategoryRecommendationService
    {
        private static readonly IReadOnlyDictionary<string, string> ExactProcessCategories =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["chrome"] = "Research/Browsing",
                ["msedge"] = "Research/Browsing",
                ["firefox"] = "Research/Browsing",
                ["brave"] = "Research/Browsing",
                ["whale"] = "Research/Browsing",
                ["opera"] = "Research/Browsing",
                ["devenv"] = "Development",
                ["code"] = "Development",
                ["cursor"] = "Development",
                ["dbeaver"] = "Development",
                ["dotnet"] = "Development",
                ["msbuild"] = "Development",
                ["git"] = "Development",
                ["gh"] = "Development",
                ["node"] = "Development",
                ["discord"] = "Communication",
                ["kakaotalk"] = "Communication",
                ["line"] = "Communication",
                ["slack"] = "Communication",
                ["teams"] = "Communication",
                ["zoom"] = "Meeting",
                ["winword"] = "Writing",
                ["excel"] = "Writing",
                ["hwp"] = "Writing",
                ["notepad"] = "Writing",
                ["photoshop"] = "Creative",
                ["illustrator"] = "Creative",
                ["blender"] = "Creative",
                ["unrealeditor"] = "Development",
                ["steam"] = "Game",
                ["epicgameslauncher"] = "Game",
                ["nexonlauncher64"] = "Game",
                ["unrealgame"] = "Game",
                ["stackobot"] = "Game",
                ["simcity4"] = "Game",
                ["potplayer64"] = "Media",
                ["vlc"] = "Media",
                ["spotify"] = "Media",
                ["obs64"] = "Media",
                ["4kvideodownloader"] = "Media",
                ["bandizip"] = "Utility",
                ["everything"] = "Utility",
                ["setup"] = "Utility",
                ["unins000"] = "Utility",
                ["uninstall"] = "Utility",
                ["pickerhost"] = "Utility",
                ["e_upbj02"] = "Utility",
                ["es2utility"] = "Utility",
                ["iptime_upgrade_notification"] = "Utility",
                ["explorer"] = "System",
                ["taskmgr"] = "System",
                ["systemsettings"] = "System",
                ["powershell"] = "System",
                ["cmd"] = "System",
                ["windowsterminal"] = "System"
            };

        private static readonly IReadOnlyDictionary<string, string> CompanyCategories =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["google"] = "Research/Browsing",
                ["adobe"] = "Creative",
                ["valve"] = "Game",
                ["epic games"] = "Game",
                ["discord"] = "Communication",
                ["kakao"] = "Communication",
                ["epson"] = "Utility",
                ["brother"] = "Utility",
                ["iptime"] = "Utility"
            };

        private static readonly string[] BackgroundProcessHints =
        [
            "helper",
            "updater",
            "update",
            "service",
            "broker",
            "crash",
            "sync"
        ];

        private static readonly string[] UtilityProcessHints =
        [
            "setup",
            "install",
            "uninstall",
            "unins",
            "zip",
            "archive",
            "extract",
            "backup",
            "printer",
            "scan",
            "scanner",
            "epson",
            "brother",
            "iptime",
            "router"
        ];

        public static AppCategoryRecommendation? Recommend(
            AppCategoryManagementRow row,
            IReadOnlyList<AppCategoryOption> categories,
            UiLanguage language)
        {
            if (row.PrimaryCategoryId is not null)
                return null;

            var processName = NormalizeProcessName(row.ProcessName);
            if (IsWindowsPath(row.ExecutablePath))
            {
                return CreateRecommendation(
                    categories,
                    "System",
                    language == UiLanguage.English ? "Windows system path" : "Windows 시스템 경로");
            }

            if (!row.HasForegroundActivity && !row.HasMainWindow && row.HasRuntimeObservation)
            {
                return CreateRecommendation(
                    categories,
                    "Background",
                    language == UiLanguage.English ? "Background-only observed process" : "백그라운드 전용 관측 프로세스");
            }

            if (ExactProcessCategories.TryGetValue(processName, out var exactCategory))
            {
                return CreateRecommendation(
                    categories,
                    exactCategory,
                    language == UiLanguage.English ? "Known app match" : "알려진 앱 기준");
            }

            if ((row.HasForegroundActivity || row.HasMainWindow)
                && UtilityProcessHints.Any(hint => processName.Contains(hint, StringComparison.OrdinalIgnoreCase)))
            {
                return CreateRecommendation(
                    categories,
                    "Utility",
                    language == UiLanguage.English ? "Utility app hint" : "유틸리티 앱 힌트");
            }

            var company = NormalizeText(row.CompanyName);
            if ((row.HasForegroundActivity || row.HasMainWindow) && !string.IsNullOrWhiteSpace(company))
            {
                foreach (var pair in CompanyCategories)
                {
                    if (company.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        return CreateRecommendation(
                            categories,
                            pair.Value,
                            language == UiLanguage.English ? "Known company match" : "알려진 회사 기준");
                    }
                }
            }

            if (!row.HasForegroundActivity
                && !row.HasMainWindow
                && row.ActiveUsageMs == 0
                && BackgroundProcessHints.Any(hint => processName.Contains(hint, StringComparison.OrdinalIgnoreCase)))
            {
                return CreateRecommendation(
                    categories,
                    "Background",
                    language == UiLanguage.English ? "Background helper process" : "백그라운드 보조 프로세스");
            }

            return null;
        }

        private static AppCategoryRecommendation? CreateRecommendation(
            IReadOnlyList<AppCategoryOption> categories,
            string canonicalCategoryName,
            string reason)
        {
            var category = categories.FirstOrDefault(x =>
                string.Equals(x.Name, canonicalCategoryName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(AppCategoryDisplay.GetDisplayName(x), canonicalCategoryName, StringComparison.OrdinalIgnoreCase));
            return category is null
                ? null
                : new AppCategoryRecommendation(category.Id, AppCategoryDisplay.GetDisplayName(category), reason);
        }

        private static string NormalizeProcessName(string processName)
        {
            var normalized = NormalizeText(processName);
            return normalized.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? normalized[..^4]
                : normalized;
        }

        private static string NormalizeText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
        }

        private static bool IsWindowsPath(string? executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
                return false;

            var normalized = executablePath.Trim().Replace('/', '\\');
            return normalized.StartsWith(@"C:\Windows\", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains(@"\Windows\System32\", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains(@"\Windows\SysWOW64\", StringComparison.OrdinalIgnoreCase);
        }
    }
}
