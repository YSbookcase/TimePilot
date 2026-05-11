using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    public partial class Form1 : Form
    {
        private const int SampleIntervalMs = 1000;
        private const int IdleThresholdMs = 120000;

        private readonly System.Windows.Forms.Timer sampleTimer = new();
        private TimePilotStorage? storage;
        private ForegroundSessionTracker? foregroundSessionTracker;

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
            var processName = ForegroundWindowReader.TryGetForegroundProcessName();

            storage?.UpdateRuntimeHeartbeat(observedAt);
            foregroundSessionTracker?.Track(processName, isIdle, observedAt);

            var idleText = isIdle ? "유휴" : "활성";
            statusLabel.Text = string.IsNullOrEmpty(processName)
                ? $"전경: (없음) · {idleText}"
                : $"전경: {processName} · {idleText}";
            RefreshUsageGrid(observedAt);
        }

        private void OnFormClosed(object? sender, FormClosedEventArgs e)
        {
            var endedAt = DateTimeOffset.UtcNow;
            sampleTimer.Stop();
            foregroundSessionTracker?.EndCurrentSession(endedAt);
            storage?.EndRuntimeSession(endedAt, "normal");
            storage?.Dispose();
            sampleTimer.Dispose();
        }

        private void ConfigureDesignPreview()
        {
            statusLabel.Text = "전경: Visual Studio · 활성";
            usageGrid.DataSource = new List<UsageSummaryRow>
            {
                new("devenv", 3_900_000, 0.54, DateTimeOffset.Now.AddHours(-2), DateTimeOffset.Now),
                new("chrome", 1_680_000, 0.23, DateTimeOffset.Now.AddHours(-1), DateTimeOffset.Now.AddMinutes(-12)),
                new("explorer", 900_000, 0.13, DateTimeOffset.Now.AddMinutes(-45), DateTimeOffset.Now.AddMinutes(-5))
            };
        }

        private void RefreshUsageGrid(DateTimeOffset observedAt)
        {
            if (storage is null)
                return;

            usageGrid.DataSource = UsageSummaryRowBuilder.FromForegroundUsage(
                storage.GetForegroundUsageForDay(observedAt));
        }

        private static bool IsRunningInDesigner()
        {
            return System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime;
        }
    }
}
