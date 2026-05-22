namespace TimePilot.WinForms
{
    internal sealed class TimelineActivitySegmentContextEventArgs : EventArgs
    {
        public TimelineActivitySegmentContextEventArgs(ActivityTimelineRow row, Point location)
        {
            Row = row;
            Location = location;
        }

        public ActivityTimelineRow Row { get; }

        public Point Location { get; }
    }
}
