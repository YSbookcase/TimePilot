using System.ComponentModel;
using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Timeline
{
    internal static class TimelinePopupFactory
    {
        public static Form CreateCategorySegmentStatsPopup(
            Icon? icon,
            string description,
            IReadOnlyList<UsageSummaryRow> rows)
        {
            var popup = CreatePopup(
                TimelineCategorySegmentStatsPresenter.GetTitle(),
                new Size(720, 300),
                icon);
            var label = CreateDescriptionLabel(description, 64);
            var grid = CreateReadOnlyGrid();

            grid.Columns.AddRange(
                CreateTextColumn(nameof(UsageSummaryRow.AppName), UiText.Main.App, 180),
                CreateTextColumn(nameof(UsageSummaryRow.CategoryText), UiText.Main.Category, 120),
                CreateTextColumn(nameof(UsageSummaryRow.ActiveUsageTimeText), UiText.Main.ActiveUsageTime, 120),
                CreateTextColumn(nameof(UsageSummaryRow.UsageRatioText), UiText.Main.ActiveRatio, 90),
                CreateTextColumn(nameof(UsageSummaryRow.SwitchCountText), UiText.Main.SwitchCount, 90),
                CreateTextColumn(nameof(UsageSummaryRow.FirstStartedAtText), UiText.Main.FirstStartedAt, 110),
                CreateTextColumn(nameof(UsageSummaryRow.LastObservedAtText), UiText.Main.LastObservedAt, 110));
            grid.ColumnHeaderMouseClick += OnCategoryStatsGridColumnHeaderMouseClick;
            grid.DataSource = rows;

            popup.Controls.Add(grid);
            popup.Controls.Add(label);
            return popup;
        }

        public static Form CreateSystemEventsPopup(
            Icon? icon,
            DateTime selectedDate,
            IReadOnlyList<SystemTimelineEvent> events)
        {
            var popup = CreatePopup(
                SystemTimelineEventTextFormatter.GetListTitle(selectedDate),
                new Size(720, 260),
                icon);
            var label = CreateDescriptionLabel(
                SystemTimelineEventTextFormatter.GetListDescription(selectedDate),
                28);
            var grid = CreateReadOnlyGrid();

            grid.Columns.AddRange(
                CreateTextColumn(nameof(SystemTimelineEventRow.OccurredAtText), SystemTimelineEventTextFormatter.GetTimeHeaderText(), 90),
                CreateTextColumn(nameof(SystemTimelineEventRow.EventTypeText), UiText.Main.Type, 110),
                CreateTextColumn(nameof(SystemTimelineEventRow.PreviousIntervalText), SystemTimelineEventTextFormatter.GetPreviousIntervalHeaderText(), 110),
                CreateTextColumn(nameof(SystemTimelineEventRow.RelationText), SystemTimelineEventTextFormatter.GetRelationHeaderText(), 150),
                CreateTextColumn(nameof(SystemTimelineEventRow.DetailsText), SystemTimelineEventTextFormatter.GetDetailsHeaderText(), 220));
            grid.ColumnHeaderMouseClick += OnSystemEventsGridColumnHeaderMouseClick;
            grid.DataSource = TimelineSystemEventPresenter.BuildRows(events);

            popup.Controls.Add(grid);
            popup.Controls.Add(label);
            return popup;
        }

        public static Point GetPopupLocation(Control owner, Point location, Size popupSize)
        {
            var screenLocation = owner.PointToScreen(location);
            var requestedLocation = new Point(screenLocation.X + 8, screenLocation.Y + 8);
            var workingArea = Screen.FromPoint(requestedLocation).WorkingArea;
            var x = Math.Min(requestedLocation.X, workingArea.Right - popupSize.Width);
            var y = Math.Min(requestedLocation.Y, workingArea.Bottom - popupSize.Height);
            return new Point(Math.Max(workingArea.Left, x), Math.Max(workingArea.Top, y));
        }

        private static Form CreatePopup(string title, Size size, Icon? icon)
        {
            return new Form
            {
                Text = title,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Size = size,
                MinimizeBox = false,
                MaximizeBox = false,
                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                Icon = icon
            };
        }

        private static Label CreateDescriptionLabel(string text, int height)
        {
            return new Label
            {
                Dock = DockStyle.Top,
                Height = height,
                Padding = new Padding(8, 6, 8, 0),
                Text = text
            };
        }

        private static DataGridView CreateReadOnlyGrid()
        {
            return new BufferedDataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToOrderColumns = true,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                Dock = DockStyle.Fill,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                ScrollBars = ScrollBars.Both,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
        }

        private static DataGridViewTextBoxColumn CreateTextColumn(
            string propertyName,
            string headerText,
            int width)
        {
            return new DataGridViewTextBoxColumn
            {
                DataPropertyName = propertyName,
                HeaderText = headerText,
                Name = propertyName,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.Programmatic,
                Width = width
            };
        }

        private static void OnCategoryStatsGridColumnHeaderMouseClick(
            object? sender,
            DataGridViewCellMouseEventArgs e)
        {
            SortGrid<UsageSummaryRow>(
                sender,
                e,
                TimelineCategorySegmentStatsPresenter.SortRows);
        }

        private static void OnSystemEventsGridColumnHeaderMouseClick(
            object? sender,
            DataGridViewCellMouseEventArgs e)
        {
            SortGrid<SystemTimelineEventRow>(
                sender,
                e,
                TimelineSystemEventPresenter.SortRows);
        }

        private static void SortGrid<TRow>(
            object? sender,
            DataGridViewCellMouseEventArgs e,
            Func<IReadOnlyList<TRow>, string, ListSortDirection, IReadOnlyList<TRow>> sortRows)
        {
            if (sender is not DataGridView grid
                || e.ColumnIndex < 0
                || e.ColumnIndex >= grid.Columns.Count)
                return;

            var column = grid.Columns[e.ColumnIndex];
            var direction = column.HeaderCell.SortGlyphDirection == SortOrder.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
            var rows = grid.Rows
                .Cast<DataGridViewRow>()
                .Select(row => row.DataBoundItem)
                .OfType<TRow>()
                .ToList();

            grid.DataSource = sortRows(rows, column.DataPropertyName, direction);
            foreach (DataGridViewColumn gridColumn in grid.Columns)
            {
                if (gridColumn.SortMode != DataGridViewColumnSortMode.Programmatic)
                    continue;

                gridColumn.HeaderCell.SortGlyphDirection = gridColumn.Index == e.ColumnIndex
                    ? direction == ListSortDirection.Ascending
                        ? SortOrder.Ascending
                        : SortOrder.Descending
                    : SortOrder.None;
            }
        }
    }
}
