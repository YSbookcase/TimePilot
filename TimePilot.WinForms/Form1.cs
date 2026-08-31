using System.ComponentModel;
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
        private readonly AppExecutableMetadataCache appExecutableMetadataCache = new();
        private readonly object processRuntimeTrackingLock = new();
        private readonly Form headerToolTipForm = new();
        private readonly Label headerToolTipLabel = new();
        private readonly List<Label> runtimeCoverageSummaryLabels = new();
        private readonly NotifyIcon trayIcon = new();
        private readonly ContextMenuStrip trayMenu = new();
        private readonly ContextMenuStrip appCategoryMenu = new();
        private readonly ContextMenuStrip usageGridMenu = new();
        private readonly ContextMenuStrip timelineGridMenu = new();
        private readonly FlowLayoutPanel summaryOverviewPanel = new();
        private readonly List<Label> summaryOverviewLabels = new();
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
            InitializeFormUi();

            if (IsRunningInDesigner())
            {
                ConfigureDesignPreview();
                return;
            }

            InitializeRuntimeTracking();
            RegisterUiEventsAndStartSampling();
        }

    }
}
