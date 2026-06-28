namespace TimePilot.WinForms.Navigation
{
    internal static class RecordedDatePopupFactory
    {
        public static Form Create(
            DateTime selectedDate,
            DateTime today,
            Func<DateTime, DateTime, IReadOnlyList<DateTime>> getRecordedDates,
            Action<DateTime> applyDate,
            Action closePopup,
            Action<Form> popupClosed)
        {
            var picker = new RecordedDatePickerPopup(selectedDate, today, getRecordedDates);
            var popup = new Form
            {
                AutoScaleMode = AutoScaleMode.None,
                ClientSize = picker.Size,
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                TopMost = true
            };
            popup.Controls.Add(picker);
            picker.Dock = DockStyle.Fill;

            picker.DateApplied += (_, date) =>
            {
                closePopup();
                applyDate(date);
            };
            picker.CloseRequested += (_, _) => closePopup();
            popup.Deactivate += (_, _) => closePopup();
            popup.FormClosed += (_, _) => popupClosed(popup);
            return popup;
        }
    }
}
