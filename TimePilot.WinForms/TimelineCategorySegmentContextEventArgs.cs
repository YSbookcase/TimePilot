using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal sealed class TimelineCategorySegmentContextEventArgs : EventArgs
    {
        public TimelineCategorySegmentContextEventArgs(CategoryTimelineSegment segment, Point location)
        {
            Segment = segment;
            Location = location;
        }

        public CategoryTimelineSegment Segment { get; }

        public Point Location { get; }
    }
}
