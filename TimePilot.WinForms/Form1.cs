using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    public partial class Form1 : Form
    {
        private const int SampleIntervalMs = 1000;
        private const int IdleThresholdMs = 120000;

        private readonly System.Windows.Forms.Timer sampleTimer = new();
        private readonly AppIconCache appIconCache = new();
        private TimePilotStorage? storage;
        private ForegroundSessionTracker? foregroundSessionTracker;
        private IdleSessionTracker? idleSessionTracker;

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

            var startedAt = DateTimeOffset.UtcNow;
            storage.Initialize(startedAt);
            storage.BeginRuntimeSession(startedAt, Application.ProductVersion);

            sampleTimer.Interval = SampleIntervalMs;
            sampleTimer.Tick += OnSampleTick;
            sampleTimer.Start();
            FormClosed += OnFormClosed;
        }

        private void OnSampleTick(object? sender, EventArgs e)
        {
            var observedAt = DateTimeOffset.UtcNow;
            var isIdle = UserIdleChecker.IsIdle(IdleThresholdMs);
            var foregroundApp = ForegroundWindowReader.TryGetForegroundApp();

            storage?.UpdateRuntimeHeartbeat(observedAt);
            idleSessionTracker?.Track(isIdle, foregroundApp, IdleThresholdMs, observedAt);
            foregroundSessionTracker?.Track(foregroundApp, isIdle, observedAt);

            var idleText = isIdle ? "유휴" : "활성";
            statusLabel.Text = foregroundApp is null
                ? $"전경: (없음) · {idleText}"
                : $"전경: {foregroundApp.DisplayName} · {idleText}";
            RefreshViews(observedAt);
        }

        private void OnFormClosed(object? sender, FormClosedEventArgs e)
        {
            var endedAt = DateTimeOffset.UtcNow;
            sampleTimer.Stop();
            idleSessionTracker?.EndCurrentSession(endedAt);
            foregroundSessionTracker?.EndCurrentSession(endedAt);
            storage?.EndRuntimeSession(endedAt, "normal");
            storage?.Dispose();
            appIconCache.Dispose();
            sampleTimer.Dispose();
        }

        private void ConfigureDesignPreview()
        {
            statusLabel.Text = "전경: Visual Studio · 활성";
            usageGrid.DataSource = AddIcons(new List<UsageSummaryRow>
            {
                new("Microsoft Visual Studio", null, 3_900_000, 0.54, null, DateTimeOffset.Now.AddHours(-2), DateTimeOffset.Now),
                new("Google Chrome", null, 1_680_000, 0.23, null, DateTimeOffset.Now.AddHours(-1), DateTimeOffset.Now.AddMinutes(-12)),
                new("File Explorer", null, 900_000, 0.13, null, DateTimeOffset.Now.AddMinutes(-45), DateTimeOffset.Now.AddMinutes(-5))
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
                AddIcons(UsageSummaryRowBuilder.FromForegroundUsage(
                    storage.GetForegroundUsageForDay(observedAt))));
            SetGridDataSourcePreservingView(
                timelineGrid,
                AddIcons(storage.GetActivityTimelineForDay(observedAt)));
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

        private static void SetGridDataSourcePreservingView<T>(DataGridView grid, IReadOnlyList<T> rows)
        {
            var firstDisplayedIndex = GetFirstDisplayedRowIndex(grid);
            var selectedIndex = grid.CurrentRow?.Index ?? -1;

            grid.DataSource = rows;

            if (grid.Rows.Count == 0)
                return;

            var restoredFirstIndex = Math.Min(firstDisplayedIndex, grid.Rows.Count - 1);
            TrySetFirstDisplayedRowIndex(grid, restoredFirstIndex);

            if (selectedIndex < 0)
                return;

            var restoredSelectedIndex = Math.Min(selectedIndex, grid.Rows.Count - 1);
            grid.ClearSelection();
            grid.Rows[restoredSelectedIndex].Selected = true;
            grid.CurrentCell = grid.Rows[restoredSelectedIndex].Cells[0];
            TrySetFirstDisplayedRowIndex(grid, restoredFirstIndex);
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

        private static bool IsRunningInDesigner()
        {
            return System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime;
        }
    }
}
