using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Tables;
using TimePilot.WinForms.Timeline;

namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private TimelineSelectorCoordinator CreateTimelineSelectorCoordinator()
        {
            return new TimelineSelectorCoordinator(new TimelineSelectorControls(
                timelineZoomPanel,
                timelineCategoryBucketLabel,
                timelineCategoryBucketComboBox,
                timelineTypeHighlightLabel,
                timelineTypeHighlightComboBox,
                timelineSystemEventFilterLabel,
                timelineSystemEventFilterComboBox));
        }

        private TimelineZoomCoordinator CreateTimelineZoomCoordinator()
        {
            return new TimelineZoomCoordinator(
                new TimelineZoomControls(
                    timelineZoomRangeLabel,
                    timelineZoomOutButton,
                    timelineZoomInButton,
                    timelineZoomPreviousButton,
                    timelineZoomNextButton,
                    timelineZoomResetButton,
                    timelineZoomScrollBar),
                new TimelineZoomActions(
                    timelineOverviewControl.ZoomOut,
                    timelineOverviewControl.ZoomIn,
                    timelineOverviewControl.PanPrevious,
                    timelineOverviewControl.PanNext,
                    timelineOverviewControl.ResetView,
                    timelineOverviewControl.SetViewStartRatio),
                () => new TimelineZoomState(
                    timelineOverviewControl.ViewRangeText,
                    timelineOverviewControl.IsZoomed,
                    timelineOverviewControl.CanPanPrevious,
                    timelineOverviewControl.CanPanNext,
                    timelineOverviewControl.ViewWidthRatio,
                    timelineOverviewControl.ViewStartRatio));
        }

        private void OnTimelineGridColumnHeaderMouseClick(
            object? sender,
            DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0)
                return;

            var propertyName = GridSortPropertyResolver.GetTimelineSortPropertyName(
                timelineGrid.Columns[e.ColumnIndex].Name);
            if (propertyName is null)
                return;

            timelineSortOrder = string.Equals(
                timelineSortProperty,
                propertyName,
                StringComparison.Ordinal)
                ? GridSortOrderHelper.Toggle(timelineSortOrder)
                : SortOrder.Descending;
            timelineSortProperty = propertyName;
            SaveTableSortState();
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnTimelineDatePickerValueChanged(object? sender, EventArgs e)
        {
            if (!isInitializingDateSelectors)
                ApplyTimelineDate(timelineDatePicker.Value.Date);
        }

        private void OnTimelineCalendarButtonClick(object? sender, EventArgs e)
        {
            ShowRecordedDateCalendar(
                timelineCalendarButton,
                selectedTimelineDate,
                ApplyTimelineDate);
        }

        private void OnTimelinePreviousDateButtonClick(object? sender, EventArgs e)
        {
            ApplyTimelineDate(selectedTimelineDate.AddDays(-1));
        }

        private void OnTimelineNextDateButtonClick(object? sender, EventArgs e)
        {
            ApplyTimelineDate(selectedTimelineDate.AddDays(1));
        }

        private void OnTimelineTodayButtonClick(object? sender, EventArgs e)
        {
            ApplyTimelineDate(DateTime.Today);
        }

        private void OnTimelineOverviewViewRangeChanged(object? sender, EventArgs e)
        {
            UpdateTimelineZoomControls();
        }

        private void OnTimelineHelpButtonClick(object? sender, EventArgs e)
        {
            CenteredMessageDialog.Show(
                this,
                UiText.Main.TimelineHelpMessage,
                UiText.Main.TimelineHelpTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void OnTimelineHighlightClearButtonClick(object? sender, EventArgs e)
        {
            ClearTimelineHighlights(resetTypeHighlight: true);
        }

        private void OnTimelineCategoryBucketComboBoxSelectedIndexChanged(
            object? sender,
            EventArgs e)
        {
            if (timelineCategoryBucketComboBox.SelectedItem is not TimelineCategoryBucketOption option
                || option.Minutes == selectedTimelineCategoryBucketMinutes)
                return;

            selectedTimelineCategoryBucketMinutes = option.Minutes;
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnTimelineTypeHighlightComboBoxSelectedIndexChanged(
            object? sender,
            EventArgs e)
        {
            if (timelineTypeHighlightComboBox.SelectedItem
                    is not TimelineActivityTypeHighlightOption option
                || option.Value == selectedTimelineActivityTypeHighlight)
                return;

            selectedTimelineActivityTypeHighlight = option.Value;
            if (selectedTimelineActivityTypeHighlight == TimelineActivityTypeHighlight.None)
            {
                ClearTimelineHighlights(resetTypeHighlight: false);
                return;
            }

            ApplyTimelineActivityTypeHighlight();
            timelineGrid.Invalidate();
        }

        private void OnTimelineSystemEventFilterComboBoxSelectedIndexChanged(
            object? sender,
            EventArgs e)
        {
            if (timelineSystemEventFilterComboBox.SelectedItem
                    is not TimelineSystemEventFilterOption option
                || option.Value == selectedTimelineSystemEventFilter)
                return;

            selectedTimelineSystemEventFilter = option.Value;
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnTimelineTypeHighlightComboBoxDropDownClosed(object? sender, EventArgs e)
        {
            if (timelineTypeHighlightComboBox.SelectedItem is TimelineActivityTypeHighlightOption
                {
                    Value: TimelineActivityTypeHighlight.None
                })
            {
                ClearTimelineHighlights(resetTypeHighlight: false);
            }
        }

        private void OnTimelineGridCellMouseDown(
            object? sender,
            DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0)
                return;

            if (SelectGridRow<ActivityTimelineRow>(
                    timelineGrid,
                    e.RowIndex,
                    e.ColumnIndex) is not { } row
                || string.IsNullOrWhiteSpace(row.ProcessName))
                return;

            ShowTimelineActivityContextMenu(
                row,
                timelineGrid,
                timelineGrid.PointToClient(Cursor.Position));
        }

        private void OnTimelineOverviewActivitySegmentContextRequested(
            object? sender,
            TimelineActivitySegmentContextEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Row.ProcessName))
                ShowTimelineActivityContextMenu(e.Row, timelineOverviewControl, e.Location);
        }

        private void OnTimelineOverviewCategorySegmentContextRequested(
            object? sender,
            TimelineCategorySegmentContextEventArgs e)
        {
            ShowTimelineCategorySegmentContextMenu(
                e.Segment,
                timelineOverviewControl,
                e.Location);
        }

        private void OnTimelineOverviewWindowsTrackContextRequested(
            object? sender,
            TimelineWindowsTrackContextEventArgs e)
        {
            ShowTimelineWindowsContextMenu(timelineOverviewControl, e.Location);
        }

        private void ShowTimelineCategorySegmentContextMenu(
            CategoryTimelineSegment segment,
            Control owner,
            Point location)
        {
            timelineGridMenu.Items.Clear();
            var item = new ToolStripMenuItem(
                TimelineCategorySegmentStatsPresenter.GetMenuText());
            item.Click += (_, _) =>
                ShowTimelineCategorySegmentAppStatsPopup(segment, owner, location);
            timelineGridMenu.Items.Add(item);
            timelineGridMenu.Show(owner, location);
        }

        private void ShowTimelineCategorySegmentAppStatsPopup(
            CategoryTimelineSegment segment,
            Control owner,
            Point location)
        {
            var rows = UsageSummaryRowBuilder.FromForegroundUsage(
                storage?.GetForegroundUsageForPeriod(segment.StartedAt, segment.EndedAt)
                    ?? Array.Empty<ForegroundUsageSummary>());
            var description = TimelineCategorySegmentStatsPresenter.BuildDescription(
                segment,
                currentTimelineRows,
                currentTimelineWindowsRuntimeRanges,
                currentTimelineSystemRanges);
            var popup = TimelinePopupFactory.CreateCategorySegmentStatsPopup(
                Icon,
                description,
                rows);
            popup.Location = TimelinePopupFactory.GetPopupLocation(owner, location, popup.Size);
            popup.Show(this);
        }

        private void ShowTimelineWindowsContextMenu(Control owner, Point location)
        {
            timelineGridMenu.Items.Clear();
            var item = new ToolStripMenuItem(
                SystemTimelineEventTextFormatter.GetListTitle(selectedTimelineDate));
            item.Click += (_, _) => ShowTimelineSystemEventsPopup(owner, location);
            timelineGridMenu.Items.Add(item);
            timelineGridMenu.Show(owner, location);
        }

        private void ShowTimelineSystemEventsPopup(Control owner, Point location)
        {
            var popup = TimelinePopupFactory.CreateSystemEventsPopup(
                Icon,
                selectedTimelineDate,
                currentTimelineSystemEvents);
            popup.Location = TimelinePopupFactory.GetPopupLocation(owner, location, popup.Size);
            popup.Show(this);
        }

        private void ShowTimelineActivityContextMenu(
            ActivityTimelineRow row,
            Control owner,
            Point location)
        {
            timelineGridMenu.Items.Clear();
            var segmentItem = new ToolStripMenuItem(GetHighlightTimelineSegmentMenuText());
            segmentItem.Click += (_, _) => HighlightTimelineSegment(row);
            timelineGridMenu.Items.Add(segmentItem);
            var appItem = new ToolStripMenuItem(GetHighlightTimelineAppMenuText());
            appItem.Click += (_, _) => HighlightTimelineRow(row);
            timelineGridMenu.Items.Add(appItem);
            if (row.AppId is { } appId)
            {
                timelineGridMenu.Items.Add(CreateSetCategoryMenuItem(
                    row.PrimaryCategoryId,
                    categoryId => SetAppCategory(appId, row.DisplayName, categoryId)));
            }

            timelineGridMenu.Items.Add(
                CreateSearchWebMenuItem(row.DisplayName, row.ProcessName));
            if (timelineHighlightState.HasSegmentHighlight
                || string.Equals(
                    row.ProcessName,
                    timelineHighlightState.ProcessName,
                    StringComparison.OrdinalIgnoreCase))
            {
                var clearItem = new ToolStripMenuItem(UiText.Main.ClearTimelineHighlight);
                clearItem.Click += (_, _) => ClearTimelineHighlights(resetTypeHighlight: true);
                timelineGridMenu.Items.Add(clearItem);
            }

            timelineGridMenu.Show(owner, location);
        }

        private void ClearTimelineHighlights(bool resetTypeHighlight)
        {
            timelineHighlightState = TimelineHighlightState.Empty;
            timelineOverviewControl.SetHighlightedProcessName(null);
            timelineOverviewControl.SetHighlightedActivitySegment(null);
            if (resetTypeHighlight)
                SetTimelineTypeHighlight(TimelineActivityTypeHighlight.None);

            ApplyTimelineActivityTypeHighlight();
            UpdateTimelineHighlightUi();
            timelineGrid.Invalidate();
        }

        private void SetTimelineTypeHighlight(TimelineActivityTypeHighlight value)
        {
            selectedTimelineActivityTypeHighlight = value;
            for (var index = 0; index < timelineTypeHighlightComboBox.Items.Count; index++)
            {
                if (timelineTypeHighlightComboBox.Items[index]
                        is TimelineActivityTypeHighlightOption option
                    && option.Value == value)
                {
                    timelineTypeHighlightComboBox.SelectedIndex = index;
                    break;
                }
            }
        }

        private void HighlightUsageRowInTimeline(UsageSummaryRow? row)
        {
            if (row is null || string.IsNullOrWhiteSpace(row.ProcessName))
                return;

            timelineHighlightState = TimelineHighlightState.ForApp(row.ProcessName, row.AppName);
            selectedTimelineDate = GetTimelineDateForSummarySelection(row);
            SetDatePickerValue(timelineDatePicker, selectedTimelineDate);
            mainTabs.SelectedTab = timelineTab;
            timelineOverviewControl.SetHighlightedProcessName(timelineHighlightState.ProcessName);
            UpdateTimelineHighlightUi();
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void HighlightTimelineRow(ActivityTimelineRow? row)
        {
            if (row is null || string.IsNullOrWhiteSpace(row.ProcessName))
                return;

            timelineHighlightState = TimelineHighlightState.ForApp(
                row.ProcessName,
                row.DisplayName);
            timelineOverviewControl.SetHighlightedProcessName(timelineHighlightState.ProcessName);
            UpdateTimelineHighlightUi();
            timelineGrid.Invalidate();
        }

        private void HighlightTimelineSegment(ActivityTimelineRow? row)
        {
            if (row is null)
                return;

            SetTimelineTypeHighlight(TimelineActivityTypeHighlight.None);
            timelineHighlightState = TimelineHighlightState.ForSegment(row);
            timelineOverviewControl.SetHighlightedActivitySegment(row);
            UpdateTimelineHighlightUi();
            timelineGrid.Invalidate();
        }

        private static string GetHighlightTimelineSegmentMenuText()
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? "Highlight this segment"
                : "이 구간 강조";
        }

        private static string GetHighlightTimelineAppMenuText()
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? "Highlight this app"
                : "이 앱 강조";
        }

        private DateTime GetTimelineDateForSummarySelection(UsageSummaryRow row)
        {
            if (selectedSummaryPeriod == SummaryPeriod.SpecificDate)
                return selectedSummarySpecificDate;
            if (selectedSummaryPeriod == SummaryPeriod.Today)
                return DateTime.Today;

            return row.LastObservedAt?.ToLocalTime().Date
                ?? row.FirstStartedAt?.ToLocalTime().Date
                ?? selectedTimelineDate;
        }

        private void UpdateTimelineZoomControls()
        {
            timelineZoomCoordinator.Update();
        }

        private void UpdateTimelineHighlightUi()
        {
            var hasHighlight = timelineHighlightState.HasHighlight;
            timelineHighlightLabel.Visible = hasHighlight;
            timelineHighlightClearButton.Visible = hasHighlight;
            timelineHighlightHintLabel.Visible = !hasHighlight;
            timelineHighlightLabel.Text =
                hasHighlight ? timelineHighlightState.GetDisplayText() : "";
            UpdateTimelineHighlightSummary();
        }

        private string? GetTimelineHighlightedActivityTypeText()
        {
            return TimelineHighlightMatcher.GetActivityTypeText(
                selectedTimelineActivityTypeHighlight);
        }

        private void ApplyTimelineActivityTypeHighlight()
        {
            timelineOverviewControl.SetWindowsHighlighted(
                selectedTimelineActivityTypeHighlight == TimelineActivityTypeHighlight.Windows);
            if (selectedTimelineActivityTypeHighlight != TimelineActivityTypeHighlight.Windows)
            {
                timelineOverviewControl.SetHighlightedActivityType(
                    GetTimelineHighlightedActivityTypeText());
            }
        }

        private void ApplyTimelineHighlightToOverview()
        {
            if (timelineHighlightState.SegmentKey is { } segmentKey)
            {
                var highlightedRow =
                    currentTimelineRows.FirstOrDefault(row => segmentKey.Matches(row));
                if (highlightedRow is not null)
                {
                    timelineHighlightState = TimelineHighlightState.ForSegment(highlightedRow);
                    timelineOverviewControl.SetHighlightedActivitySegment(highlightedRow);
                    return;
                }

                timelineHighlightState = TimelineHighlightState.Empty;
            }

            if (!string.IsNullOrWhiteSpace(timelineHighlightState.ProcessName))
            {
                timelineOverviewControl.SetHighlightedProcessName(
                    timelineHighlightState.ProcessName);
                return;
            }

            ApplyTimelineActivityTypeHighlight();
        }

        private bool HasTimelineHighlight()
        {
            return TimelineHighlightMatcher.HasHighlight(
                timelineHighlightState,
                selectedTimelineActivityTypeHighlight);
        }

        private bool IsTimelineRowHighlighted(ActivityTimelineRow row)
        {
            return TimelineHighlightMatcher.IsRowHighlighted(
                row,
                timelineHighlightState,
                selectedTimelineActivityTypeHighlight);
        }

        private void UpdateTimelineHighlightSummary()
        {
            var summaryText = TimelineHighlightSummaryBuilder.Build(
                timelineHighlightState,
                currentTimelineForegroundUsage,
                currentTimelineRows,
                RuntimeDiagnosticsMessageBuilder.FormatDuration);
            if (summaryText is null)
            {
                timelineHighlightSummaryPanel.Visible = false;
                timelineHighlightSummaryLabel.Text = "";
                return;
            }

            timelineHighlightSummaryLabel.Text = summaryText;
            timelineHighlightSummaryPanel.Visible = true;
        }

        private void OnTimelineGridRowPrePaint(
            object? sender,
            DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= timelineGrid.Rows.Count)
                return;

            var gridRow = timelineGrid.Rows[e.RowIndex];
            if (!HasTimelineHighlight()
                || selectedTimelineActivityTypeHighlight == TimelineActivityTypeHighlight.Windows)
            {
                ResetTimelineRowStyle(gridRow);
                return;
            }

            if (gridRow.DataBoundItem is not ActivityTimelineRow row)
                return;

            var isHighlighted = IsTimelineRowHighlighted(row);
            if (timelineHighlightState.HasSegmentHighlight && !isHighlighted)
            {
                ResetTimelineRowStyle(gridRow);
                return;
            }

            gridRow.DefaultCellStyle.ForeColor =
                isHighlighted ? SystemColors.WindowText : SystemColors.GrayText;
            gridRow.DefaultCellStyle.BackColor =
                isHighlighted ? Color.FromArgb(218, 235, 255) : SystemColors.Window;
            gridRow.DefaultCellStyle.SelectionForeColor =
                isHighlighted ? SystemColors.WindowText : SystemColors.GrayText;
            gridRow.DefaultCellStyle.SelectionBackColor = isHighlighted
                ? Color.FromArgb(198, 224, 255)
                : Color.FromArgb(245, 245, 245);
            gridRow.DefaultCellStyle.Font =
                isHighlighted ? GetTimelineHighlightedRowFont() : null;
        }

        private static void ResetTimelineRowStyle(DataGridViewRow row)
        {
            row.DefaultCellStyle.ForeColor = SystemColors.WindowText;
            row.DefaultCellStyle.BackColor = SystemColors.Window;
            row.DefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;
            row.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            row.DefaultCellStyle.Font = null;
        }

        private void OnTimelineGridRowPostPaint(
            object? sender,
            DataGridViewRowPostPaintEventArgs e)
        {
            if (!HasTimelineHighlight()
                || selectedTimelineActivityTypeHighlight == TimelineActivityTypeHighlight.Windows
                || e.RowIndex < 0
                || e.RowIndex >= timelineGrid.Rows.Count
                || timelineGrid.Rows[e.RowIndex].DataBoundItem is not ActivityTimelineRow row
                || !IsTimelineRowHighlighted(row))
                return;

            var bounds = GetVisibleTimelineRowCellsBounds(e.RowIndex);
            if (bounds.IsEmpty)
                return;

            var stripeBounds = new Rectangle(
                bounds.Left,
                bounds.Top + 1,
                5,
                Math.Max(1, bounds.Height - 2));
            using var stripeBrush = new SolidBrush(Color.FromArgb(28, 91, 170));
            using var borderPen = new Pen(Color.FromArgb(28, 91, 170));
            e.Graphics.FillRectangle(stripeBrush, stripeBounds);
            e.Graphics.DrawRectangle(
                borderPen,
                bounds.Left,
                bounds.Top,
                bounds.Width - 1,
                bounds.Height - 1);
        }

        private void OnTimelineGridCellMouseEnter(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0
                || e.RowIndex >= timelineGrid.Rows.Count
                || timelineGrid.Rows[e.RowIndex].DataBoundItem is not ActivityTimelineRow row)
            {
                timelineOverviewControl.SetExternalHoverText(null);
                return;
            }

            timelineOverviewControl.SetExternalHoverText(
                TimelineOverviewControl.FormatActivityHoverText(row));
        }

        private void OnTimelineGridMouseLeave(object? sender, EventArgs e)
        {
            timelineOverviewControl.SetExternalHoverText(null);
        }

        private Rectangle GetVisibleTimelineRowCellsBounds(int rowIndex)
        {
            var bounds = Rectangle.Empty;
            foreach (DataGridViewColumn column in timelineGrid.Columns)
            {
                if (!column.Visible)
                    continue;

                var cellBounds = timelineGrid.GetCellDisplayRectangle(
                    column.Index,
                    rowIndex,
                    cutOverflow: true);
                if (cellBounds.IsEmpty)
                    continue;

                bounds = bounds.IsEmpty ? cellBounds : Rectangle.Union(bounds, cellBounds);
            }

            return Rectangle.Intersect(bounds, timelineGrid.ClientRectangle);
        }

        private Font GetTimelineHighlightedRowFont()
        {
            if (timelineHighlightedRowFont is null
                || !string.Equals(
                    timelineHighlightedRowFont.Name,
                    timelineGrid.Font.Name,
                    StringComparison.Ordinal)
                || Math.Abs(timelineHighlightedRowFont.Size - timelineGrid.Font.Size) > 0.01f)
            {
                timelineHighlightedRowFont?.Dispose();
                timelineHighlightedRowFont = new Font(timelineGrid.Font, FontStyle.Bold);
            }

            return timelineHighlightedRowFont;
        }

    }
}
