using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal sealed class RecordedDatePickerPopup : UserControl
    {
        private const int DayCount = 42;

        private readonly Func<DateTime, DateTime, IReadOnlyList<DateTime>> getRecordedDates;
        private readonly Button previousYearButton = new();
        private readonly Button previousMonthButton = new();
        private readonly Button nextMonthButton = new();
        private readonly Button nextYearButton = new();
        private readonly Label monthLabel = new();
        private readonly BufferedTableLayoutPanel dayHeaderGrid = new();
        private readonly BufferedTableLayoutPanel dayGrid = new();
        private readonly BufferedTableLayoutPanel selectorGrid = new();
        private readonly Button applyButton = new();
        private readonly Button cancelButton = new();
        private readonly Label[] dayHeaderLabels = new Label[7];
        private readonly Button[] dayButtons = new Button[DayCount];
        private readonly Button[] selectorButtons = new Button[12];
        private readonly Dictionary<(int Year, int Month), bool> monthRecordCache = new();
        private readonly Dictionary<int, bool> yearRecordCache = new();
        private readonly Font boldFont;

        private DateTime visibleMonth;
        private DateTime selectedDate;
        private DateTime maxDate;
        private CalendarPickerViewMode viewMode = CalendarPickerViewMode.Day;
        private CalendarMonthModel? currentModel;

        public RecordedDatePickerPopup(
            DateTime selectedDate,
            DateTime maxDate,
            Func<DateTime, DateTime, IReadOnlyList<DateTime>> getRecordedDates)
        {
            this.selectedDate = selectedDate.Date;
            this.maxDate = maxDate.Date;
            this.visibleMonth = new DateTime(selectedDate.Year, selectedDate.Month, 1);
            this.getRecordedDates = getRecordedDates;
            this.boldFont = new Font(Font, FontStyle.Bold);

            BuildLayout();
            Render();
        }

        public event EventHandler<DateTime>? DateApplied;

        public event EventHandler? CloseRequested;

        private void BuildLayout()
        {
            AutoScaleMode = AutoScaleMode.None;
            BackColor = SystemColors.Window;
            BorderStyle = BorderStyle.FixedSingle;
            DoubleBuffered = true;
            Padding = new Padding(10);
            Size = new Size(368, 352);

            var root = new BufferedTableLayoutPanel
            {
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                RowCount = 4
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 222));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            Controls.Add(root);

            var header = new BufferedTableLayoutPanel
            {
                ColumnCount = 5,
                Dock = DockStyle.Fill
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));

            previousYearButton.Dock = DockStyle.Fill;
            previousYearButton.Text = "<<";
            previousYearButton.UseVisualStyleBackColor = true;
            previousYearButton.Click += (_, _) => ChangeMonth(-12);

            previousMonthButton.Dock = DockStyle.Fill;
            previousMonthButton.Text = "<";
            previousMonthButton.UseVisualStyleBackColor = true;
            previousMonthButton.Click += (_, _) => ChangeMonth(-1);

            monthLabel.Dock = DockStyle.Fill;
            monthLabel.Cursor = Cursors.Hand;
            monthLabel.Font = boldFont;
            monthLabel.TextAlign = ContentAlignment.MiddleCenter;
            monthLabel.Click += (_, _) => SwitchToParentView();

            nextMonthButton.Dock = DockStyle.Fill;
            nextMonthButton.Text = ">";
            nextMonthButton.UseVisualStyleBackColor = true;
            nextMonthButton.Click += (_, _) => ChangeMonth(1);

            nextYearButton.Dock = DockStyle.Fill;
            nextYearButton.Text = ">>";
            nextYearButton.UseVisualStyleBackColor = true;
            nextYearButton.Click += (_, _) => ChangeMonth(12);

            header.Controls.Add(previousYearButton, 0, 0);
            header.Controls.Add(previousMonthButton, 1, 0);
            header.Controls.Add(monthLabel, 2, 0);
            header.Controls.Add(nextMonthButton, 3, 0);
            header.Controls.Add(nextYearButton, 4, 0);
            root.Controls.Add(header, 0, 0);

            ConfigureDayGrid(dayHeaderGrid, 1);
            for (var column = 0; column < 7; column++)
            {
                var label = new Label
                {
                    Dock = DockStyle.Fill,
                    ForeColor = SystemColors.GrayText,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                dayHeaderLabels[column] = label;
                dayHeaderGrid.Controls.Add(label, column, 0);
            }
            root.Controls.Add(dayHeaderGrid, 0, 1);

            ConfigureDayGrid(dayGrid, 6);
            for (var index = 0; index < DayCount; index++)
            {
                var button = new Button
                {
                    Dock = DockStyle.Fill,
                    FlatStyle = FlatStyle.Flat,
                    Margin = new Padding(1),
                    UseVisualStyleBackColor = false,
                    Tag = index
                };
                button.FlatAppearance.BorderSize = 1;
                button.Click += OnDayButtonClick;
                dayButtons[index] = button;
                dayGrid.Controls.Add(button, index % 7, index / 7);
            }
            root.Controls.Add(dayGrid, 0, 2);

            selectorGrid.ColumnCount = 3;
            selectorGrid.Dock = DockStyle.Fill;
            selectorGrid.RowCount = 4;
            selectorGrid.Visible = false;
            for (var column = 0; column < 3; column++)
                selectorGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3f));

            for (var row = 0; row < 4; row++)
                selectorGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

            for (var index = 0; index < selectorButtons.Length; index++)
            {
                var button = new Button
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(3),
                    UseVisualStyleBackColor = true,
                    Tag = index
                };
                button.Click += OnSelectorButtonClick;
                selectorButtons[index] = button;
                selectorGrid.Controls.Add(button, index % 3, index / 3);
            }
            root.Controls.Add(selectorGrid, 0, 2);

            var footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 6, 0, 0),
                WrapContents = false
            };

            applyButton.AutoSize = false;
            applyButton.Size = new Size(76, 25);
            applyButton.Text = UiText.Common.Apply;
            applyButton.UseVisualStyleBackColor = true;
            applyButton.Click += (_, _) => DateApplied?.Invoke(this, selectedDate);

            cancelButton.AutoSize = false;
            cancelButton.Size = new Size(76, 25);
            cancelButton.Text = UiText.Common.Cancel;
            cancelButton.UseVisualStyleBackColor = true;
            cancelButton.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);

            footer.Controls.Add(applyButton);
            footer.Controls.Add(cancelButton);
            root.Controls.Add(footer, 0, 3);
        }

        private static void ConfigureDayGrid(TableLayoutPanel grid, int rowCount)
        {
            grid.ColumnCount = 7;
            grid.Dock = DockStyle.Fill;
            grid.RowCount = rowCount;
            grid.Margin = Padding.Empty;
            grid.Padding = Padding.Empty;

            for (var column = 0; column < 7; column++)
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 7f));

            for (var row = 0; row < rowCount; row++)
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rowCount));
        }

        private void Render()
        {
            SuspendLayout();
            dayGrid.SuspendLayout();
            selectorGrid.SuspendLayout();
            try
            {
                RenderCurrentView();
                applyButton.Text = UiText.Common.Apply;
                cancelButton.Text = UiText.Common.Cancel;
            }
            finally
            {
                selectorGrid.ResumeLayout();
                dayGrid.ResumeLayout();
                ResumeLayout();
            }
        }

        private void RenderCurrentView()
        {
            dayHeaderGrid.Visible = viewMode == CalendarPickerViewMode.Day;
            dayGrid.Visible = viewMode == CalendarPickerViewMode.Day;
            selectorGrid.Visible = viewMode != CalendarPickerViewMode.Day;

            switch (viewMode)
            {
                case CalendarPickerViewMode.Month:
                    RenderMonthSelector();
                    break;
                case CalendarPickerViewMode.Year:
                    RenderYearSelector();
                    break;
                default:
                    RenderDaySelector();
                    break;
            }
        }

        private void RenderDaySelector()
        {
            var rangeStart = visibleMonth.AddDays(-(int)visibleMonth.DayOfWeek);
            var rangeEnd = rangeStart.AddDays(DayCount);
            var recordedDates = getRecordedDates(rangeStart, rangeEnd)
                .Select(date => date.Date)
                .ToHashSet();

            currentModel = CalendarMonthModel.Create(visibleMonth, selectedDate, DateTime.Today, recordedDates);
            monthLabel.Text = UiText.CalendarPicker.MonthTitle(currentModel.FirstDayOfMonth);
            RenderDayHeaders();
            RenderDays(currentModel);
            SetNavigationState(
                canGoPrevious: visibleMonth > DateTime.MinValue.AddMonths(1),
                canGoNext: visibleMonth.AddMonths(1) <= new DateTime(maxDate.Year, maxDate.Month, 1));
        }

        private void RenderMonthSelector()
        {
            monthLabel.Text = visibleMonth.Year.ToString();
            var maxMonth = new DateTime(maxDate.Year, maxDate.Month, 1);

            for (var index = 0; index < selectorButtons.Length; index++)
            {
                var month = index + 1;
                var candidate = new DateTime(visibleMonth.Year, month, 1);
                var hasRecords = HasRecordsInMonth(visibleMonth.Year, month);
                var button = selectorButtons[index];
                button.Text = hasRecords
                    ? $"{UiText.CalendarPicker.MonthName(month)} *"
                    : UiText.CalendarPicker.MonthName(month);
                button.Tag = month;
                button.Enabled = candidate <= maxMonth;
                button.Font = (month == selectedDate.Month && visibleMonth.Year == selectedDate.Year) || hasRecords
                    ? boldFont
                    : Font;
            }

            SetNavigationState(
                canGoPrevious: visibleMonth.Year > 1,
                canGoNext: visibleMonth.Year < maxDate.Year);
        }

        private void RenderYearSelector()
        {
            var startYear = GetYearRangeStart(visibleMonth.Year);
            var endYear = startYear + 11;
            monthLabel.Text = $"{startYear} - {endYear}";

            for (var index = 0; index < selectorButtons.Length; index++)
            {
                var year = startYear + index;
                var hasRecords = HasRecordsInYear(year);
                var button = selectorButtons[index];
                button.Text = hasRecords ? $"{year} *" : year.ToString();
                button.Tag = year;
                button.Enabled = year <= maxDate.Year;
                button.Font = year == selectedDate.Year || hasRecords ? boldFont : Font;
            }

            SetNavigationState(
                canGoPrevious: startYear > 1,
                canGoNext: endYear < maxDate.Year);
        }

        private void RenderDayHeaders()
        {
            var dayNames = GetDayNames();
            for (var column = 0; column < 7; column++)
            {
                if (dayHeaderLabels[column].Text != dayNames[column])
                    dayHeaderLabels[column].Text = dayNames[column];
            }
        }

        private void RenderDays(CalendarMonthModel model)
        {
            for (var index = 0; index < DayCount; index++)
            {
                var day = model.Days[index];
                var button = dayButtons[index];
                button.Enabled = day.Date <= maxDate;
                button.Text = day.HasRecord
                    ? $"{day.Date.Day} *"
                    : day.Date.Day.ToString();
                button.Tag = day;
                button.ForeColor = GetDayForeColor(day);
                button.BackColor = GetDayBackColor(day);
                button.FlatAppearance.BorderColor = GetDayBorderColor(day);
                button.Font = day.HasRecord ? boldFont : Font;
            }
        }

        private static Color GetDayForeColor(CalendarDayCell day)
        {
            if (!day.IsCurrentMonth)
                return SystemColors.GrayText;

            return day.Date.DayOfWeek is DayOfWeek.Sunday
                ? Color.Firebrick
                : SystemColors.ControlText;
        }

        private static Color GetDayBackColor(CalendarDayCell day)
        {
            if (day.IsSelected)
                return SystemColors.Highlight;

            return day.IsToday
                ? Color.FromArgb(232, 242, 255)
                : SystemColors.Window;
        }

        private static Color GetDayBorderColor(CalendarDayCell day)
        {
            if (day.IsSelected)
                return SystemColors.Highlight;

            return day.IsToday ? Color.SteelBlue : SystemColors.ControlLight;
        }

        private void OnDayButtonClick(object? sender, EventArgs e)
        {
            if (sender is not Button { Tag: CalendarDayCell day } || day.Date > maxDate)
                return;

            selectedDate = day.Date;
            if (day.Date.Month != visibleMonth.Month || day.Date.Year != visibleMonth.Year)
                visibleMonth = new DateTime(day.Date.Year, day.Date.Month, 1);

            Render();
        }

        private void ChangeMonth(int delta)
        {
            if (viewMode == CalendarPickerViewMode.Month)
            {
                visibleMonth = visibleMonth.AddYears(delta < 0 ? -1 : 1);
                if (visibleMonth > maxDate)
                    visibleMonth = new DateTime(maxDate.Year, maxDate.Month, 1);

                Render();
                return;
            }

            if (viewMode == CalendarPickerViewMode.Year)
            {
                visibleMonth = visibleMonth.AddYears(delta < 0 ? -12 : 12);
                if (visibleMonth > maxDate)
                    visibleMonth = new DateTime(maxDate.Year, maxDate.Month, 1);

                Render();
                return;
            }

            visibleMonth = visibleMonth.AddMonths(delta);
            if (selectedDate.Year == visibleMonth.Year && selectedDate.Month == visibleMonth.Month)
            {
                Render();
                return;
            }

            var day = Math.Min(selectedDate.Day, DateTime.DaysInMonth(visibleMonth.Year, visibleMonth.Month));
            selectedDate = new DateTime(visibleMonth.Year, visibleMonth.Month, day);
            if (selectedDate > maxDate)
                selectedDate = maxDate;

            Render();
        }

        private static IReadOnlyList<string> GetDayNames() => UiText.CalendarPicker.DayNames;

        private bool HasRecordsInMonth(int year, int month)
        {
            var key = (year, month);
            if (monthRecordCache.TryGetValue(key, out var cached))
                return cached;

            var monthStart = new DateTime(year, month, 1);
            if (monthStart > maxDate)
            {
                monthRecordCache[key] = false;
                return false;
            }

            var monthEnd = monthStart.AddMonths(1);
            var hasRecords = getRecordedDates(monthStart, monthEnd).Count > 0;
            monthRecordCache[key] = hasRecords;
            return hasRecords;
        }

        private bool HasRecordsInYear(int year)
        {
            if (yearRecordCache.TryGetValue(year, out var cached))
                return cached;

            if (year > maxDate.Year)
            {
                yearRecordCache[year] = false;
                return false;
            }

            var lastMonth = year == maxDate.Year ? maxDate.Month : 12;
            for (var month = 1; month <= lastMonth; month++)
            {
                if (HasRecordsInMonth(year, month))
                {
                    yearRecordCache[year] = true;
                    return true;
                }
            }

            yearRecordCache[year] = false;
            return false;
        }

        private void OnSelectorButtonClick(object? sender, EventArgs e)
        {
            if (sender is not Button { Tag: int value } || !((Button)sender).Enabled)
                return;

            if (viewMode == CalendarPickerViewMode.Year)
            {
                visibleMonth = new DateTime(value, visibleMonth.Month, 1);
                if (visibleMonth > maxDate)
                    visibleMonth = new DateTime(maxDate.Year, maxDate.Month, 1);

                viewMode = CalendarPickerViewMode.Month;
                Render();
                return;
            }

            var day = Math.Min(selectedDate.Day, DateTime.DaysInMonth(visibleMonth.Year, value));
            selectedDate = new DateTime(visibleMonth.Year, value, day);
            if (selectedDate > maxDate)
                selectedDate = maxDate;

            visibleMonth = new DateTime(selectedDate.Year, selectedDate.Month, 1);
            viewMode = CalendarPickerViewMode.Day;
            Render();
        }

        private void SwitchToParentView()
        {
            viewMode = viewMode switch
            {
                CalendarPickerViewMode.Day => CalendarPickerViewMode.Month,
                CalendarPickerViewMode.Month => CalendarPickerViewMode.Year,
                _ => CalendarPickerViewMode.Year
            };
            Render();
        }

        private void SetNavigationState(bool canGoPrevious, bool canGoNext)
        {
            previousMonthButton.Enabled = canGoPrevious;
            previousYearButton.Enabled = canGoPrevious;
            nextMonthButton.Enabled = canGoNext;
            nextYearButton.Enabled = canGoNext;
        }

        private static int GetYearRangeStart(int year)
        {
            return Math.Max(1, ((year - 1) / 12 * 12) + 1);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                boldFont.Dispose();

            base.Dispose(disposing);
        }
    }

    internal enum CalendarPickerViewMode
    {
        Day,
        Month,
        Year
    }

    internal sealed class BufferedTableLayoutPanel : TableLayoutPanel
    {
        public BufferedTableLayoutPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }
}
