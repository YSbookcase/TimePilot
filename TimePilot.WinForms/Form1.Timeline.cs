using TimePilot.WinForms.Timeline;

namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private TimelineSelectorCoordinator CreateTimelineSelectorCoordinator()
        {
            return new TimelineSelectorCoordinator(new TimelineSelectorControls(
                timelineZoomPanel,
                timelineCategoryBucketLabel,
                timelineCategoryBucketComboBox,
                timelineTypeHighlightLabel,
                timelineTypeHighlightComboBox,
                timelineSystemEventFilterLabel,
                timelineSystemEventFilterComboBox));
        }

        private TimelineZoomCoordinator CreateTimelineZoomCoordinator()
        {
            return new TimelineZoomCoordinator(
                new TimelineZoomControls(
                    timelineZoomRangeLabel,
                    timelineZoomOutButton,
                    timelineZoomInButton,
                    timelineZoomPreviousButton,
                    timelineZoomNextButton,
                    timelineZoomResetButton,
                    timelineZoomScrollBar),
                new TimelineZoomActions(
                    timelineOverviewControl.ZoomOut,
                    timelineOverviewControl.ZoomIn,
                    timelineOverviewControl.PanPrevious,
                    timelineOverviewControl.PanNext,
                    timelineOverviewControl.ResetView,
                    timelineOverviewControl.SetViewStartRatio),
                () => new TimelineZoomState(
                    timelineOverviewControl.ViewRangeText,
                    timelineOverviewControl.IsZoomed,
                    timelineOverviewControl.CanPanPrevious,
                    timelineOverviewControl.CanPanNext,
                    timelineOverviewControl.ViewWidthRatio,
                    timelineOverviewControl.ViewStartRatio));
        }

        private RuntimeSegmentZoomCoordinator CreateRuntimeSegmentZoomCoordinator()
        {
            return new RuntimeSegmentZoomCoordinator(
                new TimelineZoomControls(
                    runtimeSegmentZoomRangeLabel,
                    runtimeSegmentZoomOutButton,
                    runtimeSegmentZoomInButton,
                    runtimeSegmentPreviousButton,
                    runtimeSegmentNextButton,
                    runtimeSegmentResetButton,
                    runtimeSegmentZoomScrollBar),
                new TimelineZoomActions(
                    runtimeSegmentTimelineControl.ZoomOut,
                    runtimeSegmentTimelineControl.ZoomIn,
                    runtimeSegmentTimelineControl.PanPrevious,
                    runtimeSegmentTimelineControl.PanNext,
                    runtimeSegmentTimelineControl.ResetView,
                    runtimeSegmentTimelineControl.SetViewStartRatio),
                () => new TimelineZoomState(
                    runtimeSegmentTimelineControl.ViewRangeText,
                    runtimeSegmentTimelineControl.IsZoomed,
                    runtimeSegmentTimelineControl.CanPanPrevious,
                    runtimeSegmentTimelineControl.CanPanNext,
                    runtimeSegmentTimelineControl.ViewWidthRatio,
                    runtimeSegmentTimelineControl.ViewStartRatio));
        }

        private RuntimeSegmentSelectionCoordinator CreateRuntimeSegmentSelectionCoordinator()
        {
            return new RuntimeSegmentSelectionCoordinator(
                runtimeSegmentsGrid,
                runtimeSegmentTimelineControl);
        }
    }
}
