namespace TimePilot.WinForms.KYS24
{
    internal static class AppCategoryDisplay
    {
        public static IReadOnlyList<AppCategoryDefinition> BuiltinCategories { get; } =
        [
            new("Development", "개발", "#2563EB", 10),
            new("Writing", "문서/글쓰기", "#7C3AED", 20),
            new("Research/Browsing", "자료조사/브라우징", "#0891B2", 30),
            new("Communication", "커뮤니케이션", "#DB2777", 40),
            new("Meeting", "회의", "#F59E0B", 50),
            new("Creative", "창작", "#16A34A", 60),
            new("Game", "게임", "#DC2626", 70),
            new("Media", "미디어", "#EA580C", 80),
            new("Utility", "유틸리티", "#0F766E", 90),
            new("System", "시스템", "#475569", 100),
            new("Background", "백그라운드", "#64748B", 110)
        ];

        public static string GetDisplayName(AppCategoryOption category)
        {
            var displayNameByName = GetDisplayName(category.Name);
            if (!string.Equals(displayNameByName, category.Name, StringComparison.Ordinal))
                return displayNameByName;

            return category.IsBuiltin
                ? GetDisplayName(category.Name, category.SortOrder)
                : category.Name;
        }

        public static string GetDisplayName(string categoryName)
        {
            var category = BuiltinCategories.FirstOrDefault(x =>
                string.Equals(x.CanonicalName, categoryName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.KoreanName, categoryName, StringComparison.OrdinalIgnoreCase));

            return category is null ? categoryName : GetLocalizedName(category);
        }

        public static string GetDisplayName(string categoryName, int sortOrder)
        {
            var category = BuiltinCategories.FirstOrDefault(x => x.SortOrder == sortOrder)
                ?? BuiltinCategories.FirstOrDefault(x =>
                    string.Equals(x.CanonicalName, categoryName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(x.KoreanName, categoryName, StringComparison.OrdinalIgnoreCase));

            return category is null ? categoryName : GetLocalizedName(category);
        }

        private static string GetLocalizedName(AppCategoryDefinition category)
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? category.CanonicalName
                : category.KoreanName;
        }
    }

    internal sealed record AppCategoryDefinition(
        string CanonicalName,
        string KoreanName,
        string Color,
        int SortOrder);
}
