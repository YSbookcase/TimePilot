using System.Runtime.ExceptionServices;
using TimePilot.WinForms;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests;

public sealed class KeepLaterRestorePreparationFormTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void SettingsChoicesOnlyUpdateReadOnlyPreparationAndNeverEnableApply(int language)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var preview = KeepLaterRestorePreparationTests.Preview();
                var plan = KeepLaterRestorePreparation.Build(preview,
                    new([new(1, "Development", "#123456", 10, true)], [new(1, "editor", "Editor", @"C:\editor.exe", 1, "user", null)]),
                    new([new(1, "개발", "#123456", 10, true)], [new(1, "editor", "Editor", @"D:\other.exe", 1, "user", null)]),
                    "BACKUP-HASH", "CURRENT-HASH", backupHasSettings: true);
                using var form = new KeepLaterRestorePreparationForm(plan, (UiLanguage)language);
                form.Show();
                Application.DoEvents();
                var choice = Assert.IsType<ComboBox>(Assert.Single(form.Controls.Find("restoreSettingsChoice", true)));
                var details = Assert.IsType<TextBox>(Assert.Single(form.Controls.Find("restorePreparationDetails", true)));
                Assert.True(details.ReadOnly);
                Assert.Contains(plan.PlanSha256, details.Text);
                choice.SelectedIndex = 1;
                Assert.Contains(plan.WithSettingsChoice(RestoreSettingsChoice.KeepCurrent).PlanSha256, details.Text);
                Assert.Contains(language == (int)UiLanguage.English ? "app path conflict" : "실행 경로 충돌", details.Text);
                choice.SelectedIndex = 2;
                Assert.Contains(language == (int)UiLanguage.English ? "backup settings unvalidated" : "검증 미완료", details.Text);
                Assert.Contains("settings-choice-required", plan.Blockers);
                var buttons = All(form).OfType<Button>().ToArray();
                Assert.Single(buttons);
                Assert.Equal("closePreparation", buttons[0].Name);
                AssertLayout(form);
                var directory = Environment.GetEnvironmentVariable("TIMEPILOT_TEST_SCREENSHOTS");
                if (!string.IsNullOrWhiteSpace(directory) && Path.IsPathFullyQualified(directory) && Directory.Exists(directory))
                {
                    using var bitmap = new Bitmap(form.Width, form.Height);
                    form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
                    bitmap.Save(Path.Combine(directory, $"restore-preparation-{language}.png"));
                }
                using var parent = new KeepLaterRestorePreviewForm(preview, (UiLanguage)language, plan);
                parent.Show();
                Assert.True(Assert.IsType<Button>(Assert.Single(parent.Controls.Find("inspectRestorePreparation", true))).Enabled);
            }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)));
        if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();
    }
    private static IEnumerable<Control> All(Control root)
    {
        foreach (Control control in root.Controls)
        {
            yield return control;
            foreach (var nested in All(control)) yield return nested;
        }
    }
    private static void AssertLayout(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (!child.Visible) continue;
            Assert.True(child.Left >= 0 && child.Top >= 0 && child.Right <= parent.ClientSize.Width + 1 && child.Bottom <= parent.ClientSize.Height + 1,
                $"{child.Name}: {child.Bounds} outside {parent.ClientSize}");
            AssertLayout(child);
        }
    }
}
