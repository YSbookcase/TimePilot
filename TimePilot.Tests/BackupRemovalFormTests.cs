using System.Diagnostics;
using System.Runtime.ExceptionServices;
using TimePilot.WinForms;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests;

public sealed class BackupRemovalFormTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void VerifiedBackupIsRequiredEvenWithHistoricalSuccessAndShellFailureCanRetry(int languageValue)
    {
        InSta(() =>
        {
            using var fixture = new RemovalFixture();
            var shellCalls = 0;
            var historyCalls = 0;
            using var form = new BackupRemovalForm(new BackupRemovalPreparation(fixture.Data), (UiLanguage)languageValue,
                lastSuccessAt: DateTimeOffset.UtcNow.AddDays(-1), lastPath: fixture.Backup,
                recordSuccess: _ => historyCalls++, openInstalledApps: () =>
                {
                    shellCalls++;
                    if (shellCalls == 1) throw new InvalidOperationException("Synthetic shell failure");
                });
            form.Show();
            Application.DoEvents();
            var open = Control<Button>(form, "openInstalledAppsButton");
            Assert.False(open.Enabled);
            SaveScreenshot(form, $"removal-{languageValue}-initial.png");
            Wait(form.OpenSettingsAsync());
            Assert.Equal(0, shellCalls);
            Wait(form.CreateBackupAsync(fixture.Backup));
            Assert.True(open.Enabled);
            Assert.Equal(1, historyCalls);
            var history = Control<Label>(form, "removalHistoryLabel").Text;
            Assert.Contains(fixture.Backup, history);
            Assert.DoesNotContain("not revalidated", history);
            Assert.DoesNotContain("재검증하지 않았습니다", history);
            SaveScreenshot(form, $"removal-{languageValue}-ready.png");
            Wait(form.OpenSettingsAsync());
            Assert.Equal(1, shellCalls);
            Assert.True(open.Enabled);
            Wait(form.OpenSettingsAsync());
            Assert.Equal(2, shellCalls);
            Assert.Equal(new BackupRemovalText((UiLanguage)languageValue).SettingsOpened, Control<Label>(form, "removalStatusLabel").Text);
            AssertLayout(form);
        });
    }

    [Fact]
    public void BackupFailureOrLaterDeletionNeverLaunchesWindowsSettings()
    {
        InSta(() =>
        {
            using var fixture = new RemovalFixture();
            var shellCalls = 0;
            using var form = new BackupRemovalForm(new BackupRemovalPreparation(fixture.Data), UiLanguage.Korean,
                openInstalledApps: () => shellCalls++);
            form.Show();
            Wait(form.CreateBackupAsync(Path.Combine(fixture.Data, "unsafe.zip")));
            Assert.False(Control<Button>(form, "openInstalledAppsButton").Enabled);
            Wait(form.OpenSettingsAsync());
            Assert.Equal(0, shellCalls);
            Wait(form.CreateBackupAsync(fixture.Backup));
            File.Delete(fixture.Backup);
            Wait(form.OpenSettingsAsync());
            Assert.Equal(0, shellCalls);
            Assert.False(Control<Button>(form, "openInstalledAppsButton").Enabled);
        });
    }

    [Fact]
    public void BackupInProgressBlocksDuplicateRequestsAndClose()
    {
        InSta(() =>
        {
            using var fixture = new RemovalFixture();
            using var form = new BackupRemovalForm(new BackupRemovalPreparation(fixture.Data), UiLanguage.English);
            form.Show();
            var first = form.CreateBackupAsync(fixture.Backup);
            Assert.False(Control<Button>(form, "createRemovalBackupButton").Enabled);
            Assert.False(Control<Button>(form, "closeRemovalButton").Enabled);
            form.Close();
            Assert.False(form.IsDisposed);
            Wait(form.CreateBackupAsync(Path.Combine(fixture.External, "duplicate.zip")));
            Wait(first);
            Assert.False(File.Exists(Path.Combine(fixture.External, "duplicate.zip")));
            Assert.True(Control<Button>(form, "closeRemovalButton").Enabled);
        });
    }

    [Fact]
    public void PreferencesEntryIsSeparateFromSavingOrClearingRecords()
    {
        InSta(() =>
        {
            using var fixture = new RemovalFixture();
            var settingsPath = Path.Combine(fixture.Data, "settings.json");
            var before = File.ReadAllText(settingsPath);
            using var form = new PreferencesForm(AppSettings.Load(settingsPath));
            var called = false;
            form.BackupRemovalRequested += (_, _) => called = true;
            form.Show();
            form.Controls.OfType<TabControl>().Single().SelectedIndex = 1;
            Application.DoEvents();
            Control<Button>(form, "backupRemovalButton").PerformClick();
            Assert.True(called);
            Assert.False(form.ClearUsageDataRequested);
            Assert.Equal(DialogResult.None, form.DialogResult);
            Assert.Equal(before, File.ReadAllText(settingsPath));
            SaveScreenshot(form, "preferences-removal-entry.png");
        });
    }

    private static T Control<T>(Form form, string name) where T : Control =>
        Assert.IsType<T>(Assert.Single(form.Controls.Find(name, true)));

    private static void SaveScreenshot(Form form, string fileName)
    {
        var directory = Environment.GetEnvironmentVariable("TIMEPILOT_TEST_SCREENSHOTS");
        if (string.IsNullOrWhiteSpace(directory) || !Path.IsPathFullyQualified(directory) || !Directory.Exists(directory))
            return;
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
        bitmap.Save(Path.Combine(directory, fileName), System.Drawing.Imaging.ImageFormat.Png);
    }

    private static void AssertLayout(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (!child.Visible) continue;
            Assert.True(child.Left >= 0 && child.Top >= 0);
            Assert.True(child.Right <= parent.ClientSize.Width && child.Bottom <= parent.ClientSize.Height, child.Name);
            AssertLayout(child);
        }
    }

    private static void Wait(Task task)
    {
        var watch = Stopwatch.StartNew();
        while (!task.IsCompleted && watch.Elapsed < TimeSpan.FromSeconds(30))
        {
            Application.DoEvents();
            Thread.Sleep(5);
        }
        Assert.True(task.IsCompleted, "UI task did not finish.");
        task.GetAwaiter().GetResult();
    }

    private static void InSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
