using System.Diagnostics;
using System.Runtime.ExceptionServices;
using TimePilot.WinForms;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests;

public sealed class KeepLaterRestorePreviewFormTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void AnalysisBlocksClosingThenExposesReadOnlyPreviewWithoutEnablingMerge(int language)
    {
        InSta(() =>
        {
            var preview = CreatePreview();
            var empty = new RestoreIntervalComparisonResult([], []);
            var legacy = new DataBackupTableComparison("foreground_sessions", empty, empty);
            var comparison = new DataBackupDetailedComparison(legacy, legacy, legacy, legacy, preview);
            var plan = new DataBackupRestorePlan(true, true, 0, preview.ReferenceAt, DataBackupRecordCounts.Empty, null);
            using var release = new ManualResetEventSlim();
            using var entered = new ManualResetEventSlim();
            var calls = 0;
            using var chooser = new RestoreModeChoiceForm((UiLanguage)language, plan, _ =>
            {
                Interlocked.Increment(ref calls);
                entered.Set();
                if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Synthetic loader timeout");
                return comparison;
            });
            chooser.Show();
            Assert.False(Find<Button>(chooser, "keepLaterPreview").Enabled);
            var analysis = chooser.AnalyzeAsync();
            Assert.True(entered.Wait(TimeSpan.FromSeconds(2)));
            Assert.False(Find<Button>(chooser, "continueFullRestore").Enabled);
            Assert.False(Find<Button>(chooser, "cancelRestore").Enabled);
            Assert.True(Find<ProgressBar>(chooser, "restoreAnalysisProgress").Visible);
            Assert.Equal(ProgressBarStyle.Marquee, Find<ProgressBar>(chooser, "restoreAnalysisProgress").Style);
            Assert.Contains(language == (int)UiLanguage.English ? "Analyzing" : "분석 중", Find<Label>(chooser, "restoreAnalysisStatus").Text);
            Application.DoEvents();
            Save(chooser, $"restore-analysis-busy-{language}.png");
            chooser.Close();
            Assert.False(chooser.IsDisposed);
            Wait(chooser.AnalyzeAsync());
            Assert.Equal(1, calls);
            release.Set();
            Wait(analysis);
            Assert.True(Find<Button>(chooser, "keepLaterPreview").Enabled);
            Assert.False(Find<ProgressBar>(chooser, "restoreAnalysisProgress").Visible);
            Assert.Contains(language == (int)UiLanguage.English ? "completed" : "완료", Find<Label>(chooser, "restoreAnalysisStatus").Text);
            Assert.Equal(RestoreModeChoice.Cancel, chooser.Choice);
            var radios = Children(chooser).OfType<RadioButton>().ToArray();
            Assert.Equal(1, radios.Count(r => r.Enabled));
            Assert.Equal(2, radios.Count(r => !r.Enabled));
            Save(chooser, $"restore-choice-{language}.png");
            Assert.Equal(UiText.Main.DataRestoreWarningFor((UiLanguage)language), Find<Label>(chooser, "fullRestoreWarning").Text);
            AssertLayout(chooser);
            using var form = new KeepLaterRestorePreviewForm(preview, (UiLanguage)language);
            form.Show();
            Application.DoEvents();
            Assert.Single(Children(form).OfType<Button>());
            var grid = Find<DataGridView>(form, "restorePreviewGrid");
            Assert.True(grid.ReadOnly);
            Assert.Equal(10, grid.Rows.Count);
            Save(form, $"restore-keep-later-{language}.png");
            AssertLayout(form);
        });
    }

    [Fact]
    public void RealStageReportsSwitchBetweenUnknownAndMeasuredProgressAndDoNotOverwriteOutcome()
    {
        InSta(() =>
        {
            var plan = new DataBackupRestorePlan(true, false, 0, null, DataBackupRecordCounts.Empty, null);
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            IProgress<RestoreAnalysisProgress>? reporter = null;
            using var form = new RestoreModeChoiceForm(UiLanguage.Korean, plan, progress =>
            {
                reporter = progress;
                entered.Set();
                if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException();
                return null;
            });
            form.Show();
            var task = form.AnalyzeAsync();
            Assert.True(entered.Wait(TimeSpan.FromSeconds(2)));
            reporter!.Report(new(RestoreAnalysisStage.SnapshotCurrent));
            Application.DoEvents();
            Assert.Contains("2/7단계", Find<Label>(form, "restoreAnalysisStatus").Text);
            Assert.Equal(ProgressBarStyle.Marquee, Find<ProgressBar>(form, "restoreAnalysisProgress").Style);
            reporter.Report(new(RestoreAnalysisStage.Compare, 2, 4));
            Application.DoEvents();
            var bar = Find<ProgressBar>(form, "restoreAnalysisProgress");
            Assert.Equal(ProgressBarStyle.Continuous, bar.Style);
            Assert.Equal(4, bar.Maximum);
            Assert.Equal(2, bar.Value);
            Assert.Contains("테이블 처리 2/4", Find<Label>(form, "restoreAnalysisStatus").Text);
            Save(form, "restore-analysis-measured.png");
            AssertLayout(form);
            release.Set();
            Wait(task);
            var result = Find<Label>(form, "restoreAnalysisStatus").Text;
            reporter.Report(new(RestoreAnalysisStage.Classify, 15, 15));
            Application.DoEvents();
            Assert.Equal(result, Find<Label>(form, "restoreAnalysisStatus").Text);
            Assert.False(bar.Visible);
        });
    }

    [Fact]
    public void AnalysisFailureCannotEnableKeepLaterRestore()
    {
        InSta(() =>
        {
            var plan = new DataBackupRestorePlan(true, false, 0, null, DataBackupRecordCounts.Empty, null);
            using var form = new RestoreModeChoiceForm(UiLanguage.Korean, plan, _ => throw new IOException("Synthetic unavailable snapshot"));
            form.Show();
            Wait(form.AnalyzeAsync());
            Assert.False(Find<Button>(form, "keepLaterPreview").Enabled);
            Assert.True(Find<Button>(form, "continueFullRestore").Enabled);
            Assert.Equal(RestoreModeChoice.Cancel, form.Choice);
            Assert.False(Find<ProgressBar>(form, "restoreAnalysisProgress").Visible);
            Assert.Contains("실패", Find<Label>(form, "restoreAnalysisStatus").Text);
            AssertLayout(form);
        });
    }

    internal static KeepLaterRestorePreview CreatePreview()
    {
        var at = DateTimeOffset.Parse("2026-11-22T12:00:00Z");
        var counts = new RestorePreviewCounts(10, 3, 1, 2, 1, 0, at.AddDays(-1), at.AddHours(2));
        return new(at, RestoreReferenceSource.RuntimeHeartbeat, at.AddDays(1), null, at.AddHours(1),
            new[] { "foreground_sessions", "idle_sessions", "app_runtime_sessions", "process_runtime_sessions", "system_events" }
                .Select(table => new RestorePreviewTable(table, counts, counts, true, true)).ToArray());
    }
    private static T Find<T>(Form form, string name) where T : Control => Assert.IsType<T>(Assert.Single(form.Controls.Find(name, true)));
    private static IEnumerable<Control> Children(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (var nested in Children(child)) yield return nested;
        }
    }
    private static void AssertLayout(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (!child.Visible || parent is DataGridView) continue;
            Assert.True(child.Left >= 0 && child.Top >= 0 && child.Right <= parent.ClientSize.Width + 1 && child.Bottom <= parent.ClientSize.Height + 1,
                $"{child.GetType().Name} {child.Bounds} outside {parent.ClientSize}");
            AssertLayout(child);
        }
    }
    private static void Save(Form form, string name)
    {
        var directory = Environment.GetEnvironmentVariable("TIMEPILOT_TEST_SCREENSHOTS");
        if (string.IsNullOrWhiteSpace(directory) || !Path.IsPathFullyQualified(directory) || !Directory.Exists(directory)) return;
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
        bitmap.Save(Path.Combine(directory, name), System.Drawing.Imaging.ImageFormat.Png);
    }
    private static void Wait(Task operation)
    {
        var watch = Stopwatch.StartNew();
        while (!operation.IsCompleted && watch.Elapsed < TimeSpan.FromSeconds(20))
        {
            Application.DoEvents();
            Thread.Sleep(5);
        }
        Assert.True(operation.IsCompleted, "Operation timed out");
        operation.GetAwaiter().GetResult();
        Application.DoEvents();
    }
    private static void InSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() => { try { action(); } catch (Exception ex) { error = ex; } });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(50)));
        if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();
    }
}
