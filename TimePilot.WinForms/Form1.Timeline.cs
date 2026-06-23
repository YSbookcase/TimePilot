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
    }
}
