namespace TimePilot.WinForms.Timeline
{
    internal sealed record TimelineZoomControls(
        Label RangeLabel,
        Button ZoomOutButton,
        Button ZoomInButton,
        Button PreviousButton,
        Button NextButton,
        Button ResetButton,
        HScrollBar ScrollBar);
}
