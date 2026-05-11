using System.Globalization;
using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    public partial class Form1 : Form
    {
        private const int SampleIntervalMs = 1000;
        private const int IdleThresholdMs = 120000;

        private readonly UsageAccumulator accumulator = new();
        private readonly System.Windows.Forms.Timer sampleTimer = new();

        public Form1()
        {
            InitializeComponent();
            sampleTimer.Interval = SampleIntervalMs;
            sampleTimer.Tick += OnSampleTick;
            sampleTimer.Start();
            FormClosed += (_, _) => sampleTimer.Dispose();
        }

        private void OnSampleTick(object? sender, EventArgs e)
        {
            var isIdle = UserIdleChecker.IsIdle(IdleThresholdMs);
            var processName = ForegroundWindowReader.TryGetForegroundProcessName();
            accumulator.AddSample(processName, SampleIntervalMs, isIdle);
            var idleText = isIdle ? "유휴" : "활성";
            statusLabel.Text = string.IsNullOrEmpty(processName)
                ? $"전경: (없음) · {idleText}"
                : $"전경: {processName} · {idleText}";
            var snapshot = accumulator.SnapshotTotalsMs();
            usageListBox.BeginUpdate();
            usageListBox.Items.Clear();
            foreach (var kv in snapshot.OrderByDescending(x => x.Value))
            {
                var span = TimeSpan.FromMilliseconds(kv.Value);
                var line = string.Format(CultureInfo.CurrentCulture, "{0} — {1:D2}:{2:D2}:{3:D2}",
                    kv.Key, (int)span.TotalHours, span.Minutes, span.Seconds);
                usageListBox.Items.Add(line);
            }
            usageListBox.EndUpdate();
        }
    }
}
