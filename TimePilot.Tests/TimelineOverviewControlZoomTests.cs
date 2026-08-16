using TimePilot.WinForms;
using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Timeline;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class TimelineOverviewControlZoomTests
    {
        [Fact]
        public void ZoomIn_WhenFullDayWithActivity_CentersOnActivityRange()
        {
            using var control = new TimelineOverviewControl();
            var date = new DateTime(2026, 8, 17);

            control.SetTimeline(
                date,
                [
                    CreateRow(date, 9, 0, 11, 0)
                ],
                Array.Empty<TimelineRange>(),
                Array.Empty<SystemTimelineRange>(),
                Array.Empty<SystemTimelineEvent>(),
                Array.Empty<CategoryTimelineSegment>());

            control.ZoomIn();

            Assert.Equal(0.5, control.ViewWidthRatio, precision: 6);
            Assert.Equal(4 / 24.0, control.ViewStartRatio, precision: 6);
        }

        [Fact]
        public void ZoomIn_WhenFullDayWithMultipleRows_CentersOnLatestTrackedRow()
        {
            using var control = new TimelineOverviewControl();
            var date = new DateTime(2026, 8, 17);

            control.SetTimeline(
                date,
                [
                    CreateRow(date, 0, 0, 23, 59, UiText.Main.TimePilotUntracked),
                    CreateRow(date, 9, 0, 10, 0),
                    CreateRow(date, 15, 0, 16, 0)
                ],
                Array.Empty<TimelineRange>(),
                Array.Empty<SystemTimelineRange>(),
                Array.Empty<SystemTimelineEvent>(),
                Array.Empty<CategoryTimelineSegment>());

            control.ZoomIn();

            Assert.Equal(0.5, control.ViewWidthRatio, precision: 6);
            Assert.Equal(9.5 / 24.0, control.ViewStartRatio, precision: 6);
        }

        [Fact]
        public void ZoomIn_WhenAlreadyZoomed_KeepsCurrentViewCenter()
        {
            using var control = new TimelineOverviewControl();
            var date = new DateTime(2026, 8, 17);

            control.SetTimeline(
                date,
                [
                    CreateRow(date, 9, 0, 11, 0)
                ],
                Array.Empty<TimelineRange>(),
                Array.Empty<SystemTimelineRange>(),
                Array.Empty<SystemTimelineEvent>(),
                Array.Empty<CategoryTimelineSegment>());

            control.ZoomIn();
            control.ZoomIn();

            Assert.Equal(0.25, control.ViewWidthRatio, precision: 6);
            Assert.Equal(7 / 24.0, control.ViewStartRatio, precision: 6);
        }

        [Fact]
        public void ZoomIn_WhenLatestActivityIsNearDayEnd_KeepsActivityFocusedOnRepeatedZoom()
        {
            using var control = new TimelineOverviewControl();
            var date = new DateTime(2026, 8, 17);

            control.SetTimeline(
                date,
                [
                    CreateRow(date, 22, 0, 23, 0)
                ],
                Array.Empty<TimelineRange>(),
                Array.Empty<SystemTimelineRange>(),
                Array.Empty<SystemTimelineEvent>(),
                Array.Empty<CategoryTimelineSegment>());

            control.ZoomIn();
            control.ZoomIn();

            Assert.Equal(0.25, control.ViewWidthRatio, precision: 6);
            Assert.Equal(18 / 24.0, control.ViewStartRatio, precision: 6);
        }

        [Fact]
        public void ZoomOut_WhenLatestActivityIsVisible_UsesSameActivityFocusAsZoomIn()
        {
            using var control = new TimelineOverviewControl();
            var date = new DateTime(2026, 8, 17);

            control.SetTimeline(
                date,
                [
                    CreateRow(date, 9, 0, 10, 0)
                ],
                Array.Empty<TimelineRange>(),
                Array.Empty<SystemTimelineRange>(),
                Array.Empty<SystemTimelineEvent>(),
                Array.Empty<CategoryTimelineSegment>());

            control.ZoomIn();
            control.ZoomIn();
            control.SetViewStartRatio(5 / 24.0);

            control.ZoomOut();

            Assert.Equal(0.5, control.ViewWidthRatio, precision: 6);
            Assert.Equal(3.5 / 24.0, control.ViewStartRatio, precision: 6);
        }

        private static ActivityTimelineRow CreateRow(
            DateTime date,
            int startHour,
            int startMinute,
            int endHour,
            int endMinute,
            string activityType = "Active")
        {
            var startedAt = new DateTimeOffset(
                date.AddHours(startHour).AddMinutes(startMinute),
                TimeZoneInfo.Local.GetUtcOffset(date));
            var endedAt = new DateTimeOffset(
                date.AddHours(endHour).AddMinutes(endMinute),
                TimeZoneInfo.Local.GetUtcOffset(date));

            return new ActivityTimelineRow(
                activityType,
                startedAt,
                endedAt,
                (long)(endedAt - startedAt).TotalMilliseconds,
                "App");
        }
    }
}
