using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Win32;
using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    public partial class Form1 : Form
    {
        private const int SampleIntervalMs = 1000;
        private const int SampleIntervalToleranceMs = 200;
        private const int SafeModeUnexpectedExitCount = 2;
        private const long SlowOperationThresholdMs = 250;
        private static readonly TimeSpan PerformanceStatusDuration = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan SafeModeShortRuntimeThreshold = TimeSpan.FromMinutes(2);

        private readonly System.Windows.Forms.Timer sampleTimer = new();
        private readonly AppIconCache appIconCache = new();
        private readonly object processRuntimeTrackingLock = new();
        private readonly Form headerToolTipForm = new();
        private readonly Label headerToolTipLabel = new();
        private readonly List<Label> runtimeCoverageSummaryLabels = new();
        private readonly NotifyIcon trayIcon = new();
        private readonly ContextMenuStrip trayMenu = new();
        private readonly ContextMenuStrip appCategoryMenu = new();
        private readonly ContextMenuStrip usageGridMenu = new();
        private readonly ContextMenuStrip timelineGridMenu = new();
        private readonly TableLayoutPanel runtimeSegmentPanel = new();
        private readonly TableLayoutPanel runtimeSegmentTimelinePanel = new();
        private readonly Label runtimeSegmentObservationFilterLabel = new();
        private readonly ComboBox runtimeSegmentObservationFilterComboBox = new();
        private readonly RuntimeSegmentTimelineControl runtimeSegmentTimelineControl = new();
        private readonly FlowLayoutPanel runtimeSegmentZoomPanel = new();
        private readonly Label runtimeSegmentZoomRangeLabel = new();
        private readonly Button runtimeSegmentZoomOutButton = new();
        private readonly Button runtimeSegmentZoomInButton = new();
        private readonly Button runtimeSegmentPreviousButton = new();
        private readonly Button runtimeSegmentNextButton = new();
        private readonly Button runtimeSegmentResetButton = new();
        private readonly Button runtimeSegmentHelpButton = new();
        private readonly HScrollBar runtimeSegmentZoomScrollBar = new();
        private bool isUpdatingRuntimeSegmentScrollBar;
        private RuntimeSegmentSelectionKey? selectedRuntimeSegmentKey;
        private readonly bool startMinimizedToTray;
        private Dictionary<string, List<AppSettings.TableColumnLayout>> defaultTableColumnLayouts = new();
        private AppSettings settings = AppSettings.LoadDefault();
        private string usageSortProperty = nameof(UsageSummaryRow.ActiveUsageMs);
        private string dailyUsageTrendSortProperty = nameof(DailyUsageTrendRow.Date);
        private string timelineSortProperty = nameof(ActivityTimelineRow.StartedAt);
        private string runtimeSortProperty = nameof(ProcessRuntimeSummaryRow.RuntimeMs);
        private string runtimeSegmentSortProperty = nameof(ProcessRuntimeSegmentRow.StartedAt);
        private SummaryPeriod selectedSummaryPeriod = SummaryPeriod.Today;
        private DateTime selectedSummarySpecificDate = DateTime.Today;
        private DateTime selectedSummaryCustomStartDate = DateTime.Today;
        private DateTime selectedSummaryCustomEndDate = DateTime.Today;
        private DateTime summaryPeriodOptionsDate = DateTime.Today;
        private DateTime dateSelectorOptionsDate = DateTime.Today;
        private DateTime selectedDetailDate = DateTime.Today;
        private DateTime selectedTimelineDate = DateTime.Today;
        private int selectedTimelineCategoryBucketMinutes = 30;
        private TimelineActivityTypeHighlight selectedTimelineActivityTypeHighlight = TimelineActivityTypeHighlight.None;
        private TimelineSystemEventFilter selectedTimelineSystemEventFilter = TimelineSystemEventFilter.All;
        private SortOrder usageSortOrder = SortOrder.Descending;
        private SortOrder dailyUsageTrendSortOrder = SortOrder.Descending;
        private SortOrder timelineSortOrder = SortOrder.Descending;
        private SortOrder runtimeSortOrder = SortOrder.Descending;
        private SortOrder runtimeSegmentSortOrder = SortOrder.Descending;
        private DetailRuntimeFilter selectedDetailRuntimeFilter = DetailRuntimeFilter.SummaryApps;
        private RuntimeSegmentObservationFilter selectedRuntimeSegmentObservationFilter = RuntimeSegmentObservationFilter.All;
        private bool showRunningRuntimeOnly;
        private bool isRefreshingRuntimeGrid;
        private bool isSelectingRuntimeGridRow;
        private bool isRestoringRuntimeSegmentSelection;
        private bool isUpdatingDetailRuntimeFilterOptions;
        private bool isExplicitExitRequested;
        private volatile bool isViewRefreshRunning;
        private long? selectedRuntimeAppId;
        private volatile bool isClosing;
        private volatile bool isProcessRuntimeSampleRunning;
        private bool isExportRunning;
        private string statusText = string.Empty;
        private string? exportStatusText;
        private string? performanceStatusText;
        private DateTimeOffset? performanceStatusExpiresAt;
        private DataGridView? hoveredHeaderGrid;
        private int hoveredHeaderColumnIndex = -1;
        private TimePilotStorage? storage;
        private ForegroundSessionTracker? foregroundSessionTracker;
        private IdleSessionTracker? idleSessionTracker;
        private ProcessRuntimeSessionTracker? processRuntimeSessionTracker;
        private DateTimeOffset? lastProcessRuntimeSampleAt;
        private DateTimeOffset? lastSampleTickAt;
        private bool processRuntimeSafeModeActivated;
        private bool isInitializingSummaryPeriodSelector;
        private bool isInitializingDateSelectors;
        private bool isUpdatingTimelineScrollBar;
        private bool isApplyingTableColumnLayouts;
        private bool systemEventHandlersRegistered;
        private string? highlightedTimelineProcessName;
        private string? highlightedTimelineAppName;
        private TimelineSegmentSelectionKey? highlightedTimelineSegmentKey;
        private string? highlightedTimelineSegmentLabel;
        private IReadOnlyList<ForegroundUsageSummary> currentTimelineForegroundUsage = Array.Empty<ForegroundUsageSummary>();
        private IReadOnlyList<ActivityTimelineRow> currentTimelineRows = Array.Empty<ActivityTimelineRow>();
        private IReadOnlyList<TimelineRange> currentTimelineWindowsRuntimeRanges = Array.Empty<TimelineRange>();
        private IReadOnlyList<SystemTimelineRange> currentTimelineSystemRanges = Array.Empty<SystemTimelineRange>();
        private IReadOnlyList<SystemTimelineEvent> currentTimelineSystemEvents = Array.Empty<SystemTimelineEvent>();
        private Font? timelineHighlightedRowFont;
        private Form? recordedDatePickerPopupForm;
        private readonly Label timelineSystemEventFilterLabel = new();
        private readonly ComboBox timelineSystemEventFilterComboBox = new();

        public Form1(bool startMinimizedToTray = false)
        {
            this.startMinimizedToTray = startMinimizedToTray;

            UiText.UseLanguage(settings.UiLanguage);
            InitializeComponent();
            InitializeRuntimeSegmentTimeline();
            defaultTableColumnLayouts = CaptureTableColumnLayouts();
            ApplySavedWindowPlacement();
            ApplySavedTableSortState();
            ApplySavedTableColumnLayouts();
            ApplyInitialDetailSplitDistance();
            RegisterTableColumnLayoutPersistence();
            InitializeRecordedDateCalendar();
            InitializeSummaryPeriodSelector();
            InitializeDateSelectors();
            InitializeTimelineCategoryBucketSelector();
            InitializeTimelineTypeHighlightSelector();
            InitializeTimelineSystemEventFilterSelector();
            ApplyUiText();

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
            var systemBootedAt = GetCurrentSystemBootedAt(startedAt);
            storage.Initialize(startedAt, systemBootedAt);
            ApplyProcessRuntimeSafeModeIfNeeded();
            UpdateDetailTrackingDisabledBanner();
            storage.BeginRuntimeSession(startedAt, systemBootedAt, Application.ProductVersion);
            RecordWindowsSystemEvent("timepilot-start", "ApplicationStarted");
            RegisterWindowsSystemEventHandlers();

            Icon = LoadAppIcon();
            ConfigureHeaderToolTip();
            ConfigureTrayIcon();
            usageGrid.CellMouseEnter += OnGridCellMouseEnter;
            usageGrid.CellMouseLeave += OnGridCellMouseLeave;
            usageGrid.CellMouseDown += OnUsageGridCellMouseDown;
            timelineGrid.CellMouseDown += OnTimelineGridCellMouseDown;
            timelineGrid.RowPrePaint += OnTimelineGridRowPrePaint;
            timelineGrid.RowPostPaint += OnTimelineGridRowPostPaint;
            timelineZoomScrollBar.Scroll += OnTimelineZoomScrollBarScroll;
            timelineOverviewControl.ActivitySegmentContextRequested += OnTimelineOverviewActivitySegmentContextRequested;
            timelineOverviewControl.CategorySegmentContextRequested += OnTimelineOverviewCategorySegmentContextRequested;
            timelineOverviewControl.WindowsTrackContextRequested += OnTimelineOverviewWindowsTrackContextRequested;
            runtimeGrid.CellMouseEnter += OnGridCellMouseEnter;
            runtimeGrid.CellMouseLeave += OnGridCellMouseLeave;
            runtimeGrid.CellMouseDown += OnRuntimeGridCellMouseDown;
            mainTabs.SelectedIndexChanged += OnMainTabsSelectedIndexChanged;
            sampleTimer.Interval = SampleIntervalMs;
            sampleTimer.Tick += OnSampleTick;
            sampleTimer.Start();
            FormClosing += OnFormClosing;
            FormClosed += OnFormClosed;
            Shown += OnShown;
        }

        private void OnSampleTick(object? sender, EventArgs e)
        {
            var observedAt = DateTimeOffset.UtcNow;
            if (lastSampleTickAt is { } lastTickAt)
            {
                var tickGapMs = (long)(observedAt - lastTickAt).TotalMilliseconds;
                if (tickGapMs >= SampleIntervalMs + 500)
                    ReportPerformanceEvents($"tick-gap {tickGapMs}ms");
            }

            lastSampleTickAt = observedAt;
            var idleThresholdMs = settings.IdleThresholdMs;
            var isIdle = UserIdleChecker.IsIdle(idleThresholdMs);
            var foregroundApp = ForegroundWindowReader.TryGetForegroundApp();

            storage?.UpdateRuntimeHeartbeat(observedAt);
            idleSessionTracker?.Track(isIdle, foregroundApp, idleThresholdMs, observedAt);
            foregroundSessionTracker?.Track(foregroundApp, isIdle, observedAt);
            _ = TrackProcessRuntimeSessionsAsync(observedAt);

            var idleText = isIdle ? UiText.Main.Idle : UiText.Main.Active;
            statusLabel.Text = foregroundApp is null
                ? $"{UiText.Main.ForegroundPrefix}{UiText.Main.NoForegroundApp} · {idleText}"
                : $"{UiText.Main.ForegroundPrefix}{foregroundApp.DisplayName} · {idleText}";
            SetStatusText(statusLabel.Text);
            RefreshViews(observedAt);
        }

        private void OnFormClosed(object? sender, FormClosedEventArgs e)
        {
            var endedAt = DateTimeOffset.UtcNow;
            isClosing = true;
            UnregisterWindowsSystemEventHandlers();
            SaveWindowPlacement();
            sampleTimer.Stop();
            idleSessionTracker?.EndCurrentSession(endedAt);
            foregroundSessionTracker?.EndCurrentSession(endedAt);
            lock (processRuntimeTrackingLock)
            {
                processRuntimeSessionTracker?.EndCurrentSessions(endedAt);
            }

            RecordWindowsSystemEvent("timepilot-exit", "ApplicationClosed");
            storage?.EndRuntimeSession(endedAt, "normal");
            storage?.Dispose();
            appIconCache.Dispose();
            CloseRecordedDatePickerDropDown();
            headerToolTipForm.Dispose();
            timelineHighlightedRowFont?.Dispose();
            trayIcon.Visible = false;
            trayIcon.Dispose();
            trayMenu.Dispose();
            sampleTimer.Dispose();
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (isExplicitExitRequested || e.CloseReason != CloseReason.UserClosing)
                return;

            SaveWindowPlacement();
            e.Cancel = true;
            HideToTray();
        }

        private void ApplySavedWindowPlacement()
        {
            if (settings.WindowLeft is not { } left
                || settings.WindowTop is not { } top
                || settings.WindowWidth is not { } width
                || settings.WindowHeight is not { } height)
                return;

            var bounds = new Rectangle(
                left,
                top,
                Math.Max(width, MinimumSize.Width),
                Math.Max(height, MinimumSize.Height));

            if (!IsWindowBoundsVisible(bounds))
                return;

            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;

            if (settings.WindowMaximized)
                WindowState = FormWindowState.Maximized;
        }

        private void SaveWindowPlacement()
        {
            if (WindowState == FormWindowState.Minimized)
                return;

            var normalBounds = WindowState == FormWindowState.Maximized ? RestoreBounds : Bounds;
            if (normalBounds.Width <= 0 || normalBounds.Height <= 0)
                return;

            settings.SetWindowPlacement(normalBounds, WindowState == FormWindowState.Maximized);
        }

        private void ApplyInitialDetailSplitDistance()
        {
            if (detailSplitContainer.Height <= 0)
                return;

            var availableHeight = detailSplitContainer.Height - detailSplitContainer.SplitterWidth;
            var splitterDistance = Math.Clamp(
                (int)Math.Round(availableHeight * 0.4),
                detailSplitContainer.Panel1MinSize,
                Math.Max(detailSplitContainer.Panel1MinSize, availableHeight - detailSplitContainer.Panel2MinSize));
            if (splitterDistance > 0)
                detailSplitContainer.SplitterDistance = splitterDistance;
        }

        private void ApplySavedTableSortState()
        {
            usageSortProperty = NormalizeUsageSortProperty(settings.UsageSortProperty);
            usageSortOrder = GetSavedSortOrder(settings.UsageSortDescending, SortOrder.Descending);
            dailyUsageTrendSortProperty = NormalizeDailyUsageTrendSortProperty(settings.DailyUsageTrendSortProperty);
            dailyUsageTrendSortOrder = GetSavedSortOrder(settings.DailyUsageTrendSortDescending, SortOrder.Descending);
            timelineSortProperty = NormalizeTimelineSortProperty(settings.TimelineSortProperty);
            timelineSortOrder = GetSavedSortOrder(settings.TimelineSortDescending, SortOrder.Descending);
            runtimeSortProperty = NormalizeRuntimeSortProperty(settings.RuntimeSortProperty);
            runtimeSortOrder = GetSavedSortOrder(settings.RuntimeSortDescending, SortOrder.Descending);
            runtimeSegmentSortProperty = NormalizeRuntimeSegmentSortProperty(settings.RuntimeSegmentSortProperty);
            runtimeSegmentSortOrder = GetSavedSortOrder(settings.RuntimeSegmentSortDescending, SortOrder.Descending);
        }

        private void SaveTableSortState()
        {
            settings.UsageSortProperty = usageSortProperty;
            settings.UsageSortDescending = usageSortOrder == SortOrder.Descending;
            settings.DailyUsageTrendSortProperty = dailyUsageTrendSortProperty;
            settings.DailyUsageTrendSortDescending = dailyUsageTrendSortOrder == SortOrder.Descending;
            settings.TimelineSortProperty = timelineSortProperty;
            settings.TimelineSortDescending = timelineSortOrder == SortOrder.Descending;
            settings.RuntimeSortProperty = runtimeSortProperty;
            settings.RuntimeSortDescending = runtimeSortOrder == SortOrder.Descending;
            settings.RuntimeSegmentSortProperty = runtimeSegmentSortProperty;
            settings.RuntimeSegmentSortDescending = runtimeSegmentSortOrder == SortOrder.Descending;
            settings.Save();
        }

        private void RegisterTableColumnLayoutPersistence()
        {
            foreach (var grid in GetLayoutPersistedGrids())
            {
                grid.ColumnDisplayIndexChanged += OnTableColumnLayoutChanged;
                grid.ColumnWidthChanged += OnTableColumnLayoutChanged;
            }
        }

        private void ApplySavedTableColumnLayouts()
        {
            ApplyTableColumnLayouts(settings.TableColumnLayouts);
        }

        private void ApplyTableColumnLayouts(
            IReadOnlyDictionary<string, List<AppSettings.TableColumnLayout>> layouts)
        {
            isApplyingTableColumnLayouts = true;
            try
            {
                foreach (var grid in GetLayoutPersistedGrids())
                {
                    if (layouts.TryGetValue(grid.Name, out var layout))
                        ApplyTableColumnLayout(grid, layout);
                }
            }
            finally
            {
                isApplyingTableColumnLayouts = false;
            }
        }

        private static void ApplyTableColumnLayout(
            DataGridView grid,
            IReadOnlyList<AppSettings.TableColumnLayout> layout)
        {
            foreach (var columnLayout in layout.OrderBy(x => x.DisplayIndex))
            {
                if (!grid.Columns.Contains(columnLayout.Name))
                    continue;

                var column = grid.Columns[columnLayout.Name];
                column.Width = Math.Clamp(columnLayout.Width, column.MinimumWidth, 10000);
                column.DisplayIndex = Math.Clamp(columnLayout.DisplayIndex, 0, grid.Columns.Count - 1);
            }
        }

        private Dictionary<string, List<AppSettings.TableColumnLayout>> CaptureTableColumnLayouts()
        {
            return GetLayoutPersistedGrids()
                .ToDictionary(
                    grid => grid.Name,
                    CaptureTableColumnLayout,
                    StringComparer.Ordinal);
        }

        private static List<AppSettings.TableColumnLayout> CaptureTableColumnLayout(DataGridView grid)
        {
            return grid.Columns
                .Cast<DataGridViewColumn>()
                .Select(column => new AppSettings.TableColumnLayout
                {
                    Name = column.Name,
                    DisplayIndex = column.DisplayIndex,
                    Width = column.Width
                })
                .OrderBy(column => column.DisplayIndex)
                .ToList();
        }

        private IEnumerable<DataGridView> GetLayoutPersistedGrids()
        {
            yield return usageGrid;
            yield return dailyUsageTrendGrid;
            yield return timelineGrid;
            yield return runtimeGrid;
            yield return runtimeSegmentsGrid;
        }

        private void SaveTableColumnLayouts()
        {
            settings.TableColumnLayouts = CaptureTableColumnLayouts();
            settings.Save();
        }

        private void OnTableColumnLayoutChanged(object? sender, DataGridViewColumnEventArgs e)
        {
            if (isApplyingTableColumnLayouts)
                return;

            SaveTableColumnLayouts();
        }

        private void ResetTableSortState()
        {
            settings.ResetTableSortStates();
            ApplySavedTableSortState();
            ApplyTableColumnLayouts(defaultTableColumnLayouts);
            UpdateSortGlyphs();
            RefreshViews(DateTimeOffset.UtcNow);
            SetStatusText(GetTableSortResetStatusText());
        }

        private string GetResetTableSortMenuText()
        {
            return settings.UiLanguage == UiLanguage.English
                ? "Reset table sorting"
                : "화면 정렬 초기화";
        }

        private string GetTableSortResetStatusText()
        {
            return settings.UiLanguage == UiLanguage.English
                ? "Table sorting has been reset to defaults."
                : "화면 정렬을 기본값으로 되돌렸습니다.";
        }

        private string GetAppCategoryManagementMenuText()
        {
            return settings.UiLanguage == UiLanguage.English
                ? "App Categories..."
                : "앱 분류 관리...";
        }

        private static bool IsWindowBoundsVisible(Rectangle bounds)
        {
            return Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(bounds));
        }

        private void OnShown(object? sender, EventArgs e)
        {
            if (startMinimizedToTray)
            {
                BeginInvoke(() =>
                {
                    HideToTray();
                    ShowProcessRuntimeSafeModeNoticeIfNeeded();
                });
                return;
            }

            BeginInvoke(ShowStartupNotices);
        }

        private void ShowStartupNotices()
        {
            ShowProcessRuntimeSafeModeNoticeIfNeeded();
            ShowStartupPromptIfNeeded();
        }

        private void ShowProcessRuntimeSafeModeNoticeIfNeeded()
        {
            if (!processRuntimeSafeModeActivated)
                return;

            if (startMinimizedToTray)
            {
                trayIcon.ShowBalloonTip(
                    8000,
                    UiText.Main.SafeModeTitle,
                    UiText.Main.SafeModeBalloonMessage,
                    ToolTipIcon.Warning);
                return;
            }

            CenteredMessageDialog.Show(
                this,
                UiText.Main.SafeModeMessage,
                UiText.Main.SafeModeTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void ShowStartupPromptIfNeeded()
        {
            if (settings.StartupPromptShown || startMinimizedToTray || isClosing)
                return;

            var result = CenteredMessageDialog.Show(
                this,
                UiText.Main.StartupPromptMessage,
                UiText.Main.StartupPromptTitle,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            settings.SetStartupPromptResult(result == DialogResult.Yes);
        }

        private void ApplyProcessRuntimeSafeModeIfNeeded()
        {
            if (storage is null
                || !AppSettings.IsDangerousProcessRuntimeTracking(
                    settings.ProcessRuntimeTrackingEnabled,
                    settings.ProcessRuntimeTrackingScope,
                    settings.ProcessRuntimeSampleIntervalSeconds))
                return;

            if (!storage.HasRecentRepeatedShortUnexpectedRuntimeSessions(
                    SafeModeUnexpectedExitCount,
                    SafeModeShortRuntimeThreshold))
                return;

            settings.DisableProcessRuntimeTrackingForSafeMode();
            processRuntimeSafeModeActivated = true;
        }

        private static DateTimeOffset GetCurrentSystemBootedAt(DateTimeOffset now)
        {
            return now - TimeSpan.FromMilliseconds(Environment.TickCount64);
        }

        private void RegisterWindowsSystemEventHandlers()
        {
            if (systemEventHandlersRegistered)
                return;

            SystemEvents.SessionSwitch += OnSystemSessionSwitch;
            SystemEvents.PowerModeChanged += OnSystemPowerModeChanged;
            SystemEvents.SessionEnding += OnSystemSessionEnding;
            systemEventHandlersRegistered = true;
        }

        private void UnregisterWindowsSystemEventHandlers()
        {
            if (!systemEventHandlersRegistered)
                return;

            SystemEvents.SessionSwitch -= OnSystemSessionSwitch;
            SystemEvents.PowerModeChanged -= OnSystemPowerModeChanged;
            SystemEvents.SessionEnding -= OnSystemSessionEnding;
            systemEventHandlersRegistered = false;
        }

        private void OnSystemSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            var eventType = e.Reason switch
            {
                SessionSwitchReason.SessionLock => "lock",
                SessionSwitchReason.SessionUnlock => "unlock",
                SessionSwitchReason.SessionLogon => "logon",
                SessionSwitchReason.SessionLogoff => "logoff",
                SessionSwitchReason.ConsoleConnect => "console-connect",
                SessionSwitchReason.ConsoleDisconnect => "console-disconnect",
                SessionSwitchReason.RemoteConnect => "remote-connect",
                SessionSwitchReason.RemoteDisconnect => "remote-disconnect",
                SessionSwitchReason.SessionRemoteControl => "remote-control",
                _ => "session-switch"
            };

            RecordWindowsSystemEvent(eventType, e.Reason.ToString());
        }

        private void OnSystemPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            var eventType = e.Mode switch
            {
                PowerModes.Suspend => "suspend",
                PowerModes.Resume => "resume",
                PowerModes.StatusChange => "power-status-change",
                _ => "power-mode"
            };

            RecordWindowsSystemEvent(eventType, e.Mode.ToString());
        }

        private void OnSystemSessionEnding(object sender, SessionEndingEventArgs e)
        {
            var eventType = e.Reason == SessionEndReasons.Logoff
                ? "logoff"
                : "system-shutdown";

            RecordWindowsSystemEvent(eventType, $"SessionEnding:{e.Reason}");
        }

        private void RecordWindowsSystemEvent(string eventType, string details)
        {
            if (storage is null)
                return;

            try
            {
                var observedAt = DateTimeOffset.UtcNow;
                storage.RecordSystemEvent(
                    eventType,
                    observedAt,
                    GetCurrentSystemBootedAt(observedAt),
                    details);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to record Windows system event: {ex}");
            }
        }

        private void InitializeSummaryPeriodSelector()
        {
            selectedSummaryPeriod = SummaryPeriod.Today;
            selectedSummarySpecificDate = DateTime.Today;
            selectedSummaryCustomStartDate = DateTime.Today;
            selectedSummaryCustomEndDate = DateTime.Today;
            RefreshDetailRuntimeFilterOptions();
            RefreshSummaryPeriodOptions(DateTime.Today);
        }

        private void RefreshDetailRuntimeFilterOptions()
        {
            isUpdatingDetailRuntimeFilterOptions = true;
            detailRuntimeFilterComboBox.BeginUpdate();
            try
            {
                detailRuntimeFilterComboBox.Items.Clear();
                detailRuntimeFilterComboBox.Items.AddRange(new object[]
                {
                    UiText.Main.DetailFilterSummaryApps,
                    UiText.Main.DetailFilterCurrentScope,
                    UiText.Main.DetailFilterVisibleApps,
                    UiText.Main.DetailFilterUserProcesses,
                    UiText.Main.DetailFilterAllRecords
                });
                SyncDetailRuntimeFilterComboBoxSelection();
            }
            finally
            {
                detailRuntimeFilterComboBox.EndUpdate();
                isUpdatingDetailRuntimeFilterOptions = false;
            }
        }

        private void SyncDetailRuntimeFilterComboBoxSelection()
        {
            if (detailRuntimeFilterComboBox.Items.Count == 0)
                return;

            var selectedIndex = Math.Clamp(
                (int)selectedDetailRuntimeFilter,
                0,
                detailRuntimeFilterComboBox.Items.Count - 1);

            if (detailRuntimeFilterComboBox.SelectedIndex != selectedIndex)
                detailRuntimeFilterComboBox.SelectedIndex = selectedIndex;
        }

        private void RefreshSummaryPeriodOptions(DateTime today)
        {
            var options = SummaryPeriodOption.GetOptions(today);
            var selectedIndex = Array.FindIndex(options.ToArray(), option => option.Period == selectedSummaryPeriod);

            isInitializingSummaryPeriodSelector = true;
            summaryPeriodComboBox.BeginUpdate();
            try
            {
                summaryPeriodComboBox.Items.Clear();
                summaryPeriodComboBox.Items.AddRange(options.Cast<object>().ToArray());
                summaryPeriodComboBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;

                summaryPeriodOptionsDate = today;
                selectedSummaryPeriod = ((SummaryPeriodOption)summaryPeriodComboBox.SelectedItem!).Period;

                if (selectedSummarySpecificDate > today)
                    selectedSummarySpecificDate = today;
                if (selectedSummaryCustomStartDate > today)
                    selectedSummaryCustomStartDate = today;
                if (selectedSummaryCustomEndDate > today)
                    selectedSummaryCustomEndDate = today;
                if (selectedSummaryCustomEndDate < selectedSummaryCustomStartDate)
                    selectedSummaryCustomEndDate = selectedSummaryCustomStartDate;

                if (summarySpecificDatePicker.Value.Date > today)
                    summarySpecificDatePicker.Value = today;

                summarySpecificDatePicker.MaxDate = today;
                summarySpecificDatePicker.Value = selectedSummarySpecificDate;
                UpdateSummaryPeriodControlsVisibility();
            }
            finally
            {
                summaryPeriodComboBox.EndUpdate();
                isInitializingSummaryPeriodSelector = false;
            }
        }

        private void RefreshSummaryPeriodOptionsIfDateChanged(DateTimeOffset observedAt)
        {
            var today = observedAt.ToLocalTime().Date;

            if (today == summaryPeriodOptionsDate)
                return;

            RefreshSummaryPeriodOptions(today);
        }

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

        private void InitializeTimelineCategoryBucketSelector()
        {
            RefreshTimelineCategoryBucketOptions();
        }

        private void RefreshTimelineCategoryBucketOptions()
        {
            var options = TimelineCategoryBucketOption.GetOptions();
            var selectedIndex = Array.FindIndex(options.ToArray(), option => option.Minutes == selectedTimelineCategoryBucketMinutes);
            timelineCategoryBucketComboBox.BeginUpdate();
            try
            {
                timelineCategoryBucketComboBox.Items.Clear();
                timelineCategoryBucketComboBox.Items.AddRange(options.Cast<object>().ToArray());
                timelineCategoryBucketComboBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 1;
                selectedTimelineCategoryBucketMinutes = ((TimelineCategoryBucketOption)timelineCategoryBucketComboBox.SelectedItem!).Minutes;
            }
            finally
            {
                timelineCategoryBucketComboBox.EndUpdate();
            }
        }

        private void InitializeTimelineTypeHighlightSelector()
        {
            RefreshTimelineTypeHighlightOptions();
        }

        private void InitializeTimelineSystemEventFilterSelector()
        {
            timelineSystemEventFilterLabel.AutoSize = true;
            timelineSystemEventFilterLabel.Margin = new Padding(12, 7, 3, 0);
            timelineSystemEventFilterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            timelineSystemEventFilterComboBox.Width = 112;
            timelineSystemEventFilterComboBox.SelectedIndexChanged += OnTimelineSystemEventFilterComboBoxSelectedIndexChanged;
            timelineZoomPanel.WrapContents = false;
            timelineZoomPanel.Height = 32;
            timelineZoomPanel.Controls.Add(timelineSystemEventFilterLabel);
            timelineZoomPanel.Controls.Add(timelineSystemEventFilterComboBox);
            RefreshTimelineSystemEventFilterOptions();
        }

        private void InitializeRuntimeSegmentTimeline()
        {
            detailSplitContainer.Panel2.Controls.Remove(runtimeSegmentsGrid);

            runtimeSegmentPanel.SuspendLayout();
            runtimeSegmentPanel.Dock = DockStyle.Fill;
            runtimeSegmentPanel.ColumnCount = 1;
            runtimeSegmentPanel.RowCount = 2;
            runtimeSegmentPanel.ColumnStyles.Clear();
            runtimeSegmentPanel.RowStyles.Clear();
            runtimeSegmentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            runtimeSegmentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 138));
            runtimeSegmentPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            runtimeSegmentTimelinePanel.Dock = DockStyle.Fill;
            runtimeSegmentTimelinePanel.ColumnCount = 1;
            runtimeSegmentTimelinePanel.RowCount = 3;
            runtimeSegmentTimelinePanel.ColumnStyles.Clear();
            runtimeSegmentTimelinePanel.RowStyles.Clear();
            runtimeSegmentTimelinePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            runtimeSegmentTimelinePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            runtimeSegmentTimelinePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            runtimeSegmentTimelinePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 17));

            runtimeSegmentZoomPanel.Dock = DockStyle.Fill;
            runtimeSegmentZoomPanel.Height = 36;
            runtimeSegmentZoomPanel.Padding = new Padding(8, 3, 8, 3);
            runtimeSegmentZoomPanel.WrapContents = false;
            runtimeSegmentZoomRangeLabel.AutoSize = true;
            runtimeSegmentZoomRangeLabel.ForeColor = SystemColors.GrayText;
            runtimeSegmentZoomRangeLabel.Margin = new Padding(0, 7, 12, 0);
            ConfigureRuntimeSegmentZoomButton(runtimeSegmentZoomOutButton, OnRuntimeSegmentZoomOutButtonClick);
            ConfigureRuntimeSegmentZoomButton(runtimeSegmentZoomInButton, OnRuntimeSegmentZoomInButtonClick);
            ConfigureRuntimeSegmentZoomButton(runtimeSegmentPreviousButton, OnRuntimeSegmentPreviousButtonClick);
            ConfigureRuntimeSegmentZoomButton(runtimeSegmentNextButton, OnRuntimeSegmentNextButtonClick);
            ConfigureRuntimeSegmentZoomButton(runtimeSegmentResetButton, OnRuntimeSegmentResetButtonClick, width: 52);

            runtimeSegmentTimelineControl.Dock = DockStyle.Fill;
            runtimeSegmentTimelineControl.ViewRangeChanged += OnRuntimeSegmentTimelineViewRangeChanged;
            runtimeSegmentObservationFilterLabel.AutoSize = true;
            runtimeSegmentObservationFilterLabel.Margin = new Padding(12, 8, 3, 0);
            runtimeSegmentObservationFilterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            runtimeSegmentObservationFilterComboBox.Width = 132;
            runtimeSegmentObservationFilterComboBox.SelectedIndexChanged += OnRuntimeSegmentObservationFilterComboBoxSelectedIndexChanged;
            runtimeSegmentHelpButton.Size = new Size(28, 23);
            runtimeSegmentHelpButton.Margin = new Padding(3, 2, 3, 0);
            runtimeSegmentHelpButton.UseVisualStyleBackColor = true;
            runtimeSegmentHelpButton.Click += OnRuntimeSegmentHelpButtonClick;
            runtimeSegmentZoomScrollBar.Dock = DockStyle.Fill;
            runtimeSegmentZoomScrollBar.Enabled = false;
            runtimeSegmentZoomScrollBar.Visible = false;
            runtimeSegmentZoomScrollBar.Minimum = 0;
            runtimeSegmentZoomScrollBar.Maximum = 1000;
            runtimeSegmentZoomScrollBar.LargeChange = 1000;
            runtimeSegmentZoomScrollBar.Scroll += OnRuntimeSegmentZoomScrollBarScroll;
            runtimeSegmentsGrid.Dock = DockStyle.Fill;
            runtimeSegmentTimelineControl.SetSegments(
                selectedDetailDate,
                null,
                Array.Empty<ProcessRuntimeSegmentRow>());
            runtimeSegmentZoomPanel.Controls.Add(runtimeSegmentZoomRangeLabel);
            runtimeSegmentZoomPanel.Controls.Add(runtimeSegmentZoomOutButton);
            runtimeSegmentZoomPanel.Controls.Add(runtimeSegmentZoomInButton);
            runtimeSegmentZoomPanel.Controls.Add(runtimeSegmentPreviousButton);
            runtimeSegmentZoomPanel.Controls.Add(runtimeSegmentNextButton);
            runtimeSegmentZoomPanel.Controls.Add(runtimeSegmentResetButton);
            runtimeSegmentZoomPanel.Controls.Add(runtimeSegmentHelpButton);
            runtimeSegmentZoomPanel.Controls.Add(runtimeSegmentObservationFilterLabel);
            runtimeSegmentZoomPanel.Controls.Add(runtimeSegmentObservationFilterComboBox);
            runtimeSegmentTimelinePanel.Controls.Add(runtimeSegmentZoomPanel, 0, 0);
            runtimeSegmentTimelinePanel.Controls.Add(runtimeSegmentTimelineControl, 0, 1);
            runtimeSegmentTimelinePanel.Controls.Add(runtimeSegmentZoomScrollBar, 0, 2);
            runtimeSegmentPanel.Controls.Add(runtimeSegmentTimelinePanel, 0, 0);
            runtimeSegmentPanel.Controls.Add(runtimeSegmentsGrid, 0, 1);
            runtimeSegmentPanel.ResumeLayout();

            detailSplitContainer.Panel2.Controls.Add(runtimeSegmentPanel);
            runtimeSegmentsGrid.SelectionChanged += OnRuntimeSegmentsGridSelectionChanged;
            RefreshRuntimeSegmentObservationFilterOptions();
            UpdateRuntimeSegmentZoomControls();
        }

        private void RefreshTimelineTypeHighlightOptions()
        {
            var options = TimelineActivityTypeHighlightOption.GetOptions();
            var selectedIndex = Array.FindIndex(options.ToArray(), option => option.Value == selectedTimelineActivityTypeHighlight);
            timelineTypeHighlightComboBox.BeginUpdate();
            try
            {
                timelineTypeHighlightComboBox.Items.Clear();
                timelineTypeHighlightComboBox.Items.AddRange(options.Cast<object>().ToArray());
                timelineTypeHighlightComboBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
                selectedTimelineActivityTypeHighlight = ((TimelineActivityTypeHighlightOption)timelineTypeHighlightComboBox.SelectedItem!).Value;
                ApplyTimelineActivityTypeHighlight();
            }
            finally
            {
                timelineTypeHighlightComboBox.EndUpdate();
            }
        }

        private void RefreshTimelineSystemEventFilterOptions()
        {
            var options = TimelineSystemEventFilterOption.GetOptions();
            var selectedIndex = Array.FindIndex(options.ToArray(), option => option.Value == selectedTimelineSystemEventFilter);
            timelineSystemEventFilterComboBox.BeginUpdate();
            try
            {
                timelineSystemEventFilterComboBox.Items.Clear();
                timelineSystemEventFilterComboBox.Items.AddRange(options.Cast<object>().ToArray());
                timelineSystemEventFilterComboBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
                selectedTimelineSystemEventFilter = ((TimelineSystemEventFilterOption)timelineSystemEventFilterComboBox.SelectedItem!).Value;
            }
            finally
            {
                timelineSystemEventFilterComboBox.EndUpdate();
            }
        }

        private void RefreshRuntimeSegmentObservationFilterOptions()
        {
            var options = RuntimeSegmentObservationFilterOption.GetOptions();
            var selectedIndex = Array.FindIndex(options.ToArray(), option => option.Value == selectedRuntimeSegmentObservationFilter);
            runtimeSegmentObservationFilterComboBox.BeginUpdate();
            try
            {
                runtimeSegmentObservationFilterComboBox.Items.Clear();
                runtimeSegmentObservationFilterComboBox.Items.AddRange(options.Cast<object>().ToArray());
                runtimeSegmentObservationFilterComboBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
                selectedRuntimeSegmentObservationFilter = ((RuntimeSegmentObservationFilterOption)runtimeSegmentObservationFilterComboBox.SelectedItem!).Value;
            }
            finally
            {
                runtimeSegmentObservationFilterComboBox.EndUpdate();
            }
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

        private void ConfigureDesignPreview()
        {
            statusLabel.Text = $"{UiText.Main.ForegroundPrefix}Visual Studio · {UiText.Main.Active}";
            SetRuntimeCoverageSummaryParts(
                UiText.RuntimeCoverage.Coverage(0.981),
                UiText.RuntimeCoverage.Tracked("07:48:12"),
                UiText.RuntimeCoverage.Missing("00:09:00"),
                UiText.RuntimeCoverage.LongestMissing("00:04:12"));
            usageGrid.DataSource = AddIcons(new List<UsageSummaryRow>
            {
                new(1, "Microsoft Visual Studio", "devenv", null, null, "개발", 3_900_000, 0.54, 8, null, DateTimeOffset.Now.AddHours(-2), DateTimeOffset.Now),
                new(2, "Google Chrome", "chrome", null, null, "자료조사/브라우징", 1_680_000, 0.23, 15, null, DateTimeOffset.Now.AddHours(-1), DateTimeOffset.Now.AddMinutes(-12)),
                new(3, "File Explorer", "explorer", null, null, null, 900_000, 0.13, 4, null, DateTimeOffset.Now.AddMinutes(-45), DateTimeOffset.Now.AddMinutes(-5))
            });
            dailyUsageTrendGrid.DataSource = new List<DailyUsageTrendRow>
            {
                new(DateTime.Today, 6_480_000, "Microsoft Visual Studio", 3_900_000),
                new(DateTime.Today.AddDays(-1), 4_560_000, "Google Chrome", 2_400_000)
            };
            timelineGrid.DataSource = AddIcons(new List<ActivityTimelineRow>
            {
                new(UiText.Main.Active, DateTimeOffset.Now.AddHours(-2), DateTimeOffset.Now.AddHours(-1), 3_600_000, "devenv"),
                new(UiText.Main.Idle, DateTimeOffset.Now.AddHours(-1), DateTimeOffset.Now.AddMinutes(-45), 900_000, "devenv"),
                new(UiText.Main.Active, DateTimeOffset.Now.AddMinutes(-45), null, 2_700_000, "chrome")
            });
            timelineOverviewControl.SetTimeline(
                DateTime.Today,
                (IReadOnlyList<ActivityTimelineRow>)timelineGrid.DataSource,
                new[]
                {
                    new TimelineRange(DateTimeOffset.Now.AddHours(-3), DateTimeOffset.Now)
                },
                new[]
                {
                    new SystemTimelineRange(DateTimeOffset.Now.AddHours(-1.5), DateTimeOffset.Now.AddHours(-1.25), SystemTimelineRangeType.LockSession)
                },
                new[]
                {
                    new SystemTimelineEvent(DateTimeOffset.Now.AddHours(-2.5), "timepilot-start", "Preview"),
                    new SystemTimelineEvent(DateTimeOffset.Now.AddMinutes(-50), "lock", "Preview")
                },
                new[]
                {
                    new CategoryTimelineSegment(
                        DateTimeOffset.Now.AddHours(-3),
                        DateTimeOffset.Now.AddHours(-2),
                        "개발",
                        "#2563EB",
                        false,
                        3_600_000,
                        "개발 100%"),
                    new CategoryTimelineSegment(
                        DateTimeOffset.Now.AddHours(-2),
                        DateTimeOffset.Now.AddHours(-1),
                        "개발",
                        "#2563EB",
                        true,
                        3_600_000,
                        "개발 45%, 자료조사/브라우징 40%")
                });
            currentTimelineSystemEvents =
            [
                new SystemTimelineEvent(DateTimeOffset.Now.AddHours(-2.5), "timepilot-start", "Preview"),
                new SystemTimelineEvent(DateTimeOffset.Now.AddMinutes(-50), "lock", "Preview"),
                new SystemTimelineEvent(DateTimeOffset.Now.AddMinutes(-30), "unlock", "Preview")
            ];
        }

        private void SetRuntimeCoverageSummary(RuntimeCoverageSummary? summary)
        {
            if (summary is null)
            {
                SetRuntimeCoverageSummaryParts(UiText.RuntimeCoverage.NotChecked);
                return;
            }

            SetRuntimeCoverageSummaryParts(summary.SummaryParts);
        }

        private void SetSummaryIdleAnalysis(
            IReadOnlyList<ForegroundUsageSummary> foregroundUsage,
            IdleUsageSummary? idleUsage)
        {
            var activeMs = foregroundUsage.Sum(x => x.ActiveUsageMs);
            var idleMs = idleUsage?.IdleMs ?? 0;
            var totalMs = activeMs + idleMs;
            if (totalMs <= 0)
            {
                summaryIdleAnalysisPanel.Visible = false;
                summaryIdleAnalysisLabel.Text = "";
                return;
            }

            var inputActivityRatio = (double)activeMs / totalMs;
            summaryIdleAnalysisLabel.Text = UiText.Main.SummaryIdleAnalysis(
                FormatDiagnosticDuration(activeMs),
                FormatDiagnosticDuration(idleMs),
                inputActivityRatio,
                settings.IdleThresholdMinutes);
            summaryIdleAnalysisPanel.Visible = true;
            UpdateSummaryIdleAnalysisPanelHeight();
        }

        private void SetRuntimeCoverageSummaryParts(params string[] parts)
        {
            SetRuntimeCoverageSummaryParts((IEnumerable<string>)parts);
        }

        private void SetRuntimeCoverageSummaryParts(IEnumerable<string> parts)
        {
            var partList = parts
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToList();
            runtimeCoverageSummaryPanel.Visible = partList.Count > 0;
            var toolTipText = runtimeCoverageSummaryToolTip.GetToolTip(runtimeCoverageSummaryPanel);

            runtimeCoverageSummaryPanel.SuspendLayout();
            try
            {
                while (runtimeCoverageSummaryLabels.Count < partList.Count)
                {
                    var label = new Label
                    {
                        AutoSize = true,
                        Margin = new Padding(0, 4, 14, 0),
                        TextAlign = ContentAlignment.MiddleLeft
                    };

                    runtimeCoverageSummaryToolTip.SetToolTip(label, toolTipText);
                    runtimeCoverageSummaryLabels.Add(label);
                    runtimeCoverageSummaryPanel.Controls.Add(label);
                }

                for (var i = 0; i < partList.Count; i++)
                {
                    if (runtimeCoverageSummaryLabels[i].Text != partList[i])
                        runtimeCoverageSummaryLabels[i].Text = partList[i];
                }

                while (runtimeCoverageSummaryLabels.Count > partList.Count)
                {
                    var lastIndex = runtimeCoverageSummaryLabels.Count - 1;
                    var label = runtimeCoverageSummaryLabels[lastIndex];
                    runtimeCoverageSummaryLabels.RemoveAt(lastIndex);
                    runtimeCoverageSummaryPanel.Controls.Remove(label);
                    label.Dispose();
                }

                UpdateRuntimeCoverageSummaryPanelHeight();
            }
            finally
            {
                runtimeCoverageSummaryPanel.ResumeLayout();
            }
        }

        private void UpdateRuntimeCoverageSummaryPanelHeight()
        {
            var preferredHeight = runtimeCoverageSummaryPanel.GetPreferredSize(
                new Size(runtimeCoverageSummaryPanel.Width, 0)).Height;
            runtimeCoverageSummaryPanel.Height = Math.Clamp(preferredHeight, 24, 48);
        }

        private void UpdateSummaryIdleAnalysisPanelHeight()
        {
            var preferredHeight = summaryIdleAnalysisPanel.GetPreferredSize(
                new Size(summaryIdleAnalysisPanel.Width, 0)).Height;
            summaryIdleAnalysisPanel.Height = Math.Clamp(preferredHeight, 24, 48);
        }

        private void ConfigureTrayIcon()
        {
            var openMenuItem = new ToolStripMenuItem(UiText.Main.OpenWindow);
            openMenuItem.Click += (_, _) => ShowMainWindow();

            var exitTrayMenuItem = new ToolStripMenuItem(UiText.Main.Exit);
            exitTrayMenuItem.Click += (_, _) => ExitApplication();

            trayMenu.Items.AddRange(new ToolStripItem[]
            {
                openMenuItem,
                new ToolStripSeparator(),
                exitTrayMenuItem
            });

            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Icon = LoadAppIcon();
            trayIcon.Text = UiText.AppName;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += (_, _) => ShowMainWindow();
        }

        private void ApplyUiText()
        {
            Text = UiText.AppName;
            fileMenuItem.Text = UiText.Main.FileMenu;
            exportCsvMenuItem.Text = UiText.Main.ExportCsv;
            exportRawDataMenuItem.Text = UiText.Main.ExportRawData;
            createDataBackupMenuItem.Text = UiText.Main.CreateDataBackup;
            restoreDataBackupMenuItem.Text = UiText.Main.RestoreDataBackup;
            exitMenuItem.Text = UiText.Main.Exit;
            settingsMenuItem.Text = UiText.Main.SettingsMenu;
            preferencesMenuItem.Text = UiText.Main.Preferences;
            appCategoryManagementMenuItem.Text = GetAppCategoryManagementMenuText();
            resetTableSortMenuItem.Text = GetResetTableSortMenuText();
            helpMenuItem.Text = UiText.Main.HelpMenu;
            runtimeDiagnosticsMenuItem.Text = UiText.Main.RuntimeDiagnostics;
            sponsorMenuItem.Text = UiText.Main.Sponsor;
            aboutMenuItem.Text = UiText.Main.About;

            summaryTab.Text = UiText.Main.SummaryTab;
            detailTab.Text = UiText.Main.DetailTab;
            timelineTab.Text = UiText.Main.TimelineTab;
            summaryPeriodLabel.Text = UiText.Main.Period;
            summarySpecificDateCalendarButton.Text = UiText.Main.Calendar;
            summaryCustomRangeButton.Text = UiText.SummaryPeriod.CustomRangeButton;
            UpdateSummaryCustomRangeLabel();
            detailDateLabel.Text = UiText.Main.Date;
            detailCalendarButton.Text = UiText.Main.Calendar;
            detailTodayButton.Text = UiText.Main.Today;
            detailRuntimeFilterLabel.Text = UiText.Main.DetailRuntimeFilter;
            runningRuntimeOnlyCheckBox.Text = UiText.Main.RunningOnly;
            detailHelpButton.Text = UiText.Main.DetailHelp;
            detailDescriptionLabel.Text = UiText.Main.DetailDescription;
            detailTrackingDisabledLabel.Text = UiText.Main.DetailTrackingDisabledMessage;
            detailTrackingDisabledPreferencesButton.Text = UiText.Main.DetailTrackingDisabledOpenPreferences;
            runtimeSegmentTimelineControl.Invalidate();
            runtimeSegmentZoomOutButton.Text = UiText.Main.TimelineZoomOut;
            runtimeSegmentZoomInButton.Text = UiText.Main.TimelineZoomIn;
            runtimeSegmentPreviousButton.Text = UiText.Main.TimelinePanPrevious;
            runtimeSegmentNextButton.Text = UiText.Main.TimelinePanNext;
            runtimeSegmentObservationFilterLabel.Text = GetRuntimeSegmentObservationFilterLabelText();
            runtimeSegmentResetButton.Text = GetRuntimeSegmentResetText();
            runtimeSegmentHelpButton.Text = UiText.Main.DetailHelp;
            UpdateRuntimeSegmentZoomControls();
            timelineDateLabel.Text = UiText.Main.Date;
            timelineCalendarButton.Text = UiText.Main.Calendar;
            timelineTodayButton.Text = UiText.Main.Today;
            timelineHighlightClearButton.Text = UiText.Main.ClearTimelineHighlight;
            timelineHighlightHintLabel.Text = UiText.Main.TimelineHighlightHint;
            timelineZoomOutButton.Text = UiText.Main.TimelineZoomOut;
            timelineZoomInButton.Text = UiText.Main.TimelineZoomIn;
            timelineZoomPreviousButton.Text = UiText.Main.TimelinePanPrevious;
            timelineZoomNextButton.Text = UiText.Main.TimelinePanNext;
            timelineZoomResetButton.Text = UiText.Main.TimelineResetView;
            timelineHelpButton.Text = UiText.Main.TimelineHelp;
            timelineCategoryBucketLabel.Text = UiText.Main.TimelineCategoryBucket;
            timelineTypeHighlightLabel.Text = settings.UiLanguage == UiLanguage.English ? "Highlight type" : "유형 강조";
            timelineSystemEventFilterLabel.Text = settings.UiLanguage == UiLanguage.English ? "System event" : "시스템 이벤트";
            timelineOverviewControl.Invalidate();
            UpdateTimelineZoomControls();
            UpdateTimelineHighlightUi();
            RefreshTimelineCategoryBucketOptions();
            RefreshTimelineTypeHighlightOptions();
            RefreshTimelineSystemEventFilterOptions();
            RefreshRuntimeSegmentObservationFilterOptions();

            dailyUsageDateColumn.HeaderText = UiText.Main.Date;
            dailyUsageActiveTimeColumn.HeaderText = UiText.Main.TotalActiveUsageTime;
            dailyUsageTopAppColumn.HeaderText = UiText.Main.TopApp;
            dailyUsageTopAppTimeColumn.HeaderText = UiText.Main.TopAppTime;
            appNameColumn.HeaderText = UiText.Main.App;
            appCategoryColumn.HeaderText = UiText.Main.Category;
            firstStartedAtColumn.HeaderText = UiText.Main.FirstStartedAt;
            lastObservedAtColumn.HeaderText = UiText.Main.LastObservedAt;
            activeUsageTimeColumn.HeaderText = UiText.Main.ActiveUsageTime;
            idleRecordedTimeColumn.HeaderText = UiText.Main.IdleRecordedTime;
            idleRecordedTimeColumn.ToolTipText = UiText.Main.IdleRecordedTimeTooltip;
            usageRatioColumn.HeaderText = UiText.Main.ActiveRatio;
            usageRatioColumn.ToolTipText = UiText.Main.UsageRatioTooltip;
            switchCountColumn.HeaderText = UiText.Main.SwitchCount;
            summaryHighlightHintLabel.Text = UiText.Main.SummaryTimelineHighlightHint;

            runtimeAppNameColumn.HeaderText = UiText.Main.App;
            runtimeCategoryColumn.HeaderText = UiText.Main.Category;
            runtimeTrackingTypeColumn.HeaderText = UiText.Main.Type;
            runtimeTrackingTypeColumn.ToolTipText = UiText.Main.RuntimeTrackingTypeTooltip;
            runtimeFirstObservedAtColumn.HeaderText = UiText.Main.FirstObservedAt;
            runtimeLastObservedAtColumn.HeaderText = UiText.Main.LastObservedAt;
            runtimeLastObservedAtColumn.ToolTipText = UiText.Main.RuntimeLastObservedTooltip;
            runtimeDurationColumn.HeaderText = UiText.Main.Runtime;
            runtimeDurationColumn.ToolTipText = UiText.Main.RuntimeDurationTooltip;
            runtimeActiveUsageColumn.HeaderText = UiText.Main.ActiveUsageTime;
            runtimeIdleRecordedColumn.HeaderText = UiText.Main.IdleRecordedTime;
            runtimeIdleRecordedColumn.ToolTipText = UiText.Main.IdleRecordedTimeTooltip;
            runtimeActualUsageRatioColumn.HeaderText = UiText.Main.ActualUsageRatio;
            runtimeActualUsageRatioColumn.ToolTipText = UiText.Main.RuntimeActualUsageRatioTooltip;
            runtimeSessionCountColumn.HeaderText = UiText.Main.RuntimeSegmentCount;
            runtimeSessionCountColumn.ToolTipText = UiText.Main.RuntimeSegmentCountTooltip;
            runtimeStatusColumn.HeaderText = UiText.Main.Status;
            runtimeStatusColumn.ToolTipText = UiText.Main.RuntimeStatusTooltip;
            runtimeSegmentStartedAtColumn.HeaderText = UiText.Main.Start;
            runtimeSegmentEndedAtColumn.HeaderText = UiText.Main.End;
            runtimeSegmentDurationColumn.HeaderText = UiText.Main.Duration;
            runtimeSegmentStatusColumn.HeaderText = UiText.Main.Status;
            runtimeSegmentObservationTypeColumn.HeaderText = UiText.Main.ObservationBasis;
            runtimeSegmentObservationTypeColumn.ToolTipText = UiText.Main.RuntimeSegmentObservationTooltip;
            runtimeSegmentProcessIdColumn.HeaderText = UiText.Main.Pid;

            timelineTypeColumn.HeaderText = UiText.Main.Type;
            timelineStartedAtColumn.HeaderText = UiText.Main.Start;
            timelineEndedAtColumn.HeaderText = UiText.Main.End;
            timelineDurationColumn.HeaderText = UiText.Main.Duration;
            timelineDisplayNameColumn.HeaderText = UiText.Main.App;
            timelineCategoryColumn.HeaderText = UiText.Main.Category;

            if (trayMenu.Items.Count > 0)
            {
                if (trayMenu.Items[0] is ToolStripMenuItem openItem)
                    openItem.Text = UiText.Main.OpenWindow;

                if (trayMenu.Items[^1] is ToolStripMenuItem exitItem)
                    exitItem.Text = UiText.Main.Exit;
            }

            trayIcon.Text = UiText.AppName;
            runtimeCoverageSummaryToolTip.SetToolTip(runtimeCoverageSummaryPanel, UiText.RuntimeCoverage.Tooltip);
            runtimeCoverageSummaryToolTip.SetToolTip(summarySpecificDateCalendarButton, UiText.Main.RecordedDateCalendarTooltip);
            runtimeCoverageSummaryToolTip.SetToolTip(summaryCustomRangeButton, UiText.SummaryPeriod.CustomRangeTitle);
            runtimeCoverageSummaryToolTip.SetToolTip(detailCalendarButton, UiText.Main.RecordedDateCalendarTooltip);
            runtimeCoverageSummaryToolTip.SetToolTip(detailRuntimeFilterComboBox, UiText.Main.RuntimeTrackingTypeTooltip);
            runtimeCoverageSummaryToolTip.SetToolTip(detailHelpButton, UiText.Main.DetailHelpTitle);
            runtimeCoverageSummaryToolTip.SetToolTip(runtimeSegmentResetButton, GetRuntimeSegmentResetTooltip());
            runtimeCoverageSummaryToolTip.SetToolTip(runtimeSegmentHelpButton, GetRuntimeSegmentHelpTitle());
            runtimeCoverageSummaryToolTip.SetToolTip(detailDescriptionLabel, UiText.Main.DetailDescription);
            runtimeCoverageSummaryToolTip.SetToolTip(timelineCalendarButton, UiText.Main.RecordedDateCalendarTooltip);
            runtimeCoverageSummaryToolTip.SetToolTip(timelineHelpButton, UiText.Main.TimelineHelpTitle);
            runtimeCoverageSummaryToolTip.SetToolTip(timelineHighlightSummaryPanel, UiText.Main.TimelineHighlightSummaryTooltip);
            runtimeCoverageSummaryToolTip.SetToolTip(timelineHighlightSummaryLabel, UiText.Main.TimelineHighlightSummaryTooltip);
            runtimeCoverageSummaryToolTip.SetToolTip(summaryIdleAnalysisPanel, UiText.Main.SummaryIdleAnalysisTooltip);
            runtimeCoverageSummaryToolTip.SetToolTip(summaryIdleAnalysisLabel, UiText.Main.SummaryIdleAnalysisTooltip);
            RefreshDetailRuntimeFilterOptions();
            RefreshSummaryPeriodOptions(DateTime.Today);
            UpdateDetailTrackingDisabledBanner();
            SetDateStatus(detailDateStatusLabel, null);
            SetDateStatus(timelineDateStatusLabel, null);
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private static Icon LoadAppIcon()
        {
            var assetIconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "TimePilot.ico");
            if (File.Exists(assetIconPath))
                return new Icon(assetIconPath);

            return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        }

        private void HideToTray()
        {
            Hide();
            ShowInTaskbar = false;
        }

        private void ShowMainWindow()
        {
            Show();
            ShowInTaskbar = true;
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;

            Activate();
        }

        private void ExitApplication()
        {
            isExplicitExitRequested = true;
            Close();
        }

        private void RefreshViews(DateTimeOffset observedAt)
        {
            _ = RefreshViewsAsync(observedAt);
        }

        private async Task RefreshViewsAsync(DateTimeOffset observedAt)
        {
            if (storage is null)
                return;

            if (isViewRefreshRunning)
            {
                ReportPerformanceEvents("view-skip");
                return;
            }

            var totalStopwatch = Stopwatch.StartNew();
            var appIdToRestore = selectedRuntimeAppId ?? GetSelectedRuntimeAppId();
            var runtimeFirstDisplayedRowIndex = GetFirstDisplayedRowIndex(runtimeGrid);
            var runtimeFirstDisplayedColumnIndex = GetFirstDisplayedColumnIndex(runtimeGrid);
            var runtimeHorizontalOffset = GetHorizontalScrollingOffset(runtimeGrid);
            var selectedTab = mainTabs.SelectedTab;
            RefreshSummaryPeriodOptionsIfDateChanged(observedAt);
            RefreshDateSelectorsIfDateChanged(observedAt);
            var summaryPeriodRange = SummaryPeriodCalculator.GetRange(
                observedAt,
                selectedSummaryPeriod,
                selectedSummarySpecificDate,
                selectedSummaryCustomStartDate,
                selectedSummaryCustomEndDate);
            var detailDate = selectedDetailDate;
            var timelineDate = selectedTimelineDate;
            isViewRefreshRunning = true;
            ViewRefreshSnapshot snapshot;
            try
            {
                snapshot = await Task.Run(() =>
                {
                    var readStopwatch = Stopwatch.StartNew();
                    var foregroundUsage = selectedTab == summaryTab
                        ? storage.GetForegroundUsageForPeriod(summaryPeriodRange.Start, summaryPeriodRange.End)
                        : null;
                    var dailyUsageTrendRows = selectedTab == summaryTab
                        ? storage.GetDailyUsageTrendForPeriod(summaryPeriodRange.Start, summaryPeriodRange.End)
                        : null;
                    var idleUsage = selectedTab == summaryTab
                        ? storage.GetIdleUsageForPeriod(summaryPeriodRange.Start, summaryPeriodRange.End)
                        : null;
                    var runtimeCoverage = selectedTab == summaryTab
                        ? storage.GetRuntimeCoverageForPeriod(summaryPeriodRange.Start, summaryPeriodRange.End, observedAt)
                        : null;
                    var timelineRows = selectedTab == timelineTab
                        ? storage.GetActivityTimelineForDate(timelineDate, observedAt)
                        : null;
                    var windowsRuntimeRanges = selectedTab == timelineTab
                        ? storage.GetWindowsRuntimeRangesForDate(timelineDate, observedAt)
                        : null;
                    var systemTimelineEvents = selectedTab == timelineTab
                        ? storage.GetSystemTimelineEventsForDate(timelineDate, observedAt)
                        : null;
                    var inferredSystemTimelineEvents = selectedTab == timelineTab
                        ? storage.GetInferredSystemTimelineEventsForDate(timelineDate, observedAt)
                        : null;
                    var systemTimelineRanges = selectedTab == timelineTab
                        ? storage.GetSystemTimelineRangesForDate(timelineDate, observedAt)
                        : null;
                    var categoryTimelineSegments = selectedTab == timelineTab
                        ? storage.GetCategoryTimelineSegmentsForDate(
                            timelineDate,
                            observedAt,
                            TimeSpan.FromMinutes(selectedTimelineCategoryBucketMinutes),
                            selectedTimelineCategoryBucketMinutes == 0)
                        : null;
                    var timelineForegroundUsage = selectedTab == timelineTab
                        ? storage.GetForegroundUsageForDate(timelineDate)
                        : null;
                    var timelineDateHasData = selectedTab == timelineTab
                        ? storage.HasActivityDataForDate(timelineDate, observedAt)
                        : (bool?)null;
                    var runtimeRows = selectedTab == detailTab
                        ? storage.GetProcessRuntimeUsageForDate(detailDate, observedAt)
                        : null;
                    var detailSummaryAppIds = selectedTab == detailTab
                        ? storage.GetForegroundUsageForDate(detailDate)
                            .Select(x => x.AppId)
                            .ToHashSet()
                        : null;
                    var detailDateHasData = selectedTab == detailTab
                        ? storage.HasActivityDataForDate(detailDate, observedAt)
                        : (bool?)null;
                    var runtimeSegmentRows = selectedTab == detailTab && appIdToRestore is { } appId
                        ? storage.GetProcessRuntimeSegmentsForDate(appId, detailDate, observedAt)
                        : null;
                    readStopwatch.Stop();

                    return new ViewRefreshSnapshot(
                        foregroundUsage,
                        dailyUsageTrendRows,
                        idleUsage,
                        runtimeCoverage,
                        summaryPeriodRange.ShowDateInTimestamps,
                        detailDateHasData,
                        timelineDateHasData,
                        timelineRows,
                        windowsRuntimeRanges,
                        systemTimelineRanges,
                        systemTimelineEvents,
                        inferredSystemTimelineEvents,
                        categoryTimelineSegments,
                        timelineForegroundUsage,
                        runtimeRows,
                        detailSummaryAppIds,
                        runtimeSegmentRows,
                        readStopwatch.ElapsedMilliseconds);
                });
            }
            catch
            {
                return;
            }
            finally
            {
                isViewRefreshRunning = false;
            }

            if (isClosing)
                return;

            var applyStopwatch = Stopwatch.StartNew();
            if (snapshot.ForegroundUsage is not null)
            {
                SetRuntimeCoverageSummary(snapshot.RuntimeCoverage);
                SetSummaryIdleAnalysis(snapshot.ForegroundUsage, snapshot.IdleUsage);
                SetGridDataSourcePreservingView(
                    usageGrid,
                    AddIcons(SortUsageSummaryRows(UsageSummaryRowBuilder.FromForegroundUsage(
                        snapshot.ForegroundUsage,
                        snapshot.ShowDateInUsageTimestamps))));
                SetGridDataSourcePreservingView(
                    dailyUsageTrendGrid,
                    SortDailyUsageTrendRows(snapshot.DailyUsageTrendRows ?? Array.Empty<DailyUsageTrendRow>()));
            }

            if (snapshot.TimelineRows is not null)
            {
                currentTimelineRows = snapshot.TimelineRows;
                currentTimelineForegroundUsage = snapshot.TimelineForegroundUsage ?? Array.Empty<ForegroundUsageSummary>();
                SetDateStatus(timelineDateStatusLabel, snapshot.TimelineDateHasData);
                var filteredSystemRanges = FilterSystemTimelineRanges(snapshot.SystemTimelineRanges ?? Array.Empty<SystemTimelineRange>());
                var filteredSystemEvents = FilterSystemTimelineEvents(snapshot.SystemTimelineEvents ?? Array.Empty<SystemTimelineEvent>());
                currentTimelineWindowsRuntimeRanges = snapshot.WindowsRuntimeRanges ?? Array.Empty<TimelineRange>();
                currentTimelineSystemRanges = snapshot.SystemTimelineRanges ?? Array.Empty<SystemTimelineRange>();
                currentTimelineSystemEvents = FilterSystemTimelineEvents(
                    (snapshot.SystemTimelineEvents ?? Array.Empty<SystemTimelineEvent>())
                    .Concat(snapshot.InferredSystemTimelineEvents ?? Array.Empty<SystemTimelineEvent>())
                    .ToList());
                timelineOverviewControl.SetTimeline(
                    selectedTimelineDate,
                    snapshot.TimelineRows,
                    currentTimelineWindowsRuntimeRanges,
                    filteredSystemRanges,
                    filteredSystemEvents,
                    snapshot.CategoryTimelineSegments ?? Array.Empty<CategoryTimelineSegment>());
                timelineOverviewControl.SetSystemEventHighlightEnabled(selectedTimelineSystemEventFilter != TimelineSystemEventFilter.All);
                ApplyTimelineHighlightToOverview();
                SetGridDataSourcePreservingView(
                    timelineGrid,
                    AddIcons(SortTimelineRows(snapshot.TimelineRows)));
                UpdateTimelineHighlightUi();
            }

            if (snapshot.RuntimeRows is not null)
            {
                SetDateStatus(detailDateStatusLabel, snapshot.DetailDateHasData);
                var appIdToRestoreOnApply = selectedRuntimeAppId ?? appIdToRestore;
                isRefreshingRuntimeGrid = true;
                try
                {
                    var runtimeRows = ApplyCurrentTrackingScope(snapshot.RuntimeRows);
                    SetGridDataSourcePreservingView(
                        runtimeGrid,
                        AddIcons(SortRuntimeSummaryRows(FilterRuntimeSummaryRows(
                            runtimeRows,
                            snapshot.DetailSummaryAppIds))),
                        preserveSelection: false);
                    RestoreRuntimeSelection(
                        appIdToRestoreOnApply,
                        runtimeFirstDisplayedRowIndex,
                        runtimeFirstDisplayedColumnIndex,
                        runtimeHorizontalOffset);
                }
                finally
                {
                    isRefreshingRuntimeGrid = false;
                }

                selectedRuntimeAppId = appIdToRestoreOnApply ?? GetSelectedRuntimeAppId();
                if (selectedRuntimeAppId == appIdToRestore && snapshot.RuntimeSegmentRows is not null)
                {
                    var sortedSegments = SortRuntimeSegmentRows(FilterRuntimeSegmentRows(snapshot.RuntimeSegmentRows));
                    SetRuntimeSegmentsDataSource(sortedSegments);
                    RestoreRuntimeSegmentSelection(selectedRuntimeSegmentKey, selectFirstWhenMissing: selectedRuntimeSegmentKey is null);
                    UpdateRuntimeSegmentTimeline(GetRuntimeRowForSelectedApp(), sortedSegments);
                }
                else
                {
                    RefreshRuntimeSegments(observedAt);
                }

                RestoreRuntimeGridView(
                    runtimeFirstDisplayedRowIndex,
                    runtimeFirstDisplayedColumnIndex,
                    runtimeHorizontalOffset);
                ScheduleRuntimeGridViewRestore(
                    runtimeFirstDisplayedRowIndex,
                    runtimeFirstDisplayedColumnIndex,
                    runtimeHorizontalOffset);
            }

            UpdateSortGlyphs();
            RepositionHeaderToolTip();
            applyStopwatch.Stop();
            totalStopwatch.Stop();
            ReportPerformanceTimings(
                ("view-read", snapshot.ReadElapsedMs),
                ("view-apply", applyStopwatch.ElapsedMilliseconds),
                ("view-total", totalStopwatch.ElapsedMilliseconds));
        }

        private void RefreshRuntimeSegments(
            DateTimeOffset observedAt,
            RuntimeSegmentSelectionKey? selectionKeyToRestore = null,
            bool selectFirstWhenMissing = true)
        {
            if (storage is null)
                return;

            var selectedRow = GetRuntimeRowForSelectedApp();
            if (selectedRow is null)
            {
                selectedRuntimeSegmentKey = null;
                SetGridDataSourcePreservingView(runtimeSegmentsGrid, Array.Empty<ProcessRuntimeSegmentRow>());
                UpdateRuntimeSegmentTimeline(null, Array.Empty<ProcessRuntimeSegmentRow>());
                return;
            }

            var segmentRows = SortRuntimeSegmentRows(FilterRuntimeSegmentRows(
                storage.GetProcessRuntimeSegmentsForDate(selectedRow.AppId, selectedDetailDate, observedAt)));
            var keyToRestore = selectionKeyToRestore ?? selectedRuntimeSegmentKey;
            SetRuntimeSegmentsDataSource(segmentRows);
            RestoreRuntimeSegmentSelection(keyToRestore, selectFirstWhenMissing: selectFirstWhenMissing && keyToRestore is null);
            UpdateRuntimeSegmentTimeline(selectedRow, segmentRows);
        }

        private void UpdateRuntimeSegmentTimeline(
            ProcessRuntimeSummaryRow? selectedRow,
            IReadOnlyList<ProcessRuntimeSegmentRow> segmentRows)
        {
            runtimeSegmentTimelineControl.SetSegments(
                selectedDetailDate,
                selectedRow,
                segmentRows);
            UpdateRuntimeSegmentZoomControls();
        }

        private void SetRuntimeSegmentsDataSource(IReadOnlyList<ProcessRuntimeSegmentRow> segmentRows)
        {
            isRestoringRuntimeSegmentSelection = true;
            try
            {
                SetGridDataSourcePreservingView(
                    runtimeSegmentsGrid,
                    segmentRows,
                    preserveSelection: false);
                runtimeSegmentsGrid.ClearSelection();
                runtimeSegmentsGrid.CurrentCell = null;
            }
            finally
            {
                isRestoringRuntimeSegmentSelection = false;
            }
        }

        private bool RestoreRuntimeSegmentSelection(RuntimeSegmentSelectionKey? key, bool selectFirstWhenMissing)
        {
            if (runtimeSegmentsGrid.Rows.Count == 0)
            {
                selectedRuntimeSegmentKey = null;
                runtimeSegmentTimelineControl.SetHighlightedSegment(null);
                return false;
            }

            if (key is null)
                return SelectFirstRuntimeSegmentIfNeeded(selectFirstWhenMissing);

            foreach (DataGridViewRow row in runtimeSegmentsGrid.Rows)
            {
                if (row.DataBoundItem is not ProcessRuntimeSegmentRow segment || !key.Value.Matches(segment))
                    continue;

                isRestoringRuntimeSegmentSelection = true;
                try
                {
                    runtimeSegmentsGrid.ClearSelection();
                    row.Selected = true;
                    runtimeSegmentsGrid.CurrentCell = null;
                    selectedRuntimeSegmentKey = key;
                    runtimeSegmentTimelineControl.SetHighlightedSegment(segment);
                }
                finally
                {
                    isRestoringRuntimeSegmentSelection = false;
                }

                return true;
            }

            selectedRuntimeSegmentKey = null;
            runtimeSegmentTimelineControl.SetHighlightedSegment(null);
            return SelectFirstRuntimeSegmentIfNeeded(selectFirstWhenMissing);
        }

        private bool SelectFirstRuntimeSegmentIfNeeded(bool shouldSelect)
        {
            if (!shouldSelect || runtimeSegmentsGrid.CurrentRow is not null || runtimeSegmentsGrid.Rows.Count == 0)
                return false;

            isRestoringRuntimeSegmentSelection = true;
            try
            {
                runtimeSegmentsGrid.ClearSelection();
                runtimeSegmentsGrid.Rows[0].Selected = true;
                runtimeSegmentsGrid.CurrentCell = null;
                if (runtimeSegmentsGrid.Rows[0].DataBoundItem is ProcessRuntimeSegmentRow segment)
                {
                    selectedRuntimeSegmentKey = RuntimeSegmentSelectionKey.From(segment);
                    runtimeSegmentTimelineControl.SetHighlightedSegment(segment);
                }
            }
            finally
            {
                isRestoringRuntimeSegmentSelection = false;
            }

            return true;
        }

        private static void SetDateStatus(Label label, bool? hasData)
        {
            label.Text = hasData switch
            {
                true => UiText.DateStatus.HasData,
                false => UiText.DateStatus.NoData,
                _ => UiText.DateStatus.NotChecked
            };
            label.ForeColor = hasData switch
            {
                true => Color.DarkGreen,
                false => SystemColors.GrayText,
                _ => SystemColors.GrayText
            };
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
                && (observedAt - lastSample).TotalMilliseconds + SampleIntervalToleranceMs
                    < settings.ProcessRuntimeSampleIntervalMs)
                return;

            if (isProcessRuntimeSampleRunning)
            {
                ReportPerformanceEvents("process-skip");
                return;
            }

            var scope = settings.ProcessRuntimeTrackingScope;
            lastProcessRuntimeSampleAt = observedAt;
            isProcessRuntimeSampleRunning = true;

            try
            {
                long scanElapsedMs = 0;
                long writeElapsedMs = 0;
                await Task.Run(() =>
                {
                    var scanStopwatch = Stopwatch.StartNew();
                    var processes = RunningProcessReader.GetProcesses(scope);
                    scanStopwatch.Stop();
                    scanElapsedMs = scanStopwatch.ElapsedMilliseconds;
                    if (isClosing)
                        return;

                    var writeStopwatch = Stopwatch.StartNew();
                    lock (processRuntimeTrackingLock)
                    {
                        if (!isClosing)
                            processRuntimeSessionTracker.Track(processes, scope, observedAt);
                    }
                    writeStopwatch.Stop();
                    writeElapsedMs = writeStopwatch.ElapsedMilliseconds;
                });
                ReportPerformanceTimings(
                    ("process-scan", scanElapsedMs),
                    ("process-write", writeElapsedMs));
            }
            catch
            {
            }
            finally
            {
                isProcessRuntimeSampleRunning = false;
            }
        }

        private void SetStatusText(string text)
        {
            statusText = text;
            RefreshStatusLabel();
        }

        private void ReportPerformanceTimings(params (string Name, long ElapsedMs)[] timings)
        {
            if (!settings.PerformanceDiagnosticsEnabled)
                return;

            var slowTimings = timings
                .Where(x => x.ElapsedMs >= SlowOperationThresholdMs)
                .Select(x => $"{x.Name} {x.ElapsedMs}ms")
                .ToList();

            if (slowTimings.Count == 0)
                return;

            performanceStatusText = UiText.Main.PerformancePrefix + string.Join(", ", slowTimings);
            performanceStatusExpiresAt = DateTimeOffset.UtcNow.Add(PerformanceStatusDuration);
            RefreshStatusLabel();
        }

        private void ReportPerformanceEvents(params string[] events)
        {
            if (!settings.PerformanceDiagnosticsEnabled)
                return;

            if (events.Length == 0)
                return;

            performanceStatusText = UiText.Main.PerformancePrefix + string.Join(", ", events);
            performanceStatusExpiresAt = DateTimeOffset.UtcNow.Add(PerformanceStatusDuration);
            RefreshStatusLabel();
        }

        private void RefreshStatusLabel()
        {
            if (performanceStatusExpiresAt <= DateTimeOffset.UtcNow)
            {
                performanceStatusText = null;
                performanceStatusExpiresAt = null;
            }

            var parts = new List<string> { statusText };
            if (!string.IsNullOrWhiteSpace(exportStatusText))
                parts.Add(exportStatusText);
            if (!string.IsNullOrWhiteSpace(performanceStatusText))
                parts.Add(performanceStatusText);

            statusLabel.Text = string.Join(" | ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
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

        private IReadOnlyList<ProcessRuntimeSummaryRow> ApplyCurrentTrackingScope(
            IReadOnlyList<ProcessRuntimeSummaryRow> rows)
        {
            return rows
                .Select(row => row with { IsInCurrentTrackingScope = IsInCurrentTrackingScope(row) })
                .ToList();
        }

        private IReadOnlyList<UsageSummaryRow> SortUsageSummaryRows(IReadOnlyList<UsageSummaryRow> rows)
        {
            IOrderedEnumerable<UsageSummaryRow> sortedRows = usageSortProperty switch
            {
                nameof(UsageSummaryRow.AppName) => OrderUsageRows(rows, x => x.AppName),
                nameof(UsageSummaryRow.CategoryText) => OrderUsageRows(rows, x => x.CategoryText),
                nameof(UsageSummaryRow.FirstStartedAt) => OrderUsageRows(rows, x => x.FirstStartedAt),
                nameof(UsageSummaryRow.LastObservedAt) => OrderUsageRows(rows, x => x.LastObservedAt),
                nameof(UsageSummaryRow.UsageRatio) => OrderUsageRows(rows, x => x.UsageRatio),
                nameof(UsageSummaryRow.IdleRecordedMs) => OrderUsageRows(rows, x => x.IdleRecordedMs),
                nameof(UsageSummaryRow.SwitchCount) => OrderUsageRows(rows, x => x.SwitchCount),
                _ => OrderUsageRows(rows, x => x.ActiveUsageMs)
            };

            return sortedRows
                .ThenBy(x => x.AppName)
                .ToList();
        }

        private IReadOnlyList<DailyUsageTrendRow> SortDailyUsageTrendRows(IReadOnlyList<DailyUsageTrendRow> rows)
        {
            IOrderedEnumerable<DailyUsageTrendRow> sortedRows = dailyUsageTrendSortProperty switch
            {
                nameof(DailyUsageTrendRow.ActiveUsageMs) => OrderDailyUsageTrendRows(rows, x => x.ActiveUsageMs),
                nameof(DailyUsageTrendRow.TopAppName) => OrderDailyUsageTrendRows(rows, x => x.TopAppName),
                nameof(DailyUsageTrendRow.TopAppUsageMs) => OrderDailyUsageTrendRows(rows, x => x.TopAppUsageMs),
                _ => OrderDailyUsageTrendRows(rows, x => x.Date)
            };

            return sortedRows
                .ThenByDescending(x => x.Date)
                .ToList();
        }

        private IReadOnlyList<ActivityTimelineRow> SortTimelineRows(IReadOnlyList<ActivityTimelineRow> rows)
        {
            IOrderedEnumerable<ActivityTimelineRow> sortedRows = timelineSortProperty switch
            {
                nameof(ActivityTimelineRow.ActivityType) => OrderTimelineRows(rows, x => x.ActivityType),
                nameof(ActivityTimelineRow.EndedAt) => OrderTimelineRows(rows, x => x.EndedAt),
                nameof(ActivityTimelineRow.DurationMs) => OrderTimelineRows(rows, x => x.DurationMs),
                nameof(ActivityTimelineRow.DisplayName) => OrderTimelineRows(rows, x => x.DisplayName),
                nameof(ActivityTimelineRow.CategoryText) => OrderTimelineRows(rows, x => x.CategoryText),
                _ => OrderTimelineRows(rows, x => x.StartedAt)
            };

            return sortedRows
                .ThenByDescending(x => x.StartedAt)
                .ToList();
        }

        private IReadOnlyList<ProcessRuntimeSummaryRow> FilterRuntimeSummaryRows(
            IReadOnlyList<ProcessRuntimeSummaryRow> rows,
            IReadOnlySet<long>? summaryAppIds)
        {
            IEnumerable<ProcessRuntimeSummaryRow> filteredRows = rows;

            filteredRows = selectedDetailRuntimeFilter switch
            {
                DetailRuntimeFilter.SummaryApps => filteredRows.Where(x => summaryAppIds?.Contains(x.AppId) == true),
                DetailRuntimeFilter.CurrentTrackingScope => filteredRows.Where(x => x.IsInCurrentTrackingScope),
                DetailRuntimeFilter.VisibleApps => filteredRows.Where(x => x.HasMainWindow),
                DetailRuntimeFilter.UserProcesses => filteredRows.Where(x => x.IsCurrentSessionProcess),
                _ => filteredRows
            };

            if (showRunningRuntimeOnly)
                filteredRows = filteredRows.Where(x => x.HasRunningSession);

            return filteredRows.ToList();
        }

        private IReadOnlyList<ProcessRuntimeSegmentRow> FilterRuntimeSegmentRows(
            IReadOnlyList<ProcessRuntimeSegmentRow> rows)
        {
            IEnumerable<ProcessRuntimeSegmentRow> filteredRows = rows;

            filteredRows = selectedRuntimeSegmentObservationFilter switch
            {
                RuntimeSegmentObservationFilter.VisibleApps => filteredRows.Where(x => x.HasMainWindow),
                RuntimeSegmentObservationFilter.UserProcesses => filteredRows.Where(x => !x.HasMainWindow && x.IsCurrentSessionProcess),
                RuntimeSegmentObservationFilter.AllProcesses => filteredRows.Where(x => !x.HasMainWindow && !x.IsCurrentSessionProcess),
                _ => filteredRows
            };

            return filteredRows.ToList();
        }

        private IReadOnlyList<ProcessRuntimeSummaryRow> SortRuntimeSummaryRows(IReadOnlyList<ProcessRuntimeSummaryRow> rows)
        {
            IOrderedEnumerable<ProcessRuntimeSummaryRow> sortedRows = runtimeSortProperty switch
            {
                nameof(ProcessRuntimeSummaryRow.AppName) => OrderRuntimeRows(rows, x => x.AppName),
                nameof(ProcessRuntimeSummaryRow.CategoryText) => OrderRuntimeRows(rows, x => x.CategoryText),
                nameof(ProcessRuntimeSummaryRow.FirstObservedAt) => OrderRuntimeRows(rows, x => x.FirstObservedAt),
                nameof(ProcessRuntimeSummaryRow.LastObservedAt) => OrderRuntimeRows(rows, x => x.LastObservedAt),
                nameof(ProcessRuntimeSummaryRow.ActiveUsageMs) => OrderRuntimeRows(rows, x => x.ActiveUsageMs),
                nameof(ProcessRuntimeSummaryRow.IdleRecordedMs) => OrderRuntimeRows(rows, x => x.IdleRecordedMs),
                nameof(ProcessRuntimeSummaryRow.ActualUsageRatio) => OrderRuntimeRows(rows, x => x.ActualUsageRatio ?? -1),
                nameof(ProcessRuntimeSummaryRow.RuntimeSegmentCount) => OrderRuntimeRows(rows, x => x.RuntimeSegmentCount),
                nameof(ProcessRuntimeSummaryRow.TrackingTypeText) => OrderRuntimeRows(rows, x => x.TrackingTypeText),
                nameof(ProcessRuntimeSummaryRow.StatusText) => OrderRuntimeRows(rows, x => x.StatusText),
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

        private IOrderedEnumerable<DailyUsageTrendRow> OrderDailyUsageTrendRows<TKey>(
            IReadOnlyList<DailyUsageTrendRow> rows,
            Func<DailyUsageTrendRow, TKey> keySelector)
        {
            return dailyUsageTrendSortOrder == SortOrder.Ascending
                ? rows.OrderBy(keySelector)
                : rows.OrderByDescending(keySelector);
        }

        private IOrderedEnumerable<ActivityTimelineRow> OrderTimelineRows<TKey>(
            IReadOnlyList<ActivityTimelineRow> rows,
            Func<ActivityTimelineRow, TKey> keySelector)
        {
            return timelineSortOrder == SortOrder.Ascending
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
            SaveTableSortState();
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnDailyUsageTrendGridColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0)
                return;

            var propertyName = GetDailyUsageTrendSortPropertyName(dailyUsageTrendGrid.Columns[e.ColumnIndex]);
            if (propertyName is null)
                return;

            dailyUsageTrendSortOrder = string.Equals(dailyUsageTrendSortProperty, propertyName, StringComparison.Ordinal)
                ? ToggleSortOrder(dailyUsageTrendSortOrder)
                : SortOrder.Descending;
            dailyUsageTrendSortProperty = propertyName;
            SaveTableSortState();
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnUsageGridCellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0)
                return;

            if (SelectGridRow<UsageSummaryRow>(usageGrid, e.RowIndex, e.ColumnIndex) is not { } row)
                return;

            usageGridMenu.Items.Clear();
            var showInTimelineItem = new ToolStripMenuItem(UiText.Main.ShowInTimeline);
            showInTimelineItem.Click += (_, _) => HighlightUsageRowInTimeline(row);
            usageGridMenu.Items.Add(showInTimelineItem);
            if (row.AppId is { } appId)
            {
                usageGridMenu.Items.Add(CreateSetCategoryMenuItem(
                    row.PrimaryCategoryId,
                    categoryId => SetAppCategory(appId, row.AppName, categoryId)));
            }

            usageGridMenu.Items.Add(CreateSearchWebMenuItem(row.AppName, row.ProcessName));
            usageGridMenu.Show(usageGrid, usageGrid.PointToClient(Cursor.Position));
        }

        private void HighlightUsageRowInTimeline(UsageSummaryRow? row)
        {
            if (row is null || string.IsNullOrWhiteSpace(row.ProcessName))
                return;

            highlightedTimelineProcessName = row.ProcessName;
            highlightedTimelineAppName = row.AppName;
            selectedTimelineDate = GetTimelineDateForSummarySelection(row);
            SetDatePickerValue(timelineDatePicker, selectedTimelineDate);
            mainTabs.SelectedTab = timelineTab;
            timelineOverviewControl.SetHighlightedProcessName(highlightedTimelineProcessName);
            UpdateTimelineHighlightUi();
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void HighlightTimelineRow(ActivityTimelineRow? row)
        {
            if (row is null || string.IsNullOrWhiteSpace(row.ProcessName))
                return;

            highlightedTimelineSegmentKey = null;
            highlightedTimelineSegmentLabel = null;
            highlightedTimelineProcessName = row.ProcessName;
            highlightedTimelineAppName = row.DisplayName;
            timelineOverviewControl.SetHighlightedProcessName(highlightedTimelineProcessName);
            UpdateTimelineHighlightUi();
            timelineGrid.Invalidate();
        }

        private void HighlightTimelineSegment(ActivityTimelineRow? row)
        {
            if (row is null)
                return;

            SetTimelineTypeHighlight(TimelineActivityTypeHighlight.None);
            highlightedTimelineProcessName = null;
            highlightedTimelineAppName = null;
            highlightedTimelineSegmentKey = TimelineSegmentSelectionKey.From(row);
            highlightedTimelineSegmentLabel = GetTimelineSegmentHighlightLabel(row);
            timelineOverviewControl.SetHighlightedActivitySegment(row);
            UpdateTimelineHighlightUi();
            timelineGrid.Invalidate();
        }

        private static string GetHighlightTimelineSegmentMenuText()
        {
            return UiText.CurrentLanguage == UiLanguage.English ? "Highlight this segment" : "이 구간 강조";
        }

        private static string GetHighlightTimelineAppMenuText()
        {
            return UiText.CurrentLanguage == UiLanguage.English ? "Highlight this app" : "이 앱 강조";
        }

        private static string GetTimelineSegmentHighlightLabel(ActivityTimelineRow row)
        {
            return $"{row.DisplayName} {row.StartedAtText}-{row.EndedAtText}";
        }

        private string GetTimelineSegmentHighlightText()
        {
            var label = highlightedTimelineSegmentLabel ?? "";
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"Highlight segment: {label}"
                : $"구간 강조: {label}";
        }

        private DateTime GetTimelineDateForSummarySelection(UsageSummaryRow row)
        {
            if (selectedSummaryPeriod == SummaryPeriod.SpecificDate)
                return selectedSummarySpecificDate;

            if (selectedSummaryPeriod == SummaryPeriod.Today)
                return DateTime.Today;

            return row.LastObservedAt?.ToLocalTime().DateTime.Date
                ?? row.FirstStartedAt?.ToLocalTime().DateTime.Date
                ?? selectedTimelineDate;
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
            if (e.ColumnIndex < 0)
                return;

            var propertyName = GetTimelineSortPropertyName(timelineGrid.Columns[e.ColumnIndex]);
            if (propertyName is null)
                return;

            timelineSortOrder = string.Equals(timelineSortProperty, propertyName, StringComparison.Ordinal)
                ? ToggleSortOrder(timelineSortOrder)
                : SortOrder.Descending;
            timelineSortProperty = propertyName;
            SaveTableSortState();
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnMainTabsSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (mainTabs.SelectedTab == detailTab)
            {
                isUpdatingDetailRuntimeFilterOptions = true;
                try
                {
                    SyncDetailRuntimeFilterComboBoxSelection();
                }
                finally
                {
                    isUpdatingDetailRuntimeFilterOptions = false;
                }
            }

            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnDetailDatePickerValueChanged(object? sender, EventArgs e)
        {
            if (isInitializingDateSelectors)
                return;

            ApplyDetailDate(detailDatePicker.Value.Date);
        }

        private void OnTimelineDatePickerValueChanged(object? sender, EventArgs e)
        {
            if (isInitializingDateSelectors)
                return;

            ApplyTimelineDate(timelineDatePicker.Value.Date);
        }

        private void OnDetailCalendarButtonClick(object? sender, EventArgs e)
        {
            ShowRecordedDateCalendar(detailCalendarButton, selectedDetailDate, ApplyDetailDate);
        }

        private void OnTimelineCalendarButtonClick(object? sender, EventArgs e)
        {
            ShowRecordedDateCalendar(timelineCalendarButton, selectedTimelineDate, ApplyTimelineDate);
        }

        private void OnDetailPreviousDateButtonClick(object? sender, EventArgs e)
        {
            ApplyDetailDate(selectedDetailDate.AddDays(-1));
        }

        private void OnDetailNextDateButtonClick(object? sender, EventArgs e)
        {
            ApplyDetailDate(selectedDetailDate.AddDays(1));
        }

        private void OnDetailTodayButtonClick(object? sender, EventArgs e)
        {
            ApplyDetailDate(DateTime.Today);
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

        private void OnRuntimeSegmentTimelineViewRangeChanged(object? sender, EventArgs e)
        {
            UpdateRuntimeSegmentZoomControls();
        }

        private void OnTimelineZoomOutButtonClick(object? sender, EventArgs e)
        {
            timelineOverviewControl.ZoomOut();
        }

        private void OnTimelineZoomInButtonClick(object? sender, EventArgs e)
        {
            timelineOverviewControl.ZoomIn();
        }

        private void OnTimelineZoomPreviousButtonClick(object? sender, EventArgs e)
        {
            timelineOverviewControl.PanPrevious();
        }

        private void OnTimelineZoomNextButtonClick(object? sender, EventArgs e)
        {
            timelineOverviewControl.PanNext();
        }

        private void OnTimelineZoomResetButtonClick(object? sender, EventArgs e)
        {
            timelineOverviewControl.ResetView();
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

        private void OnRuntimeSegmentResetButtonClick(object? sender, EventArgs e)
        {
            runtimeSegmentTimelineControl.ResetView();
        }

        private void OnRuntimeSegmentZoomOutButtonClick(object? sender, EventArgs e)
        {
            runtimeSegmentTimelineControl.ZoomOut();
        }

        private void OnRuntimeSegmentZoomInButtonClick(object? sender, EventArgs e)
        {
            runtimeSegmentTimelineControl.ZoomIn();
        }

        private void OnRuntimeSegmentPreviousButtonClick(object? sender, EventArgs e)
        {
            runtimeSegmentTimelineControl.PanPrevious();
        }

        private void OnRuntimeSegmentNextButtonClick(object? sender, EventArgs e)
        {
            runtimeSegmentTimelineControl.PanNext();
        }

        private void OnRuntimeSegmentHelpButtonClick(object? sender, EventArgs e)
        {
            CenteredMessageDialog.Show(
                this,
                GetRuntimeSegmentHelpMessage(),
                GetRuntimeSegmentHelpTitle(),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void OnRuntimeSegmentObservationFilterComboBoxSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (runtimeSegmentObservationFilterComboBox.SelectedItem is not RuntimeSegmentObservationFilterOption option
                || option.Value == selectedRuntimeSegmentObservationFilter)
                return;

            selectedRuntimeSegmentObservationFilter = option.Value;
            RefreshRuntimeSegments(DateTimeOffset.UtcNow);
        }

        private void OnRuntimeSegmentZoomScrollBarScroll(object? sender, ScrollEventArgs e)
        {
            if (isUpdatingRuntimeSegmentScrollBar)
                return;

            runtimeSegmentTimelineControl.SetViewStartRatio(e.NewValue / 1000d);
        }

        private void OnTimelineZoomScrollBarScroll(object? sender, ScrollEventArgs e)
        {
            if (isUpdatingTimelineScrollBar)
                return;

            timelineOverviewControl.SetViewStartRatio(e.NewValue / 1000d);
        }

        private void OnTimelineHighlightClearButtonClick(object? sender, EventArgs e)
        {
            ClearTimelineHighlights(resetTypeHighlight: true);
        }

        private void OnTimelineCategoryBucketComboBoxSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (timelineCategoryBucketComboBox.SelectedItem is not TimelineCategoryBucketOption option
                || option.Minutes == selectedTimelineCategoryBucketMinutes)
                return;

            selectedTimelineCategoryBucketMinutes = option.Minutes;
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnTimelineTypeHighlightComboBoxSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (timelineTypeHighlightComboBox.SelectedItem is not TimelineActivityTypeHighlightOption option
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

        private void OnTimelineSystemEventFilterComboBoxSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (timelineSystemEventFilterComboBox.SelectedItem is not TimelineSystemEventFilterOption option
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

        private void OnTimelineGridCellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0)
                return;

            if (SelectGridRow<ActivityTimelineRow>(timelineGrid, e.RowIndex, e.ColumnIndex) is not { } row
                || string.IsNullOrWhiteSpace(row.ProcessName))
                return;

            ShowTimelineActivityContextMenu(row, timelineGrid, timelineGrid.PointToClient(Cursor.Position));
        }

        private void OnTimelineOverviewActivitySegmentContextRequested(object? sender, TimelineActivitySegmentContextEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.Row.ProcessName))
                return;

            ShowTimelineActivityContextMenu(e.Row, timelineOverviewControl, e.Location);
        }

        private void OnTimelineOverviewCategorySegmentContextRequested(object? sender, TimelineCategorySegmentContextEventArgs e)
        {
            ShowTimelineCategorySegmentContextMenu(e.Segment, timelineOverviewControl, e.Location);
        }

        private void OnTimelineOverviewWindowsTrackContextRequested(object? sender, TimelineWindowsTrackContextEventArgs e)
        {
            ShowTimelineWindowsContextMenu(timelineOverviewControl, e.Location);
        }

        private void ShowTimelineCategorySegmentContextMenu(CategoryTimelineSegment segment, Control owner, Point location)
        {
            timelineGridMenu.Items.Clear();
            var appStatsItem = new ToolStripMenuItem(GetTimelineCategorySegmentAppStatsMenuText());
            appStatsItem.Click += (_, _) => ShowTimelineCategorySegmentAppStatsPopup(segment, owner, location);
            timelineGridMenu.Items.Add(appStatsItem);
            timelineGridMenu.Show(owner, location);
        }

        private void ShowTimelineCategorySegmentAppStatsPopup(CategoryTimelineSegment segment, Control owner, Point location)
        {
            var rows = UsageSummaryRowBuilder.FromForegroundUsage(
                storage?.GetForegroundUsageForPeriod(segment.StartedAt, segment.EndedAt)
                    ?? Array.Empty<ForegroundUsageSummary>());

            var popup = new Form
            {
                Text = GetTimelineCategorySegmentAppStatsTitle(),
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Size = new Size(720, 300),
                MinimizeBox = false,
                MaximizeBox = false,
                FormBorderStyle = FormBorderStyle.SizableToolWindow
            };
            popup.Icon = Icon;

            var label = new Label
            {
                Dock = DockStyle.Top,
                Height = 64,
                Padding = new Padding(8, 6, 8, 0),
                Text = GetTimelineCategorySegmentAppStatsDescription(segment)
            };

            var grid = CreateTimelineCategorySegmentAppStatsGrid();
            grid.DataSource = rows;
            popup.Controls.Add(grid);
            popup.Controls.Add(label);

            var screenLocation = owner.PointToScreen(location);
            popup.Location = KeepPopupOnScreen(new Point(screenLocation.X + 8, screenLocation.Y + 8), popup.Size);
            popup.Show(this);
        }

        private void ShowTimelineWindowsContextMenu(Control owner, Point location)
        {
            timelineGridMenu.Items.Clear();
            var eventListItem = new ToolStripMenuItem(GetTimelineSystemEventsTitle(selectedTimelineDate));
            eventListItem.Click += (_, _) => ShowTimelineSystemEventsPopup(owner, location);
            timelineGridMenu.Items.Add(eventListItem);
            timelineGridMenu.Show(owner, location);
        }

        private void ShowTimelineSystemEventsPopup(Control owner, Point location)
        {
            var popup = new Form
            {
                Text = GetTimelineSystemEventsTitle(selectedTimelineDate),
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Size = new Size(720, 260),
                MinimizeBox = false,
                MaximizeBox = false,
                FormBorderStyle = FormBorderStyle.SizableToolWindow
            };
            popup.Icon = Icon;

            var label = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                Padding = new Padding(8, 6, 8, 0),
                Text = GetTimelineSystemEventsPopupDescription(selectedTimelineDate)
            };

            var grid = CreateTimelineSystemEventsGrid();
            grid.DataSource = BuildSystemTimelineEventRows(currentTimelineSystemEvents);
            popup.Controls.Add(grid);
            popup.Controls.Add(label);

            var screenLocation = owner.PointToScreen(location);
            popup.Location = KeepPopupOnScreen(new Point(screenLocation.X + 8, screenLocation.Y + 8), popup.Size);
            popup.Show(this);
        }

        private static DataGridView CreateTimelineSystemEventsGrid()
        {
            var grid = new BufferedDataGridView
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

            grid.Columns.AddRange(
                CreateTextColumn(nameof(SystemTimelineEventRow.OccurredAtText), GetTimelineSystemEventTimeHeaderText(), 90),
                CreateTextColumn(nameof(SystemTimelineEventRow.EventTypeText), UiText.Main.Type, 110),
                CreateTextColumn(nameof(SystemTimelineEventRow.PreviousIntervalText), GetTimelineSystemEventPreviousIntervalHeaderText(), 110),
                CreateTextColumn(nameof(SystemTimelineEventRow.RelationText), GetTimelineSystemEventRelationHeaderText(), 150),
                CreateTextColumn(nameof(SystemTimelineEventRow.DetailsText), GetTimelineSystemEventDetailsHeaderText(), 220));
            grid.ColumnHeaderMouseClick += OnTimelineSystemEventsGridColumnHeaderMouseClick;

            return grid;
        }

        private static DataGridView CreateTimelineCategorySegmentAppStatsGrid()
        {
            var grid = new BufferedDataGridView
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

            grid.Columns.AddRange(
                CreateTextColumn(nameof(UsageSummaryRow.AppName), UiText.Main.App, 180),
                CreateTextColumn(nameof(UsageSummaryRow.CategoryText), UiText.Main.Category, 120),
                CreateTextColumn(nameof(UsageSummaryRow.ActiveUsageTimeText), UiText.Main.ActiveUsageTime, 120),
                CreateTextColumn(nameof(UsageSummaryRow.UsageRatioText), UiText.Main.ActiveRatio, 90),
                CreateTextColumn(nameof(UsageSummaryRow.SwitchCountText), UiText.Main.SwitchCount, 90),
                CreateTextColumn(nameof(UsageSummaryRow.FirstStartedAtText), UiText.Main.FirstStartedAt, 110),
                CreateTextColumn(nameof(UsageSummaryRow.LastObservedAtText), UiText.Main.LastObservedAt, 110));
            grid.ColumnHeaderMouseClick += OnTimelineCategorySegmentAppStatsGridColumnHeaderMouseClick;

            return grid;
        }

        private static void OnTimelineCategorySegmentAppStatsGridColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
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
                .OfType<UsageSummaryRow>()
                .ToList();
            grid.DataSource = SortTimelineCategorySegmentAppStatsRows(rows, column.DataPropertyName, direction);
            foreach (DataGridViewColumn gridColumn in grid.Columns)
            {
                if (gridColumn.SortMode != DataGridViewColumnSortMode.Programmatic)
                    continue;

                gridColumn.HeaderCell.SortGlyphDirection = gridColumn.Index == e.ColumnIndex
                    ? (direction == ListSortDirection.Ascending ? SortOrder.Ascending : SortOrder.Descending)
                    : SortOrder.None;
            }
        }

        private static IReadOnlyList<UsageSummaryRow> SortTimelineCategorySegmentAppStatsRows(
            IReadOnlyList<UsageSummaryRow> rows,
            string propertyName,
            ListSortDirection direction)
        {
            IOrderedEnumerable<UsageSummaryRow> orderedRows = propertyName switch
            {
                nameof(UsageSummaryRow.AppName) => rows.OrderBy(row => row.AppName, StringComparer.CurrentCulture),
                nameof(UsageSummaryRow.CategoryText) => rows.OrderBy(row => row.CategoryText, StringComparer.CurrentCulture),
                nameof(UsageSummaryRow.ActiveUsageTimeText) => rows.OrderBy(row => row.ActiveUsageMs),
                nameof(UsageSummaryRow.UsageRatioText) => rows.OrderBy(row => row.UsageRatio),
                nameof(UsageSummaryRow.SwitchCountText) => rows.OrderBy(row => row.SwitchCount),
                nameof(UsageSummaryRow.FirstStartedAtText) => rows.OrderBy(row => row.FirstStartedAt),
                nameof(UsageSummaryRow.LastObservedAtText) => rows.OrderBy(row => row.LastObservedAt),
                _ => rows.OrderBy(row => row.ActiveUsageMs)
            };

            return direction == ListSortDirection.Ascending
                ? orderedRows.ToList()
                : orderedRows.Reverse().ToList();
        }

        private static DataGridViewTextBoxColumn CreateTextColumn(string propertyName, string headerText, int width)
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

        private static Point KeepPopupOnScreen(Point location, Size size)
        {
            var workingArea = Screen.FromPoint(location).WorkingArea;
            var x = Math.Min(location.X, workingArea.Right - size.Width);
            var y = Math.Min(location.Y, workingArea.Bottom - size.Height);
            return new Point(Math.Max(workingArea.Left, x), Math.Max(workingArea.Top, y));
        }

        private static void OnTimelineSystemEventsGridColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
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
                .OfType<SystemTimelineEventRow>()
                .ToList();
            grid.DataSource = SortSystemTimelineEventRows(rows, column.DataPropertyName, direction);
            foreach (DataGridViewColumn gridColumn in grid.Columns)
            {
                if (gridColumn.SortMode != DataGridViewColumnSortMode.Programmatic)
                    continue;

                gridColumn.HeaderCell.SortGlyphDirection = gridColumn.Index == e.ColumnIndex
                    ? (direction == ListSortDirection.Ascending ? SortOrder.Ascending : SortOrder.Descending)
                    : SortOrder.None;
            }
        }

        private static IReadOnlyList<SystemTimelineEventRow> SortSystemTimelineEventRows(
            IReadOnlyList<SystemTimelineEventRow> rows,
            string propertyName,
            ListSortDirection direction)
        {
            IOrderedEnumerable<SystemTimelineEventRow> orderedRows = propertyName switch
            {
                nameof(SystemTimelineEventRow.OccurredAtText) => rows.OrderBy(row => row.OccurredAt),
                nameof(SystemTimelineEventRow.PreviousIntervalText) => rows.OrderBy(row => row.PreviousIntervalMs),
                nameof(SystemTimelineEventRow.EventTypeText) => rows.OrderBy(row => row.EventTypeText, StringComparer.CurrentCulture),
                nameof(SystemTimelineEventRow.RelationText) => rows.OrderBy(row => row.RelationText, StringComparer.CurrentCulture),
                nameof(SystemTimelineEventRow.DetailsText) => rows.OrderBy(row => row.DetailsText, StringComparer.CurrentCulture),
                _ => rows.OrderBy(row => row.OccurredAt)
            };

            return direction == ListSortDirection.Ascending
                ? orderedRows.ToList()
                : orderedRows.Reverse().ToList();
        }

        private void ShowTimelineActivityContextMenu(ActivityTimelineRow row, Control owner, Point location)
        {
            timelineGridMenu.Items.Clear();
            var highlightSegmentItem = new ToolStripMenuItem(GetHighlightTimelineSegmentMenuText());
            highlightSegmentItem.Click += (_, _) => HighlightTimelineSegment(row);
            timelineGridMenu.Items.Add(highlightSegmentItem);
            var highlightItem = new ToolStripMenuItem(GetHighlightTimelineAppMenuText());
            highlightItem.Click += (_, _) => HighlightTimelineRow(row);
            timelineGridMenu.Items.Add(highlightItem);
            if (row.AppId is { } appId)
            {
                timelineGridMenu.Items.Add(CreateSetCategoryMenuItem(
                    row.PrimaryCategoryId,
                    categoryId => SetAppCategory(appId, row.DisplayName, categoryId)));
            }

            timelineGridMenu.Items.Add(CreateSearchWebMenuItem(row.DisplayName, row.ProcessName));
            if (highlightedTimelineSegmentKey is not null
                || string.Equals(row.ProcessName, highlightedTimelineProcessName, StringComparison.OrdinalIgnoreCase))
            {
                var clearHighlightItem = new ToolStripMenuItem(UiText.Main.ClearTimelineHighlight);
                clearHighlightItem.Click += (_, _) => ClearTimelineHighlights(resetTypeHighlight: true);
                timelineGridMenu.Items.Add(clearHighlightItem);
            }

            timelineGridMenu.Show(owner, location);
        }

        private void ClearTimelineHighlights(bool resetTypeHighlight)
        {
            highlightedTimelineProcessName = null;
            highlightedTimelineAppName = null;
            highlightedTimelineSegmentKey = null;
            highlightedTimelineSegmentLabel = null;
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
            for (var i = 0; i < timelineTypeHighlightComboBox.Items.Count; i++)
            {
                if (timelineTypeHighlightComboBox.Items[i] is TimelineActivityTypeHighlightOption option
                    && option.Value == value)
                {
                    timelineTypeHighlightComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        private void ApplyDetailDate(DateTime date)
        {
            var normalizedDate = NormalizeSelectableDate(date);
            selectedDetailDate = normalizedDate;
            selectedRuntimeAppId = null;
            SetDatePickerValue(detailDatePicker, normalizedDate);
            UpdateDateNavigationButtons();
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void ApplyTimelineDate(DateTime date)
        {
            var normalizedDate = NormalizeSelectableDate(date);
            selectedTimelineDate = normalizedDate;
            SetDatePickerValue(timelineDatePicker, normalizedDate);
            UpdateDateNavigationButtons();
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private static DateTime NormalizeSelectableDate(DateTime date)
        {
            var today = DateTime.Today;
            return date.Date > today ? today : date.Date;
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

        private void ShowRecordedDateCalendar(Control anchor, DateTime selectedDate, Action<DateTime> applyDate)
        {
            var today = DateTime.Today;
            var normalizedDate = NormalizeSelectableDate(selectedDate);
            CloseRecordedDatePickerDropDown();

            var picker = new RecordedDatePickerPopup(normalizedDate, today, GetRecordedDates);
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
                CloseRecordedDatePickerDropDown();
                applyDate(date);
            };
            picker.CloseRequested += (_, _) => CloseRecordedDatePickerDropDown();
            popupForm.Deactivate += (_, _) => CloseRecordedDatePickerDropDown();
            popupForm.FormClosed += (_, _) =>
            {
                if (ReferenceEquals(recordedDatePickerPopupForm, popupForm))
                    recordedDatePickerPopupForm = null;
            };

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

        private IReadOnlyList<DateTime> GetRecordedDates(DateTime rangeStart, DateTime rangeEnd)
        {
            if (storage is null)
                return Array.Empty<DateTime>();

            var now = DateTimeOffset.UtcNow;
            return storage.GetActivityDates(rangeStart, rangeEnd, now);
        }

        private void UpdateDateNavigationButtons()
        {
            var today = DateTime.Today;
            detailNextDateButton.Enabled = selectedDetailDate < today;
            detailTodayButton.Enabled = selectedDetailDate < today;
            timelineNextDateButton.Enabled = selectedTimelineDate < today;
            timelineTodayButton.Enabled = selectedTimelineDate < today;
        }

        private void RefreshDateSelectorsIfDateChanged(DateTimeOffset observedAt)
        {
            var today = observedAt.ToLocalTime().Date;
            if (today == dateSelectorOptionsDate)
                return;

            var previousToday = dateSelectorOptionsDate;
            var shouldAutoMoveTodayViews = !IsMainWindowActivelyViewed();
            var shouldMoveDetailToToday = shouldAutoMoveTodayViews && selectedDetailDate == previousToday;
            var shouldMoveTimelineToToday = shouldAutoMoveTodayViews && selectedTimelineDate == previousToday;
            isInitializingDateSelectors = true;
            try
            {
                if (detailDatePicker.MaxDate.Date < today)
                    detailDatePicker.MaxDate = today;
                if (timelineDatePicker.MaxDate.Date < today)
                    timelineDatePicker.MaxDate = today;

                if (shouldMoveDetailToToday)
                {
                    selectedDetailDate = today;
                    detailDatePicker.Value = today;
                    selectedRuntimeAppId = null;
                }

                if (shouldMoveTimelineToToday)
                {
                    selectedTimelineDate = today;
                    timelineDatePicker.Value = today;
                }

                dateSelectorOptionsDate = today;
            }
            finally
            {
                isInitializingDateSelectors = false;
            }

            UpdateDateNavigationButtons();
            if (shouldMoveDetailToToday || shouldMoveTimelineToToday)
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
            var dateText = today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.CurrentCulture);
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"Moved today's views to {dateText}"
                : $"오늘 보기 날짜를 {dateText}(으)로 갱신했습니다.";
        }

        private void UpdateTimelineZoomControls()
        {
            timelineZoomRangeLabel.Text = UiText.Main.TimelineViewRange(timelineOverviewControl.ViewRangeText);
            timelineZoomOutButton.Enabled = timelineOverviewControl.IsZoomed;
            timelineZoomInButton.Enabled = true;
            timelineZoomPreviousButton.Enabled = timelineOverviewControl.CanPanPrevious;
            timelineZoomNextButton.Enabled = timelineOverviewControl.CanPanNext;
            timelineZoomResetButton.Enabled = timelineOverviewControl.IsZoomed;
            UpdateTimelineZoomScrollBar();
        }

        private void UpdateTimelineZoomScrollBar()
        {
            isUpdatingTimelineScrollBar = true;
            try
            {
                const int scale = 1000;
                var width = Math.Clamp((int)Math.Round(timelineOverviewControl.ViewWidthRatio * scale), 1, scale);
                var maxValue = Math.Max(0, scale - width);
                var value = Math.Clamp((int)Math.Round(timelineOverviewControl.ViewStartRatio * scale), 0, maxValue);

                timelineZoomScrollBar.Visible = timelineOverviewControl.IsZoomed;
                timelineZoomScrollBar.Enabled = timelineOverviewControl.IsZoomed;
                timelineZoomScrollBar.Minimum = 0;
                timelineZoomScrollBar.Maximum = scale;
                timelineZoomScrollBar.LargeChange = width;
                timelineZoomScrollBar.SmallChange = Math.Max(1, width / 10);
                timelineZoomScrollBar.Value = value;
            }
            finally
            {
                isUpdatingTimelineScrollBar = false;
            }
        }

        private void UpdateRuntimeSegmentZoomControls()
        {
            runtimeSegmentZoomRangeLabel.Text = GetRuntimeSegmentViewRangeText();
            runtimeSegmentZoomOutButton.Enabled = runtimeSegmentTimelineControl.IsZoomed;
            runtimeSegmentZoomInButton.Enabled = true;
            runtimeSegmentPreviousButton.Enabled = runtimeSegmentTimelineControl.CanPanPrevious;
            runtimeSegmentNextButton.Enabled = runtimeSegmentTimelineControl.CanPanNext;
            runtimeSegmentResetButton.Enabled = runtimeSegmentTimelineControl.IsZoomed;
            UpdateRuntimeSegmentZoomScrollBar();
        }

        private void UpdateRuntimeSegmentZoomScrollBar()
        {
            isUpdatingRuntimeSegmentScrollBar = true;
            try
            {
                const int scale = 1000;
                var width = Math.Clamp((int)Math.Round(runtimeSegmentTimelineControl.ViewWidthRatio * scale), 1, scale);
                var maxValue = Math.Max(0, scale - width);
                var value = Math.Clamp((int)Math.Round(runtimeSegmentTimelineControl.ViewStartRatio * scale), 0, maxValue);

                runtimeSegmentZoomScrollBar.Visible = runtimeSegmentTimelineControl.IsZoomed;
                runtimeSegmentZoomScrollBar.Enabled = runtimeSegmentTimelineControl.IsZoomed;
                runtimeSegmentZoomScrollBar.Minimum = 0;
                runtimeSegmentZoomScrollBar.Maximum = scale;
                runtimeSegmentZoomScrollBar.LargeChange = width;
                runtimeSegmentZoomScrollBar.SmallChange = Math.Max(1, width / 10);
                runtimeSegmentZoomScrollBar.Value = value;
            }
            finally
            {
                isUpdatingRuntimeSegmentScrollBar = false;
            }
        }

        private void UpdateTimelineHighlightUi()
        {
            var hasHighlight = highlightedTimelineSegmentKey is not null
                || !string.IsNullOrWhiteSpace(highlightedTimelineProcessName);
            timelineHighlightLabel.Visible = hasHighlight;
            timelineHighlightClearButton.Visible = hasHighlight;
            timelineHighlightHintLabel.Visible = !hasHighlight;
            timelineHighlightLabel.Text = highlightedTimelineSegmentKey is not null
                ? GetTimelineSegmentHighlightText()
                : hasHighlight
                    ? UiText.Main.TimelineHighlight(highlightedTimelineAppName ?? highlightedTimelineProcessName!)
                    : "";
            UpdateTimelineHighlightSummary();
        }

        private string? GetTimelineHighlightedActivityTypeText()
        {
            return selectedTimelineActivityTypeHighlight switch
            {
                TimelineActivityTypeHighlight.Active => UiText.Main.Active,
                TimelineActivityTypeHighlight.Idle => UiText.Main.Idle,
                TimelineActivityTypeHighlight.Untracked => UiText.Main.Untracked,
                _ => null
            };
        }

        private void ApplyTimelineActivityTypeHighlight()
        {
            timelineOverviewControl.SetWindowsHighlighted(selectedTimelineActivityTypeHighlight == TimelineActivityTypeHighlight.Windows);
            if (selectedTimelineActivityTypeHighlight != TimelineActivityTypeHighlight.Windows)
                timelineOverviewControl.SetHighlightedActivityType(GetTimelineHighlightedActivityTypeText());
        }

        private void ApplyTimelineHighlightToOverview()
        {
            if (highlightedTimelineSegmentKey is not null)
            {
                var highlightedRow = currentTimelineRows.FirstOrDefault(row => highlightedTimelineSegmentKey.Value.Matches(row));
                if (highlightedRow is not null)
                {
                    highlightedTimelineSegmentLabel = GetTimelineSegmentHighlightLabel(highlightedRow);
                    timelineOverviewControl.SetHighlightedActivitySegment(highlightedRow);
                    return;
                }

                highlightedTimelineSegmentKey = null;
                highlightedTimelineSegmentLabel = null;
            }

            if (!string.IsNullOrWhiteSpace(highlightedTimelineProcessName))
            {
                timelineOverviewControl.SetHighlightedProcessName(highlightedTimelineProcessName);
                return;
            }

            ApplyTimelineActivityTypeHighlight();
        }

        private bool HasTimelineHighlight()
        {
            return highlightedTimelineSegmentKey is not null
                || !string.IsNullOrWhiteSpace(highlightedTimelineProcessName)
                || selectedTimelineActivityTypeHighlight != TimelineActivityTypeHighlight.None;
        }

        private bool IsTimelineRowHighlighted(ActivityTimelineRow row)
        {
            if (highlightedTimelineSegmentKey is not null)
                return highlightedTimelineSegmentKey.Value.Matches(row);

            if (selectedTimelineActivityTypeHighlight == TimelineActivityTypeHighlight.Windows
                && string.IsNullOrWhiteSpace(highlightedTimelineProcessName))
                return false;

            var processMatches = string.IsNullOrWhiteSpace(highlightedTimelineProcessName)
                || string.Equals(row.ProcessName, highlightedTimelineProcessName, StringComparison.OrdinalIgnoreCase);
            var typeText = GetTimelineHighlightedActivityTypeText();
            var typeMatches = typeText is null
                || string.Equals(row.ActivityType, typeText, StringComparison.Ordinal);

            if (selectedTimelineActivityTypeHighlight == TimelineActivityTypeHighlight.Untracked)
            {
                typeMatches = string.Equals(row.ActivityType, UiText.Main.Untracked, StringComparison.Ordinal)
                    || string.Equals(row.ActivityType, UiText.Main.TimePilotUntracked, StringComparison.Ordinal);
            }

            return processMatches && typeMatches;
        }

        private IReadOnlyList<SystemTimelineEvent> FilterSystemTimelineEvents(IReadOnlyList<SystemTimelineEvent> events)
        {
            if (selectedTimelineSystemEventFilter == TimelineSystemEventFilter.All)
                return events;

            return events
                .Where(systemEvent => MatchesTimelineSystemEventFilter(systemEvent.EventType, selectedTimelineSystemEventFilter))
                .ToList();
        }

        private IReadOnlyList<SystemTimelineRange> FilterSystemTimelineRanges(IReadOnlyList<SystemTimelineRange> ranges)
        {
            return selectedTimelineSystemEventFilter switch
            {
                TimelineSystemEventFilter.All => ranges,
                TimelineSystemEventFilter.Lock => ranges.Where(range => range.RangeType == SystemTimelineRangeType.LockSession).ToList(),
                TimelineSystemEventFilter.Power => ranges.Where(range => range.RangeType == SystemTimelineRangeType.SleepEstimate).ToList(),
                _ => Array.Empty<SystemTimelineRange>()
            };
        }

        private static IReadOnlyList<SystemTimelineEventRow> BuildSystemTimelineEventRows(IReadOnlyList<SystemTimelineEvent> events)
        {
            var orderedEvents = events
                .OrderBy(systemEvent => systemEvent.OccurredAt)
                .ToList();
            var rows = new List<SystemTimelineEventRow>(orderedEvents.Count);
            SystemTimelineEvent? previousEvent = null;

            foreach (var systemEvent in orderedEvents)
            {
                var intervalText = previousEvent is null
                    ? "-"
                    : FormatDiagnosticDuration((long)(systemEvent.OccurredAt - previousEvent.OccurredAt).TotalMilliseconds);
                rows.Add(new SystemTimelineEventRow(
                    systemEvent.OccurredAt,
                    systemEvent.OccurredAt.ToLocalTime().ToString("HH:mm:ss", System.Globalization.CultureInfo.CurrentCulture),
                    GetSystemEventTypeText(systemEvent.EventType),
                    previousEvent is null ? -1 : (long)(systemEvent.OccurredAt - previousEvent.OccurredAt).TotalMilliseconds,
                    intervalText,
                    GetSystemEventRelationText(systemEvent.EventType),
                    FormatSystemEventDetails(systemEvent)));
                previousEvent = systemEvent;
            }

            return rows
                .OrderByDescending(row => row.OccurredAt)
                .ToList();
        }

        private static bool MatchesTimelineSystemEventFilter(string eventType, TimelineSystemEventFilter filter)
        {
            var normalized = eventType.ToLowerInvariant();
            return filter switch
            {
                TimelineSystemEventFilter.Lock => normalized is "lock" or "unlock" or "logon" or "logoff",
                TimelineSystemEventFilter.Power => normalized is "suspend" or "resume" or "power-status-change" or "power-mode",
                TimelineSystemEventFilter.Shutdown => normalized is "system-shutdown" or "recording-end-estimate",
                TimelineSystemEventFilter.TimePilot => normalized is "timepilot-start" or "timepilot-exit" or "windows-boot-estimate" or "recording-end-estimate",
                _ => true
            };
        }

        private static string GetTimelineSystemEventsTitle(DateTime date)
        {
            var dateText = date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.CurrentCulture);
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"System event list ({dateText})"
                : $"시스템 이벤트 목록 ({dateText})";
        }

        private static string GetTimelineSystemEventsPopupDescription(DateTime date)
        {
            var dateText = date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.CurrentCulture);
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"Selected date: {dateText}. Event intervals are hints for interpretation, not confirmed causes of missing records."
                : $"선택 날짜: {dateText}. 이벤트 간격은 해석 보조 정보이며, 미기록 원인을 확정하지 않습니다.";
        }

        private static string GetTimelineCategorySegmentAppStatsMenuText()
        {
            return UiText.CurrentLanguage == UiLanguage.English ? "Segment app stats" : "구간 앱 통계";
        }

        private static string GetTimelineCategorySegmentAppStatsTitle()
        {
            return UiText.CurrentLanguage == UiLanguage.English ? "Timeline Segment App Stats" : "타임라인 구간 앱 통계";
        }

        private string GetTimelineCategorySegmentAppStatsDescription(CategoryTimelineSegment segment)
        {
            var start = segment.StartedAt.ToLocalTime().ToString("HH:mm:ss", System.Globalization.CultureInfo.CurrentCulture);
            var end = segment.EndedAt.ToLocalTime().ToString("HH:mm:ss", System.Globalization.CultureInfo.CurrentCulture);
            var duration = FormatDiagnosticDuration((long)(segment.EndedAt - segment.StartedAt).TotalMilliseconds);
            var activeUsage = FormatDiagnosticDuration(segment.ActiveUsageMs);
            var stateSummary = GetTimelineSegmentStateSummary(segment);
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"{segment.CategoryName} | {start}-{end} | segment {duration} | recorded active {activeUsage} | {segment.DetailText}\n{stateSummary}"
                : $"{segment.CategoryName} | {start}-{end} | 구간 {duration} | 기록된 활성 {activeUsage} | {segment.DetailText}\n{stateSummary}";
        }

        private string GetTimelineSegmentStateSummary(CategoryTimelineSegment segment)
        {
            var activeMs = SumTimelineRowDuration(segment, row =>
                !string.Equals(row.ActivityType, UiText.Main.Idle, StringComparison.Ordinal)
                && !IsUntrackedTimelineActivity(row));
            var idleMs = SumTimelineRowDuration(segment, row =>
                string.Equals(row.ActivityType, UiText.Main.Idle, StringComparison.Ordinal));
            var untrackedMs = SumTimelineRowDuration(segment, IsUntrackedTimelineActivity);
            var windowsRuntimeMs = SumTimelineRangeDuration(segment, currentTimelineWindowsRuntimeRanges);
            var sleepMs = SumSystemTimelineRangeDuration(segment, SystemTimelineRangeType.SleepEstimate);
            var lockMs = SumSystemTimelineRangeDuration(segment, SystemTimelineRangeType.LockSession);

            return UiText.CurrentLanguage == UiLanguage.English
                ? $"Status: active apps {FormatDiagnosticDuration(activeMs)} | idle {FormatDiagnosticDuration(idleMs)} | not tracked {FormatDiagnosticDuration(untrackedMs)} | Windows runtime {FormatDiagnosticDuration(windowsRuntimeMs)} | sleep estimate {FormatDiagnosticDuration(sleepMs)} | lock {FormatDiagnosticDuration(lockMs)}"
                : $"상태: 활성 앱 {FormatDiagnosticDuration(activeMs)} | 유휴 {FormatDiagnosticDuration(idleMs)} | 미기록 {FormatDiagnosticDuration(untrackedMs)} | Windows 실행 {FormatDiagnosticDuration(windowsRuntimeMs)} | 절전 추정 {FormatDiagnosticDuration(sleepMs)} | 잠금 {FormatDiagnosticDuration(lockMs)}";
        }

        private long SumTimelineRowDuration(CategoryTimelineSegment segment, Func<ActivityTimelineRow, bool> predicate)
        {
            return currentTimelineRows
                .Where(predicate)
                .Sum(row => GetOverlapDurationMs(segment.StartedAt, segment.EndedAt, row.StartedAt, row.EndedAt ?? segment.EndedAt));
        }

        private long SumTimelineRangeDuration(CategoryTimelineSegment segment, IEnumerable<TimelineRange> ranges)
        {
            return ranges.Sum(range => GetOverlapDurationMs(segment.StartedAt, segment.EndedAt, range.StartedAt, range.EndedAt));
        }

        private long SumSystemTimelineRangeDuration(CategoryTimelineSegment segment, SystemTimelineRangeType rangeType)
        {
            return currentTimelineSystemRanges
                .Where(range => range.RangeType == rangeType)
                .Sum(range => GetOverlapDurationMs(segment.StartedAt, segment.EndedAt, range.StartedAt, range.EndedAt));
        }

        private static bool IsUntrackedTimelineActivity(ActivityTimelineRow row)
        {
            return string.Equals(row.ActivityType, UiText.Main.Untracked, StringComparison.Ordinal)
                || string.Equals(row.ActivityType, UiText.Main.TimePilotUntracked, StringComparison.Ordinal);
        }

        private static long GetOverlapDurationMs(
            DateTimeOffset leftStart,
            DateTimeOffset leftEnd,
            DateTimeOffset rightStart,
            DateTimeOffset rightEnd)
        {
            var start = leftStart > rightStart ? leftStart : rightStart;
            var end = leftEnd < rightEnd ? leftEnd : rightEnd;
            return end <= start ? 0 : (long)(end - start).TotalMilliseconds;
        }

        private static string GetTimelineSystemEventTimeHeaderText()
        {
            return UiText.CurrentLanguage == UiLanguage.English ? "Time" : "시각";
        }

        private static string GetTimelineSystemEventPreviousIntervalHeaderText()
        {
            return UiText.CurrentLanguage == UiLanguage.English ? "Since previous" : "직전 간격";
        }

        private static string GetTimelineSystemEventRelationHeaderText()
        {
            return UiText.CurrentLanguage == UiLanguage.English ? "Hint" : "해석 단서";
        }

        private static string GetTimelineSystemEventDetailsHeaderText()
        {
            return UiText.CurrentLanguage == UiLanguage.English ? "Details" : "상세";
        }

        private static string GetSystemEventRelationText(string eventType)
        {
            var isEnglish = UiText.CurrentLanguage == UiLanguage.English;
            return eventType.ToLowerInvariant() switch
            {
                "lock" => isEnglish ? "Lock range start" : "잠금 구간 시작",
                "unlock" or "logon" => isEnglish ? "Lock range end candidate" : "잠금 구간 종료 후보",
                "logoff" => isEnglish ? "Logoff event" : "로그오프 이벤트",
                "suspend" => isEnglish ? "Sleep range start" : "절전 구간 시작",
                "resume" => isEnglish ? "Sleep range end candidate" : "절전 구간 종료 후보",
                "system-shutdown" => isEnglish ? "Shutdown/restart event" : "종료/재시작 이벤트",
                "timepilot-start" => isEnglish ? "TimePilot recording start" : "TimePilot 기록 시작",
                "timepilot-exit" => isEnglish ? "TimePilot recording end" : "TimePilot 기록 종료",
                "windows-boot-estimate" => isEnglish ? "Windows startup estimate" : "Windows 시작 추정",
                "recording-end-estimate" => isEnglish ? "Recording end estimate" : "기록 종료 추정",
                _ => "-"
            };
        }

        private static string FormatSystemEventDetails(SystemTimelineEvent systemEvent)
        {
            if (string.IsNullOrWhiteSpace(systemEvent.Details))
                return systemEvent.IsInferred ? GetInferredSystemEventDetailsText(systemEvent.EventType) : "-";

            if (systemEvent.Details.StartsWith("TimePilotStartedAt:", StringComparison.OrdinalIgnoreCase)
                && DateTimeOffset.TryParse(systemEvent.Details["TimePilotStartedAt:".Length..], out var startedAt))
            {
                return UiText.CurrentLanguage == UiLanguage.English
                    ? $"TimePilot started at {startedAt.ToLocalTime():HH:mm:ss}"
                    : $"TimePilot 시작 {startedAt.ToLocalTime():HH:mm:ss}";
            }

            if (systemEvent.Details.StartsWith("Reason:", StringComparison.OrdinalIgnoreCase))
            {
                var reason = systemEvent.Details["Reason:".Length..];
                return UiText.CurrentLanguage == UiLanguage.English
                    ? $"Reason: {GetShutdownReasonText(reason)}"
                    : $"사유: {GetShutdownReasonText(reason)}";
            }

            return systemEvent.Details;
        }

        private static string GetInferredSystemEventDetailsText(string eventType)
        {
            var isEnglish = UiText.CurrentLanguage == UiLanguage.English;
            return eventType.ToLowerInvariant() switch
            {
                "windows-boot-estimate" => isEnglish
                    ? "Estimated from Windows system startup time."
                    : "Windows 시스템 시작 시간을 기준으로 추정했습니다.",
                "recording-end-estimate" => isEnglish
                    ? "Estimated from the last TimePilot runtime record."
                    : "TimePilot의 마지막 실행 기록을 기준으로 추정했습니다.",
                _ => "-"
            };
        }

        private void UpdateTimelineHighlightSummary()
        {
            if (string.IsNullOrWhiteSpace(highlightedTimelineProcessName))
            {
                timelineHighlightSummaryPanel.Visible = false;
                timelineHighlightSummaryLabel.Text = "";
                return;
            }

            var usage = currentTimelineForegroundUsage.FirstOrDefault(x =>
                string.Equals(x.ProcessName, highlightedTimelineProcessName, StringComparison.OrdinalIgnoreCase));
            var highlightedRows = currentTimelineRows
                .Where(x => string.Equals(x.ProcessName, highlightedTimelineProcessName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (usage is null && highlightedRows.Count == 0)
            {
                timelineHighlightSummaryPanel.Visible = false;
                timelineHighlightSummaryLabel.Text = "";
                return;
            }

            var activeUsageMs = usage?.ActiveUsageMs ?? 0;
            var totalActiveUsageMs = currentTimelineForegroundUsage.Sum(x => x.ActiveUsageMs);
            var usageRatio = activeUsageMs > 0 && totalActiveUsageMs > 0
                ? (double)activeUsageMs / totalActiveUsageMs
                : 0;
            var switchCount = usage?.SwitchCount ?? 0;
            var segmentCount = highlightedRows.Count;
            var longestSegmentMs = highlightedRows.Count == 0 ? 0 : highlightedRows.Max(x => x.DurationMs);

            timelineHighlightSummaryLabel.Text = UiText.Main.TimelineHighlightSummary(
                FormatDiagnosticDuration(activeUsageMs),
                usageRatio,
                switchCount,
                segmentCount,
                FormatDiagnosticDuration(longestSegmentMs));
            timelineHighlightSummaryPanel.Visible = true;
        }

        private void OnTimelineGridRowPrePaint(object? sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0
                || e.RowIndex >= timelineGrid.Rows.Count)
                return;

            var gridRow = timelineGrid.Rows[e.RowIndex];
            if (!HasTimelineHighlight()
                || selectedTimelineActivityTypeHighlight == TimelineActivityTypeHighlight.Windows)
            {
                gridRow.DefaultCellStyle.ForeColor = SystemColors.WindowText;
                gridRow.DefaultCellStyle.BackColor = SystemColors.Window;
                gridRow.DefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;
                gridRow.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
                gridRow.DefaultCellStyle.Font = null;
                return;
            }

            if (timelineGrid.Rows[e.RowIndex].DataBoundItem is not ActivityTimelineRow row)
                return;

            var isHighlighted = IsTimelineRowHighlighted(row);
            if (highlightedTimelineSegmentKey is not null && !isHighlighted)
            {
                gridRow.DefaultCellStyle.ForeColor = SystemColors.WindowText;
                gridRow.DefaultCellStyle.BackColor = SystemColors.Window;
                gridRow.DefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;
                gridRow.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
                gridRow.DefaultCellStyle.Font = null;
                return;
            }

            gridRow.DefaultCellStyle.ForeColor = isHighlighted
                ? SystemColors.WindowText
                : SystemColors.GrayText;
            gridRow.DefaultCellStyle.BackColor = isHighlighted
                ? Color.FromArgb(218, 235, 255)
                : SystemColors.Window;
            gridRow.DefaultCellStyle.SelectionForeColor = isHighlighted
                ? SystemColors.WindowText
                : SystemColors.GrayText;
            gridRow.DefaultCellStyle.SelectionBackColor = isHighlighted
                ? Color.FromArgb(198, 224, 255)
                : Color.FromArgb(245, 245, 245);
            gridRow.DefaultCellStyle.Font = isHighlighted
                ? GetTimelineHighlightedRowFont()
                : null;
        }

        private void OnTimelineGridRowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
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

            var stripeBounds = new Rectangle(bounds.Left, bounds.Top + 1, 5, Math.Max(1, bounds.Height - 2));
            using var stripeBrush = new SolidBrush(Color.FromArgb(28, 91, 170));
            using var borderPen = new Pen(Color.FromArgb(28, 91, 170));
            e.Graphics.FillRectangle(stripeBrush, stripeBounds);
            e.Graphics.DrawRectangle(borderPen, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);
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

            timelineOverviewControl.SetExternalHoverText(TimelineOverviewControl.FormatActivityHoverText(row));
        }

        private void OnTimelineGridMouseLeave(object? sender, EventArgs e)
        {
            timelineOverviewControl.SetExternalHoverText(null);
        }

        private Rectangle GetVisibleTimelineRowCellsBounds(int rowIndex)
        {
            Rectangle bounds = Rectangle.Empty;
            foreach (DataGridViewColumn column in timelineGrid.Columns)
            {
                if (!column.Visible)
                    continue;

                var cellBounds = timelineGrid.GetCellDisplayRectangle(column.Index, rowIndex, cutOverflow: true);
                if (cellBounds.IsEmpty)
                    continue;

                bounds = bounds.IsEmpty
                    ? cellBounds
                    : Rectangle.Union(bounds, cellBounds);
            }

            return Rectangle.Intersect(bounds, timelineGrid.ClientRectangle);
        }

        private Font GetTimelineHighlightedRowFont()
        {
            if (timelineHighlightedRowFont is null
                || !string.Equals(timelineHighlightedRowFont.Name, timelineGrid.Font.Name, StringComparison.Ordinal)
                || Math.Abs(timelineHighlightedRowFont.Size - timelineGrid.Font.Size) > 0.01f)
            {
                timelineHighlightedRowFont?.Dispose();
                timelineHighlightedRowFont = new Font(timelineGrid.Font, FontStyle.Bold);
            }

            return timelineHighlightedRowFont;
        }

        private void OnSummaryPeriodComboBoxSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (isInitializingSummaryPeriodSelector)
                return;

            if (summaryPeriodComboBox.SelectedItem is not SummaryPeriodOption option)
                return;

            selectedSummaryPeriod = option.Period;
            UpdateSummaryPeriodControlsVisibility();
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnSummarySpecificDatePickerValueChanged(object? sender, EventArgs e)
        {
            ApplySummarySpecificDate(summarySpecificDatePicker.Value.Date);
        }

        private void OnSummarySpecificDateCalendarButtonClick(object? sender, EventArgs e)
        {
            ShowRecordedDateCalendar(
                summarySpecificDateCalendarButton,
                selectedSummarySpecificDate,
                ApplySummarySpecificDate);
        }

        private void OnSummaryCustomRangeButtonClick(object? sender, EventArgs e)
        {
            using var dialog = new SummaryPeriodRangeForm(
                selectedSummaryCustomStartDate,
                selectedSummaryCustomEndDate,
                DateTime.Today,
                GetRecordedDates);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            selectedSummaryCustomStartDate = dialog.StartDate;
            selectedSummaryCustomEndDate = dialog.EndDate;
            UpdateSummaryCustomRangeLabel();
            if (selectedSummaryPeriod == SummaryPeriod.CustomRange)
                RefreshViews(DateTimeOffset.UtcNow);
        }

        private void ApplySummarySpecificDate(DateTime date)
        {
            selectedSummarySpecificDate = NormalizeSelectableDate(date);
            if (summarySpecificDatePicker.Value.Date != selectedSummarySpecificDate)
            {
                isInitializingSummaryPeriodSelector = true;
                try
                {
                    EnsureDatePickerRangeIncludes(summarySpecificDatePicker, selectedSummarySpecificDate);
                    summarySpecificDatePicker.Value = selectedSummarySpecificDate;
                }
                finally
                {
                    isInitializingSummaryPeriodSelector = false;
                }
            }

            if (!isInitializingSummaryPeriodSelector && selectedSummaryPeriod == SummaryPeriod.SpecificDate)
                RefreshViews(DateTimeOffset.UtcNow);
        }

        private void UpdateSummaryPeriodControlsVisibility()
        {
            var isSpecificDate = selectedSummaryPeriod == SummaryPeriod.SpecificDate;
            var isCustomRange = selectedSummaryPeriod == SummaryPeriod.CustomRange;
            summarySpecificDatePicker.Visible = isSpecificDate;
            summarySpecificDateCalendarButton.Visible = isSpecificDate;
            summaryCustomRangeButton.Visible = isCustomRange;
            summaryCustomRangeLabel.Visible = isCustomRange;
            UpdateSummaryCustomRangeLabel();
        }

        private void UpdateSummaryCustomRangeLabel()
        {
            summaryCustomRangeLabel.Text = FormatSummaryCustomRangeLabel(
                selectedSummaryCustomStartDate,
                selectedSummaryCustomEndDate);
        }

        private static string FormatSummaryCustomRangeLabel(DateTime startDate, DateTime endDate)
        {
            var dateText = startDate.Date == endDate.Date
                ? startDate.ToString("yyyy-MM-dd (ddd)")
                : $"{startDate:yyyy-MM-dd (ddd)} ~ {endDate:yyyy-MM-dd (ddd)}";
            var durationText = CalendarRangeDurationFormatter.Format(startDate, endDate, includePrefix: false);
            return $"{dateText} · {durationText}";
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
            SaveTableSortState();
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnRuntimeGridSelectionChanged(object? sender, EventArgs e)
        {
            if (isRefreshingRuntimeGrid || isSelectingRuntimeGridRow)
                return;

            selectedRuntimeAppId = GetSelectedRuntimeAppId();
            selectedRuntimeSegmentKey = null;
            RefreshRuntimeSegments(DateTimeOffset.UtcNow);
        }

        private void OnRuntimeGridCellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0)
                return;

            if (SelectRuntimeGridRow(e.RowIndex, e.ColumnIndex, refreshSegments: false) is not { } row)
                return;

            ShowRuntimeCategoryMenu(row, runtimeGrid.PointToClient(Cursor.Position));
        }

        private void ShowRuntimeCategoryMenu(ProcessRuntimeSummaryRow row, Point location)
        {
            appCategoryMenu.Items.Clear();
            appCategoryMenu.Items.Add(CreateSetCategoryMenuItem(
                row.PrimaryCategoryId,
                categoryId => SetRuntimeAppCategory(row.AppId, row.AppName, categoryId)));
            appCategoryMenu.Items.Add(CreateSearchWebMenuItem(row.AppName, row.ProcessName));
            appCategoryMenu.Show(runtimeGrid, location);
        }

        private ToolStripMenuItem CreateSetCategoryMenuItem(long? currentCategoryId, Action<long?> setCategory)
        {
            var setCategoryMenuItem = new ToolStripMenuItem(UiText.Main.SetCategory);

            var uncategorizedItem = new ToolStripMenuItem(UiText.Main.Uncategorized)
            {
                Checked = currentCategoryId is null,
                Tag = (long?)null
            };
            uncategorizedItem.Click += (_, _) => setCategory(null);
            setCategoryMenuItem.DropDownItems.Add(uncategorizedItem);

            if (storage is null)
                return setCategoryMenuItem;

            var categories = storage.GetAppCategoryOptions();
            if (categories.Count > 0)
                setCategoryMenuItem.DropDownItems.Add(new ToolStripSeparator());

            foreach (var category in categories)
            {
                var categoryItem = new ToolStripMenuItem(AppCategoryDisplay.GetDisplayName(category))
                {
                    Checked = currentCategoryId == category.Id,
                    Tag = category.Id
                };
                categoryItem.Click += (_, _) => setCategory(category.Id);
                setCategoryMenuItem.DropDownItems.Add(categoryItem);
            }

            return setCategoryMenuItem;
        }

        private ToolStripMenuItem CreateSearchWebMenuItem(string appName, string processName)
        {
            var item = new ToolStripMenuItem(settings.UiLanguage == UiLanguage.English ? "Search web" : "웹에서 검색");
            item.Click += (_, _) => OpenAppWebSearch(appName, processName);
            return item;
        }

        private void OpenAppWebSearch(string appName, string processName)
        {
            var query = BuildAppWebSearchQuery(appName, processName);
            if (string.IsNullOrWhiteSpace(query))
                return;

            if (settings.UiLanguage != UiLanguage.English)
                query += " 이란";

            var url = "https://www.google.com/search?q=" + Uri.EscapeDataString(query);
            try
            {
                Process.Start(new ProcessStartInfo(url)
                {
                    UseShellExecute = true
                });
            }
            catch
            {
                MessageBox.Show(
                    this,
                    settings.UiLanguage == UiLanguage.English ? "Unable to open the browser." : "브라우저를 열 수 없습니다.",
                    UiText.AppName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private static string BuildAppWebSearchQuery(string appName, string processName)
        {
            var parts = new[] { appName, processName }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim())
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .Take(2);

            return string.Join(" ", parts);
        }

        private void SetRuntimeAppCategory(long appId, string appName, long? categoryId)
        {
            SetAppCategory(appId, appName, categoryId, selectRuntimeApp: true);
        }

        private void SetAppCategory(long appId, string appName, long? categoryId, bool selectRuntimeApp = false)
        {
            if (storage is null)
                return;

            storage.SetAppPrimaryCategory(appId, categoryId);
            var category = categoryId is null
                ? null
                : storage.GetAppCategoryOptions().FirstOrDefault(x => x.Id == categoryId);
            var categoryName = category is null
                ? UiText.Main.Uncategorized
                : AppCategoryDisplay.GetDisplayName(category);

            if (selectRuntimeApp)
                selectedRuntimeAppId = appId;

            SetStatusText(UiText.Main.CategoryUpdated(appName, categoryName));
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnRuntimeSegmentsGridColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0)
                return;

            var selectionKey = (runtimeSegmentsGrid.CurrentRow?.DataBoundItem as ProcessRuntimeSegmentRow) is { } selectedSegment
                ? RuntimeSegmentSelectionKey.From(selectedSegment)
                : selectedRuntimeSegmentKey;
            var propertyName = GetRuntimeSegmentSortPropertyName(runtimeSegmentsGrid.Columns[e.ColumnIndex]);
            if (propertyName is null)
                return;

            runtimeSegmentSortOrder = string.Equals(runtimeSegmentSortProperty, propertyName, StringComparison.Ordinal)
                ? ToggleSortOrder(runtimeSegmentSortOrder)
                : SortOrder.Descending;
            runtimeSegmentSortProperty = propertyName;
            SaveTableSortState();
            RefreshRuntimeSegments(
                DateTimeOffset.UtcNow,
                selectionKeyToRestore: selectionKey,
                selectFirstWhenMissing: false);
            UpdateSortGlyphs();
        }

        private void OnRuntimeSegmentsGridSelectionChanged(object? sender, EventArgs e)
        {
            if (isRestoringRuntimeSegmentSelection)
                return;

            if (runtimeSegmentsGrid.CurrentRow is null && runtimeSegmentsGrid.Rows.Count > 0)
                return;

            var selectedSegment = runtimeSegmentsGrid.CurrentRow?.DataBoundItem as ProcessRuntimeSegmentRow;
            selectedRuntimeSegmentKey = selectedSegment is null ? null : RuntimeSegmentSelectionKey.From(selectedSegment);
            runtimeSegmentTimelineControl.SetHighlightedSegment(selectedSegment);
        }

        private void OnRunningRuntimeOnlyCheckBoxCheckedChanged(object? sender, EventArgs e)
        {
            showRunningRuntimeOnly = runningRuntimeOnlyCheckBox.Checked;
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnDetailRuntimeFilterComboBoxSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (isUpdatingDetailRuntimeFilterOptions || detailRuntimeFilterComboBox.SelectedIndex < 0)
                return;

            if (mainTabs.SelectedTab != detailTab)
            {
                isUpdatingDetailRuntimeFilterOptions = true;
                try
                {
                    SyncDetailRuntimeFilterComboBoxSelection();
                }
                finally
                {
                    isUpdatingDetailRuntimeFilterOptions = false;
                }

                return;
            }

            selectedDetailRuntimeFilter = (DetailRuntimeFilter)detailRuntimeFilterComboBox.SelectedIndex;
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnDetailHelpButtonClick(object? sender, EventArgs e)
        {
            var currentSelection = GetDetailRuntimeFilterText(selectedDetailRuntimeFilter);
            var currentDescription = GetDetailRuntimeFilterDescription(selectedDetailRuntimeFilter);
            var message = UiText.Main.DetailHelpCurrentSelection(currentSelection, currentDescription)
                + Environment.NewLine
                + Environment.NewLine
                + UiText.Main.DetailHelpMessage;

            CenteredMessageDialog.Show(
                this,
                message,
                UiText.Main.DetailHelpTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void OnDetailTrackingDisabledPreferencesButtonClick(object? sender, EventArgs e)
        {
            ShowPreferencesDialog();
        }

        private static void ConfigureRuntimeSegmentZoomButton(
            Button button,
            EventHandler clickHandler,
            int width = 32)
        {
            button.Size = new Size(width, 24);
            button.Margin = new Padding(3, 2, 3, 0);
            button.UseVisualStyleBackColor = true;
            button.Click += clickHandler;
        }

        private string GetRuntimeSegmentViewRangeText()
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"Runtime view: {runtimeSegmentTimelineControl.ViewRangeText}"
                : $"실행 구간 보기: {runtimeSegmentTimelineControl.ViewRangeText}";
        }

        private static string GetRuntimeSegmentResetText()
        {
            return UiText.CurrentLanguage == UiLanguage.English ? "Full" : "전체";
        }

        private static string GetRuntimeSegmentObservationFilterLabelText()
        {
            return UiText.CurrentLanguage == UiLanguage.English ? "Basis" : "관측 기준";
        }

        private static string GetRuntimeSegmentResetTooltip()
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? "Return the selected app runtime chart to the full-day view."
                : "선택 앱 실행 구간 그래프를 하루 전체 보기로 되돌립니다.";
        }

        private static string GetRuntimeSegmentHelpTitle()
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? "Selected App Runtime Chart Help"
                : "선택 앱 실행 구간 도움말";
        }

        private static string GetRuntimeSegmentHelpMessage()
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? "Selected app runtime chart controls\n\n"
                    + "- Drag the chart: zoom into the selected time range.\n"
                    + "- Ctrl+wheel: zoom around the pointer.\n"
                    + "- Shift+wheel: pan left/right in the zoomed view.\n"
                    + "- Left / Right keys: pan left/right after clicking the chart.\n"
                    + "- Esc or Full: return to the full-day view.\n"
                    + "- Selecting a runtime segment row highlights that segment on the chart.\n\n"
                    + "Overlapping or dense segments may look close together. Zoom into the range when you need to inspect the exact position."
                : "선택 앱 실행 구간 그래프 조작\n\n"
                    + "- 그래프 드래그: 선택한 시간 범위로 확대합니다.\n"
                    + "- Ctrl+휠: 마우스 위치를 기준으로 확대/축소합니다.\n"
                    + "- Shift+휠: 확대한 보기에서 좌우로 이동합니다.\n"
                    + "- 왼쪽/오른쪽 방향키: 그래프를 클릭한 뒤 좌우로 이동합니다.\n"
                    + "- Esc 또는 전체: 하루 전체 보기로 되돌립니다.\n"
                    + "- 실행 구간 목록의 행을 선택하면 해당 구간이 그래프에서 강조됩니다.\n\n"
                    + "구간이 많이 겹치거나 촘촘한 앱은 한눈에 구분하기 어려울 수 있습니다. 정확한 위치를 보려면 해당 범위를 확대해서 확인하세요.";
        }

        private static string GetDetailRuntimeFilterText(DetailRuntimeFilter filter)
        {
            return filter switch
            {
                DetailRuntimeFilter.CurrentTrackingScope => UiText.Main.DetailFilterCurrentScope,
                DetailRuntimeFilter.VisibleApps => UiText.Main.DetailFilterVisibleApps,
                DetailRuntimeFilter.UserProcesses => UiText.Main.DetailFilterUserProcesses,
                DetailRuntimeFilter.AllRecords => UiText.Main.DetailFilterAllRecords,
                _ => UiText.Main.DetailFilterSummaryApps
            };
        }

        private static string GetDetailRuntimeFilterDescription(DetailRuntimeFilter filter)
        {
            return filter switch
            {
                DetailRuntimeFilter.CurrentTrackingScope => UiText.Main.DetailFilterCurrentScopeDescription,
                DetailRuntimeFilter.VisibleApps => UiText.Main.DetailFilterVisibleAppsDescription,
                DetailRuntimeFilter.UserProcesses => UiText.Main.DetailFilterUserProcessesDescription,
                DetailRuntimeFilter.AllRecords => UiText.Main.DetailFilterAllRecordsDescription,
                _ => UiText.Main.DetailFilterSummaryAppsDescription
            };
        }

        private string? GetUsageSortPropertyName(DataGridViewColumn column)
        {
            return column.Name switch
            {
                nameof(appNameColumn) => nameof(UsageSummaryRow.AppName),
                nameof(appCategoryColumn) => nameof(UsageSummaryRow.CategoryText),
                nameof(firstStartedAtColumn) => nameof(UsageSummaryRow.FirstStartedAt),
                nameof(lastObservedAtColumn) => nameof(UsageSummaryRow.LastObservedAt),
                nameof(activeUsageTimeColumn) => nameof(UsageSummaryRow.ActiveUsageMs),
                nameof(idleRecordedTimeColumn) => nameof(UsageSummaryRow.IdleRecordedMs),
                nameof(usageRatioColumn) => nameof(UsageSummaryRow.UsageRatio),
                nameof(switchCountColumn) => nameof(UsageSummaryRow.SwitchCount),
                _ => null
            };
        }

        private string? GetDailyUsageTrendSortPropertyName(DataGridViewColumn column)
        {
            return column.Name switch
            {
                nameof(dailyUsageDateColumn) => nameof(DailyUsageTrendRow.Date),
                nameof(dailyUsageActiveTimeColumn) => nameof(DailyUsageTrendRow.ActiveUsageMs),
                nameof(dailyUsageTopAppColumn) => nameof(DailyUsageTrendRow.TopAppName),
                nameof(dailyUsageTopAppTimeColumn) => nameof(DailyUsageTrendRow.TopAppUsageMs),
                _ => null
            };
        }

        private string? GetTimelineSortPropertyName(DataGridViewColumn column)
        {
            return column.Name switch
            {
                nameof(timelineTypeColumn) => nameof(ActivityTimelineRow.ActivityType),
                nameof(timelineStartedAtColumn) => nameof(ActivityTimelineRow.StartedAt),
                nameof(timelineEndedAtColumn) => nameof(ActivityTimelineRow.EndedAt),
                nameof(timelineDurationColumn) => nameof(ActivityTimelineRow.DurationMs),
                nameof(timelineDisplayNameColumn) => nameof(ActivityTimelineRow.DisplayName),
                nameof(timelineCategoryColumn) => nameof(ActivityTimelineRow.CategoryText),
                _ => null
            };
        }

        private string? GetRuntimeSortPropertyName(DataGridViewColumn column)
        {
            return column.Name switch
            {
                nameof(runtimeAppNameColumn) => nameof(ProcessRuntimeSummaryRow.AppName),
                nameof(runtimeCategoryColumn) => nameof(ProcessRuntimeSummaryRow.CategoryText),
                nameof(runtimeTrackingTypeColumn) => nameof(ProcessRuntimeSummaryRow.TrackingTypeText),
                nameof(runtimeFirstObservedAtColumn) => nameof(ProcessRuntimeSummaryRow.FirstObservedAt),
                nameof(runtimeLastObservedAtColumn) => nameof(ProcessRuntimeSummaryRow.LastObservedAt),
                nameof(runtimeDurationColumn) => nameof(ProcessRuntimeSummaryRow.RuntimeMs),
                nameof(runtimeActiveUsageColumn) => nameof(ProcessRuntimeSummaryRow.ActiveUsageMs),
                nameof(runtimeIdleRecordedColumn) => nameof(ProcessRuntimeSummaryRow.IdleRecordedMs),
                nameof(runtimeActualUsageRatioColumn) => nameof(ProcessRuntimeSummaryRow.ActualUsageRatio),
                nameof(runtimeSessionCountColumn) => nameof(ProcessRuntimeSummaryRow.RuntimeSegmentCount),
                nameof(runtimeStatusColumn) => nameof(ProcessRuntimeSummaryRow.StatusText),
                _ => null
            };
        }

        private bool IsInCurrentTrackingScope(ProcessRuntimeSummaryRow row)
        {
            return settings.ProcessRuntimeTrackingScope switch
            {
                ProcessRuntimeTrackingScope.WindowedApps => row.HasMainWindow,
                ProcessRuntimeTrackingScope.UserProcesses => row.IsCurrentSessionProcess,
                _ => true
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

            foreach (DataGridViewColumn column in dailyUsageTrendGrid.Columns)
            {
                column.HeaderCell.SortGlyphDirection = GetDailyUsageTrendSortPropertyName(column) == dailyUsageTrendSortProperty
                    ? dailyUsageTrendSortOrder
                    : SortOrder.None;
            }

            foreach (DataGridViewColumn column in timelineGrid.Columns)
            {
                column.HeaderCell.SortGlyphDirection = GetTimelineSortPropertyName(column) == timelineSortProperty
                    ? timelineSortOrder
                    : SortOrder.None;
            }

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
            headerToolTipLabel.Text = UiText.Main.UsageRatioTooltip;

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
                return UiText.Main.UsageRatioTooltip;

            if (grid == usageGrid && column == idleRecordedTimeColumn)
                return UiText.Main.IdleRecordedTimeTooltip;

            if (grid == runtimeGrid && column == runtimeLastObservedAtColumn)
                return UiText.Main.RuntimeLastObservedTooltip;

            if (grid == runtimeGrid && column == runtimeDurationColumn)
                return UiText.Main.RuntimeDurationTooltip;

            if (grid == runtimeGrid && column == runtimeIdleRecordedColumn)
                return UiText.Main.IdleRecordedTimeTooltip;

            if (grid == runtimeGrid && column == runtimeActualUsageRatioColumn)
                return UiText.Main.RuntimeActualUsageRatioTooltip;

            if (grid == runtimeGrid && column == runtimeSessionCountColumn)
                return UiText.Main.RuntimeSegmentCountTooltip;

            if (grid == runtimeGrid && column == runtimeStatusColumn)
                return UiText.Main.RuntimeStatusTooltip;

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

        private ProcessRuntimeSummaryRow? GetSelectedRuntimeSummaryRow()
        {
            return runtimeGrid.CurrentRow?.DataBoundItem as ProcessRuntimeSummaryRow;
        }

        private ProcessRuntimeSummaryRow? GetRuntimeRowForSelectedApp()
        {
            if (selectedRuntimeAppId is { } appId)
            {
                foreach (DataGridViewRow row in runtimeGrid.Rows)
                {
                    if (row.DataBoundItem is ProcessRuntimeSummaryRow runtimeRow
                        && runtimeRow.AppId == appId)
                        return runtimeRow;
                }
            }

            return runtimeGrid.CurrentRow?.DataBoundItem as ProcessRuntimeSummaryRow;
        }

        private static T? SelectGridRow<T>(DataGridView grid, int rowIndex, int columnIndex)
            where T : class
        {
            if (rowIndex < 0
                || rowIndex >= grid.Rows.Count
                || grid.Rows[rowIndex].DataBoundItem is not T row)
                return null;

            grid.ClearSelection();
            var targetColumnIndex = columnIndex >= 0
                ? columnIndex
                : GetFirstDisplayedColumnIndex(grid);
            targetColumnIndex = Math.Clamp(targetColumnIndex, 0, grid.Columns.Count - 1);
            grid.CurrentCell = grid.Rows[rowIndex].Cells[targetColumnIndex];
            grid.Rows[rowIndex].Selected = true;
            return row;
        }

        private ProcessRuntimeSummaryRow? SelectRuntimeGridRow(int rowIndex, int columnIndex, bool refreshSegments = true)
        {
            if (rowIndex < 0
                || rowIndex >= runtimeGrid.Rows.Count
                || runtimeGrid.Rows[rowIndex].DataBoundItem is not ProcessRuntimeSummaryRow row)
                return null;

            isSelectingRuntimeGridRow = true;
            try
            {
                runtimeGrid.ClearSelection();
                var targetColumnIndex = columnIndex >= 0
                    ? columnIndex
                    : GetFirstDisplayedColumnIndex(runtimeGrid);
                targetColumnIndex = Math.Clamp(targetColumnIndex, 0, runtimeGrid.Columns.Count - 1);
                runtimeGrid.CurrentCell = runtimeGrid.Rows[rowIndex].Cells[targetColumnIndex];
                runtimeGrid.Rows[rowIndex].Selected = true;
                selectedRuntimeAppId = row.AppId;
            }
            finally
            {
                isSelectingRuntimeGridRow = false;
            }

            if (refreshSegments)
                RefreshRuntimeSegments(DateTimeOffset.UtcNow);

            return row;
        }

        private void RestoreRuntimeSelection(
            long? appId,
            int firstDisplayedRowIndex,
            int firstDisplayedColumnIndex,
            int horizontalScrollingOffset)
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
                TrySetFirstDisplayedRowIndex(runtimeGrid, firstDisplayedRowIndex);
                TrySetFirstDisplayedColumnIndex(runtimeGrid, firstDisplayedColumnIndex);
                TrySetHorizontalScrollingOffset(runtimeGrid, horizontalScrollingOffset);
                return;
            }
        }

        private void RestoreRuntimeGridView(
            int firstDisplayedRowIndex,
            int firstDisplayedColumnIndex,
            int horizontalScrollingOffset)
        {
            TrySetFirstDisplayedRowIndex(runtimeGrid, firstDisplayedRowIndex);
            TrySetFirstDisplayedColumnIndex(runtimeGrid, firstDisplayedColumnIndex);
            TrySetHorizontalScrollingOffset(runtimeGrid, horizontalScrollingOffset);
        }

        private void ScheduleRuntimeGridViewRestore(
            int firstDisplayedRowIndex,
            int firstDisplayedColumnIndex,
            int horizontalScrollingOffset)
        {
            if (runtimeGrid.IsDisposed || !runtimeGrid.IsHandleCreated)
                return;

            runtimeGrid.BeginInvoke(new Action(() =>
            {
                if (runtimeGrid.IsDisposed)
                    return;

                RestoreRuntimeGridView(
                    firstDisplayedRowIndex,
                    firstDisplayedColumnIndex,
                    horizontalScrollingOffset);
            }));
        }

        private static SortOrder ToggleSortOrder(SortOrder sortOrder)
        {
            return sortOrder == SortOrder.Descending
                ? SortOrder.Ascending
                : SortOrder.Descending;
        }

        private static SortOrder GetSavedSortOrder(bool? descending, SortOrder defaultSortOrder)
        {
            return descending is null
                ? defaultSortOrder
                : descending.Value ? SortOrder.Descending : SortOrder.Ascending;
        }

        private static string NormalizeUsageSortProperty(string? value)
        {
            return value switch
            {
                nameof(UsageSummaryRow.AppName) => value,
                nameof(UsageSummaryRow.CategoryText) => value,
                nameof(UsageSummaryRow.FirstStartedAt) => value,
                nameof(UsageSummaryRow.LastObservedAt) => value,
                nameof(UsageSummaryRow.IdleRecordedMs) => value,
                nameof(UsageSummaryRow.UsageRatio) => value,
                nameof(UsageSummaryRow.SwitchCount) => value,
                _ => nameof(UsageSummaryRow.ActiveUsageMs)
            };
        }

        private static string NormalizeDailyUsageTrendSortProperty(string? value)
        {
            return value switch
            {
                nameof(DailyUsageTrendRow.ActiveUsageMs) => value,
                nameof(DailyUsageTrendRow.TopAppName) => value,
                nameof(DailyUsageTrendRow.TopAppUsageMs) => value,
                _ => nameof(DailyUsageTrendRow.Date)
            };
        }

        private static string NormalizeTimelineSortProperty(string? value)
        {
            return value switch
            {
                nameof(ActivityTimelineRow.ActivityType) => value,
                nameof(ActivityTimelineRow.EndedAt) => value,
                nameof(ActivityTimelineRow.DurationMs) => value,
                nameof(ActivityTimelineRow.DisplayName) => value,
                nameof(ActivityTimelineRow.CategoryText) => value,
                _ => nameof(ActivityTimelineRow.StartedAt)
            };
        }

        private static string NormalizeRuntimeSortProperty(string? value)
        {
            return value switch
            {
                nameof(ProcessRuntimeSummaryRow.AppName) => value,
                nameof(ProcessRuntimeSummaryRow.CategoryText) => value,
                nameof(ProcessRuntimeSummaryRow.TrackingTypeText) => value,
                nameof(ProcessRuntimeSummaryRow.FirstObservedAt) => value,
                nameof(ProcessRuntimeSummaryRow.LastObservedAt) => value,
                nameof(ProcessRuntimeSummaryRow.ActiveUsageMs) => value,
                nameof(ProcessRuntimeSummaryRow.IdleRecordedMs) => value,
                nameof(ProcessRuntimeSummaryRow.ActualUsageRatio) => value,
                nameof(ProcessRuntimeSummaryRow.RuntimeSegmentCount) => value,
                nameof(ProcessRuntimeSummaryRow.StatusText) => value,
                _ => nameof(ProcessRuntimeSummaryRow.RuntimeMs)
            };
        }

        private static string NormalizeRuntimeSegmentSortProperty(string? value)
        {
            return value switch
            {
                nameof(ProcessRuntimeSegmentRow.EndedAt) => value,
                nameof(ProcessRuntimeSegmentRow.DurationMs) => value,
                nameof(ProcessRuntimeSegmentRow.IsRunning) => value,
                nameof(ProcessRuntimeSegmentRow.ObservationTypeText) => value,
                nameof(ProcessRuntimeSegmentRow.ProcessId) => value,
                _ => nameof(ProcessRuntimeSegmentRow.StartedAt)
            };
        }

        private static void SetGridDataSourcePreservingView<T>(
            DataGridView grid,
            IReadOnlyList<T> rows,
            bool preserveSelection = true)
        {
            var firstDisplayedRowIndex = GetFirstDisplayedRowIndex(grid);
            var firstDisplayedColumnIndex = GetFirstDisplayedColumnIndex(grid);
            var horizontalScrollingOffset = GetHorizontalScrollingOffset(grid);
            var selectedIndex = grid.CurrentRow?.Index ?? -1;

            grid.DataSource = rows;

            if (grid.Rows.Count == 0)
                return;

            var restoredFirstRowIndex = Math.Min(firstDisplayedRowIndex, grid.Rows.Count - 1);
            var restoredFirstColumnIndex = Math.Min(firstDisplayedColumnIndex, grid.Columns.Count - 1);
            TrySetFirstDisplayedRowIndex(grid, restoredFirstRowIndex);
            TrySetFirstDisplayedColumnIndex(grid, restoredFirstColumnIndex);
            TrySetHorizontalScrollingOffset(grid, horizontalScrollingOffset);

            if (!preserveSelection || selectedIndex < 0)
                return;

            var restoredSelectedIndex = Math.Min(selectedIndex, grid.Rows.Count - 1);
            var restoredSelectedColumnIndex = Math.Min(restoredFirstColumnIndex, grid.Columns.Count - 1);
            grid.ClearSelection();
            grid.Rows[restoredSelectedIndex].Selected = true;
            grid.CurrentCell = grid.Rows[restoredSelectedIndex].Cells[restoredSelectedColumnIndex];
            TrySetFirstDisplayedRowIndex(grid, restoredFirstRowIndex);
            TrySetFirstDisplayedColumnIndex(grid, restoredFirstColumnIndex);
            TrySetHorizontalScrollingOffset(grid, horizontalScrollingOffset);
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

        private static int GetHorizontalScrollingOffset(DataGridView grid)
        {
            try
            {
                return Math.Max(grid.HorizontalScrollingOffset, 0);
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

        private static void TrySetHorizontalScrollingOffset(DataGridView grid, int offset)
        {
            try
            {
                if (offset >= 0)
                    grid.HorizontalScrollingOffset = offset;
            }
            catch (ArgumentOutOfRangeException)
            {
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
            ShowPreferencesDialog();
        }

        private void OnResetTableSortMenuItemClick(object? sender, EventArgs e)
        {
            ResetTableSortState();
        }

        private void OnAppCategoryManagementMenuItemClick(object? sender, EventArgs e)
        {
            if (storage is null)
                return;

            using var form = new AppCategoryManagementForm(storage, settings.UiLanguage);
            form.Icon = Icon;
            if (form.ShowDialog(this) == DialogResult.OK && form.CategoriesChanged)
                RefreshViews(DateTimeOffset.UtcNow);
        }

        private void ShowPreferencesDialog()
        {
            using var form = new PreferencesForm(settings);
            if (form.ShowDialog(this) != DialogResult.OK)
                return;

            settings.SetIdleThresholdMinutes(form.IdleThresholdMinutes);
            var languageChanged = settings.UiLanguage != form.UiLanguage;
            settings.SetUiLanguage(form.UiLanguage);
            if (languageChanged)
            {
                UiText.UseLanguage(settings.UiLanguage);
                ApplyUiText();
            }

            settings.SetStartWithWindows(form.StartWithWindows);
            settings.SetPerformanceDiagnosticsEnabled(form.PerformanceDiagnosticsEnabled);
            if (!settings.PerformanceDiagnosticsEnabled)
            {
                performanceStatusText = null;
                performanceStatusExpiresAt = null;
                RefreshStatusLabel();
            }

            settings.SetProcessRuntimeTracking(
                form.ProcessRuntimeTrackingEnabled,
                form.ProcessRuntimeTrackingScope,
                form.ProcessRuntimeSampleIntervalSeconds,
                form.ProcessRuntimeRiskAccepted);
            lastProcessRuntimeSampleAt = null;
            UpdateDetailTrackingDisabledBanner();
            RefreshViews(DateTimeOffset.UtcNow);

            if (form.ClearUsageDataRequested)
                ClearUsageData();
        }

        private void UpdateDetailTrackingDisabledBanner()
        {
            if (detailTrackingDisabledPanel.IsDisposed)
                return;

            detailTrackingDisabledPanel.Visible = !settings.ProcessRuntimeTrackingEnabled;
        }

        private void ClearUsageData()
        {
            if (storage is null)
                return;

            var now = DateTimeOffset.UtcNow;
            sampleTimer.Stop();

            try
            {
                idleSessionTracker?.EndCurrentSession(now);
                foregroundSessionTracker?.EndCurrentSession(now);
                lock (processRuntimeTrackingLock)
                {
                    processRuntimeSessionTracker?.EndCurrentSessions(now);
                }

                storage.EndRuntimeSession(now, "clear-data");
                storage.ClearUsageData();
                storage.BeginRuntimeSession(now, GetCurrentSystemBootedAt(now), Application.ProductVersion);
                RecordWindowsSystemEvent("timepilot-start", "ApplicationRestartedAfterClearData");

                foregroundSessionTracker = new ForegroundSessionTracker(storage);
                idleSessionTracker = new IdleSessionTracker(storage);
                processRuntimeSessionTracker = new ProcessRuntimeSessionTracker(storage);
                lastProcessRuntimeSampleAt = null;
                lastSampleTickAt = null;
                selectedRuntimeAppId = null;
                performanceStatusText = null;
                performanceStatusExpiresAt = null;

                SetGridDataSourcePreservingView(usageGrid, Array.Empty<UsageSummaryRow>());
                SetGridDataSourcePreservingView(dailyUsageTrendGrid, Array.Empty<DailyUsageTrendRow>());
                SetRuntimeCoverageSummary(null);
                timelineOverviewControl.SetTimeline(
                    selectedTimelineDate,
                    Array.Empty<ActivityTimelineRow>(),
                    Array.Empty<TimelineRange>(),
                    Array.Empty<SystemTimelineRange>(),
                    Array.Empty<SystemTimelineEvent>(),
                    Array.Empty<CategoryTimelineSegment>());
                SetGridDataSourcePreservingView(timelineGrid, Array.Empty<ActivityTimelineRow>());
                currentTimelineForegroundUsage = Array.Empty<ForegroundUsageSummary>();
                currentTimelineRows = Array.Empty<ActivityTimelineRow>();
                currentTimelineWindowsRuntimeRanges = Array.Empty<TimelineRange>();
                currentTimelineSystemRanges = Array.Empty<SystemTimelineRange>();
                currentTimelineSystemEvents = Array.Empty<SystemTimelineEvent>();
                SetGridDataSourcePreservingView(runtimeGrid, Array.Empty<ProcessRuntimeSummaryRow>());
                SetGridDataSourcePreservingView(runtimeSegmentsGrid, Array.Empty<ProcessRuntimeSegmentRow>());
                SetStatusText(UiText.Main.UsageDataCleared);
            }
            finally
            {
                if (!isClosing)
                    sampleTimer.Start();
            }
        }

        private async void OnExportCsvMenuItemClick(object? sender, EventArgs e)
        {
            if (storage is null || isExportRunning)
                return;

            var storageSnapshot = storage;
            var now = DateTimeOffset.UtcNow;
            var today = now.ToLocalTime().Date;
            using var rangeDialog = new CsvExportRangeForm(today, settings.UiLanguage, GetRecordedDates);
            if (rangeDialog.ShowDialog(this) != DialogResult.OK)
                return;

            var rangeText = FormatCsvExportRangeForFileName(rangeDialog.StartDate, rangeDialog.EndDate);
            using var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "csv",
                FileName = $"TimePilot-usage-{rangeText}.csv",
                Filter = UiText.Main.CsvFilter,
                OverwritePrompt = false,
                Title = UiText.Main.CsvExportTitle
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            var startDate = rangeDialog.StartDate;
            var endDate = rangeDialog.EndDate;
            try
            {
                SetExportRunning(true, BuildExportInProgressStatus(UiText.Main.CsvExportTitle));
                var fileName = dialog.FileName;
                var exportedFiles = await Task.Run(() =>
                {
                    var exporter = new UsageCsvExporter(storageSnapshot);
                    return exporter.ExportRange(fileName, startDate, endDate, now);
                });

                SetExportRunning(false, BuildExportCompletedStatus(UiText.Main.CsvExportTitle));
                CenteredMessageDialog.Show(
                    this,
                    UiText.Main.CsvExportCompleted(exportedFiles.Count, Path.GetDirectoryName(dialog.FileName)),
                    UiText.Main.CsvExportTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                ClearExportStatus();
            }
            catch (Exception ex)
            {
                SetExportRunning(false, BuildExportFailedStatus(UiText.Main.CsvExportTitle));
                CenteredMessageDialog.Show(
                    this,
                    UiText.Main.CsvExportFailed(ex.Message),
                    UiText.Main.CsvExportTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                ClearExportStatus();
            }
        }

        private static string FormatCsvExportRangeForFileName(DateTime startDate, DateTime endDate)
        {
            return startDate.Date == endDate.Date
                ? startDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                : $"{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}";
        }

        private void SetExportRunning(bool isRunning, string? message)
        {
            isExportRunning = isRunning;
            exportStatusText = message;
            exportCsvMenuItem.Enabled = !isRunning;
            exportRawDataMenuItem.Enabled = !isRunning;
            createDataBackupMenuItem.Enabled = !isRunning;
            restoreDataBackupMenuItem.Enabled = !isRunning;
            UseWaitCursor = isRunning;
            RefreshStatusLabel();
        }

        private static string BuildExportInProgressStatus(string title)
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"{title} in progress..."
                : $"{title} 진행 중...";
        }

        private static string BuildExportCompletedStatus(string title)
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"{title} completed."
                : $"{title} 완료.";
        }

        private static string BuildExportFailedStatus(string title)
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"{title} failed."
                : $"{title} 실패.";
        }

        private void ClearExportStatus()
        {
            exportStatusText = null;
            RefreshStatusLabel();
        }

        private async void OnExportRawDataMenuItemClick(object? sender, EventArgs e)
        {
            if (storage is null || isExportRunning)
                return;

            var storageSnapshot = storage;
            var confirm = CenteredMessageDialog.Show(
                this,
                UiText.Main.RawDataExportWarning,
                UiText.Main.RawDataExportTitle,
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (confirm != DialogResult.OK)
                return;

            var now = DateTimeOffset.UtcNow;
            using var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "zip",
                FileName = $"TimePilot-raw-data-{now.ToLocalTime():yyyy-MM-dd}.zip",
                Filter = UiText.Main.ZipFilter,
                OverwritePrompt = true,
                Title = UiText.Main.RawDataExportTitle
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                SetExportRunning(true, BuildExportInProgressStatus(UiText.Main.RawDataExportTitle));
                var fileName = dialog.FileName;
                var exportedFiles = await Task.Run(() =>
                {
                    var exporter = new RawDataZipExporter(storageSnapshot);
                    return exporter.Export(fileName);
                });

                SetExportRunning(false, BuildExportCompletedStatus(UiText.Main.RawDataExportTitle));
                CenteredMessageDialog.Show(
                    this,
                    UiText.Main.RawDataExportCompleted(dialog.FileName, exportedFiles.Count),
                    UiText.Main.RawDataExportTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                ClearExportStatus();
            }
            catch (Exception ex)
            {
                SetExportRunning(false, BuildExportFailedStatus(UiText.Main.RawDataExportTitle));
                CenteredMessageDialog.Show(
                    this,
                    UiText.Main.RawDataExportFailed(ex.Message),
                    UiText.Main.RawDataExportTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                ClearExportStatus();
            }
        }

        private async void OnCreateDataBackupMenuItemClick(object? sender, EventArgs e)
        {
            if (storage is null || isExportRunning)
                return;

            var confirm = CenteredMessageDialog.Show(
                this,
                UiText.Main.DataBackupWarning,
                UiText.Main.DataBackupTitle,
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);
            if (confirm != DialogResult.OK)
                return;

            var now = DateTimeOffset.UtcNow;
            using var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "zip",
                FileName = $"TimePilot-backup-{now.ToLocalTime():yyyy-MM-dd-HHmm}.zip",
                Filter = UiText.Main.ZipFilter,
                OverwritePrompt = true,
                Title = UiText.Main.DataBackupTitle
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            var wasTimerEnabled = sampleTimer.Enabled;
            try
            {
                SetExportRunning(true, BuildExportInProgressStatus(UiText.Main.DataBackupTitle));
                sampleTimer.Stop();
                storage.UpdateRuntimeHeartbeat(now);

                var fileName = dialog.FileName;
                var entries = await Task.Run(() =>
                {
                    var service = new DataBackupService();
                    return service.CreateBackup(fileName, now);
                });

                SetExportRunning(false, BuildExportCompletedStatus(UiText.Main.DataBackupTitle));
                CenteredMessageDialog.Show(
                    this,
                    UiText.Main.DataBackupCompleted(dialog.FileName, entries.Count),
                    UiText.Main.DataBackupTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                ClearExportStatus();
            }
            catch (Exception ex)
            {
                SetExportRunning(false, BuildExportFailedStatus(UiText.Main.DataBackupTitle));
                CenteredMessageDialog.Show(
                    this,
                    UiText.Main.DataBackupFailed(ex.Message),
                    UiText.Main.DataBackupTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                ClearExportStatus();
            }
            finally
            {
                if (wasTimerEnabled && !isClosing)
                    sampleTimer.Start();
            }
        }

        private async void OnRestoreDataBackupMenuItemClick(object? sender, EventArgs e)
        {
            if (isExportRunning)
                return;

            using var dialog = new OpenFileDialog
            {
                AddExtension = true,
                DefaultExt = "zip",
                Filter = UiText.Main.ZipFilter,
                Title = UiText.Main.DataRestoreTitle
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            var service = new DataBackupService();
            DataBackupRestorePlan plan;
            try
            {
                plan = service.InspectBackup(dialog.FileName);
            }
            catch (Exception ex)
            {
                CenteredMessageDialog.Show(
                    this,
                    UiText.Main.DataRestoreFailed(ex.Message),
                    UiText.Main.DataRestoreTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var confirm = CenteredMessageDialog.Show(
                this,
                $"{UiText.Main.DataRestorePlan(plan.HasSettings, plan.LogCount)}\n\n{UiText.Main.DataRestoreWarning}",
                UiText.Main.DataRestoreTitle,
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (confirm != DialogResult.OK)
                return;

            var now = DateTimeOffset.UtcNow;
            try
            {
                SetExportRunning(true, BuildExportInProgressStatus(UiText.Main.DataRestoreTitle));
                sampleTimer.Stop();
                EndCurrentTrackingSessions(now, "restore-data");
                storage?.Dispose();
                storage = null;

                var fileName = dialog.FileName;
                var result = await Task.Run(() => service.RestoreBackup(fileName));
                ReinitializeStorageAfterDataRestore(DateTimeOffset.UtcNow);

                SetExportRunning(false, BuildExportCompletedStatus(UiText.Main.DataRestoreTitle));
                CenteredMessageDialog.Show(
                    this,
                    UiText.Main.DataRestoreCompleted(result.RestoredFiles.Count),
                    UiText.Main.DataRestoreTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                ClearExportStatus();
            }
            catch (Exception ex)
            {
                TryReinitializeStorageAfterRestoreFailure();
                SetExportRunning(false, BuildExportFailedStatus(UiText.Main.DataRestoreTitle));
                CenteredMessageDialog.Show(
                    this,
                    UiText.Main.DataRestoreFailed(ex.Message),
                    UiText.Main.DataRestoreTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                ClearExportStatus();
            }
            finally
            {
                if (!isClosing && storage is not null)
                    sampleTimer.Start();
            }
        }

        private void EndCurrentTrackingSessions(DateTimeOffset endedAt, string shutdownReason)
        {
            idleSessionTracker?.EndCurrentSession(endedAt);
            foregroundSessionTracker?.EndCurrentSession(endedAt);
            lock (processRuntimeTrackingLock)
            {
                processRuntimeSessionTracker?.EndCurrentSessions(endedAt);
            }

            storage?.EndRuntimeSession(endedAt, shutdownReason);
            foregroundSessionTracker = null;
            idleSessionTracker = null;
            processRuntimeSessionTracker = null;
        }

        private void ReinitializeStorageAfterDataRestore(DateTimeOffset startedAt)
        {
            settings = AppSettings.LoadDefault();
            WindowsStartupRegistration.SetEnabled(settings.StartWithWindows);
            UiText.UseLanguage(settings.UiLanguage);
            ApplyUiText();
            ApplySavedTableSortState();
            ApplySavedTableColumnLayouts();
            storage = TimePilotStorage.CreateDefault();
            foregroundSessionTracker = new ForegroundSessionTracker(storage);
            idleSessionTracker = new IdleSessionTracker(storage);
            processRuntimeSessionTracker = new ProcessRuntimeSessionTracker(storage);

            var systemBootedAt = GetCurrentSystemBootedAt(startedAt);
            storage.Initialize(startedAt, systemBootedAt);
            ApplyProcessRuntimeSafeModeIfNeeded();
            UpdateDetailTrackingDisabledBanner();
            storage.BeginRuntimeSession(startedAt, systemBootedAt, Application.ProductVersion);
            RecordWindowsSystemEvent("timepilot-start", "ApplicationRestartedAfterRestore");
            lastProcessRuntimeSampleAt = null;
            lastSampleTickAt = null;
            selectedRuntimeAppId = null;
            RefreshViews(startedAt);
        }

        private void TryReinitializeStorageAfterRestoreFailure()
        {
            if (storage is not null)
                return;

            try
            {
                ReinitializeStorageAfterDataRestore(DateTimeOffset.UtcNow);
            }
            catch
            {
            }
        }

        private void OnExitMenuItemClick(object? sender, EventArgs e)
        {
            ExitApplication();
        }

        private void OnAboutMenuItemClick(object? sender, EventArgs e)
        {
            CenteredMessageDialog.Show(
                this,
                $"TimePilot {Application.ProductVersion}\n\n{UiText.Main.SponsorAboutMessage}",
                UiText.Main.AboutTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void OnSponsorMenuItemClick(object? sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(ExternalLinks.SponsorUrl)
                {
                    UseShellExecute = true
                });
                SetStatusText(UiText.Main.SponsorOpened);
            }
            catch (Exception ex)
            {
                CenteredMessageDialog.Show(
                    this,
                    UiText.Main.SponsorOpenFailed(ex.Message),
                    UiText.Main.Sponsor,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void OnRuntimeDiagnosticsMenuItemClick(object? sender, EventArgs e)
        {
            if (storage is null)
                return;

            var sessions = storage.GetRecentRuntimeSessionDiagnostics(10);
            var systemEvents = storage.GetRecentSystemEventDiagnostics(5);
            var message = BuildRuntimeDiagnosticsMessage(sessions, systemEvents);
            CenteredMessageDialog.Show(
                this,
                message,
                UiText.Main.RuntimeDiagnosticsTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static string BuildRuntimeDiagnosticsMessage(
            IReadOnlyList<AppRuntimeSessionDiagnostic> sessions,
            IReadOnlyList<SystemEventDiagnostic> systemEvents)
        {
            if (sessions.Count == 0)
            {
                if (systemEvents.Count == 0)
                    return UiText.Main.RuntimeDiagnosticsNoHistory;

                var eventOnlyLines = new List<string>
                {
                    UiText.Main.RuntimeDiagnosticsNoHistory,
                    string.Empty
                };
                AddSystemEventDiagnostics(eventOnlyLines, systemEvents);
                return string.Join(Environment.NewLine, eventOnlyLines);
            }

            var lastSession = sessions[0];
            var unexpectedCount = sessions.Count(x => IsShutdownReason(x, "unexpected"));
            var lines = new List<string>
            {
                UiText.Main.RuntimeDiagnosticsLastRun,
                UiText.Main.RuntimeDiagnosticsStartedAt(FormatDiagnosticDateTime(lastSession.StartedAt)),
                UiText.Main.RuntimeDiagnosticsEndedAt(FormatDiagnosticDateTime(lastSession.EndedAt)),
                UiText.Main.RuntimeDiagnosticsDuration(FormatDiagnosticDuration(lastSession)),
                UiText.Main.RuntimeDiagnosticsShutdownReason(GetShutdownReasonText(lastSession.ShutdownReason)),
                UiText.Main.RuntimeDiagnosticsRecentUnexpectedCount(unexpectedCount, sessions.Count),
                string.Empty,
                UiText.Main.RuntimeDiagnosticsHistory
            };

            foreach (var session in sessions.Take(5))
            {
                lines.Add(UiText.Main.RuntimeDiagnosticsHistoryItem(
                    FormatDiagnosticDateTime(session.StartedAt),
                    FormatDiagnosticDateTime(session.EndedAt),
                    GetShutdownReasonText(session.ShutdownReason),
                    FormatDiagnosticDuration(session)));
            }

            AddSystemEventDiagnostics(lines, systemEvents);

            lines.AddRange(new[]
            {
                string.Empty,
                UiText.Main.RuntimeDiagnosticsNote
            });

            return string.Join(Environment.NewLine, lines);
        }

        private static void AddSystemEventDiagnostics(
            List<string> lines,
            IReadOnlyList<SystemEventDiagnostic> systemEvents)
        {
            if (systemEvents.Count == 0)
                return;

            lines.Add(string.Empty);
            lines.Add(UiText.Main.RuntimeDiagnosticsSystemEvents);
            foreach (var systemEvent in systemEvents)
            {
                lines.Add(UiText.Main.RuntimeDiagnosticsSystemEventItem(
                    FormatDiagnosticDateTime(systemEvent.OccurredAt),
                    GetSystemEventTypeText(systemEvent.EventType),
                    systemEvent.Details ?? "-"));
            }
        }

        private static bool IsShutdownReason(AppRuntimeSessionDiagnostic session, string reason)
        {
            return string.Equals(session.ShutdownReason, reason, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetShutdownReasonText(string? reason)
        {
            return reason?.ToLowerInvariant() switch
            {
                "normal" => UiText.Main.ShutdownReasonNormal,
                "unexpected" => UiText.Main.ShutdownReasonUnexpected,
                "system-shutdown" => UiText.Main.ShutdownReasonSystemShutdown,
                "clear-data" => UiText.Main.ShutdownReasonClearData,
                "running" => UiText.Main.ShutdownReasonRunning,
                _ => UiText.Main.ShutdownReasonUnknown
            };
        }

        private static string GetSystemEventTypeText(string eventType)
        {
            return eventType.ToLowerInvariant() switch
            {
                "lock" => UiText.Main.SystemEventLock,
                "unlock" => UiText.Main.SystemEventUnlock,
                "logon" => UiText.Main.SystemEventLogon,
                "logoff" => UiText.Main.SystemEventLogoff,
                "suspend" => UiText.Main.SystemEventSuspend,
                "resume" => UiText.Main.SystemEventResume,
                "timepilot-start" => UiText.Main.SystemEventTimePilotStart,
                "timepilot-exit" => UiText.Main.SystemEventTimePilotExit,
                "system-shutdown" => UiText.Main.ShutdownReasonSystemShutdown,
                "windows-boot-estimate" => UiText.CurrentLanguage == UiLanguage.English ? "Windows startup estimate" : "Windows 시작 추정",
                "recording-end-estimate" => UiText.CurrentLanguage == UiLanguage.English ? "Recording end estimate" : "기록 종료 추정",
                _ => eventType
            };
        }

        private static string FormatDiagnosticDateTime(DateTimeOffset? timestamp)
        {
            return timestamp?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.CurrentCulture)
                ?? "-";
        }

        private static string FormatDiagnosticDuration(AppRuntimeSessionDiagnostic session)
        {
            var durationMs = session.DurationMs;
            if (durationMs is null && session.EndedAt is { } endedAt)
                durationMs = Math.Max(0, (long)(endedAt - session.StartedAt).TotalMilliseconds);

            return durationMs is null ? "-" : FormatDiagnosticDuration(durationMs.Value);
        }

        private static string FormatDiagnosticDuration(long durationMs)
        {
            var duration = TimeSpan.FromMilliseconds(Math.Max(0, durationMs));
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";

            return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        }

        private sealed record ViewRefreshSnapshot(
            IReadOnlyList<ForegroundUsageSummary>? ForegroundUsage,
            IReadOnlyList<DailyUsageTrendRow>? DailyUsageTrendRows,
            IdleUsageSummary? IdleUsage,
            RuntimeCoverageSummary? RuntimeCoverage,
            bool ShowDateInUsageTimestamps,
            bool? DetailDateHasData,
            bool? TimelineDateHasData,
            IReadOnlyList<ActivityTimelineRow>? TimelineRows,
            IReadOnlyList<TimelineRange>? WindowsRuntimeRanges,
            IReadOnlyList<SystemTimelineRange>? SystemTimelineRanges,
            IReadOnlyList<SystemTimelineEvent>? SystemTimelineEvents,
            IReadOnlyList<SystemTimelineEvent>? InferredSystemTimelineEvents,
            IReadOnlyList<CategoryTimelineSegment>? CategoryTimelineSegments,
            IReadOnlyList<ForegroundUsageSummary>? TimelineForegroundUsage,
            IReadOnlyList<ProcessRuntimeSummaryRow>? RuntimeRows,
            IReadOnlySet<long>? DetailSummaryAppIds,
            IReadOnlyList<ProcessRuntimeSegmentRow>? RuntimeSegmentRows,
            long ReadElapsedMs);

        private sealed record TimelineCategoryBucketOption(string Label, int Minutes)
        {
            public override string ToString() => Label;

            public static IReadOnlyList<TimelineCategoryBucketOption> GetOptions()
            {
                return
                [
                    new(UiText.Main.TimelineCategoryBucketAll, 0),
                    new(UiText.Main.TimelineCategoryBucketMinutes(15), 15),
                    new(GetDefaultBucketLabel(), 30),
                    new(UiText.Main.TimelineCategoryBucketHours(1), 60),
                    new(UiText.Main.TimelineCategoryBucketHours(2), 120)
                ];
            }

            private static string GetDefaultBucketLabel()
            {
                return UiText.CurrentLanguage == UiLanguage.English
                    ? $"{UiText.Main.TimelineCategoryBucketMinutes(30)} (Default)"
                    : $"{UiText.Main.TimelineCategoryBucketMinutes(30)} (기본)";
            }
        }

        private sealed record TimelineActivityTypeHighlightOption(string Label, TimelineActivityTypeHighlight Value)
        {
            public override string ToString() => Label;

            public static IReadOnlyList<TimelineActivityTypeHighlightOption> GetOptions()
            {
                return
                [
                    new(UiText.Main.ClearTimelineHighlight, TimelineActivityTypeHighlight.None),
                    new(UiText.Main.Active, TimelineActivityTypeHighlight.Active),
                    new(UiText.Main.Idle, TimelineActivityTypeHighlight.Idle),
                    new(UiText.Main.Untracked, TimelineActivityTypeHighlight.Untracked),
                    new(UiText.Main.WindowsRuntimeTrack, TimelineActivityTypeHighlight.Windows)
                ];
            }
        }

        private sealed record TimelineSystemEventFilterOption(string Label, TimelineSystemEventFilter Value)
        {
            public override string ToString() => Label;

            public static IReadOnlyList<TimelineSystemEventFilterOption> GetOptions()
            {
                var isEnglish = UiText.CurrentLanguage == UiLanguage.English;
                return
                [
                    new(isEnglish ? "All events" : "전체 이벤트", TimelineSystemEventFilter.All),
                    new(isEnglish ? "Lock/logon" : "잠금/로그온", TimelineSystemEventFilter.Lock),
                    new(isEnglish ? "Sleep/resume" : "절전/복귀", TimelineSystemEventFilter.Power),
                    new(isEnglish ? "Shutdown" : "종료/재시작", TimelineSystemEventFilter.Shutdown),
                    new(isEnglish ? "TimePilot" : "TimePilot", TimelineSystemEventFilter.TimePilot)
                ];
            }
        }

        private sealed record RuntimeSegmentObservationFilterOption(string Label, RuntimeSegmentObservationFilter Value)
        {
            public override string ToString() => Label;

            public static IReadOnlyList<RuntimeSegmentObservationFilterOption> GetOptions()
            {
                var isEnglish = UiText.CurrentLanguage == UiLanguage.English;
                return
                [
                    new(isEnglish ? "All basis" : "전체 기준", RuntimeSegmentObservationFilter.All),
                    new(UiText.Main.WindowedApp, RuntimeSegmentObservationFilter.VisibleApps),
                    new(UiText.Main.UserProcess, RuntimeSegmentObservationFilter.UserProcesses),
                    new(UiText.Main.AllProcesses, RuntimeSegmentObservationFilter.AllProcesses)
                ];
            }
        }

        private sealed record SystemTimelineEventRow(
            DateTimeOffset OccurredAt,
            string OccurredAtText,
            string EventTypeText,
            long PreviousIntervalMs,
            string PreviousIntervalText,
            string RelationText,
            string DetailsText);

        private readonly record struct TimelineSegmentSelectionKey(
            string ActivityType,
            DateTimeOffset StartedAt,
            DateTimeOffset? EndedAt,
            string DisplayName,
            string ProcessName,
            long? AppId)
        {
            public static TimelineSegmentSelectionKey From(ActivityTimelineRow row)
            {
                return new TimelineSegmentSelectionKey(
                    row.ActivityType,
                    row.StartedAt,
                    row.EndedAt,
                    row.DisplayName,
                    row.ProcessName,
                    row.AppId);
            }

            public bool Matches(ActivityTimelineRow row)
            {
                return string.Equals(ActivityType, row.ActivityType, StringComparison.Ordinal)
                    && StartedAt == row.StartedAt
                    && EndedAt == row.EndedAt
                    && string.Equals(DisplayName, row.DisplayName, StringComparison.Ordinal)
                    && string.Equals(ProcessName, row.ProcessName, StringComparison.OrdinalIgnoreCase)
                    && AppId == row.AppId;
            }
        }

        private readonly record struct RuntimeSegmentSelectionKey(
            DateTimeOffset StartedAt,
            DateTimeOffset? EndedAt,
            int ProcessId,
            bool HasMainWindow,
            bool IsCurrentSessionProcess)
        {
            public static RuntimeSegmentSelectionKey From(ProcessRuntimeSegmentRow segment)
            {
                return new RuntimeSegmentSelectionKey(
                    segment.StartedAt,
                    segment.EndedAt,
                    segment.ProcessId,
                    segment.HasMainWindow,
                    segment.IsCurrentSessionProcess);
            }

            public bool Matches(ProcessRuntimeSegmentRow segment)
            {
                return StartedAt == segment.StartedAt
                    && EndedAt == segment.EndedAt
                    && ProcessId == segment.ProcessId
                    && HasMainWindow == segment.HasMainWindow
                    && IsCurrentSessionProcess == segment.IsCurrentSessionProcess;
            }
        }

    }
}
