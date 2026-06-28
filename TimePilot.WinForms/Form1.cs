using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Win32;
using TimePilot.WinForms.Details;
using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Menus;
using TimePilot.WinForms.Navigation;
using TimePilot.WinForms.Tables;
using TimePilot.WinForms.Timeline;

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
        private static readonly TimeSpan HeavyViewRefreshInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan PastHeavyViewRefreshInterval = TimeSpan.FromMinutes(5);

        private readonly System.Windows.Forms.Timer sampleTimer = new();
        private readonly MainMenuController mainMenuController;
        private readonly TimelineSelectorCoordinator timelineSelectorCoordinator;
        private readonly TimelineZoomCoordinator timelineZoomCoordinator;
        private readonly RuntimeSegmentZoomCoordinator runtimeSegmentZoomCoordinator;
        private readonly RuntimeSegmentSelectionCoordinator runtimeSegmentSelectionCoordinator;
        private readonly DetailRuntimeFilterCoordinator detailRuntimeFilterCoordinator;
        private readonly RuntimeSegmentObservationFilterCoordinator runtimeSegmentObservationFilterCoordinator;
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
        private readonly FlowLayoutPanel summaryUsageBarsModePanel = new();
        private readonly Label summaryUsageBarsModeLabel = new();
        private readonly ComboBox summaryUsageBarsModeComboBox = new();
        private readonly SummaryUsageBarsControl summaryUsageBarsControl = new();
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
        private SummaryUsageBarMode selectedSummaryUsageBarMode = SummaryUsageBarMode.App;
        private bool showRunningRuntimeOnly;
        private bool isRefreshingRuntimeGrid;
        private bool isSelectingRuntimeGridRow;
        private bool isExplicitExitRequested;
        private volatile bool isViewRefreshRunning;
        private long? selectedRuntimeAppId;
        private volatile bool isClosing;
        private volatile bool isProcessRuntimeSampleRunning;
        private long timelineDataVersion;
        private long processRuntimeDataVersion;
        private bool isExportRunning;
        private bool isViewRefreshWaitCursorActive;
        private string statusText = string.Empty;
        private string? exportStatusText;
        private string? viewRefreshStatusText;
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
        private bool isApplyingTableColumnLayouts;
        private bool systemEventHandlersRegistered;
        private TimelineHighlightState timelineHighlightState = TimelineHighlightState.Empty;
        private string? lastForegroundViewKey;
        private bool? lastForegroundIdleState;
        private IReadOnlyList<ForegroundUsageSummary> currentTimelineForegroundUsage = Array.Empty<ForegroundUsageSummary>();
        private IReadOnlyList<ActivityTimelineRow> currentTimelineRows = Array.Empty<ActivityTimelineRow>();
        private IReadOnlyList<TimelineRange> currentTimelineWindowsRuntimeRanges = Array.Empty<TimelineRange>();
        private IReadOnlyList<SystemTimelineRange> currentTimelineSystemRanges = Array.Empty<SystemTimelineRange>();
        private IReadOnlyList<SystemTimelineEvent> currentTimelineSystemEvents = Array.Empty<SystemTimelineEvent>();
        private ViewRefreshSnapshot? cachedTimelineSnapshot;
        private HeavyViewRefreshKey? cachedTimelineSnapshotKey;
        private DateTimeOffset? cachedTimelineSnapshotAt;
        private ViewRefreshSnapshot? cachedSummarySnapshot;
        private SummaryViewRefreshKey? cachedSummarySnapshotKey;
        private DateTimeOffset? cachedSummarySnapshotAt;
        private ViewRefreshSnapshot? cachedDetailSnapshot;
        private HeavyViewRefreshKey? cachedDetailSnapshotKey;
        private DateTimeOffset? cachedDetailSnapshotAt;
        private Font? timelineHighlightedRowFont;
        private Form? recordedDatePickerPopupForm;
        private readonly Label timelineSystemEventFilterLabel = new();
        private readonly ComboBox timelineSystemEventFilterComboBox = new();

        public Form1(bool startMinimizedToTray = false)
        {
            this.startMinimizedToTray = startMinimizedToTray;

            UiText.UseLanguage(settings.UiLanguage);
            InitializeComponent();
            mainMenuController = CreateMainMenuController();
            timelineSelectorCoordinator = CreateTimelineSelectorCoordinator();
            timelineZoomCoordinator = CreateTimelineZoomCoordinator();
            runtimeSegmentZoomCoordinator = CreateRuntimeSegmentZoomCoordinator();
            runtimeSegmentSelectionCoordinator = CreateRuntimeSegmentSelectionCoordinator();
            detailRuntimeFilterCoordinator = CreateDetailRuntimeFilterCoordinator();
            runtimeSegmentObservationFilterCoordinator = CreateRuntimeSegmentObservationFilterCoordinator();
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
            InitializeTimelineSelectors();
            timelineZoomCoordinator.Initialize();
            runtimeSegmentZoomCoordinator.Initialize();
            runtimeSegmentSelectionCoordinator.Initialize();
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
            usageGrid.SelectionChanged += OnUsageGridSelectionChanged;
            InitializeSummaryUsageBars();
            timelineGrid.CellMouseDown += OnTimelineGridCellMouseDown;
            timelineGrid.RowPrePaint += OnTimelineGridRowPrePaint;
            timelineGrid.RowPostPaint += OnTimelineGridRowPostPaint;
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
            UpdateTimelineDataVersion(foregroundApp, isIdle);

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
            usageSortProperty = GridSortPropertyResolver.NormalizeUsageSortProperty(settings.UsageSortProperty);
            usageSortOrder = GridSortOrderHelper.FromSavedDescending(settings.UsageSortDescending, SortOrder.Descending);
            dailyUsageTrendSortProperty = GridSortPropertyResolver.NormalizeDailyUsageTrendSortProperty(settings.DailyUsageTrendSortProperty);
            dailyUsageTrendSortOrder = GridSortOrderHelper.FromSavedDescending(settings.DailyUsageTrendSortDescending, SortOrder.Descending);
            timelineSortProperty = GridSortPropertyResolver.NormalizeTimelineSortProperty(settings.TimelineSortProperty);
            timelineSortOrder = GridSortOrderHelper.FromSavedDescending(settings.TimelineSortDescending, SortOrder.Descending);
            runtimeSortProperty = GridSortPropertyResolver.NormalizeRuntimeSortProperty(settings.RuntimeSortProperty);
            runtimeSortOrder = GridSortOrderHelper.FromSavedDescending(settings.RuntimeSortDescending, SortOrder.Descending);
            runtimeSegmentSortProperty = GridSortPropertyResolver.NormalizeRuntimeSegmentSortProperty(settings.RuntimeSegmentSortProperty);
            runtimeSegmentSortOrder = GridSortOrderHelper.FromSavedDescending(settings.RuntimeSegmentSortDescending, SortOrder.Descending);
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
            detailRuntimeFilterCoordinator.RefreshOptions(selectedDetailRuntimeFilter);
        }

        private void SyncDetailRuntimeFilterComboBoxSelection()
        {
            detailRuntimeFilterCoordinator.SyncSelection(selectedDetailRuntimeFilter);
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
            selectedTimelineCategoryBucketMinutes = timelineSelectorCoordinator.RefreshCategoryBucketOptions(
                selectedTimelineCategoryBucketMinutes);
        }

        private void InitializeTimelineTypeHighlightSelector()
        {
            RefreshTimelineTypeHighlightOptions();
        }

        private void InitializeTimelineSelectors()
        {
            timelineSelectorCoordinator.Initialize(
                OnTimelineCategoryBucketComboBoxSelectedIndexChanged,
                OnTimelineTypeHighlightComboBoxSelectedIndexChanged,
                OnTimelineTypeHighlightComboBoxDropDownClosed,
                OnTimelineSystemEventFilterComboBoxSelectedIndexChanged);
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
            ConfigureRuntimeSegmentZoomButton(runtimeSegmentZoomOutButton);
            ConfigureRuntimeSegmentZoomButton(runtimeSegmentZoomInButton);
            ConfigureRuntimeSegmentZoomButton(runtimeSegmentPreviousButton);
            ConfigureRuntimeSegmentZoomButton(runtimeSegmentNextButton);
            ConfigureRuntimeSegmentZoomButton(runtimeSegmentResetButton, width: 52);

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
            RefreshRuntimeSegmentObservationFilterOptions();
            UpdateRuntimeSegmentZoomControls();
        }

        private void RefreshTimelineTypeHighlightOptions()
        {
            selectedTimelineActivityTypeHighlight = timelineSelectorCoordinator.RefreshTypeHighlightOptions(
                selectedTimelineActivityTypeHighlight);
            ApplyTimelineActivityTypeHighlight();
        }

        private void RefreshTimelineSystemEventFilterOptions()
        {
            selectedTimelineSystemEventFilter = timelineSelectorCoordinator.RefreshSystemEventFilterOptions(
                selectedTimelineSystemEventFilter);
        }

        private void RefreshRuntimeSegmentObservationFilterOptions()
        {
            runtimeSegmentObservationFilterCoordinator.RefreshOptions(selectedRuntimeSegmentObservationFilter);
            if (runtimeSegmentObservationFilterCoordinator.TryGetSelectedFilter(out var selectedFilter))
                selectedRuntimeSegmentObservationFilter = selectedFilter;
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
                RuntimeDiagnosticsMessageBuilder.FormatDuration(activeMs),
                RuntimeDiagnosticsMessageBuilder.FormatDuration(idleMs),
                inputActivityRatio,
                settings.IdleThresholdMinutes);
            summaryIdleAnalysisPanel.Visible = true;
            UpdateSummaryIdleAnalysisPanelHeight();
        }

        private void InitializeSummaryUsageBars()
        {
            summaryUsageBarsModePanel.Dock = DockStyle.Top;
            summaryUsageBarsModePanel.Height = 30;
            summaryUsageBarsModePanel.Padding = new Padding(0, 0, 0, 2);
            summaryUsageBarsModePanel.WrapContents = false;

            summaryUsageBarsModeLabel.AutoSize = true;
            summaryUsageBarsModeLabel.Margin = new Padding(0, 6, 8, 0);
            summaryUsageBarsModePanel.Controls.Add(summaryUsageBarsModeLabel);

            summaryUsageBarsModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            summaryUsageBarsModeComboBox.Width = 110;
            summaryUsageBarsModeComboBox.SelectedIndexChanged += OnSummaryUsageBarsModeComboBoxSelectedIndexChanged;
            summaryUsageBarsModePanel.Controls.Add(summaryUsageBarsModeComboBox);

            summaryUsageBarsControl.Dock = DockStyle.Fill;
            summaryUsageBarsControl.Font = usageGrid.Font;
            summaryUsageBarsPanel.Controls.Add(summaryUsageBarsControl);
            summaryUsageBarsPanel.Controls.Add(summaryUsageBarsModePanel);
            summaryUsageBarsModePanel.BringToFront();
            RefreshSummaryUsageBarsModeOptions();
        }

        private void SetSummaryUsageBars(IReadOnlyList<UsageSummaryRow> rows)
        {
            summaryUsageBarsControl.SetRows(rows, GetSelectedUsageSummaryRow(), selectedSummaryUsageBarMode);
            summaryUsageBarsPanel.Visible = rows.Count > 0;
        }

        private void RefreshSummaryUsageBarsModeOptions()
        {
            summaryUsageBarsModeLabel.Text = UiText.CurrentLanguage == UiLanguage.English ? "Bar" : "\uB9C9\uB300";
            summaryUsageBarsModeComboBox.BeginUpdate();
            try
            {
                summaryUsageBarsModeComboBox.Items.Clear();
                summaryUsageBarsModeComboBox.Items.Add(UiText.CurrentLanguage == UiLanguage.English ? "App" : "\uC571");
                summaryUsageBarsModeComboBox.Items.Add(UiText.CurrentLanguage == UiLanguage.English ? "Category" : "\uBD84\uB958");
                summaryUsageBarsModeComboBox.SelectedIndex = selectedSummaryUsageBarMode == SummaryUsageBarMode.Category ? 1 : 0;
            }
            finally
            {
                summaryUsageBarsModeComboBox.EndUpdate();
            }
        }

        private void OnSummaryUsageBarsModeComboBoxSelectedIndexChanged(object? sender, EventArgs e)
        {
            selectedSummaryUsageBarMode = summaryUsageBarsModeComboBox.SelectedIndex == 1
                ? SummaryUsageBarMode.Category
                : SummaryUsageBarMode.App;

            if (usageGrid.DataSource is IReadOnlyList<UsageSummaryRow> rows)
                SetSummaryUsageBars(rows);
        }

        private void RestoreUsageGridSelection(UsageSummaryRow? previousSelection)
        {
            if (previousSelection is null || usageGrid.Rows.Count == 0)
                return;

            for (var i = 0; i < usageGrid.Rows.Count; i++)
            {
                if (usageGrid.Rows[i].DataBoundItem is not UsageSummaryRow row || !IsSameUsageSummaryApp(row, previousSelection))
                    continue;

                var columnIndex = Math.Max(GridViewStatePreserver.GetFirstDisplayedColumnIndex(usageGrid), 0);
                columnIndex = Math.Min(columnIndex, usageGrid.Columns.Count - 1);
                usageGrid.ClearSelection();
                usageGrid.Rows[i].Selected = true;
                usageGrid.CurrentCell = usageGrid.Rows[i].Cells[columnIndex];
                return;
            }
        }

        private static bool IsSameUsageSummaryApp(UsageSummaryRow left, UsageSummaryRow right)
        {
            if (left.AppId is { } leftAppId && right.AppId is { } rightAppId)
                return leftAppId == rightAppId;

            return string.Equals(left.ProcessName, right.ProcessName, StringComparison.OrdinalIgnoreCase);
        }

        private void OnUsageGridSelectionChanged(object? sender, EventArgs e)
        {
            if (usageGrid.DataSource is not IReadOnlyList<UsageSummaryRow> rows)
                return;

            summaryUsageBarsControl.SetRows(rows, GetSelectedUsageSummaryRow(), selectedSummaryUsageBarMode);
        }

        private UsageSummaryRow? GetSelectedUsageSummaryRow()
        {
            return usageGrid.CurrentRow?.DataBoundItem as UsageSummaryRow;
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
            mainMenuController.ApplyText(CreateMainMenuText());

            summaryTab.Text = UiText.Main.SummaryTab;
            detailTab.Text = UiText.Main.DetailTab;
            timelineTab.Text = UiText.Main.TimelineTab;
            summaryPeriodLabel.Text = UiText.Main.Period;
            summarySpecificDateCalendarButton.Text = UiText.Main.Calendar;
            summaryCustomRangeButton.Text = UiText.SummaryPeriod.CustomRangeButton;
            UpdateSummaryCustomRangeLabel();
            RefreshSummaryUsageBarsModeOptions();
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
            runtimeSegmentObservationFilterLabel.Text = RuntimeSegmentHelpContentBuilder.GetObservationFilterLabelText();
            runtimeSegmentHelpButton.Text = UiText.Main.DetailHelp;
            runtimeSegmentZoomCoordinator.ApplyText();
            UpdateRuntimeSegmentZoomControls();
            timelineDateLabel.Text = UiText.Main.Date;
            timelineCalendarButton.Text = UiText.Main.Calendar;
            timelineTodayButton.Text = UiText.Main.Today;
            timelineHighlightClearButton.Text = UiText.Main.ClearTimelineHighlight;
            timelineHighlightHintLabel.Text = UiText.Main.TimelineHighlightHint;
            timelineZoomCoordinator.ApplyText();
            timelineHelpButton.Text = UiText.Main.TimelineHelp;
            timelineSelectorCoordinator.ApplyText(settings.UiLanguage);
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
            runtimeCoverageSummaryToolTip.SetToolTip(runtimeSegmentResetButton, RuntimeSegmentHelpContentBuilder.GetResetTooltip());
            runtimeCoverageSummaryToolTip.SetToolTip(runtimeSegmentHelpButton, RuntimeSegmentHelpContentBuilder.GetHelpTitle());
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

        private void UpdateTimelineDataVersion(AppMetadata? foregroundApp, bool isIdle)
        {
            var foregroundKey = foregroundApp is null
                ? null
                : $"{foregroundApp.ProcessName}|{foregroundApp.ExecutablePath}";
            if (string.Equals(lastForegroundViewKey, foregroundKey, StringComparison.OrdinalIgnoreCase)
                && lastForegroundIdleState == isIdle)
                return;

            lastForegroundViewKey = foregroundKey;
            lastForegroundIdleState = isIdle;
            Interlocked.Increment(ref timelineDataVersion);
        }

        private void InvalidateCategoryDependentViewCaches()
        {
            Interlocked.Increment(ref timelineDataVersion);
            Interlocked.Increment(ref processRuntimeDataVersion);
            cachedSummarySnapshot = null;
            cachedSummarySnapshotKey = null;
            cachedSummarySnapshotAt = null;
            cachedTimelineSnapshot = null;
            cachedTimelineSnapshotKey = null;
            cachedTimelineSnapshotAt = null;
            cachedDetailSnapshot = null;
            cachedDetailSnapshotKey = null;
            cachedDetailSnapshotAt = null;
        }

        private bool TryGetCachedHeavyViewSnapshot(
            TabPage? selectedTab,
            SummaryPeriodRange summaryPeriodRange,
            DateTime timelineDate,
            DateTime detailDate,
            long? selectedRuntimeAppId,
            int timelineCategoryBucketMinutes,
            DateTimeOffset observedAt,
            out ViewRefreshSnapshot snapshot)
        {
            snapshot = default!;
            if (selectedTab == summaryTab)
            {
                var key = SummaryViewRefreshKey.FromRange(
                    summaryPeriodRange,
                    Interlocked.Read(ref timelineDataVersion));
                if (cachedSummarySnapshot is null
                    || cachedSummarySnapshotKey != key
                    || IsSummaryViewCacheExpired(cachedSummarySnapshotAt, summaryPeriodRange, observedAt))
                    return false;

                snapshot = cachedSummarySnapshot with { ReadElapsedMs = 0 };
                return true;
            }

            if (selectedTab == timelineTab)
            {
                var key = HeavyViewRefreshKey.ForTimeline(
                    timelineDate,
                    timelineCategoryBucketMinutes,
                    Interlocked.Read(ref timelineDataVersion));
                if (cachedTimelineSnapshot is null
                    || cachedTimelineSnapshotKey != key
                    || IsHeavyViewCacheExpired(cachedTimelineSnapshotAt, timelineDate, observedAt))
                    return false;

                snapshot = RefreshCachedSnapshotForObservedAt(cachedTimelineSnapshot, observedAt) with { ReadElapsedMs = 0 };
                return true;
            }

            if (selectedTab == detailTab)
            {
                var key = HeavyViewRefreshKey.ForDetail(
                    detailDate,
                    selectedRuntimeAppId,
                    Interlocked.Read(ref processRuntimeDataVersion));
                if (cachedDetailSnapshot is null
                    || cachedDetailSnapshotKey != key
                    || IsHeavyViewCacheExpired(cachedDetailSnapshotAt, detailDate, observedAt))
                    return false;

                snapshot = RefreshCachedSnapshotForObservedAt(cachedDetailSnapshot, observedAt) with { ReadElapsedMs = 0 };
                return true;
            }

            return false;
        }

        private void CacheHeavyViewSnapshot(
            TabPage? selectedTab,
            SummaryPeriodRange summaryPeriodRange,
            DateTime timelineDate,
            DateTime detailDate,
            long? selectedRuntimeAppId,
            int timelineCategoryBucketMinutes,
            DateTimeOffset observedAt,
            ViewRefreshSnapshot snapshot)
        {
            if (selectedTab == summaryTab && snapshot.ForegroundUsage is not null)
            {
                cachedSummarySnapshot = snapshot;
                cachedSummarySnapshotKey = SummaryViewRefreshKey.FromRange(
                    summaryPeriodRange,
                    Interlocked.Read(ref timelineDataVersion));
                cachedSummarySnapshotAt = observedAt;
                return;
            }

            if (selectedTab == timelineTab && snapshot.TimelineRows is not null)
            {
                cachedTimelineSnapshot = snapshot;
                cachedTimelineSnapshotKey = HeavyViewRefreshKey.ForTimeline(
                    timelineDate,
                    timelineCategoryBucketMinutes,
                    Interlocked.Read(ref timelineDataVersion));
                cachedTimelineSnapshotAt = observedAt;
                return;
            }

            if (selectedTab == detailTab && snapshot.RuntimeRows is not null)
            {
                cachedDetailSnapshot = snapshot;
                cachedDetailSnapshotKey = HeavyViewRefreshKey.ForDetail(
                    detailDate,
                    selectedRuntimeAppId,
                    Interlocked.Read(ref processRuntimeDataVersion));
                cachedDetailSnapshotAt = observedAt;
            }
        }

        private static bool IsHeavyViewCacheExpired(
            DateTimeOffset? cachedAt,
            DateTime selectedDate,
            DateTimeOffset observedAt)
        {
            if (cachedAt is null)
                return true;

            var interval = selectedDate.Date == observedAt.ToLocalTime().Date
                ? HeavyViewRefreshInterval
                : PastHeavyViewRefreshInterval;
            return observedAt - cachedAt.Value >= interval;
        }

        private static bool IsSummaryViewCacheExpired(
            DateTimeOffset? cachedAt,
            SummaryPeriodRange range,
            DateTimeOffset observedAt)
        {
            if (cachedAt is null)
                return true;

            var today = observedAt.ToLocalTime().Date;
            var todayStart = new DateTimeOffset(today, TimeZoneInfo.Local.GetUtcOffset(today));
            var includesToday = range.Start < observedAt
                && range.End > todayStart;
            var interval = includesToday
                ? HeavyViewRefreshInterval
                : PastHeavyViewRefreshInterval;
            return observedAt - cachedAt.Value >= interval;
        }

        private static ViewRefreshSnapshot RefreshCachedSnapshotForObservedAt(
            ViewRefreshSnapshot snapshot,
            DateTimeOffset observedAt)
        {
            return snapshot with
            {
                TimelineRows = RefreshTimelineRowsForObservedAt(snapshot.TimelineRows, observedAt),
                WindowsRuntimeRanges = RefreshTimelineRangesForObservedAt(snapshot.WindowsRuntimeRanges, observedAt),
                SystemTimelineRanges = RefreshSystemTimelineRangesForObservedAt(snapshot.SystemTimelineRanges, observedAt),
                CategoryTimelineSegments = RefreshCategoryTimelineSegmentsForObservedAt(snapshot.CategoryTimelineSegments, observedAt),
                RuntimeRows = RefreshRuntimeRowsForObservedAt(snapshot.RuntimeRows, observedAt),
                RuntimeSegmentRows = RefreshRuntimeSegmentRowsForObservedAt(snapshot.RuntimeSegmentRows, observedAt)
            };
        }

        private static IReadOnlyList<ActivityTimelineRow>? RefreshTimelineRowsForObservedAt(
            IReadOnlyList<ActivityTimelineRow>? rows,
            DateTimeOffset observedAt)
        {
            return rows?.Select(row =>
            {
                if (row.EndedAt is not null)
                    return row;

                return row with { DurationMs = GetDurationMs(row.StartedAt, observedAt) };
            }).ToList();
        }

        private static IReadOnlyList<TimelineRange>? RefreshTimelineRangesForObservedAt(
            IReadOnlyList<TimelineRange>? ranges,
            DateTimeOffset observedAt)
        {
            return ranges?.Select(range =>
                range.EndedAt > observedAt
                    ? range with { EndedAt = observedAt }
                    : range).ToList();
        }

        private static IReadOnlyList<SystemTimelineRange>? RefreshSystemTimelineRangesForObservedAt(
            IReadOnlyList<SystemTimelineRange>? ranges,
            DateTimeOffset observedAt)
        {
            return ranges?.Select(range =>
                range.EndedAt > observedAt
                    ? range with { EndedAt = observedAt }
                    : range).ToList();
        }

        private static IReadOnlyList<CategoryTimelineSegment>? RefreshCategoryTimelineSegmentsForObservedAt(
            IReadOnlyList<CategoryTimelineSegment>? segments,
            DateTimeOffset observedAt)
        {
            return segments?.Select(segment =>
                segment.EndedAt > observedAt
                    ? segment with { EndedAt = observedAt }
                    : segment).ToList();
        }

        private static IReadOnlyList<ProcessRuntimeSummaryRow>? RefreshRuntimeRowsForObservedAt(
            IReadOnlyList<ProcessRuntimeSummaryRow>? rows,
            DateTimeOffset observedAt)
        {
            return rows?.Select(row =>
            {
                if (!row.HasRunningSession)
                    return row;

                var baseEnd = row.LastObservedAt ?? row.FirstObservedAt ?? observedAt;
                var deltaMs = Math.Max(0, (long)(observedAt - baseEnd).TotalMilliseconds);
                return row with
                {
                    RuntimeMs = row.RuntimeMs + deltaMs
                };
            }).ToList();
        }

        private static IReadOnlyList<ProcessRuntimeSegmentRow>? RefreshRuntimeSegmentRowsForObservedAt(
            IReadOnlyList<ProcessRuntimeSegmentRow>? rows,
            DateTimeOffset observedAt)
        {
            return rows?.Select(row =>
            {
                if (row.EndedAt is not null)
                    return row;

                return row with { DurationMs = GetDurationMs(row.StartedAt, observedAt) };
            }).ToList();
        }

        private static long GetDurationMs(DateTimeOffset startedAt, DateTimeOffset endedAt)
        {
            return Math.Max(0, (long)(endedAt - startedAt).TotalMilliseconds);
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
            var runtimeFirstDisplayedRowIndex = GridViewStatePreserver.GetFirstDisplayedRowIndex(runtimeGrid);
            var runtimeFirstDisplayedColumnIndex = GridViewStatePreserver.GetFirstDisplayedColumnIndex(runtimeGrid);
            var runtimeHorizontalOffset = GridViewStatePreserver.GetHorizontalScrollingOffset(runtimeGrid);
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
            ViewRefreshSnapshot snapshot;
            if (TryGetCachedHeavyViewSnapshot(
                selectedTab,
                summaryPeriodRange,
                timelineDate,
                detailDate,
                appIdToRestore,
                selectedTimelineCategoryBucketMinutes,
                observedAt,
                out var cachedSnapshot))
            {
                snapshot = cachedSnapshot;
            }
            else
            {
                isViewRefreshRunning = true;
                var showSummaryLoading = selectedTab == summaryTab;
                if (showSummaryLoading)
                    SetViewRefreshRunning(true, BuildViewRefreshInProgressStatus());

                try
                {
                    snapshot = await Task.Run(() =>
                    {
                        var readStopwatch = Stopwatch.StartNew();
                        var summaryUsage = selectedTab == summaryTab
                            ? storage.GetForegroundUsageWithDailyTrendForPeriod(summaryPeriodRange.Start, summaryPeriodRange.End)
                            : null;
                        var foregroundUsage = summaryUsage?.ForegroundUsage;
                        var dailyUsageTrendRows = summaryUsage?.DailyUsageTrendRows;
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
                    CacheHeavyViewSnapshot(
                        selectedTab,
                        summaryPeriodRange,
                        timelineDate,
                        detailDate,
                        appIdToRestore,
                        selectedTimelineCategoryBucketMinutes,
                        observedAt,
                        snapshot);
                }
                catch
                {
                    return;
                }
                finally
                {
                    isViewRefreshRunning = false;
                    if (showSummaryLoading)
                        SetViewRefreshRunning(false, null);
                }
            }

            if (isClosing)
                return;

            var applyStopwatch = Stopwatch.StartNew();
            if (snapshot.ForegroundUsage is not null)
            {
                SetRuntimeCoverageSummary(snapshot.RuntimeCoverage);
                SetSummaryIdleAnalysis(snapshot.ForegroundUsage, snapshot.IdleUsage);
                var previousUsageSelection = GetSelectedUsageSummaryRow();
                var usageRows = AddIcons(SortUsageSummaryRows(UsageSummaryRowBuilder.FromForegroundUsage(
                    snapshot.ForegroundUsage,
                    snapshot.ShowDateInUsageTimestamps)));
                GridViewStatePreserver.SetDataSourcePreservingView(
                    usageGrid,
                    usageRows);
                RestoreUsageGridSelection(previousUsageSelection);
                SetSummaryUsageBars(usageRows);
                GridViewStatePreserver.SetDataSourcePreservingView(
                    dailyUsageTrendGrid,
                    SortDailyUsageTrendRows(snapshot.DailyUsageTrendRows ?? Array.Empty<DailyUsageTrendRow>()));
                usageGrid.Invalidate();
            }

            if (snapshot.TimelineRows is not null)
            {
                currentTimelineRows = snapshot.TimelineRows;
                currentTimelineForegroundUsage = snapshot.TimelineForegroundUsage ?? Array.Empty<ForegroundUsageSummary>();
                SetDateStatus(timelineDateStatusLabel, snapshot.TimelineDateHasData);
                var filteredSystemRanges = TimelineSystemEventPresenter.FilterRanges(
                    snapshot.SystemTimelineRanges ?? Array.Empty<SystemTimelineRange>(),
                    selectedTimelineSystemEventFilter);
                var filteredSystemEvents = TimelineSystemEventPresenter.FilterEvents(
                    snapshot.SystemTimelineEvents ?? Array.Empty<SystemTimelineEvent>(),
                    selectedTimelineSystemEventFilter);
                currentTimelineWindowsRuntimeRanges = snapshot.WindowsRuntimeRanges ?? Array.Empty<TimelineRange>();
                currentTimelineSystemRanges = snapshot.SystemTimelineRanges ?? Array.Empty<SystemTimelineRange>();
                currentTimelineSystemEvents = TimelineSystemEventPresenter.FilterEvents(
                    (snapshot.SystemTimelineEvents ?? Array.Empty<SystemTimelineEvent>())
                        .Concat(snapshot.InferredSystemTimelineEvents ?? Array.Empty<SystemTimelineEvent>())
                        .ToList(),
                    selectedTimelineSystemEventFilter);
                timelineOverviewControl.SetTimeline(
                    selectedTimelineDate,
                    snapshot.TimelineRows,
                    currentTimelineWindowsRuntimeRanges,
                    filteredSystemRanges,
                    filteredSystemEvents,
                    snapshot.CategoryTimelineSegments ?? Array.Empty<CategoryTimelineSegment>());
                timelineOverviewControl.SetSystemEventHighlightEnabled(selectedTimelineSystemEventFilter != TimelineSystemEventFilter.All);
                ApplyTimelineHighlightToOverview();
                GridViewStatePreserver.SetDataSourcePreservingView(
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
                    GridViewStatePreserver.SetDataSourcePreservingView(
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
                    var keyToRestore = runtimeSegmentSelectionCoordinator.CurrentKey;
                    runtimeSegmentSelectionCoordinator.RestoreSelection(
                        keyToRestore,
                        selectFirstWhenMissing: keyToRestore is null);
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
                runtimeSegmentSelectionCoordinator.Clear();
                GridViewStatePreserver.SetDataSourcePreservingView(runtimeSegmentsGrid, Array.Empty<ProcessRuntimeSegmentRow>());
                UpdateRuntimeSegmentTimeline(null, Array.Empty<ProcessRuntimeSegmentRow>());
                return;
            }

            var segmentRows = SortRuntimeSegmentRows(FilterRuntimeSegmentRows(
                storage.GetProcessRuntimeSegmentsForDate(selectedRow.AppId, selectedDetailDate, observedAt)));
            var keyToRestore = selectionKeyToRestore ?? runtimeSegmentSelectionCoordinator.CurrentKey;
            SetRuntimeSegmentsDataSource(segmentRows);
            runtimeSegmentSelectionCoordinator.RestoreSelection(
                keyToRestore,
                selectFirstWhenMissing: selectFirstWhenMissing && keyToRestore is null);
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
            runtimeSegmentSelectionCoordinator.RunWithoutSelectionEvents(() =>
            {
                GridViewStatePreserver.SetDataSourcePreservingView(
                    runtimeSegmentsGrid,
                    segmentRows,
                    preserveSelection: false);
                runtimeSegmentsGrid.ClearSelection();
                runtimeSegmentsGrid.CurrentCell = null;
            });
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
                    var changed = false;
                    lock (processRuntimeTrackingLock)
                    {
                        if (!isClosing)
                            changed = processRuntimeSessionTracker.Track(processes, scope, observedAt);
                    }

                    if (changed)
                        Interlocked.Increment(ref processRuntimeDataVersion);

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

        private void SetViewRefreshRunning(bool isRunning, string? message)
        {
            isViewRefreshWaitCursorActive = isRunning;
            viewRefreshStatusText = message;
            UpdateWaitCursor();
            RefreshStatusLabel();
        }

        private static string BuildViewRefreshInProgressStatus()
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? "Loading summary..."
                : "요약 불러오는 중...";
        }

        private void UpdateWaitCursor()
        {
            UseWaitCursor = isExportRunning || isViewRefreshWaitCursorActive;
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
            if (!string.IsNullOrWhiteSpace(viewRefreshStatusText))
                parts.Add(viewRefreshStatusText);
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
            return GridRowOrderer.OrderRows(rows, keySelector, usageSortOrder);
        }

        private IOrderedEnumerable<DailyUsageTrendRow> OrderDailyUsageTrendRows<TKey>(
            IReadOnlyList<DailyUsageTrendRow> rows,
            Func<DailyUsageTrendRow, TKey> keySelector)
        {
            return GridRowOrderer.OrderRows(rows, keySelector, dailyUsageTrendSortOrder);
        }

        private IOrderedEnumerable<ActivityTimelineRow> OrderTimelineRows<TKey>(
            IReadOnlyList<ActivityTimelineRow> rows,
            Func<ActivityTimelineRow, TKey> keySelector)
        {
            return GridRowOrderer.OrderRows(rows, keySelector, timelineSortOrder);
        }

        private IOrderedEnumerable<ProcessRuntimeSummaryRow> OrderRuntimeRows<TKey>(
            IReadOnlyList<ProcessRuntimeSummaryRow> rows,
            Func<ProcessRuntimeSummaryRow, TKey> keySelector)
        {
            return GridRowOrderer.OrderRows(rows, keySelector, runtimeSortOrder);
        }

        private IOrderedEnumerable<ProcessRuntimeSegmentRow> OrderRuntimeSegmentRows<TKey>(
            IReadOnlyList<ProcessRuntimeSegmentRow> rows,
            Func<ProcessRuntimeSegmentRow, TKey> keySelector)
        {
            return GridRowOrderer.OrderRows(rows, keySelector, runtimeSegmentSortOrder);
        }

        private void OnUsageGridColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0)
                return;

            var propertyName = GridSortPropertyResolver.GetUsageSortPropertyName(usageGrid.Columns[e.ColumnIndex].Name);
            if (propertyName is null)
                return;

            usageSortOrder = string.Equals(usageSortProperty, propertyName, StringComparison.Ordinal)
                ? GridSortOrderHelper.Toggle(usageSortOrder)
                : SortOrder.Descending;
            usageSortProperty = propertyName;
            SaveTableSortState();
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnDailyUsageTrendGridColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0)
                return;

            var propertyName = GridSortPropertyResolver.GetDailyUsageTrendSortPropertyName(dailyUsageTrendGrid.Columns[e.ColumnIndex].Name);
            if (propertyName is null)
                return;

            dailyUsageTrendSortOrder = string.Equals(dailyUsageTrendSortProperty, propertyName, StringComparison.Ordinal)
                ? GridSortOrderHelper.Toggle(dailyUsageTrendSortOrder)
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

            timelineHighlightState = TimelineHighlightState.ForApp(row.ProcessName, row.DisplayName);
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
            return UiText.CurrentLanguage == UiLanguage.English ? "Highlight this segment" : "이 구간 강조";
        }

        private static string GetHighlightTimelineAppMenuText()
        {
            return UiText.CurrentLanguage == UiLanguage.English ? "Highlight this app" : "이 앱 강조";
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

            var propertyName = GridSortPropertyResolver.GetTimelineSortPropertyName(timelineGrid.Columns[e.ColumnIndex].Name);
            if (propertyName is null)
                return;

            timelineSortOrder = string.Equals(timelineSortProperty, propertyName, StringComparison.Ordinal)
                ? GridSortOrderHelper.Toggle(timelineSortOrder)
                : SortOrder.Descending;
            timelineSortProperty = propertyName;
            SaveTableSortState();
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnMainTabsSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (mainTabs.SelectedTab == detailTab)
            {
                detailRuntimeFilterCoordinator.RunWithoutSelectionEvents(
                    SyncDetailRuntimeFilterComboBoxSelection);
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

        private void OnTimelineHelpButtonClick(object? sender, EventArgs e)
        {
            CenteredMessageDialog.Show(
                this,
                UiText.Main.TimelineHelpMessage,
                UiText.Main.TimelineHelpTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void OnRuntimeSegmentHelpButtonClick(object? sender, EventArgs e)
        {
            CenteredMessageDialog.Show(
                this,
                RuntimeSegmentHelpContentBuilder.GetHelpMessage(),
                RuntimeSegmentHelpContentBuilder.GetHelpTitle(),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void OnRuntimeSegmentObservationFilterComboBoxSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!runtimeSegmentObservationFilterCoordinator.TryGetSelectedFilter(out var selectedFilter)
                || selectedFilter == selectedRuntimeSegmentObservationFilter)
                return;

            selectedRuntimeSegmentObservationFilter = selectedFilter;
            RefreshRuntimeSegments(DateTimeOffset.UtcNow);
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
            var appStatsItem = new ToolStripMenuItem(TimelineCategorySegmentStatsPresenter.GetMenuText());
            appStatsItem.Click += (_, _) => ShowTimelineCategorySegmentAppStatsPopup(segment, owner, location);
            timelineGridMenu.Items.Add(appStatsItem);
            timelineGridMenu.Show(owner, location);
        }

        private void ShowTimelineCategorySegmentAppStatsPopup(CategoryTimelineSegment segment, Control owner, Point location)
        {
            var rows = UsageSummaryRowBuilder.FromForegroundUsage(
                storage?.GetForegroundUsageForPeriod(segment.StartedAt, segment.EndedAt)
                    ?? Array.Empty<ForegroundUsageSummary>());

            var description = TimelineCategorySegmentStatsPresenter.BuildDescription(
                segment,
                currentTimelineRows,
                currentTimelineWindowsRuntimeRanges,
                currentTimelineSystemRanges);
            var popup = TimelinePopupFactory.CreateCategorySegmentStatsPopup(Icon, description, rows);
            popup.Location = TimelinePopupFactory.GetPopupLocation(owner, location, popup.Size);
            popup.Show(this);
        }

        private void ShowTimelineWindowsContextMenu(Control owner, Point location)
        {
            timelineGridMenu.Items.Clear();
            var eventListItem = new ToolStripMenuItem(SystemTimelineEventTextFormatter.GetListTitle(selectedTimelineDate));
            eventListItem.Click += (_, _) => ShowTimelineSystemEventsPopup(owner, location);
            timelineGridMenu.Items.Add(eventListItem);
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
            if (timelineHighlightState.HasSegmentHighlight
                || string.Equals(row.ProcessName, timelineHighlightState.ProcessName, StringComparison.OrdinalIgnoreCase))
            {
                var clearHighlightItem = new ToolStripMenuItem(UiText.Main.ClearTimelineHighlight);
                clearHighlightItem.Click += (_, _) => ClearTimelineHighlights(resetTypeHighlight: true);
                timelineGridMenu.Items.Add(clearHighlightItem);
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

        private void ShowRecordedDateCalendar(Control anchor, DateTime selectedDate, Action<DateTime> applyDate)
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
            detailNextDateButton.Enabled = DateSelectorCoordinator.CanMoveForward(selectedDetailDate, today);
            detailTodayButton.Enabled = detailNextDateButton.Enabled;
            timelineNextDateButton.Enabled = DateSelectorCoordinator.CanMoveForward(selectedTimelineDate, today);
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
            var dateText = today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.CurrentCulture);
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"Moved today's views to {dateText}"
                : $"오늘 보기 날짜를 {dateText}(으)로 갱신했습니다.";
        }

        private void UpdateTimelineZoomControls()
        {
            timelineZoomCoordinator.Update();
        }

        private void UpdateRuntimeSegmentZoomControls()
        {
            runtimeSegmentZoomCoordinator.Update();
        }

        private void UpdateTimelineHighlightUi()
        {
            var hasHighlight = timelineHighlightState.HasHighlight;
            timelineHighlightLabel.Visible = hasHighlight;
            timelineHighlightClearButton.Visible = hasHighlight;
            timelineHighlightHintLabel.Visible = !hasHighlight;
            timelineHighlightLabel.Text = hasHighlight ? timelineHighlightState.GetDisplayText() : "";
            UpdateTimelineHighlightSummary();
        }

        private string? GetTimelineHighlightedActivityTypeText()
        {
            return TimelineHighlightMatcher.GetActivityTypeText(selectedTimelineActivityTypeHighlight);
        }

        private void ApplyTimelineActivityTypeHighlight()
        {
            timelineOverviewControl.SetWindowsHighlighted(selectedTimelineActivityTypeHighlight == TimelineActivityTypeHighlight.Windows);
            if (selectedTimelineActivityTypeHighlight != TimelineActivityTypeHighlight.Windows)
                timelineOverviewControl.SetHighlightedActivityType(GetTimelineHighlightedActivityTypeText());
        }

        private void ApplyTimelineHighlightToOverview()
        {
            if (timelineHighlightState.SegmentKey is { } segmentKey)
            {
                var highlightedRow = currentTimelineRows.FirstOrDefault(row => segmentKey.Matches(row));
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
                timelineOverviewControl.SetHighlightedProcessName(timelineHighlightState.ProcessName);
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
            if (timelineHighlightState.HasSegmentHighlight && !isHighlighted)
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
            summaryCustomRangeLabel.Text = SummaryCustomRangeLabelFormatter.Format(
                selectedSummaryCustomStartDate,
                selectedSummaryCustomEndDate);
        }

        private void OnRuntimeGridColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0)
                return;

            var propertyName = GridSortPropertyResolver.GetRuntimeSortPropertyName(runtimeGrid.Columns[e.ColumnIndex].Name);
            if (propertyName is null)
                return;

            runtimeSortOrder = string.Equals(runtimeSortProperty, propertyName, StringComparison.Ordinal)
                ? GridSortOrderHelper.Toggle(runtimeSortOrder)
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
            runtimeSegmentSelectionCoordinator.Clear();
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

            InvalidateCategoryDependentViewCaches();
            SetStatusText(UiText.Main.CategoryUpdated(appName, categoryName));
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnRuntimeSegmentsGridColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0)
                return;

            var selectionKey = runtimeSegmentSelectionCoordinator.GetCurrentOrStoredKey();
            var propertyName = GridSortPropertyResolver.GetRuntimeSegmentSortPropertyName(runtimeSegmentsGrid.Columns[e.ColumnIndex].Name);
            if (propertyName is null)
                return;

            runtimeSegmentSortOrder = string.Equals(runtimeSegmentSortProperty, propertyName, StringComparison.Ordinal)
                ? GridSortOrderHelper.Toggle(runtimeSegmentSortOrder)
                : SortOrder.Descending;
            runtimeSegmentSortProperty = propertyName;
            SaveTableSortState();
            RefreshRuntimeSegments(
                DateTimeOffset.UtcNow,
                selectionKeyToRestore: selectionKey,
                selectFirstWhenMissing: false);
            UpdateSortGlyphs();
        }

        private void OnRunningRuntimeOnlyCheckBoxCheckedChanged(object? sender, EventArgs e)
        {
            showRunningRuntimeOnly = runningRuntimeOnlyCheckBox.Checked;
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnDetailRuntimeFilterComboBoxSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (detailRuntimeFilterCoordinator.IsUpdating
                || !detailRuntimeFilterCoordinator.TryGetSelectedFilter(out var selectedFilter))
                return;

            if (mainTabs.SelectedTab != detailTab)
            {
                detailRuntimeFilterCoordinator.RunWithoutSelectionEvents(
                    SyncDetailRuntimeFilterComboBoxSelection);
                return;
            }

            selectedDetailRuntimeFilter = selectedFilter;
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnDetailHelpButtonClick(object? sender, EventArgs e)
        {
            CenteredMessageDialog.Show(
                this,
                DetailHelpContentBuilder.BuildMessage(selectedDetailRuntimeFilter),
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
            int width = 32)
        {
            button.Size = new Size(width, 24);
            button.Margin = new Padding(3, 2, 3, 0);
            button.UseVisualStyleBackColor = true;
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

        private void UpdateSortGlyphs()
        {
            GridSortGlyphUpdater.UpdateGlyphs(usageGrid, GridSortPropertyResolver.GetUsageSortPropertyName, usageSortProperty, usageSortOrder);
            GridSortGlyphUpdater.UpdateGlyphs(dailyUsageTrendGrid, GridSortPropertyResolver.GetDailyUsageTrendSortPropertyName, dailyUsageTrendSortProperty, dailyUsageTrendSortOrder);
            GridSortGlyphUpdater.UpdateGlyphs(timelineGrid, GridSortPropertyResolver.GetTimelineSortPropertyName, timelineSortProperty, timelineSortOrder);
            GridSortGlyphUpdater.UpdateGlyphs(runtimeGrid, GridSortPropertyResolver.GetRuntimeSortPropertyName, runtimeSortProperty, runtimeSortOrder);
            GridSortGlyphUpdater.UpdateGlyphs(runtimeSegmentsGrid, GridSortPropertyResolver.GetRuntimeSegmentSortPropertyName, runtimeSegmentSortProperty, runtimeSegmentSortOrder);
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
            return GridHeaderTooltipResolver.GetTooltipText(grid.Name, column.Name);
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
                : GridViewStatePreserver.GetFirstDisplayedColumnIndex(grid);
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
                    : GridViewStatePreserver.GetFirstDisplayedColumnIndex(runtimeGrid);
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
                GridViewStatePreserver.TrySetFirstDisplayedRowIndex(runtimeGrid, firstDisplayedRowIndex);
                GridViewStatePreserver.TrySetFirstDisplayedColumnIndex(runtimeGrid, firstDisplayedColumnIndex);
                GridViewStatePreserver.TrySetHorizontalScrollingOffset(runtimeGrid, horizontalScrollingOffset);
                return;
            }
        }

        private void RestoreRuntimeGridView(
            int firstDisplayedRowIndex,
            int firstDisplayedColumnIndex,
            int horizontalScrollingOffset)
        {
            GridViewStatePreserver.TrySetFirstDisplayedRowIndex(runtimeGrid, firstDisplayedRowIndex);
            GridViewStatePreserver.TrySetFirstDisplayedColumnIndex(runtimeGrid, firstDisplayedColumnIndex);
            GridViewStatePreserver.TrySetHorizontalScrollingOffset(runtimeGrid, horizontalScrollingOffset);
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

            using var form = new AppCategoryManagementForm(storage, settings, settings.UiLanguage);
            form.Icon = Icon;
            if (form.ShowDialog(this) == DialogResult.OK && form.CategoriesChanged)
            {
                InvalidateCategoryDependentViewCaches();
                RefreshViews(DateTimeOffset.UtcNow);
            }
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
                viewRefreshStatusText = null;
                isViewRefreshWaitCursorActive = false;
                UpdateWaitCursor();
                cachedSummarySnapshot = null;
                cachedSummarySnapshotKey = null;
                cachedSummarySnapshotAt = null;
                cachedTimelineSnapshot = null;
                cachedTimelineSnapshotKey = null;
                cachedTimelineSnapshotAt = null;
                cachedDetailSnapshot = null;
                cachedDetailSnapshotKey = null;
                cachedDetailSnapshotAt = null;

                GridViewStatePreserver.SetDataSourcePreservingView(usageGrid, Array.Empty<UsageSummaryRow>());
                GridViewStatePreserver.SetDataSourcePreservingView(dailyUsageTrendGrid, Array.Empty<DailyUsageTrendRow>());
                SetRuntimeCoverageSummary(null);
                timelineOverviewControl.SetTimeline(
                    selectedTimelineDate,
                    Array.Empty<ActivityTimelineRow>(),
                    Array.Empty<TimelineRange>(),
                    Array.Empty<SystemTimelineRange>(),
                    Array.Empty<SystemTimelineEvent>(),
                    Array.Empty<CategoryTimelineSegment>());
                GridViewStatePreserver.SetDataSourcePreservingView(timelineGrid, Array.Empty<ActivityTimelineRow>());
                currentTimelineForegroundUsage = Array.Empty<ForegroundUsageSummary>();
                currentTimelineRows = Array.Empty<ActivityTimelineRow>();
                currentTimelineWindowsRuntimeRanges = Array.Empty<TimelineRange>();
                currentTimelineSystemRanges = Array.Empty<SystemTimelineRange>();
                currentTimelineSystemEvents = Array.Empty<SystemTimelineEvent>();
                GridViewStatePreserver.SetDataSourcePreservingView(runtimeGrid, Array.Empty<ProcessRuntimeSummaryRow>());
                GridViewStatePreserver.SetDataSourcePreservingView(runtimeSegmentsGrid, Array.Empty<ProcessRuntimeSegmentRow>());
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

            var rangeText = DataOperationStatusFormatter.FormatCsvExportRangeForFileName(rangeDialog.StartDate, rangeDialog.EndDate);
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
                SetExportRunning(true, DataOperationStatusFormatter.BuildInProgressStatus(UiText.Main.CsvExportTitle));
                var fileName = dialog.FileName;
                var exportedFiles = await Task.Run(() =>
                {
                    var exporter = new UsageCsvExporter(storageSnapshot);
                    return exporter.ExportRange(fileName, startDate, endDate, now);
                });

                SetExportRunning(false, DataOperationStatusFormatter.BuildCompletedStatus(UiText.Main.CsvExportTitle));
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
                SetExportRunning(false, DataOperationStatusFormatter.BuildFailedStatus(UiText.Main.CsvExportTitle));
                CenteredMessageDialog.Show(
                    this,
                    UiText.Main.CsvExportFailed(ex.Message),
                    UiText.Main.CsvExportTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                ClearExportStatus();
            }
        }

        private void SetExportRunning(bool isRunning, string? message)
        {
            isExportRunning = isRunning;
            exportStatusText = message;
            mainMenuController.SetDataOperationsEnabled(!isRunning);
            UpdateWaitCursor();
            RefreshStatusLabel();
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
                SetExportRunning(true, DataOperationStatusFormatter.BuildInProgressStatus(UiText.Main.RawDataExportTitle));
                var fileName = dialog.FileName;
                var exportedFiles = await Task.Run(() =>
                {
                    var exporter = new RawDataZipExporter(storageSnapshot);
                    return exporter.Export(fileName);
                });

                SetExportRunning(false, DataOperationStatusFormatter.BuildCompletedStatus(UiText.Main.RawDataExportTitle));
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
                SetExportRunning(false, DataOperationStatusFormatter.BuildFailedStatus(UiText.Main.RawDataExportTitle));
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
                SetExportRunning(true, DataOperationStatusFormatter.BuildInProgressStatus(UiText.Main.DataBackupTitle));
                sampleTimer.Stop();
                storage.UpdateRuntimeHeartbeat(now);

                var fileName = dialog.FileName;
                var entries = await Task.Run(() =>
                {
                    var service = new DataBackupService();
                    return service.CreateBackup(fileName, now);
                });

                SetExportRunning(false, DataOperationStatusFormatter.BuildCompletedStatus(UiText.Main.DataBackupTitle));
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
                SetExportRunning(false, DataOperationStatusFormatter.BuildFailedStatus(UiText.Main.DataBackupTitle));
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
                SetExportRunning(true, UiText.Main.DataRestoreAnalyzingBackup);
                await AllowUiToRenderAsync();
                var selectedBackupPath = dialog.FileName;
                plan = await Task.Run(() => service.InspectBackup(selectedBackupPath));
                SetExportRunning(false, null);
            }
            catch (Exception ex)
            {
                SetExportRunning(false, DataOperationStatusFormatter.BuildFailedStatus(UiText.Main.DataRestoreTitle));
                CenteredMessageDialog.Show(
                    this,
                    UiText.Main.DataRestoreFailed(ex.Message),
                    UiText.Main.DataRestoreTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                ClearExportStatus();
                return;
            }

            using var restoreModeChoiceForm = new RestoreModeChoiceForm(
                settings.UiLanguage,
                plan,
                () => service.InspectBackupDetailedComparison(dialog.FileName));
            restoreModeChoiceForm.Icon = Icon;
            if (restoreModeChoiceForm.ShowDialog(this) != DialogResult.OK
                || restoreModeChoiceForm.Choice != RestoreModeChoice.FullReplace)
                return;

            using var safetyBackupChoiceForm = new RestoreSafetyBackupChoiceForm(settings.UiLanguage);
            safetyBackupChoiceForm.Icon = Icon;
            if (safetyBackupChoiceForm.ShowDialog(this) != DialogResult.OK
                || safetyBackupChoiceForm.Choice == RestoreSafetyBackupChoice.Cancel)
                return;

            var now = DateTimeOffset.UtcNow;
            string? safetyBackupPath = null;
            using var progressForm = new RestoreProgressForm(settings.UiLanguage, totalSteps: 5);
            progressForm.Icon = Icon;
            progressForm.ShowCentered(this);
            try
            {
                SetExportRunning(true, UiText.Main.DataRestorePreparing);
                progressForm.SetStep(1, UiText.Main.DataRestorePreparing);
                await AllowUiToRenderAsync();
                sampleTimer.Stop();
                EndCurrentTrackingSessions(now, "restore-data");

                if (safetyBackupChoiceForm.Choice == RestoreSafetyBackupChoice.CreateSafetyBackup)
                {
                    SetExportRunning(true, UiText.Main.DataRestoreCreatingSafetyBackup);
                    progressForm.SetStep(2, UiText.Main.DataRestoreCreatingSafetyBackup);
                    await AllowUiToRenderAsync();
                    safetyBackupPath = CreatePreRestoreSafetyBackup(service, now);
                }
                else
                {
                    progressForm.SetStep(
                        2,
                        settings.UiLanguage == UiLanguage.English
                            ? "Safety backup was not created by your choice."
                            : "사용자 선택에 따라 안전 백업은 생성되지 않았습니다.");
                    await AllowUiToRenderAsync();
                }

                storage?.Dispose();
                storage = null;

                SetExportRunning(true, UiText.Main.DataRestoreApplyingBackup);
                progressForm.SetStep(3, UiText.Main.DataRestoreApplyingBackup);
                await AllowUiToRenderAsync();
                var fileName = dialog.FileName;
                var result = await Task.Run(() => service.RestoreBackup(fileName));

                SetExportRunning(true, UiText.Main.DataRestoreRestartingSession);
                progressForm.SetStep(4, UiText.Main.DataRestoreRestartingSession);
                await AllowUiToRenderAsync();
                ReinitializeStorageAfterDataRestore(DateTimeOffset.UtcNow);

                SetExportRunning(false, DataOperationStatusFormatter.BuildCompletedStatus(UiText.Main.DataRestoreTitle));
                progressForm.ShowCompleted(
                    safetyBackupPath is null
                        ? UiText.Main.DataRestoreCompletedWithoutSafetyBackup(result.RestoredFiles.Count)
                        : UiText.Main.DataRestoreCompleted(result.RestoredFiles.Count, safetyBackupPath));
                ClearExportStatus();
                await progressForm.WaitForCloseAsync();
            }
            catch (Exception ex)
            {
                TryReinitializeStorageAfterRestoreFailure();
                SetExportRunning(false, DataOperationStatusFormatter.BuildFailedStatus(UiText.Main.DataRestoreTitle));
                progressForm.ShowFailed(UiText.Main.DataRestoreFailed(ex.Message));
                ClearExportStatus();
                await progressForm.WaitForCloseAsync();
            }
            finally
            {
                if (!isClosing && storage is not null)
                    sampleTimer.Start();
            }
        }

        private static Task AllowUiToRenderAsync()
        {
            Application.DoEvents();
            return Task.Delay(50);
        }

        private static string CreatePreRestoreSafetyBackup(DataBackupService service, DateTimeOffset now)
        {
            Directory.CreateDirectory(AppDataPaths.BackupDirectory);
            var fileName = $"TimePilot-before-restore-{now.ToLocalTime():yyyy-MM-dd-HHmmss}.zip";
            var backupPath = Path.Combine(AppDataPaths.BackupDirectory, fileName);
            service.CreateBackup(backupPath, now);
            return backupPath;
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
            var message = RuntimeDiagnosticsMessageBuilder.BuildMessage(sessions, systemEvents);
            CenteredMessageDialog.Show(
                this,
                message,
                UiText.Main.RuntimeDiagnosticsTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
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

        private sealed record HeavyViewRefreshKey(
            string ViewName,
            DateTime Date,
            long? SelectedAppId,
            int TimelineCategoryBucketMinutes,
            long DataVersion)
        {
            public static HeavyViewRefreshKey ForTimeline(
                DateTime date,
                int categoryBucketMinutes,
                long dataVersion)
            {
                return new HeavyViewRefreshKey("timeline", date.Date, null, categoryBucketMinutes, dataVersion);
            }

            public static HeavyViewRefreshKey ForDetail(
                DateTime date,
                long? selectedAppId,
                long dataVersion)
            {
                return new HeavyViewRefreshKey("detail", date.Date, selectedAppId, 0, dataVersion);
            }
        }

        private sealed record SummaryViewRefreshKey(
            DateTime StartDate,
            DateTime EndDate,
            long DataVersion)
        {
            public static SummaryViewRefreshKey FromRange(
                SummaryPeriodRange range,
                long dataVersion)
            {
                return new SummaryViewRefreshKey(
                    range.Start.ToLocalTime().Date,
                    range.End.ToLocalTime().Date,
                    dataVersion);
            }
        }

    }
}
