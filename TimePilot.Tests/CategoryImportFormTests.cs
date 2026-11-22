using System.Diagnostics;
using System.Runtime.ExceptionServices;
using TimePilot.WinForms;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests;

public sealed class CategoryImportFormTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void PreviewBlocksDuplicateApplyAndCloseThenShowsExplicitCompletion(int languageValue, bool portable)
    {
        InSta(() =>
        {
            using var fixture = new CategoryImportFixture();
            fixture.PrepareApp();
            var file = fixture.Zip;
            if (portable)
            {
                file = Path.Combine(fixture.Root, "categories.json");
                CategoryTransferFile.Export(fixture.SourceDb, file, fixture.Now);
            }
            var plan = new CategoryImportService(fixture.TargetDb).Preview(file);
            var pending = new TaskCompletionSource<string>();
            var calls = 0;
            using var form = new CategoryImportForm(plan, (UiLanguage)languageValue, () => { calls++; return pending.Task; });
            var text = new CategoryImportText((UiLanguage)languageValue);
            Assert.Equal(languageValue == (int)UiLanguage.English ? "Development" : "개발", text.Category("Development"));
            form.Show();
            Application.DoEvents();
            if (portable) Assert.Contains(text.ProcessNameOnlyWarning, Find<Label>(form, "categoryImportWarning").Text);
            var suffix = portable ? "-portable" : "";
            SaveScreenshot(form, $"category-import-{languageValue}{suffix}-preview.png");
            var operation = form.ApplyAsync();
            var apply = Find<Button>(form, "categoryImportApply");
            var close = Find<Button>(form, "categoryImportClose");
            Assert.False(apply.Enabled);
            Assert.False(close.Enabled);
            form.Close();
            Assert.False(form.IsDisposed);
            Wait(form.ApplyAsync());
            Assert.Equal(1, calls);
            pending.SetResult(Path.Combine(fixture.Root, "safety.zip"));
            Wait(operation);
            Assert.True(form.Applied);
            Assert.False(apply.Enabled);
            Assert.True(close.Enabled);
            Assert.Contains("safety.zip", Find<TextBox>(form, "categoryImportStatus").Text);
            SaveScreenshot(form, $"category-import-{languageValue}{suffix}-complete.png");
            AssertLayout(form);
        });
    }

    [Fact]
    public void FailureIsExplicitAndNeverReportsAppliedOrRetriesWithoutNewPreview()
    {
        InSta(() =>
        {
            using var fixture = new CategoryImportFixture();
            fixture.PrepareApp();
            var plan = new CategoryImportService(fixture.TargetDb).Preview(fixture.Zip);
            using var form = new CategoryImportForm(plan, UiLanguage.Korean,
                () => Task.FromException<string>(new IOException("Synthetic safety backup failure")));
            form.Show();
            Wait(form.ApplyAsync());
            Assert.False(form.Applied);
            Assert.Contains("실패", Find<TextBox>(form, "categoryImportStatus").Text);
            Assert.False(Find<Button>(form, "categoryImportApply").Enabled);
            Assert.True(Find<Button>(form, "categoryImportClose").Enabled);
        });
    }

    [Fact]
    public void NoChangesDisablesApplyAndClosingPreviewDoesNotChangeData()
    {
        InSta(() =>
        {
            using var fixture = new CategoryImportFixture();
            fixture.BuildZip();
            var before = fixture.Hash(fixture.TargetDb);
            var plan = new CategoryImportService(fixture.TargetDb).Preview(fixture.Zip);
            using var form = new CategoryImportForm(plan, UiLanguage.English, () => throw new Exception("Must not apply"));
            form.Show();
            Assert.False(Find<Button>(form, "categoryImportApply").Enabled);
            form.Close();
            Assert.False(form.Applied);
            Assert.Equal(before, fixture.Hash(fixture.TargetDb));
        });
    }

    private static T Find<T>(Form form, string name) where T : Control => Assert.IsType<T>(Assert.Single(form.Controls.Find(name, true)));
    private static void AssertLayout(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (!child.Visible || parent is DataGridView) continue;
            Assert.True(child.Left >= 0 && child.Top >= 0 && child.Right <= parent.ClientSize.Width + 1 && child.Bottom <= parent.ClientSize.Height + 1,
                $"{child.Name}: {child.Bounds} outside {parent.ClientSize}");
            AssertLayout(child);
        }
    }
    private static void SaveScreenshot(Form form, string name)
    {
        var directory = Environment.GetEnvironmentVariable("TIMEPILOT_TEST_SCREENSHOTS");
        if (string.IsNullOrWhiteSpace(directory) || !Path.IsPathFullyQualified(directory) || !Directory.Exists(directory)) return;
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, form.ClientRectangle with { Width = form.Width, Height = form.Height });
        bitmap.Save(Path.Combine(directory, name), System.Drawing.Imaging.ImageFormat.Png);
    }
    private static void Wait(Task operation)
    {
        var timeout = Stopwatch.StartNew();
        while (!operation.IsCompleted && timeout.Elapsed < TimeSpan.FromSeconds(30))
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
        Assert.True(thread.Join(TimeSpan.FromSeconds(60)), "UI test timed out");
        if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();
    }
}
