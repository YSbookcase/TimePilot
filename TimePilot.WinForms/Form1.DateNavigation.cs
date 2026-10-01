using TimePilot.WinForms.Navigation;

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

        private static DateTime NormalizeSelectableDate(DateTime date)
        {
            return DateSelectorCoordinator.NormalizeSelectableDate(date, DateTime.Today);
        }

        private void SetDatePickerValue(DateTimePicker picker, DateTime date)
        {
            isInitializingDateSelectors = true;
            try
            {
                EnsureDatePickerRangeIncludes(picker, date);
                picker.Value = date;
            }
            finally
            {
                isInitializingDateSelectors = false;
            }
        }

        private static void EnsureDatePickerRangeIncludes(DateTimePicker picker, DateTime date)
        {
            var normalizedDate = date.Date;
            if (picker.MaxDate.Date < normalizedDate)
                picker.MaxDate = normalizedDate;
            if (picker.MinDate.Date > normalizedDate)
                picker.MinDate = normalizedDate;
        }

        private void ShowRecordedDateCalendar(
            Control anchor,
            DateTime selectedDate,
            Action<DateTime> applyDate)
        {
            var today = DateTime.Today;
            var normalizedDate = NormalizeSelectableDate(selectedDate);
            CloseRecordedDatePickerDropDown();
            var popupForm = RecordedDatePopupFactory.Create(
                normalizedDate,
                today,
                GetRecordedDates,
                applyDate,
                CloseRecordedDatePickerDropDown,
                closedPopup =>
                {
                    if (ReferenceEquals(recordedDatePickerPopupForm, closedPopup))
                        recordedDatePickerPopupForm = null;
                });
            recordedDatePickerPopupForm = popupForm;
            popupForm.Location = anchor.PointToScreen(new Point(0, anchor.Height));
            popupForm.Show(this);
        }

        private void CloseRecordedDatePickerDropDown()
        {
            if (recordedDatePickerPopupForm is null)
                return;

            var popupForm = recordedDatePickerPopupForm;
            recordedDatePickerPopupForm = null;
            if (!popupForm.IsDisposed)
                popupForm.Close();
        }

        private IReadOnlyList<DateTime> GetRecordedDates(
            DateTime rangeStart,
            DateTime rangeEnd)
        {
            if (storage is null)
                return Array.Empty<DateTime>();

            return storage.GetActivityDates(rangeStart, rangeEnd, DateTimeOffset.UtcNow);
        }

        private void UpdateDateNavigationButtons()
        {
            var today = DateTime.Today;
            detailNextDateButton.Enabled =
                DateSelectorCoordinator.CanMoveForward(selectedDetailDate, today);
            detailTodayButton.Enabled = detailNextDateButton.Enabled;
            timelineNextDateButton.Enabled =
                DateSelectorCoordinator.CanMoveForward(selectedTimelineDate, today);
            timelineTodayButton.Enabled = timelineNextDateButton.Enabled;
        }

        private void RefreshDateSelectorsIfDateChanged(DateTimeOffset observedAt)
        {
            var today = observedAt.ToLocalTime().Date;
            var rollover = DateSelectorCoordinator.GetRollover(
                dateSelectorOptionsDate,
                today,
                selectedDetailDate,
                selectedTimelineDate,
                !IsMainWindowActivelyViewed());
            if (!rollover.DateChanged)
                return;

            var movedDate =
                selectedDetailDate != rollover.DetailDate
                || selectedTimelineDate != rollover.TimelineDate;
            isInitializingDateSelectors = true;
            try
            {
                if (detailDatePicker.MaxDate.Date < today)
                    detailDatePicker.MaxDate = today;
                if (timelineDatePicker.MaxDate.Date < today)
                    timelineDatePicker.MaxDate = today;

                if (selectedDetailDate != rollover.DetailDate)
                {
                    selectedDetailDate = rollover.DetailDate;
                    detailDatePicker.Value = rollover.DetailDate;
                }

                if (selectedTimelineDate != rollover.TimelineDate)
                {
                    selectedTimelineDate = rollover.TimelineDate;
                    timelineDatePicker.Value = rollover.TimelineDate;
                }

                if (rollover.ResetRuntimeSelection)
                    selectedRuntimeAppId = null;
                dateSelectorOptionsDate = today;
            }
            finally
            {
                isInitializingDateSelectors = false;
            }

            UpdateDateNavigationButtons();
            if (movedDate)
                SetStatusText(GetDateAutoAdvancedStatusText(today));
        }

        private bool IsMainWindowActivelyViewed()
        {
            return Visible
                && ShowInTaskbar
                && WindowState != FormWindowState.Minimized
                && (ContainsFocus || ActiveForm == this);
        }

        private static string GetDateAutoAdvancedStatusText(DateTime today)
        {
            var dateText = today.ToString(
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.CurrentCulture);
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"Moved today's views to {dateText}"
                : $"오늘 보기 날짜를 {dateText}(으)로 갱신했습니다.";
        }
    }
}
