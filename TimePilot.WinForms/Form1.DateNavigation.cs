namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private void InitializeDateSelectors()
        {
            isInitializingDateSelectors = true;
            try
            {
                ConfigureDatePickerFormat(summarySpecificDatePicker);
                ConfigureDatePickerFormat(detailDatePicker);
                ConfigureDatePickerFormat(timelineDatePicker);
                var today = DateTime.Today;
                selectedDetailDate = today;
                selectedTimelineDate = today;
                detailDatePicker.MaxDate = today;
                timelineDatePicker.MaxDate = today;
                detailDatePicker.Value = today;
                timelineDatePicker.Value = today;
                dateSelectorOptionsDate = today;
            }
            finally
            {
                isInitializingDateSelectors = false;
            }

            UpdateDateNavigationButtons();
        }

        private static void ConfigureDatePickerFormat(DateTimePicker picker)
        {
            picker.Format = DateTimePickerFormat.Custom;
            picker.CustomFormat = "yyyy-MM-dd (ddd)";
        }

        private void InitializeRecordedDateCalendar()
        {
            runtimeCoverageSummaryToolTip.SetToolTip(
                summarySpecificDateCalendarButton,
                UiText.Main.RecordedDateCalendarTooltip);
            runtimeCoverageSummaryToolTip.SetToolTip(
                detailCalendarButton,
                UiText.Main.RecordedDateCalendarTooltip);
            runtimeCoverageSummaryToolTip.SetToolTip(
                timelineCalendarButton,
                UiText.Main.RecordedDateCalendarTooltip);
        }
    }
}
