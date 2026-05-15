using System.Diagnostics;
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
        private readonly bool startMinimizedToTray;
        private AppSettings settings = AppSettings.LoadDefault();
        private string usageSortProperty = nameof(UsageSummaryRow.ActiveUsageMs);
        private string runtimeSortProperty = nameof(ProcessRuntimeSummaryRow.RuntimeMs);
        private string runtimeSegmentSortProperty = nameof(ProcessRuntimeSegmentRow.StartedAt);
        private SummaryPeriod selectedSummaryPeriod = SummaryPeriod.Today;
        private DateTime selectedSummarySpecificDate = DateTime.Today;
        private DateTime summaryPeriodOptionsDate = DateTime.Today;
        private DateTime selectedDetailDate = DateTime.Today;
        private DateTime selectedTimelineDate = DateTime.Today;
        private SortOrder usageSortOrder = SortOrder.Descending;
        private SortOrder runtimeSortOrder = SortOrder.Descending;
        private SortOrder runtimeSegmentSortOrder = SortOrder.Descending;
        private bool showRecentTimelineFirst = true;
        private bool showCurrentTrackingScopeOnly = true;
        private bool showRunningRuntimeOnly;
        private bool isRefreshingRuntimeGrid;
        private bool isExplicitExitRequested;
        private volatile bool isViewRefreshRunning;
        private long? selectedRuntimeAppId;
        private volatile bool isClosing;
        private volatile bool isProcessRuntimeSampleRunning;
        private string statusText = string.Empty;
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

        public Form1(bool startMinimizedToTray = false)
        {
            this.startMinimizedToTray = startMinimizedToTray;

            InitializeComponent();
            InitializeSummaryPeriodSelector();
            InitializeDateSelectors();

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
            storage.BeginRuntimeSession(startedAt, systemBootedAt, Application.ProductVersion);

            Icon = LoadAppIcon();
            ConfigureHeaderToolTip();
            ConfigureTrayIcon();
            usageGrid.CellMouseEnter += OnGridCellMouseEnter;
            usageGrid.CellMouseLeave += OnGridCellMouseLeave;
            runtimeGrid.CellMouseEnter += OnGridCellMouseEnter;
            runtimeGrid.CellMouseLeave += OnGridCellMouseLeave;
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

            var idleText = isIdle ? "유휴" : "활성";
            statusLabel.Text = foregroundApp is null
                ? $"전경: (없음) · {idleText}"
                : $"전경: {foregroundApp.DisplayName} · {idleText}";
            SetStatusText(statusLabel.Text);
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
            trayIcon.Visible = false;
            trayIcon.Dispose();
            trayMenu.Dispose();
            sampleTimer.Dispose();
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (isExplicitExitRequested || e.CloseReason != CloseReason.UserClosing)
                return;

            e.Cancel = true;
            HideToTray();
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

            const string message = "이전 실행에서 위험한 백그라운드 앱 추적 설정으로 짧은 시간 안에 비정상 종료가 반복된 것으로 보입니다.\n\n안전모드로 백그라운드 앱 추적만 자동으로 껐습니다. 현재 사용 중인 앱 기록과 유휴 감지는 계속 동작합니다.\n\n다시 사용하려면 설정 > 환경 설정에서 백그라운드 앱 추적을 켜주세요.";
            if (startMinimizedToTray)
            {
                trayIcon.ShowBalloonTip(
                    8000,
                    "TimePilot 안전모드",
                    "반복 비정상 종료를 피하기 위해 백그라운드 앱 추적을 자동으로 껐습니다.",
                    ToolTipIcon.Warning);
                return;
            }

            CenteredMessageDialog.Show(
                this,
                message,
                "TimePilot 안전모드",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void ShowStartupPromptIfNeeded()
        {
            if (settings.StartupPromptShown || startMinimizedToTray || isClosing)
                return;

            var result = CenteredMessageDialog.Show(
                this,
                "Windows 시작 시 TimePilot을 자동으로 실행할까요?\n\n나중에 환경 설정에서 언제든 변경할 수 있습니다.",
                "TimePilot 자동 시작",
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

        private void InitializeSummaryPeriodSelector()
        {
            selectedSummaryPeriod = SummaryPeriod.Today;
            selectedSummarySpecificDate = DateTime.Today;
            RefreshSummaryPeriodOptions(DateTime.Today);
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

                if (summarySpecificDatePicker.Value.Date > today)
                    summarySpecificDatePicker.Value = today;

                summarySpecificDatePicker.MaxDate = today;
                summarySpecificDatePicker.Value = selectedSummarySpecificDate;
                summarySpecificDatePicker.Visible = selectedSummaryPeriod == SummaryPeriod.SpecificDate;
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
                var today = DateTime.Today;
                selectedDetailDate = today;
                selectedTimelineDate = today;
                detailDatePicker.MaxDate = today;
                timelineDatePicker.MaxDate = today;
                detailDatePicker.Value = today;
                timelineDatePicker.Value = today;
            }
            finally
            {
                isInitializingDateSelectors = false;
            }

            UpdateDateNavigationButtons();
        }

        private void ConfigureDesignPreview()
        {
            statusLabel.Text = "전경: Visual Studio · 활성";
            SetRuntimeCoverageSummaryParts(
                "오늘 0시~현재 기록 커버리지 98.1%",
                "기록 07:48:12",
                "미기록 00:09:00",
                "최장 미기록 00:04:12");
            usageGrid.DataSource = AddIcons(new List<UsageSummaryRow>
            {
                new("Microsoft Visual Studio", "devenv", null, 3_900_000, 0.54, 8, null, DateTimeOffset.Now.AddHours(-2), DateTimeOffset.Now),
                new("Google Chrome", "chrome", null, 1_680_000, 0.23, 15, null, DateTimeOffset.Now.AddHours(-1), DateTimeOffset.Now.AddMinutes(-12)),
                new("File Explorer", "explorer", null, 900_000, 0.13, 4, null, DateTimeOffset.Now.AddMinutes(-45), DateTimeOffset.Now.AddMinutes(-5))
            });
            dailyUsageTrendGrid.DataSource = new List<DailyUsageTrendRow>
            {
                new(DateTime.Today, 6_480_000, "Microsoft Visual Studio", 3_900_000),
                new(DateTime.Today.AddDays(-1), 4_560_000, "Google Chrome", 2_400_000)
            };
            timelineGrid.DataSource = AddIcons(new List<ActivityTimelineRow>
            {
                new("활성", DateTimeOffset.Now.AddHours(-2), DateTimeOffset.Now.AddHours(-1), 3_600_000, "devenv"),
                new("유휴", DateTimeOffset.Now.AddHours(-1), DateTimeOffset.Now.AddMinutes(-45), 900_000, "devenv"),
                new("활성", DateTimeOffset.Now.AddMinutes(-45), null, 2_700_000, "chrome")
            });
        }

        private void SetRuntimeCoverageSummary(RuntimeCoverageSummary? summary)
        {
            if (summary is null)
            {
                SetRuntimeCoverageSummaryParts("기록 커버리지 -");
                return;
            }

            SetRuntimeCoverageSummaryParts(summary.SummaryParts);
        }

        private void SetRuntimeCoverageSummaryParts(params string[] parts)
        {
            SetRuntimeCoverageSummaryParts((IEnumerable<string>)parts);
        }

        private void SetRuntimeCoverageSummaryParts(IEnumerable<string> parts)
        {
            var partList = parts.ToList();
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
            }
            finally
            {
                runtimeCoverageSummaryPanel.ResumeLayout();
            }
        }

        private void ConfigureTrayIcon()
        {
            var openMenuItem = new ToolStripMenuItem("창 열기");
            openMenuItem.Click += (_, _) => ShowMainWindow();

            var exitTrayMenuItem = new ToolStripMenuItem("종료");
            exitTrayMenuItem.Click += (_, _) => ExitApplication();

            trayMenu.Items.AddRange(new ToolStripItem[]
            {
                openMenuItem,
                new ToolStripSeparator(),
                exitTrayMenuItem
            });

            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Icon = LoadAppIcon();
            trayIcon.Text = "TimePilot";
            trayIcon.Visible = true;
            trayIcon.DoubleClick += (_, _) => ShowMainWindow();
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
            var runtimeHorizontalOffset = GetHorizontalScrollingOffset(runtimeGrid);
            var selectedTab = mainTabs.SelectedTab;
            RefreshSummaryPeriodOptionsIfDateChanged(observedAt);
            var summaryPeriodRange = SummaryPeriodCalculator.GetRange(
                observedAt,
                selectedSummaryPeriod,
                selectedSummarySpecificDate);
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
                    var timelineRows = selectedTab == timelineTab
                        ? storage.GetActivityTimelineForDate(timelineDate, observedAt)
                        : null;
                    var timelineDateHasData = selectedTab == timelineTab
                        ? storage.HasActivityDataForDate(timelineDate, observedAt)
                        : (bool?)null;
                    var runtimeRows = selectedTab == detailTab
                        ? storage.GetProcessRuntimeUsageForDate(detailDate, observedAt)
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
                        null,
                        summaryPeriodRange.ShowDateInTimestamps,
                        detailDateHasData,
                        timelineDateHasData,
                        timelineRows,
                        runtimeRows,
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
                SetGridDataSourcePreservingView(
                    usageGrid,
                    AddIcons(SortUsageSummaryRows(UsageSummaryRowBuilder.FromForegroundUsage(
                        snapshot.ForegroundUsage,
                        snapshot.ShowDateInUsageTimestamps))));
                SetGridDataSourcePreservingView(
                    dailyUsageTrendGrid,
                    snapshot.DailyUsageTrendRows ?? Array.Empty<DailyUsageTrendRow>());
            }

            if (snapshot.TimelineRows is not null)
            {
                SetDateStatus(timelineDateStatusLabel, snapshot.TimelineDateHasData);
                SetGridDataSourcePreservingView(
                    timelineGrid,
                    AddIcons(SortTimelineRows(snapshot.TimelineRows)));
            }

            if (snapshot.RuntimeRows is not null)
            {
                SetDateStatus(detailDateStatusLabel, snapshot.DetailDateHasData);
                isRefreshingRuntimeGrid = true;
                try
                {
                    var runtimeRows = ApplyCurrentTrackingScope(snapshot.RuntimeRows);
                    SetGridDataSourcePreservingView(
                        runtimeGrid,
                        AddIcons(SortRuntimeSummaryRows(FilterRuntimeSummaryRows(
                            runtimeRows))));
                    RestoreRuntimeSelection(
                        appIdToRestore,
                        GetFirstDisplayedColumnIndex(runtimeGrid),
                        runtimeHorizontalOffset);
                }
                finally
                {
                    isRefreshingRuntimeGrid = false;
                }

                selectedRuntimeAppId = GetSelectedRuntimeAppId();
                if (selectedRuntimeAppId == appIdToRestore && snapshot.RuntimeSegmentRows is not null)
                {
                    SetGridDataSourcePreservingView(
                        runtimeSegmentsGrid,
                        SortRuntimeSegmentRows(snapshot.RuntimeSegmentRows));
                }
                else
                {
                    RefreshRuntimeSegments(observedAt);
                }
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
                SortRuntimeSegmentRows(storage.GetProcessRuntimeSegmentsForDate(selectedRow.AppId, selectedDetailDate, observedAt)));
        }

        private static void SetDateStatus(Label label, bool? hasData)
        {
            label.Text = hasData switch
            {
                true => "기록 있음",
                false => "기록 없음",
                _ => "확인 전"
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

            performanceStatusText = "성능: " + string.Join(", ", slowTimings);
            performanceStatusExpiresAt = DateTimeOffset.UtcNow.Add(PerformanceStatusDuration);
            RefreshStatusLabel();
        }

        private void ReportPerformanceEvents(params string[] events)
        {
            if (!settings.PerformanceDiagnosticsEnabled)
                return;

            if (events.Length == 0)
                return;

            performanceStatusText = "성능: " + string.Join(", ", events);
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

            statusLabel.Text = string.IsNullOrWhiteSpace(performanceStatusText)
                ? statusText
                : $"{statusText} | {performanceStatusText}";
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
                filteredRows = filteredRows.Where(x => x.IsInCurrentTrackingScope);

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

        private void OnMainTabsSelectedIndexChanged(object? sender, EventArgs e)
        {
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
                picker.Value = date;
            }
            finally
            {
                isInitializingDateSelectors = false;
            }
        }

        private void UpdateDateNavigationButtons()
        {
            var today = DateTime.Today;
            detailNextDateButton.Enabled = selectedDetailDate < today;
            detailTodayButton.Enabled = selectedDetailDate < today;
            timelineNextDateButton.Enabled = selectedTimelineDate < today;
            timelineTodayButton.Enabled = selectedTimelineDate < today;
        }

        private void OnSummaryPeriodComboBoxSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (isInitializingSummaryPeriodSelector)
                return;

            if (summaryPeriodComboBox.SelectedItem is not SummaryPeriodOption option)
                return;

            selectedSummaryPeriod = option.Period;
            summarySpecificDatePicker.Visible = selectedSummaryPeriod == SummaryPeriod.SpecificDate;
            RefreshViews(DateTimeOffset.UtcNow);
        }

        private void OnSummarySpecificDatePickerValueChanged(object? sender, EventArgs e)
        {
            selectedSummarySpecificDate = summarySpecificDatePicker.Value.Date;

            if (!isInitializingSummaryPeriodSelector && selectedSummaryPeriod == SummaryPeriod.SpecificDate)
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
            headerToolTipLabel.Text = "선택 기간 전체 활성 사용 시간 중 이 앱이 차지한 비율입니다.";

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
                return "선택 기간 전체 활성 사용 시간 중 이 앱이 차지한 비율입니다.";

            if (grid == runtimeGrid && column == runtimeLastObservedAtColumn)
                return "백그라운드 앱 추적이 실제로 마지막 관측한 시각입니다.";

            if (grid == runtimeGrid && column == runtimeDurationColumn)
                return "실행 중인 앱은 현재 시각 기준으로 계속 증가하고, 종료된 앱은 마지막 관측 시각까지 계산합니다.";

            if (grid == runtimeGrid && column == runtimeActualUsageRatioColumn)
                return "실행 시간 중 실제 foreground 활성 사용 시간이 차지한 비율입니다.";

            if (grid == runtimeGrid && column == runtimeSessionCountColumn)
                return "앱이 이어서 실행된 것으로 관측된 구간 수입니다.";

            if (grid == runtimeGrid && column == runtimeStatusColumn)
                return "현재 설정 기준으로 실행 중, 종료, 추적 범위 밖 상태를 표시합니다.";

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

        private void RestoreRuntimeSelection(long? appId, int firstDisplayedColumnIndex, int horizontalScrollingOffset)
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
                TrySetHorizontalScrollingOffset(runtimeGrid, horizontalScrollingOffset);
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

            if (selectedIndex < 0)
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
            using var form = new PreferencesForm(settings);
            if (form.ShowDialog(this) != DialogResult.OK)
                return;

            settings.SetIdleThresholdMinutes(form.IdleThresholdMinutes);
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

            if (form.ClearUsageDataRequested)
                ClearUsageData();
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
                SetGridDataSourcePreservingView(timelineGrid, Array.Empty<ActivityTimelineRow>());
                SetGridDataSourcePreservingView(runtimeGrid, Array.Empty<ProcessRuntimeSummaryRow>());
                SetGridDataSourcePreservingView(runtimeSegmentsGrid, Array.Empty<ProcessRuntimeSegmentRow>());
                SetStatusText("사용 기록을 삭제했습니다.");
            }
            finally
            {
                if (!isClosing)
                    sampleTimer.Start();
            }
        }

        private void OnExportCsvMenuItemClick(object? sender, EventArgs e)
        {
            if (storage is null)
                return;

            var now = DateTimeOffset.UtcNow;
            using var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "csv",
                FileName = $"TimePilot-usage-{now.ToLocalTime():yyyy-MM-dd}.csv",
                Filter = "CSV 파일 (*.csv)|*.csv",
                OverwritePrompt = false,
                Title = "CSV 내보내기"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                var exporter = new UsageCsvExporter(storage);
                var exportedFiles = exporter.ExportToday(dialog.FileName, now);
                CenteredMessageDialog.Show(
                    this,
                    $"CSV 파일 {exportedFiles.Count}개를 내보냈습니다.\n\n{Path.GetDirectoryName(dialog.FileName)}",
                    "CSV 내보내기",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                CenteredMessageDialog.Show(
                    this,
                    $"CSV 내보내기에 실패했습니다.\n\n{ex.Message}",
                    "CSV 내보내기",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
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
                $"TimePilot {Application.ProductVersion}",
                "TimePilot 정보",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private sealed record ViewRefreshSnapshot(
            IReadOnlyList<ForegroundUsageSummary>? ForegroundUsage,
            IReadOnlyList<DailyUsageTrendRow>? DailyUsageTrendRows,
            RuntimeCoverageSummary? RuntimeCoverage,
            bool ShowDateInUsageTimestamps,
            bool? DetailDateHasData,
            bool? TimelineDateHasData,
            IReadOnlyList<ActivityTimelineRow>? TimelineRows,
            IReadOnlyList<ProcessRuntimeSummaryRow>? RuntimeRows,
            IReadOnlyList<ProcessRuntimeSegmentRow>? RuntimeSegmentRows,
            long ReadElapsedMs);

    }
}
