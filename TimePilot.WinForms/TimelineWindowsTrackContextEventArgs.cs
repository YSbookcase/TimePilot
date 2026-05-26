using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal sealed class TimelineWindowsTrackContextEventArgs : EventArgs
    {
        public TimelineWindowsTrackContextEventArgs(Point location)
        {
            Location = location;
        }

        public Point Location { get; }
    }
}
