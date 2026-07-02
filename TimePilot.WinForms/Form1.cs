using System.ComponentModel;
using System.Diagnostics;
using TimePilot.WinForms.Details;
using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Menus;
using TimePilot.WinForms.Navigation;
using TimePilot.WinForms.Refresh;
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

        private readonly System.Windows.Forms.Timer sampleTimer = new();
        private readonly MainMenuController mainMenuController;
        private readonly TimelineSelectorCoordinator timelineSelectorCoordinator;
        private readonly TimelineZoomCoordinator timelineZoomCoordinator;
        private readonly RuntimeSegmentZoomCoordinator runtimeSegmentZoomCoordinator;
        private readonly RuntimeSegmentSelectionCoordinator runtimeSegmentSelectionCoordinator;
        private readonly DetailRuntimeFilterCoordinator detailRuntimeFilterCoordinator;
        private readonly RuntimeSegmentObservationFilterCoordinator runtimeSegmentObservationFilterCoordinator;
        private readonly ViewRefreshCache viewRefreshCache = new();
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

        private void OnMainTabsSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (mainTabs.SelectedTab == detailTab)
            {
                detailRuntimeFilterCoordinator.RunWithoutSelectionEvents(
                    SyncDetailRuntimeFilterComboBoxSelection);
            }

            RefreshViews(DateTimeOffset.UtcNow);
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

        private static bool IsRunningInDesigner()
        {
            return System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime;
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

    }
}
