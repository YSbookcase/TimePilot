using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Timeline
{
    internal sealed record TimelineCategoryBucketOption(string Label, int Minutes)
    {
        public override string ToString() => Label;

        public static IReadOnlyList<TimelineCategoryBucketOption> GetOptions()
        {
            return
            [
                new(UiText.Main.TimelineCategoryBucketAll, 0),
                new(UiText.Main.TimelineCategoryBucketMinutes(15), 15),
                new(GetDefaultBucketLabel(), 30),
                new(UiText.Main.TimelineCategoryBucketHours(1), 60),
                new(UiText.Main.TimelineCategoryBucketHours(2), 120)
            ];
        }

        private static string GetDefaultBucketLabel()
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"{UiText.Main.TimelineCategoryBucketMinutes(30)} (Default)"
                : $"{UiText.Main.TimelineCategoryBucketMinutes(30)} (기본)";
        }
    }
}
