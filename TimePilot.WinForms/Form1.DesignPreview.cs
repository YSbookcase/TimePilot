using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Timeline;

namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private void ConfigureDesignPreview()
        {
            statusLabel.Text =
                $"{UiText.Main.ForegroundPrefix}Visual Studio · {UiText.Main.Active}";
            SetRuntimeCoverageSummaryParts(
                UiText.RuntimeCoverage.Coverage(0.981),
                UiText.RuntimeCoverage.Tracked("07:48:12"),
                UiText.RuntimeCoverage.Missing("00:09:00"),
                UiText.RuntimeCoverage.LongestMissing("00:04:12"));
            usageGrid.DataSource = AddIcons(new List<UsageSummaryRow>
            {
                new(1, "Microsoft Visual Studio", "devenv", null, null, "개발",
                    3_900_000, 0.54, 8, null,
                    DateTimeOffset.Now.AddHours(-2), DateTimeOffset.Now),
                new(2, "Google Chrome", "chrome", null, null, "자료조사/브라우징",
                    1_680_000, 0.23, 15, null,
                    DateTimeOffset.Now.AddHours(-1),
                    DateTimeOffset.Now.AddMinutes(-12)),
                new(3, "File Explorer", "explorer", null, null, null,
                    900_000, 0.13, 4, null,
                    DateTimeOffset.Now.AddMinutes(-45),
                    DateTimeOffset.Now.AddMinutes(-5))
            });
            dailyUsageTrendGrid.DataSource = new List<DailyUsageTrendRow>
            {
                new(DateTime.Today, 6_480_000, "Microsoft Visual Studio", 3_900_000),
                new(DateTime.Today.AddDays(-1), 4_560_000, "Google Chrome", 2_400_000)
            };
            timelineGrid.DataSource = AddIcons(new List<ActivityTimelineRow>
            {
                new(UiText.Main.Active, DateTimeOffset.Now.AddHours(-2),
                    DateTimeOffset.Now.AddHours(-1), 3_600_000, "devenv"),
                new(UiText.Main.Idle, DateTimeOffset.Now.AddHours(-1),
                    DateTimeOffset.Now.AddMinutes(-45), 900_000, "devenv"),
                new(UiText.Main.Active, DateTimeOffset.Now.AddMinutes(-45),
                    null, 2_700_000, "chrome")
            });
            timelineOverviewControl.SetTimeline(
                DateTime.Today,
                (IReadOnlyList<ActivityTimelineRow>)timelineGrid.DataSource,
                new[]
                {
                    new TimelineRange(
                        DateTimeOffset.Now.AddHours(-3),
                        DateTimeOffset.Now)
                },
                new[]
                {
                    new SystemTimelineRange(
                        DateTimeOffset.Now.AddHours(-1.5),
                        DateTimeOffset.Now.AddHours(-1.25),
                        SystemTimelineRangeType.LockSession)
                },
                new[]
                {
                    new SystemTimelineEvent(
                        DateTimeOffset.Now.AddHours(-2.5),
                        "timepilot-start",
                        "Preview"),
                    new SystemTimelineEvent(
                        DateTimeOffset.Now.AddMinutes(-50),
                        "lock",
                        "Preview")
                },
                new[]
                {
                    new CategoryTimelineSegment(
                        DateTimeOffset.Now.AddHours(-3),
                        DateTimeOffset.Now.AddHours(-2),
                        "개발",
                        "#2563EB",
                        false,
                        3_600_000,
                        "개발 100%"),
                    new CategoryTimelineSegment(
                        DateTimeOffset.Now.AddHours(-2),
                        DateTimeOffset.Now.AddHours(-1),
                        "개발",
                        "#2563EB",
                        true,
                        3_600_000,
                        "개발 45%, 자료조사/브라우징 40%")
                });
            currentTimelineSystemEvents =
            [
                new SystemTimelineEvent(
                    DateTimeOffset.Now.AddHours(-2.5),
                    "timepilot-start",
                    "Preview"),
                new SystemTimelineEvent(
                    DateTimeOffset.Now.AddMinutes(-50),
                    "lock",
                    "Preview"),
                new SystemTimelineEvent(
                    DateTimeOffset.Now.AddMinutes(-30),
                    "unlock",
                    "Preview")
            ];
        }
    }
}
