namespace TimePilot.WinForms.Timeline
{
    internal sealed record TimelineZoomActions(
        Action ZoomOut,
        Action ZoomIn,
        Action PanPrevious,
        Action PanNext,
        Action ResetView,
        Action<double> SetViewStartRatio);
}
