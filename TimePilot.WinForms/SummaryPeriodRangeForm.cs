using System.Globalization;
using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal sealed class SummaryPeriodRangeForm : Form
    {
        private readonly Func<DateTime, DateTime, IReadOnlyList<DateTime>> getRecordedDates;
        private readonly TextBox startDateTextBox = new();
        private readonly TextBox endDateTextBox = new();
        private readonly Button startCalendarButton = new();
        private readonly Button endCalendarButton = new();
        private readonly Label rangeDurationLabel = new();
        private readonly Button okButton = new();
        private readonly Button cancelButton = new();
        private readonly DateTime today;
        private Form? datePickerPopupForm;

        public SummaryPeriodRangeForm(
            DateTime startDate,
            DateTime endDate,
            DateTime today,
            Func<DateTime, DateTime, IReadOnlyList<DateTime>> getRecordedDates)
        {
            this.today = today.Date;
            this.getRecordedDates = getRecordedDates;
            StartDate = NormalizeSelectableDate(startDate);
            EndDate = NormalizeSelectableDate(endDate);
            if (EndDate < StartDate)
                EndDate = StartDate;

            InitializeComponent();
            UpdateDateTexts();
        }

        public DateTime StartDate { get; private set; }

        public DateTime EndDate { get; private set; }

        private void InitializeComponent()
        {
            var mainPanel = new TableLayoutPanel();
            var startLabel = new Label();
            var endLabel = new Label();
            var buttonPanel = new FlowLayoutPanel();
            var startDatePanel = new TableLayoutPanel();
            var endDatePanel = new TableLayoutPanel();

            SuspendLayout();

            Text = UiText.SummaryPeriod.CustomRangeTitle;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(420, 172);

            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Padding = new Padding(12);
            mainPanel.ColumnCount = 2;
            mainPanel.RowCount = 4;
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            startLabel.Dock = DockStyle.Fill;
            startLabel.Text = UiText.SummaryPeriod.StartDate;
            startLabel.TextAlign = ContentAlignment.MiddleLeft;

            endLabel.Dock = DockStyle.Fill;
            endLabel.Text = UiText.SummaryPeriod.EndDate;
            endLabel.TextAlign = ContentAlignment.MiddleLeft;

            ConfigureDatePanel(startDatePanel, startDateTextBox, startCalendarButton, (_, _) => ShowDateCalendar(startCalendarButton, StartDate, ApplyStartDate));
            ConfigureDatePanel(endDatePanel, endDateTextBox, endCalendarButton, (_, _) => ShowDateCalendar(endCalendarButton, EndDate, ApplyEndDate));

            rangeDurationLabel.Dock = DockStyle.Fill;
            rangeDurationLabel.ForeColor = SystemColors.GrayText;
            rangeDurationLabel.TextAlign = ContentAlignment.MiddleLeft;

            okButton.Text = UiText.Common.Ok;
            okButton.Width = 84;
            okButton.Click += OnOkButtonClick;

            cancelButton.Text = UiText.Common.Cancel;
            cancelButton.Width = 84;
            cancelButton.DialogResult = DialogResult.Cancel;

            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(okButton);

            mainPanel.Controls.Add(startLabel, 0, 0);
            mainPanel.Controls.Add(startDatePanel, 1, 0);
            mainPanel.Controls.Add(endLabel, 0, 1);
            mainPanel.Controls.Add(endDatePanel, 1, 1);
            mainPanel.Controls.Add(rangeDurationLabel, 0, 2);
            mainPanel.SetColumnSpan(rangeDurationLabel, 2);
            mainPanel.Controls.Add(buttonPanel, 0, 3);
            mainPanel.SetColumnSpan(buttonPanel, 2);

            AcceptButton = okButton;
            CancelButton = cancelButton;
            FormClosed += (_, _) => CloseDatePickerDropDown();
            Controls.Add(mainPanel);

            ResumeLayout(false);
        }

        private static void ConfigureDatePanel(
            TableLayoutPanel panel,
            TextBox textBox,
            Button calendarButton,
            EventHandler calendarClickHandler)
        {
            panel.ColumnCount = 2;
            panel.Dock = DockStyle.Fill;
            panel.Margin = Padding.Empty;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));

            textBox.Dock = DockStyle.Fill;
            textBox.ReadOnly = true;
            textBox.TabStop = false;

            calendarButton.Dock = DockStyle.Fill;
            calendarButton.Text = UiText.Main.Calendar;
            calendarButton.UseVisualStyleBackColor = true;
            calendarButton.Click += calendarClickHandler;

            panel.Controls.Add(textBox, 0, 0);
            panel.Controls.Add(calendarButton, 1, 0);
        }

        private void ApplyStartDate(DateTime date)
        {
            StartDate = NormalizeSelectableDate(date);
            if (EndDate < StartDate)
                EndDate = StartDate;

            UpdateDateTexts();
        }

        private void ApplyEndDate(DateTime date)
        {
            EndDate = NormalizeSelectableDate(date);
            UpdateDateTexts();
        }

        private void UpdateDateTexts()
        {
            startDateTextBox.Text = FormatDate(StartDate);
            endDateTextBox.Text = FormatDate(EndDate);
            rangeDurationLabel.Text = CalendarRangeDurationFormatter.Format(StartDate, EndDate, includePrefix: true);
        }

        private DateTime NormalizeSelectableDate(DateTime date)
        {
            var normalizedDate = date.Date;
            return normalizedDate > today ? today : normalizedDate;
        }

        private void ShowDateCalendar(Control anchor, DateTime selectedDate, Action<DateTime> applyDate)
        {
            CloseDatePickerDropDown();

            var picker = new RecordedDatePickerPopup(NormalizeSelectableDate(selectedDate), today, getRecordedDates);
            var popupForm = new Form
            {
                AutoScaleMode = AutoScaleMode.None,
                ClientSize = picker.Size,
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                TopMost = true
            };
            popupForm.Controls.Add(picker);
            picker.Dock = DockStyle.Fill;

            picker.DateApplied += (_, date) =>
            {
                CloseDatePickerDropDown();
                applyDate(date);
            };
            picker.CloseRequested += (_, _) => CloseDatePickerDropDown();
            popupForm.Deactivate += (_, _) => CloseDatePickerDropDown();
            popupForm.FormClosed += (_, _) =>
            {
                if (ReferenceEquals(datePickerPopupForm, popupForm))
                    datePickerPopupForm = null;
            };

            datePickerPopupForm = popupForm;
            popupForm.Location = anchor.PointToScreen(new Point(0, anchor.Height));
            popupForm.Show(this);
        }

        private void CloseDatePickerDropDown()
        {
            if (datePickerPopupForm is null)
                return;

            var popupForm = datePickerPopupForm;
            datePickerPopupForm = null;
            if (!popupForm.IsDisposed)
                popupForm.Close();
        }

        private void OnOkButtonClick(object? sender, EventArgs e)
        {
            if (EndDate < StartDate)
            {
                CenteredMessageDialog.Show(
                    this,
                    UiText.SummaryPeriod.InvalidCustomRange,
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private static string FormatDate(DateTime date)
        {
            return date.ToString("yyyy-MM-dd (ddd)", CultureInfo.CurrentCulture);
        }

    }
}
