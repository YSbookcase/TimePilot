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

    }
}
