namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private void InitializeFormUi()
        {
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
        }

        private void InitializeRuntimeTracking()
        {
            storage = TimePilotStorage.CreateDefault();
            foregroundSessionTracker = new ForegroundSessionTracker(storage);
            idleSessionTracker = new IdleSessionTracker(storage);
            processRuntimeSessionTracker = new ProcessRuntimeSessionTracker(storage);

            var startedAt = DateTimeOffset.UtcNow;
            var systemBootedAt = GetCurrentSystemBootedAt(startedAt);
            storage.Initialize(startedAt, systemBootedAt);
            ApplyProcessRuntimeSafeModeIfNeeded();
            UpdateDetailTrackingDisabledBanner();
            storage.BeginRuntimeSession(
                startedAt,
                systemBootedAt,
                Application.ProductVersion);
            RecordWindowsSystemEvent("timepilot-start", "ApplicationStarted");
            RegisterWindowsSystemEventHandlers();
        }

        private void RegisterUiEventsAndStartSampling()
        {
            Icon = LoadAppIcon();
            ConfigureHeaderToolTip();
            ConfigureTrayIcon();
            usageGrid.CellMouseEnter += OnGridCellMouseEnter;
            usageGrid.CellMouseLeave += OnGridCellMouseLeave;
            usageGrid.CellMouseDown += OnUsageGridCellMouseDown;
            usageGrid.SelectionChanged += OnUsageGridSelectionChanged;
            InitializeSummaryOverview();
            InitializeSummaryUsageBars();
            timelineGrid.CellMouseDown += OnTimelineGridCellMouseDown;
            timelineGrid.RowPrePaint += OnTimelineGridRowPrePaint;
            timelineGrid.RowPostPaint += OnTimelineGridRowPostPaint;
            timelineOverviewControl.ActivitySegmentContextRequested +=
                OnTimelineOverviewActivitySegmentContextRequested;
            timelineOverviewControl.CategorySegmentContextRequested +=
                OnTimelineOverviewCategorySegmentContextRequested;
            timelineOverviewControl.WindowsTrackContextRequested +=
                OnTimelineOverviewWindowsTrackContextRequested;
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
    }
}
