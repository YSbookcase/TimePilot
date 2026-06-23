namespace TimePilot.WinForms.Timeline
{
    internal sealed record TimelineZoomState(
        string ViewRangeText,
        bool IsZoomed,
        bool CanPanPrevious,
        bool CanPanNext,
        double ViewWidthRatio,
        double ViewStartRatio);
}
