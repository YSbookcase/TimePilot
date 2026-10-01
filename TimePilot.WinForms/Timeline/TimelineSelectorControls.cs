namespace TimePilot.WinForms.Timeline
{
    internal sealed record TimelineSelectorControls(
        FlowLayoutPanel ZoomPanel,
        Label CategoryBucketLabel,
        ComboBox CategoryBucketComboBox,
        Label TypeHighlightLabel,
        ComboBox TypeHighlightComboBox,
        Label SystemEventFilterLabel,
        ComboBox SystemEventFilterComboBox);
}
