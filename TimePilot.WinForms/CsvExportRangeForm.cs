using System.Globalization;
using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal sealed class CsvExportRangeForm : Form
    {
        private readonly Func<DateTime, DateTime, IReadOnlyList<DateTime>> getRecordedDates;
        private readonly TextBox startDateTextBox = new();
        private readonly TextBox endDateTextBox = new();
        private readonly Button startCalendarButton = new();
        private readonly Button endCalendarButton = new();
        private readonly Button todayButton = new();
        private readonly Button yesterdayButton = new();
        private readonly Button last7DaysButton = new();
        private readonly Button thisWeekButton = new();
        private readonly Button lastWeekButton = new();
        private readonly Button thisMonthButton = new();
        private readonly Button lastMonthButton = new();
        private readonly Button thisYearButton = new();
        private readonly Button lastYearButton = new();
        private readonly Button okButton = new();
        private readonly Button cancelButton = new();
        private readonly bool isEnglish;
        private readonly DateTime today;

        private Form? datePickerPopupForm;

        public CsvExportRangeForm(
            DateTime today,
            UiLanguage language,
            Func<DateTime, DateTime, IReadOnlyList<DateTime>> getRecordedDates)
        {
            this.today = today.Date;
            this.getRecordedDates = getRecordedDates;
            isEnglish = language == UiLanguage.English;
            StartDate = this.today;
            EndDate = this.today;
            InitializeComponent();
            UpdateDateTexts();
        }

        public DateTime StartDate { get; private set; }

        public DateTime EndDate { get; private set; }

        private void InitializeComponent()
        {
            var mainPanel = new TableLayoutPanel();
            var presetPanel = new FlowLayoutPanel();
            var startLabel = new Label();
            var endLabel = new Label();
            var buttonPanel = new FlowLayoutPanel();
            var startDatePanel = new TableLayoutPanel();
            var endDatePanel = new TableLayoutPanel();

            SuspendLayout();

            Text = isEnglish ? "CSV Export Period" : "CSV 내보내기 기간";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(560, 246);

            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Padding = new Padding(12);
            mainPanel.ColumnCount = 2;
            mainPanel.RowCount = 4;
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            presetPanel.Dock = DockStyle.Fill;
            presetPanel.WrapContents = true;

            ConfigurePresetButton(todayButton, isEnglish ? "Today" : "오늘", (_, _) => SetRange(today, today));
            ConfigurePresetButton(yesterdayButton, isEnglish ? "Yesterday" : "어제", (_, _) => SetRange(today.AddDays(-1), today.AddDays(-1)));
            ConfigurePresetButton(last7DaysButton, isEnglish ? "Last 7 days" : "최근 7일", (_, _) => SetRange(today.AddDays(-6), today));
            ConfigurePresetButton(thisWeekButton, isEnglish ? "This week" : "이번 주", (_, _) => SetRange(GetWeekStart(today), today));
            ConfigurePresetButton(lastWeekButton, isEnglish ? "Last week" : "지난주", (_, _) => SetRange(GetWeekStart(today).AddDays(-7), GetWeekStart(today).AddDays(-1)));
            ConfigurePresetButton(thisMonthButton, isEnglish ? "This month" : "이번 달", (_, _) => SetRange(new DateTime(today.Year, today.Month, 1), today));
            ConfigurePresetButton(lastMonthButton, isEnglish ? "Last month" : "지난달", (_, _) => SetRange(GetLastMonthStart(today), GetLastMonthEnd(today)));
            ConfigurePresetButton(thisYearButton, isEnglish ? "This year" : "올해", (_, _) => SetRange(new DateTime(today.Year, 1, 1), today));
            ConfigurePresetButton(lastYearButton, isEnglish ? "Last year" : "작년", (_, _) => SetRange(new DateTime(today.Year - 1, 1, 1), new DateTime(today.Year - 1, 12, 31)));
            presetPanel.Controls.Add(todayButton);
            presetPanel.Controls.Add(yesterdayButton);
            presetPanel.Controls.Add(last7DaysButton);
            presetPanel.Controls.Add(thisWeekButton);
            presetPanel.Controls.Add(lastWeekButton);
            presetPanel.Controls.Add(thisMonthButton);
            presetPanel.Controls.Add(lastMonthButton);
            presetPanel.Controls.Add(thisYearButton);
            presetPanel.Controls.Add(lastYearButton);

            startLabel.Dock = DockStyle.Fill;
            startLabel.Text = isEnglish ? "Start" : "시작일";
            startLabel.TextAlign = ContentAlignment.MiddleLeft;

            endLabel.Dock = DockStyle.Fill;
            endLabel.Text = isEnglish ? "End" : "종료일";
            endLabel.TextAlign = ContentAlignment.MiddleLeft;

            ConfigureDatePanel(startDatePanel, startDateTextBox, startCalendarButton, (_, _) => ShowDateCalendar(startCalendarButton, StartDate, ApplyStartDate));
            ConfigureDatePanel(endDatePanel, endDateTextBox, endCalendarButton, (_, _) => ShowDateCalendar(endCalendarButton, EndDate, ApplyEndDate));

            okButton.Text = isEnglish ? "OK" : "확인";
            okButton.Width = 84;
            okButton.Click += OnOkButtonClick;

            cancelButton.Text = isEnglish ? "Cancel" : "취소";
            cancelButton.Width = 84;
            cancelButton.DialogResult = DialogResult.Cancel;

            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(okButton);

            mainPanel.Controls.Add(presetPanel, 0, 0);
            mainPanel.SetColumnSpan(presetPanel, 2);
            mainPanel.Controls.Add(startLabel, 0, 1);
            mainPanel.Controls.Add(startDatePanel, 1, 1);
            mainPanel.Controls.Add(endLabel, 0, 2);
            mainPanel.Controls.Add(endDatePanel, 1, 2);
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

        private static void ConfigurePresetButton(Button button, string text, EventHandler clickHandler)
        {
            button.Text = text;
            button.AutoSize = true;
            button.Click += clickHandler;
        }

        private static DateTime GetLastMonthStart(DateTime today)
        {
            return new DateTime(today.Year, today.Month, 1).AddMonths(-1);
        }

        private static DateTime GetWeekStart(DateTime date)
        {
            var offset = date.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)date.DayOfWeek - 1;
            return date.Date.AddDays(-offset);
        }

        private static DateTime GetLastMonthEnd(DateTime today)
        {
            return new DateTime(today.Year, today.Month, 1).AddDays(-1);
        }

        private void SetRange(DateTime startDate, DateTime endDate)
        {
            StartDate = NormalizeSelectableDate(startDate);
            EndDate = NormalizeSelectableDate(endDate);
            UpdateDateTexts();
        }

        private void ApplyStartDate(DateTime date)
        {
            StartDate = NormalizeSelectableDate(date);
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
        }

        private DateTime NormalizeSelectableDate(DateTime date)
        {
            var normalizedDate = date.Date;
            if (normalizedDate > today)
                return today;

            return normalizedDate;
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
                    isEnglish ? "End date must be on or after start date." : "종료일은 시작일 이후여야 합니다.",
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
