using System.Runtime.ExceptionServices;
using TimePilot.WinForms;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class PreferencesResetTests
    {
        [Fact]
        public void SearchSuffixDefaultButton_ResetsOnlySuffixUntilSave()
        {
            RunWithPreferences((form, settings, path) =>
            {
                var originalFile = File.ReadAllText(path);
                Find<TextBox>(form, "koreanWebSearchSuffixTextBox").Text = "custom";
                Find<Button>(form, "resetWebSearchSuffixButton").PerformClick();

                Assert.Equal("이란", Find<TextBox>(form, "koreanWebSearchSuffixTextBox").Text);
                Assert.Equal(DialogResult.None, form.DialogResult);
                Assert.Equal(originalFile, File.ReadAllText(path));
                Find<Button>(form, "okButton").PerformClick();

                Assert.Equal(DialogResult.OK, form.DialogResult);
                Assert.Equal("이란", form.KoreanWebSearchSuffix);
                Assert.Equal(settings.UiLanguage, form.UiLanguage);
                Assert.Equal(7, form.IdleThresholdMinutes);
                Assert.True(form.StartWithWindows);
                Assert.True(form.AutomaticBackupEnabled);
                Assert.Equal(30, form.AutomaticBackupRetentionCount);
                Assert.Equal(originalFile, File.ReadAllText(path));
            });
        }

        [Fact]
        public void RestoreDefaultPreferences_ReturnsDefaultsAndPreservesLanguageAndBackupFolder()
        {
            RunWithPreferences((form, settings, path) =>
            {
                var originalFile = File.ReadAllText(path);
                typeof(PreferencesForm).GetProperty(nameof(PreferencesForm.ClearUsageDataRequested))!
                    .SetValue(form, true);
                form.RestoreDefaultPreferences();

                Assert.False(form.ClearUsageDataRequested);
                Assert.Equal(DialogResult.None, form.DialogResult);
                Assert.Equal(originalFile, File.ReadAllText(path));
                Find<Button>(form, "okButton").PerformClick();

                Assert.Equal(DialogResult.OK, form.DialogResult);
                Assert.Equal(settings.UiLanguage, form.UiLanguage);
                Assert.Equal(AppSettings.DefaultIdleThresholdMinutes, form.IdleThresholdMinutes);
                Assert.True(form.StartWithWindows);
                Assert.Equal(AppSettings.DefaultStartupDisplayMode, form.StartupDisplayMode);
                Assert.Equal(AppSettings.DefaultPerformanceDiagnosticsEnabled, form.PerformanceDiagnosticsEnabled);
                Assert.Equal(AppSettings.DefaultProcessRuntimeTrackingEnabled, form.ProcessRuntimeTrackingEnabled);
                Assert.Equal(AppSettings.DefaultProcessRuntimeTrackingScope, form.ProcessRuntimeTrackingScope);
                Assert.Equal(AppSettings.DefaultProcessRuntimeSampleIntervalSeconds, form.ProcessRuntimeSampleIntervalSeconds);
                Assert.False(form.ProcessRuntimeRiskAccepted);
                Assert.Equal(AppSettings.DefaultAutomaticBackupEnabled, form.AutomaticBackupEnabled);
                Assert.Equal(AppSettings.DefaultAutomaticBackupRetentionCount, form.AutomaticBackupRetentionCount);
                Assert.Equal(settings.AutomaticBackupDirectory, form.AutomaticBackupDirectory);
                Assert.Equal(AppSettings.DefaultKoreanWebSearchSuffix, form.KoreanWebSearchSuffix);
                Assert.Equal(originalFile, File.ReadAllText(path));
            });
        }

        [Fact]
        public void RestoreDefaultPreferences_CancelLeavesStoredSettingsAndFilesUnchanged()
        {
            RunWithPreferences((form, settings, path) =>
            {
                var originalFile = File.ReadAllText(path);
                var root = Path.GetDirectoryName(path)!;
                var databasePath = Path.Combine(root, "timepilot.db");
                var backupPath = Path.Combine(root, "manual-backup.zip");
                File.WriteAllText(databasePath, "test database sentinel");
                File.WriteAllText(backupPath, "test backup sentinel");

                form.RestoreDefaultPreferences();
                Find<Button>(form, "cancelButton").PerformClick();

                Assert.Equal(DialogResult.Cancel, form.DialogResult);
                Assert.Equal(originalFile, File.ReadAllText(path));
                Assert.Equal("test database sentinel", File.ReadAllText(databasePath));
                Assert.Equal("test backup sentinel", File.ReadAllText(backupPath));
                var reloaded = AppSettings.Load(path);
                Assert.Equal("사용법", reloaded.KoreanWebSearchSuffix);
                Assert.True(reloaded.AutomaticBackupEnabled);
                Assert.Equal(settings.AutomaticBackupLastPath, reloaded.AutomaticBackupLastPath);
            });
        }

        [Fact]
        public void RestoreDefaultPreferences_RecommendsStartupWithoutOverwritingSavedOptOut()
        {
            RunWithPreferences((_, settings, path) =>
            {
                settings.StartWithWindows = false;
                settings.Save();
                using var form = new PreferencesForm(AppSettings.Load(path), effectiveStartWithWindows: false);
                form.Show();
                Application.DoEvents();
                Assert.False(Find<CheckBox>(form, "startWithWindowsCheckBox").Checked);

                form.RestoreDefaultPreferences();

                Assert.True(Find<CheckBox>(form, "startWithWindowsCheckBox").Checked);
                Assert.False(AppSettings.Load(path).StartWithWindows);
                Find<Button>(form, "cancelButton").PerformClick();
                Assert.False(AppSettings.Load(path).StartWithWindows);
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ResetSettingsButton_RequiresConfirmation(bool confirm)
        {
            RunWithPreferences((form, _, path) =>
            {
                var originalFile = File.ReadAllText(path);
                using var timer = new System.Windows.Forms.Timer { Interval = 25 };
                timer.Tick += (_, _) =>
                {
                    var confirmation = Application.OpenForms.OfType<CenteredMessageDialog>().FirstOrDefault();
                    if (confirmation is null)
                        return;
                    timer.Stop();
                    confirmation.DialogResult = confirm ? DialogResult.OK : DialogResult.Cancel;
                };
                timer.Start();
                Find<Button>(form, "resetPreferencesButton").PerformClick();

                Assert.Equal(confirm ? "이란" : "사용법",
                    Find<TextBox>(form, "koreanWebSearchSuffixTextBox").Text);
                Assert.Equal(DialogResult.None, form.DialogResult);
                Assert.Equal(originalFile, File.ReadAllText(path));
            });
        }

        private static T Find<T>(Control parent, string name) where T : Control
        {
            return Assert.IsType<T>(Assert.Single(parent.Controls.Find(name, true)));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DevelopmentPreferences_KeepStartupControlsLockedAndPreserveValues(bool enabled)
        {
            RunWithPreferences((_, settings, _) =>
            {
                settings.StartWithWindows = enabled;
                using var form = new PreferencesForm(settings, enabled, allowStartupChanges: false);
                form.Show();
                Application.DoEvents();
                var startup = Find<CheckBox>(form, "startWithWindowsCheckBox");
                var mode = Find<ComboBox>(form, "startupDisplayModeComboBox");
                Assert.False(startup.Enabled);
                Assert.False(mode.Enabled);

                form.RestoreDefaultPreferences();
                Find<Button>(form, "okButton").PerformClick();

                Assert.Equal(enabled, form.StartWithWindows);
                Assert.Equal(settings.StartupDisplayMode, form.StartupDisplayMode);
                Assert.False(startup.Enabled);
                Assert.False(mode.Enabled);
            });
        }

        private static void RunWithPreferences(Action<PreferencesForm, AppSettings, string> test)
        {
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                var root = Path.Combine(Path.GetTempPath(), $"TimePilotReset-{Guid.NewGuid():N}");
                try
                {
                    var path = Path.Combine(root, "settings.json");
                    var settings = AppSettings.Load(path);
                    settings.IdleThresholdMinutes = 7;
                    settings.UiLanguage = UiLanguage.English;
                    settings.KoreanWebSearchSuffix = "사용법";
                    settings.StartWithWindows = true;
                    settings.StartupDisplayMode = StartupDisplayMode.MainWindow;
                    settings.PerformanceDiagnosticsEnabled = true;
                    settings.ProcessRuntimeTrackingScope = ProcessRuntimeTrackingScope.UserProcesses;
                    settings.ProcessRuntimeSampleIntervalSeconds = 90;
                    settings.AutomaticBackupEnabled = true;
                    settings.AutomaticBackupDirectory = Path.Combine(root, "external");
                    settings.AutomaticBackupRetentionCount = 30;
                    settings.AutomaticBackupLastPath = Path.Combine(root, "existing.zip");
                    settings.Save();
                    using var form = new PreferencesForm(settings);
                    form.Show();
                    Application.DoEvents();
                    test(form, settings, path);
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
                finally
                {
                    if (Directory.Exists(root))
                        Directory.Delete(root, recursive: true);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure is not null)
                ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
