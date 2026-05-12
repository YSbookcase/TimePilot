using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    public partial class Form1 : Form
    {
        private const int SampleIntervalMs = 1000;

        private readonly System.Windows.Forms.Timer sampleTimer = new();
        private readonly AppIconCache appIconCache = new();
        private readonly object processRuntimeTrackingLock = new();
        private readonly Form headerToolTipForm = new();
        private readonly Label headerToolTipLabel = new();
        private AppSettings settings = AppSettings.LoadDefault();
        private string usageSortProperty = nameof(UsageSummaryRow.ActiveUsageMs);
        private string runtimeSortProperty = nameof(ProcessRuntimeSummaryRow.RuntimeMs);
        private string runtimeSegmentSortProperty = nameof(ProcessRuntimeSegmentRow.StartedAt);
        private SortOrder usageSortOrder = SortOrder.Descending;
        private SortOrder runtimeSortOrder = SortOrder.Descending;
        private SortOrder runtimeSegmentSortOrder = SortOrder.Descending;
        private bool showRecentTimelineFirst;
        private bool showCurrentTrackingScopeOnly = true;
        private bool showRunningRuntimeOnly;
        private bool isRefreshingRuntimeGrid;
        private long? selectedRuntimeAppId;
        private volatile bool isClosing;
        private volatile bool isProcessRuntimeSampleRunning;
        private DataGridView? hoveredHeaderGrid;
        private int hoveredHeaderColumnIndex = -1;
        private TimePilotStorage? storage;
        private ForegroundSessionTracker? foregroundSessionTracker;
        private IdleSessionTracker? idleSessionTracker;
        private ProcessRuntimeSessionTracker? processRuntimeSessionTracker;
        private DateTimeOffset? lastProcessRuntimeSampleAt;

        public Form1()
        {
            InitializeComponent();

            if (IsRunningInDesigner())
            {
                ConfigureDesignPreview();
                return;
            }

            storage = TimePilotStorage.CreateDefault();
            foregroundSessionTracker = new ForegroundSessionTracker(storage);
            idleSessionTracker = new IdleSessionTracker(storage);
            processRuntimeSessionTracker = new ProcessRuntimeSessionTracker(storage);

            var startedAt = DateTimeOffset.UtcNow;
            storage.Initialize(startedAt);
            storage.BeginRuntimeSession(startedAt, Application.ProductVersion);

            ConfigureHeaderToolTip();
            usageGrid.CellMouseEnter += OnGridCellMouseEnter;
            usageGrid.CellMouseLeave += OnGridCellMouseLeave;
            runtimeGrid.CellMouseEnter += OnGridCellMouseEnter;
            runtimeGrid.CellMouseLeave += OnGridCellMouseLeave;
            sampleTimer.Interval = SampleIntervalMs;
            sampleTimer.Tick += OnSampleTick;
            sampleTimer.Start();
            FormClosed += OnFormClosed;
        }

        private void OnSampleTick(object? sender, EventArgs e)
        {
            var observedAt = DateTimeOffset.UtcNow;
            var idleThresholdMs = settings.IdleThresholdMs;
            var isIdle = UserIdleChecker.IsIdle(idleThresholdMs);
            var foregroundApp = ForegroundWindowReader.TryGetForegroundApp();

            storage?.UpdateRuntimeHeartbeat(observedAt);
            idleSessionTracker?.Track(isIdle, foregroundApp, idleThresholdMs, observedAt);
            foregroundSessionTracker?.Track(foregroundApp, isIdle, observedAt);
            _ = TrackProcessRuntimeSessionsAsync(observedAt);

            var idleText = isIdle ? "유휴" : "활성";
            statusLabel.Text = foregroundApp is null
                ? $"전경: (없음) · {idleText}"
                : $"전경: {foregroundApp.DisplayName} · {idleText}";
            RefreshViews(observedAt);
        }

        private void OnFormClosed(object? sender, FormClosedEventArgs e)
        {
            var endedAt = DateTimeOffset.UtcNow;
            isClosing = true;
            sampleTimer.Stop();
            idleSessionTracker?.EndCurrentSession(endedAt);
            foregroundSessionTracker?.EndCurrentSession(endedAt);
            lock (processRuntimeTrackingLock)
            {
                processRuntimeSessionTracker?.EndCurrentSessions(endedAt);
            }

            storage?.EndRuntimeSession(endedAt, "normal");
            storage?.Dispose();
            appIconCache.Dispose();
            headerToolTipForm.Dispose();
            sampleTimer.Dispose();
        }

        private void ConfigureDesignPreview()
        {
            statusLabel.Text = "전경: Visual Studio · 활성";
            usageGrid.DataSource = AddIcons(new List<UsageSummaryRow>
            {
                new("Microsoft Visual Studio", null, 3_900_000, 0.54, 8, null, DateTimeOffset.Now.AddHours(-2), DateTimeOffset.Now),
                new("Google Chrome", null, 1_680_000, 0.23, 15, null, DateTimeOffset.Now.AddHours(-1), DateTimeOffset.Now.AddMinutes(-12)),
                new("File Explorer", null, 900_000, 0.13, 4, null, DateTimeOffset.Now.AddMinutes(-45), DateTimeOffset.Now.AddMinutes(-5))
            });
            timelineGrid.DataSource = AddIcons(new List<ActivityTimelineRow>
            {
                new("활성", DateTimeOffset.Now.AddHours(-2), DateTimeOffset.Now.AddHours(-1), 3_600_000, "devenv"),
                new("유휴", DateTimeOffset.Now.AddHours(-1), DateTimeOffset.Now.AddMinutes(-45), 900_000, "devenv"),
                new("활성", DateTimeOffset.Now.AddMinutes(-45), null, 2_700_000, "chrome")
            });
        }

        private void RefreshViews(DateTimeOffset observedAt)
        {
            if (storage is null)
                return;

            SetGridDataSourcePreservingView(
                usageGrid,
                AddIcons(SortUsageSummaryRows(UsageSummaryRowBuilder.FromForegroundUsage(
                    storage.GetForegroundUsageForDay(observedAt)))));
            SetGridDataSourcePreservingView(
                timelineGrid,
                AddIcons(SortTimelineRows(storage.GetActivityTimelineForDay(observedAt))));
            var appIdToRestore = selectedRuntimeAppId ?? GetSelectedRuntimeAppId();
            isRefreshingRuntimeGrid = true;
            try
            {
                SetGridDataSourcePreservingView(
                    runtimeGrid,
                    AddIcons(SortRuntimeSummaryRows(FilterRuntimeSummaryRows(
                        storage.GetProcessRuntimeUsageForDay(observedAt)))));
                RestoreRuntimeSelection(appIdToRestore, GetFirstDisplayedColumnIndex(runtimeGrid));
            }
            finally
            {
                isRefreshingRuntimeGrid = false;
            }

            selectedRuntimeAppId = GetSelectedRuntimeAppId();
            RefreshRuntimeSegments(observedAt);
            UpdateSortGlyphs();
            RepositionHeaderToolTip();
        }

        private void RefreshRuntimeSegments(DateTimeOffset observedAt)
        {
            if (storage is null)
                return;

            var selectedRow = runtimeGrid.CurrentRow?.DataBoundItem as ProcessRuntimeSummaryRow;
            if (selectedRow is null)
            {
                SetGridDataSourcePreservingView(runtimeSegmentsGrid, Array.Empty<ProcessRuntimeSegmentRow>());
                return;
            }

            SetGridDataSourcePreservingView(
                runtimeSegmentsGrid,
                SortRuntimeSegmentRows(storage.GetProcessRuntimeSegmentsForDay(selectedRow.AppId, observedAt)));
        }

        private async Task TrackProcessRuntimeSessionsAsync(DateTimeOffset observedAt)
        {
            if (processRuntimeSessionTracker is null || isClosing)
                return;

            if (!settings.ProcessRuntimeTrackingEnabled)
            {
                lock (processRuntimeTrackingLock)
                {
                    processRuntimeSessionTracker.EndCurrentSessions(observedAt);
                }

                lastProcessRuntimeSampleAt = null;
                return;
            }

            if (lastProcessRuntimeSampleAt is { } lastSample
                && (observedAt - lastSample).TotalMilliseconds < settings.ProcessRuntimeSampleIntervalMs)
                return;

            if (isProcessRuntimeSampleRunning)
                return;

            var scope = settings.ProcessRuntimeTrackingScope;
            lastProcessRuntimeSampleAt = observedAt;
            isProcessRuntimeSampleRunning = true;

            try
            {
                await Task.Run(() =>
                {
                    var processes = RunningProcessReader.GetProcesses(scope);
                    if (isClosing)
                        return;

                    lock (processRuntimeTrackingLock)
                    {
                        if (!isClosing)
                            processRuntimeSessionTracker.Track(processes, scope, observedAt);
                    }
                });
            }
            catch
            {
            }
            finally
            {
                isProcessRuntimeSampleRunning = false;
            }
        }

        private IReadOnlyList<UsageSummaryRow> AddIcons(IReadOnlyList<UsageSummaryRow> rows)
        {
            return rows
                .Select(row => row with { AppIcon = appIconCache.GetIcon(row.ExecutablePath) })
                .ToList();
        }

        private IReadOnlyList<ActivityTimelineRow> AddIcons(IReadOnlyList<ActivityTimelineRow> rows)
        {
            return rows
                .Select(row => row with { AppIcon = appIconCache.GetIcon(row.ExecutablePath) })
                .ToList();
        }

        private IReadOnlyList<ProcessRuntimeSummaryRow> AddIcons(IReadOnlyList<ProcessRuntimeSummaryRow> rows)
        {
            return rows
                .Select(row => row with { AppIcon = appIconCache.GetIcon(row.ExecutablePath) })
                .ToList();
        }

        private IReadOnlyList<UsageSummaryRow> SortUsageSummaryRows(IReadOnlyList<UsageSummaryRow> rows)
        {
            IOrderedEnumerable<UsageSummaryRow> sortedRows = usageSortProperty switch
            {
                nameof(UsageSummaryRow.AppName) => OrderUsageRows(rows, x => x.AppName),
                nameof(UsageSummaryRow.FirstStartedAt) => OrderUsageRows(rows, x => x.FirstStartedAt),
                nameof(UsageSummaryRow.LastObservedAt) => OrderUsageRows(rows, x => x.LastObservedAt),
                nameof(UsageSummaryRow.UsageRatio) => OrderUsageRows(rows, x => x.UsageRatio),
                nameof(UsageSummaryRow.SwitchCount) => OrderUsageRows(rows, x => x.SwitchCount),
                _ => OrderUsageRows(rows, x => x.ActiveUsageMs)
            };

            return sortedRows
                .ThenBy(x => x.AppName)
                .ToList();
        }

        private IReadOnlyList<ActivityTimelineRow> SortTimelineRows(IReadOnlyList<ActivityTimelineRow> rows)
        {
            return showRecentTimelineFirst
                ? rows.OrderByDescending(x => x.StartedAt).ToList()
                : rows.OrderBy(x => x.StartedAt).ToList();
        }

        private IReadOnlyList<ProcessRuntimeSummaryRow> FilterRuntimeSummaryRows(IReadOnlyList<ProcessRuntimeSummaryRow> rows)
        {
            IEnumerable<ProcessRuntimeSummaryRow> filteredRows = rows;

            if (showCurrentTrackingScopeOnly)
            {
                filteredRows = settings.ProcessRuntimeTrackingScope switch
                {
                    ProcessRuntimeTrackingScope.WindowedApps => filteredRows.Where(x => x.HasMainWindow),
                    ProcessRuntimeTrackingScope.UserProcesses => filteredRows.Where(x => x.IsCurrentSessionProcess),
                    _ => filteredRows
                };
            }

            if (showRunningRuntimeOnly)
                filteredRows = filteredRows.Where(x => x.HasRunningSession);

            return filteredRows.ToList();
        }

        private IReadOnlyList<ProcessRuntimeSummaryRow> SortRuntimeSummaryRows(IReadOnlyList<ProcessRuntimeSummaryRow> rows)
        {
            IOrderedEnumerable<ProcessRuntimeSummaryRow> sortedRows = runtimeSortProperty switch
            {
                nameof(ProcessRuntimeSummaryRow.AppName) => OrderRuntimeRows(rows, x => x.AppName),
                nameof(ProcessRuntimeSummaryRow.FirstObservedAt) => OrderRuntimeRows(rows, x => x.FirstObservedAt),
                nameof(ProcessRuntimeSummaryRow.LastObservedAt) => OrderRuntimeRows(rows, x => x.LastObservedAt),
                nameof(ProcessRuntimeSummaryRow.ActiveUsageMs) => OrderRuntimeRows(rows, x => x.ActiveUsageMs),
                nameof(ProcessRuntimeSummaryRow.ActualUsageRatio) => OrderRuntimeRows(rows, x => x.ActualUsageRatio ?? -1),
                nameof(ProcessRuntimeSummaryRow.RuntimeSegmentCount) => OrderRuntimeRows(rows, x => x.RuntimeSegmentCount),
                nameof(ProcessRuntimeSummaryRow.HasRunningSession) => OrderRuntimeRows(rows, x => x.HasRunningSession),
                _ => OrderRuntimeRows(rows, x => x.RuntimeMs)
            };

            return sortedRows
                .ThenBy(x => x.AppName)
                .ToList();
        }

        private IReadOnlyList<ProcessRuntimeSegmentRow> SortRuntimeSegmentRows(IReadOnlyList<ProcessRuntimeSegmentRow> rows)
        {
            IOrderedEnumerable<ProcessRuntimeSegmentRow> sortedRows = runtimeSegmentSortProperty switch
            {
                nameof(ProcessRuntimeSegmentRow.EndedAt) => OrderRuntimeSegmentRows(rows, x => x.EndedAt),
                nameof(ProcessRuntimeSegmentRow.DurationMs) => OrderRuntimeSegmentRows(rows, x => x.DurationMs),
                nameof(ProcessRuntimeSegmentRow.IsRunning) => OrderRuntimeSegmentRows(rows, x => x.IsRunning),
                nameof(ProcessRuntimeSegmentRow.ObservationTypeText) => OrderRuntimeSegmentRows(rows, x => x.ObservationTypeText),
                nameof(ProcessRuntimeSegmentRow.ProcessId) => OrderRuntimeSegmentRows(rows, x => x.ProcessId),
                _ => OrderRuntimeSegmentRows(rows, x => x.StartedAt)
            };

            return sortedRows
                .ThenByDescending(x => x.StartedAt)
                .ToList();
        }

        private IOrderedEnumerable<UsageSummaryRow> OrderUsageRows<TKey>(
            IReadOnlyList<UsageSummaryRow> rows,
            Func<UsageSummaryRow, TKey> keySelector)
        {
            return usageSortOrder == SortOrder.Ascending
                ? rows.OrderBy(keySelector)
                : rows.OrderByDescending(keySelector);
        }

        private IOrderedEnumerable<ProcessRuntimeSummaryRow> OrderRuntimeRows<TKey>(
            IReadOnlyList<ProcessRuntimeSummaryRow> rows,
            Func<ProcessRuntimeSummaryRow, TKey> keySelector)
        {
            return runtimeSortOrder == SortOrder.Ascending
                ? rows.OrderBy(keySelector)
                : rows.OrderByDescending(keySelector);
        }

        private IOrderedEnumerable<ProcessRuntimeSegmentRow> OrderRuntimeSegmentRows<TKey>(
            IReadOnlyList<ProcessRuntimeSegmentRow> rows,
            Func<ProcessRuntimeSegmentRow, TKey> keySelector)
        {
            return runtimeSegmentSortOrder == SortOrder.Ascending
                ? rows.OrderBy(keySelector)
                : rows.OrderByDescending(keySelector);
        }

        private void OnUsageGridColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0)
                return;

            var propertyName = GetUsageSortPropertyName(usageGrid.Columns[e.ColumnIndex]);
            if (propertyName is null)
                return;

            usageSortOrder = string.Equals(usageSortProperty, propertyName, StringComparison.Ordinal)
                ? ToggleSortOrder(usageSortOrder)
                : SortOrder.Descending;
            usageSortProperty = propertyName;
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnGridCellMouseEnter(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex != -1 || sender is not DataGridView grid)
                return;

            hoveredHeaderGrid = grid;
            hoveredHeaderColumnIndex = e.ColumnIndex;
            ShowHeaderToolTip(grid, e.ColumnIndex);
        }

        private void OnGridCellMouseLeave(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex != -1 || sender is not DataGridView grid || grid != hoveredHeaderGrid || e.ColumnIndex != hoveredHeaderColumnIndex)
                return;

            hoveredHeaderGrid = null;
            hoveredHeaderColumnIndex = -1;
            headerToolTipForm.Hide();
        }

        private void OnTimelineGridColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || timelineGrid.Columns[e.ColumnIndex] != timelineStartedAtColumn)
                return;

            showRecentTimelineFirst = !showRecentTimelineFirst;
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnRuntimeGridColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0)
                return;

            var propertyName = GetRuntimeSortPropertyName(runtimeGrid.Columns[e.ColumnIndex]);
            if (propertyName is null)
                return;

            runtimeSortOrder = string.Equals(runtimeSortProperty, propertyName, StringComparison.Ordinal)
                ? ToggleSortOrder(runtimeSortOrder)
                : SortOrder.Descending;
            runtimeSortProperty = propertyName;
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnRuntimeGridSelectionChanged(object? sender, EventArgs e)
        {
            if (isRefreshingRuntimeGrid)
                return;

            selectedRuntimeAppId = GetSelectedRuntimeAppId();
            RefreshRuntimeSegments(DateTimeOffset.UtcNow);
        }

        private void OnRuntimeSegmentsGridColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0)
                return;

            var propertyName = GetRuntimeSegmentSortPropertyName(runtimeSegmentsGrid.Columns[e.ColumnIndex]);
            if (propertyName is null)
                return;

            runtimeSegmentSortOrder = string.Equals(runtimeSegmentSortProperty, propertyName, StringComparison.Ordinal)
                ? ToggleSortOrder(runtimeSegmentSortOrder)
                : SortOrder.Descending;
            runtimeSegmentSortProperty = propertyName;
            RefreshRuntimeSegments(DateTimeOffset.UtcNow);
            UpdateSortGlyphs();
        }

        private void OnRunningRuntimeOnlyCheckBoxCheckedChanged(object? sender, EventArgs e)
        {
            showRunningRuntimeOnly = runningRuntimeOnlyCheckBox.Checked;
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnCurrentTrackingScopeOnlyCheckBoxCheckedChanged(object? sender, EventArgs e)
        {
            showCurrentTrackingScopeOnly = currentTrackingScopeOnlyCheckBox.Checked;
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private string? GetUsageSortPropertyName(DataGridViewColumn column)
        {
            return column.Name switch
            {
                nameof(appNameColumn) => nameof(UsageSummaryRow.AppName),
                nameof(firstStartedAtColumn) => nameof(UsageSummaryRow.FirstStartedAt),
                nameof(lastObservedAtColumn) => nameof(UsageSummaryRow.LastObservedAt),
                nameof(activeUsageTimeColumn) => nameof(UsageSummaryRow.ActiveUsageMs),
                nameof(usageRatioColumn) => nameof(UsageSummaryRow.UsageRatio),
                nameof(switchCountColumn) => nameof(UsageSummaryRow.SwitchCount),
                _ => null
            };
        }

        private string? GetRuntimeSortPropertyName(DataGridViewColumn column)
        {
            return column.Name switch
            {
                nameof(runtimeAppNameColumn) => nameof(ProcessRuntimeSummaryRow.AppName),
                nameof(runtimeFirstObservedAtColumn) => nameof(ProcessRuntimeSummaryRow.FirstObservedAt),
                nameof(runtimeLastObservedAtColumn) => nameof(ProcessRuntimeSummaryRow.LastObservedAt),
                nameof(runtimeDurationColumn) => nameof(ProcessRuntimeSummaryRow.RuntimeMs),
                nameof(runtimeActiveUsageColumn) => nameof(ProcessRuntimeSummaryRow.ActiveUsageMs),
                nameof(runtimeActualUsageRatioColumn) => nameof(ProcessRuntimeSummaryRow.ActualUsageRatio),
                nameof(runtimeSessionCountColumn) => nameof(ProcessRuntimeSummaryRow.RuntimeSegmentCount),
                nameof(runtimeStatusColumn) => nameof(ProcessRuntimeSummaryRow.HasRunningSession),
                _ => null
            };
        }

        private string? GetRuntimeSegmentSortPropertyName(DataGridViewColumn column)
        {
            return column.Name switch
            {
                nameof(runtimeSegmentStartedAtColumn) => nameof(ProcessRuntimeSegmentRow.StartedAt),
                nameof(runtimeSegmentEndedAtColumn) => nameof(ProcessRuntimeSegmentRow.EndedAt),
                nameof(runtimeSegmentDurationColumn) => nameof(ProcessRuntimeSegmentRow.DurationMs),
                nameof(runtimeSegmentStatusColumn) => nameof(ProcessRuntimeSegmentRow.IsRunning),
                nameof(runtimeSegmentObservationTypeColumn) => nameof(ProcessRuntimeSegmentRow.ObservationTypeText),
                nameof(runtimeSegmentProcessIdColumn) => nameof(ProcessRuntimeSegmentRow.ProcessId),
                _ => null
            };
        }

        private void UpdateSortGlyphs()
        {
            foreach (DataGridViewColumn column in usageGrid.Columns)
            {
                column.HeaderCell.SortGlyphDirection = GetUsageSortPropertyName(column) == usageSortProperty
                    ? usageSortOrder
                    : SortOrder.None;
            }

            foreach (DataGridViewColumn column in timelineGrid.Columns)
            {
                column.HeaderCell.SortGlyphDirection = SortOrder.None;
            }

            timelineStartedAtColumn.HeaderCell.SortGlyphDirection = showRecentTimelineFirst
                ? SortOrder.Descending
                : SortOrder.Ascending;

            foreach (DataGridViewColumn column in runtimeGrid.Columns)
            {
                column.HeaderCell.SortGlyphDirection = GetRuntimeSortPropertyName(column) == runtimeSortProperty
                    ? runtimeSortOrder
                    : SortOrder.None;
            }

            foreach (DataGridViewColumn column in runtimeSegmentsGrid.Columns)
            {
                column.HeaderCell.SortGlyphDirection = GetRuntimeSegmentSortPropertyName(column) == runtimeSegmentSortProperty
                    ? runtimeSegmentSortOrder
                    : SortOrder.None;
            }
        }

        private void ConfigureHeaderToolTip()
        {
            headerToolTipLabel.AutoSize = false;
            headerToolTipLabel.BackColor = SystemColors.Info;
            headerToolTipLabel.BorderStyle = BorderStyle.FixedSingle;
            headerToolTipLabel.ForeColor = SystemColors.InfoText;
            headerToolTipLabel.Location = new Point(0, 0);
            headerToolTipLabel.Padding = new Padding(8, 5, 8, 5);
            headerToolTipLabel.Size = new Size(274, 42);
            headerToolTipLabel.Text = "오늘 전체 활성 사용 시간 중 이 앱이 차지한 비율입니다.";

            headerToolTipForm.BackColor = SystemColors.Info;
            headerToolTipForm.ClientSize = headerToolTipLabel.Size;
            headerToolTipForm.Controls.Add(headerToolTipLabel);
            headerToolTipForm.FormBorderStyle = FormBorderStyle.None;
            headerToolTipForm.ShowInTaskbar = false;
            headerToolTipForm.StartPosition = FormStartPosition.Manual;
            headerToolTipForm.TopMost = true;
        }

        private void RepositionHeaderToolTip()
        {
            if (headerToolTipForm.Visible && hoveredHeaderGrid is not null && hoveredHeaderColumnIndex >= 0)
                PositionHeaderToolTip(hoveredHeaderGrid, hoveredHeaderColumnIndex);
        }

        private void ShowHeaderToolTip(DataGridView grid, int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= grid.Columns.Count)
            {
                headerToolTipForm.Hide();
                return;
            }

            var text = GetHeaderToolTipText(grid, grid.Columns[columnIndex]);
            if (text is null)
            {
                headerToolTipForm.Hide();
                return;
            }

            headerToolTipLabel.Text = text;
            var textSize = TextRenderer.MeasureText(text, headerToolTipLabel.Font, new Size(360, 0), TextFormatFlags.WordBreak);
            headerToolTipLabel.Size = new Size(Math.Min(360, Math.Max(240, textSize.Width + 18)), textSize.Height + 14);
            headerToolTipForm.ClientSize = headerToolTipLabel.Size;
            PositionHeaderToolTip(grid, columnIndex);
            if (!headerToolTipForm.Visible)
                headerToolTipForm.Show(this);
        }

        private string? GetHeaderToolTipText(DataGridView grid, DataGridViewColumn column)
        {
            if (grid == usageGrid && column == usageRatioColumn)
                return "오늘 전체 활성 사용 시간 중 이 앱이 차지한 비율입니다.";

            if (grid == runtimeGrid && column == runtimeDurationColumn)
                return "설정한 백그라운드 앱 추적 주기로 관측한 실행 시간입니다.";

            if (grid == runtimeGrid && column == runtimeActualUsageRatioColumn)
                return "실행 시간 중 실제 foreground 활성 사용 시간이 차지한 비율입니다.";

            if (grid == runtimeGrid && column == runtimeSessionCountColumn)
                return "앱이 이어서 실행된 것으로 관측된 구간 수입니다.";

            return null;
        }

        private void PositionHeaderToolTip(DataGridView grid, int columnIndex)
        {
            var headerRectangle = grid.GetCellDisplayRectangle(columnIndex, -1, true);
            var location = grid.PointToScreen(new Point(headerRectangle.Left, headerRectangle.Bottom + 4));
            headerToolTipForm.Location = location;
        }

        private long? GetSelectedRuntimeAppId()
        {
            return (runtimeGrid.CurrentRow?.DataBoundItem as ProcessRuntimeSummaryRow)?.AppId;
        }

        private void RestoreRuntimeSelection(long? appId, int firstDisplayedColumnIndex)
        {
            if (appId is null)
                return;

            foreach (DataGridViewRow row in runtimeGrid.Rows)
            {
                if (row.DataBoundItem is not ProcessRuntimeSummaryRow runtimeRow || runtimeRow.AppId != appId.Value)
                    continue;

                runtimeGrid.ClearSelection();
                row.Selected = true;
                var currentCellIndex = Math.Min(
                    Math.Max(firstDisplayedColumnIndex, 0),
                    runtimeGrid.Columns.Count - 1);
                runtimeGrid.CurrentCell = row.Cells[currentCellIndex];
                TrySetFirstDisplayedColumnIndex(runtimeGrid, firstDisplayedColumnIndex);
                return;
            }
        }

        private static SortOrder ToggleSortOrder(SortOrder sortOrder)
        {
            return sortOrder == SortOrder.Descending
                ? SortOrder.Ascending
                : SortOrder.Descending;
        }

        private static void SetGridDataSourcePreservingView<T>(DataGridView grid, IReadOnlyList<T> rows)
        {
            var firstDisplayedRowIndex = GetFirstDisplayedRowIndex(grid);
            var firstDisplayedColumnIndex = GetFirstDisplayedColumnIndex(grid);
            var selectedIndex = grid.CurrentRow?.Index ?? -1;

            grid.DataSource = rows;

            if (grid.Rows.Count == 0)
                return;

            var restoredFirstRowIndex = Math.Min(firstDisplayedRowIndex, grid.Rows.Count - 1);
            var restoredFirstColumnIndex = Math.Min(firstDisplayedColumnIndex, grid.Columns.Count - 1);
            TrySetFirstDisplayedRowIndex(grid, restoredFirstRowIndex);
            TrySetFirstDisplayedColumnIndex(grid, restoredFirstColumnIndex);

            if (selectedIndex < 0)
                return;

            var restoredSelectedIndex = Math.Min(selectedIndex, grid.Rows.Count - 1);
            var restoredSelectedColumnIndex = Math.Min(restoredFirstColumnIndex, grid.Columns.Count - 1);
            grid.ClearSelection();
            grid.Rows[restoredSelectedIndex].Selected = true;
            grid.CurrentCell = grid.Rows[restoredSelectedIndex].Cells[restoredSelectedColumnIndex];
            TrySetFirstDisplayedRowIndex(grid, restoredFirstRowIndex);
            TrySetFirstDisplayedColumnIndex(grid, restoredFirstColumnIndex);
        }

        private static int GetFirstDisplayedRowIndex(DataGridView grid)
        {
            try
            {
                return Math.Max(grid.FirstDisplayedScrollingRowIndex, 0);
            }
            catch (InvalidOperationException)
            {
                return 0;
            }
        }

        private static int GetFirstDisplayedColumnIndex(DataGridView grid)
        {
            try
            {
                return Math.Max(grid.FirstDisplayedScrollingColumnIndex, 0);
            }
            catch (InvalidOperationException)
            {
                return 0;
            }
        }

        private static void TrySetFirstDisplayedRowIndex(DataGridView grid, int rowIndex)
        {
            try
            {
                grid.FirstDisplayedScrollingRowIndex = rowIndex;
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static void TrySetFirstDisplayedColumnIndex(DataGridView grid, int columnIndex)
        {
            try
            {
                if (columnIndex >= 0 && grid.Columns.Count > 0)
                    grid.FirstDisplayedScrollingColumnIndex = columnIndex;
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static bool IsRunningInDesigner()
        {
            return System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime;
        }

        private void OnPreferencesMenuItemClick(object? sender, EventArgs e)
        {
            using var form = new PreferencesForm(settings);
            if (form.ShowDialog(this) != DialogResult.OK)
                return;

            settings.SetIdleThresholdMinutes(form.IdleThresholdMinutes);
            settings.SetProcessRuntimeTracking(
                form.ProcessRuntimeTrackingEnabled,
                form.ProcessRuntimeTrackingScope,
                form.ProcessRuntimeSampleIntervalSeconds);
            lastProcessRuntimeSampleAt = null;
        }

        private void OnExitMenuItemClick(object? sender, EventArgs e)
        {
            Close();
        }

        private void OnAboutMenuItemClick(object? sender, EventArgs e)
        {
            MessageBox.Show(
                this,
                $"TimePilot {Application.ProductVersion}",
                "TimePilot 정보",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
