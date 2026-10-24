using System.Runtime.ExceptionServices;
using TimePilot.WinForms;
using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class PreferencesFormLayoutTests
    {
        [Fact]
        public void PreferencesForm_KeepsGeneralAndBackupControlsInsideTabs()
        {
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                var root = Path.Combine(Path.GetTempPath(), $"TimePilotPreferences-{Guid.NewGuid():N}");
                try
                {
                    var settings = AppSettings.Load(Path.Combine(root, "settings.json"));
                    settings.SetAutomaticBackup(true, Path.Combine(root, "external"), 14);
                    using var form = new PreferencesForm(settings);
                    form.Show();
                    Application.DoEvents();

                    var tabControl = Assert.Single(form.Controls.OfType<TabControl>());
                    Assert.Equal(2, tabControl.TabPages.Count);
                    Assert.Equal(new Size(470, 565), form.ClientSize);

                    var generalTab = tabControl.TabPages[0];
                    var dataTab = tabControl.TabPages[1];
                    tabControl.SelectedTab = generalTab;
                    _ = generalTab.Handle;
                    tabControl.PerformLayout();
                    Assert.Single(generalTab.Controls.OfType<GroupBox>());
                    Assert.Equal(2, dataTab.Controls.OfType<GroupBox>().Count());
                    AssertControlsFit(generalTab);
                    var suffixInput = Assert.IsType<TextBox>(
                        Assert.Single(generalTab.Controls.Find("koreanWebSearchSuffixTextBox", false)));
                    Assert.Equal(settings.KoreanWebSearchSuffix, suffixInput.Text);
                    tabControl.SelectedTab = dataTab;
                    _ = dataTab.Handle;
                    tabControl.PerformLayout();
                    AssertControlsFit(dataTab);
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

        [Theory]
        [InlineData(" 사용법 ", "사용법")]
        [InlineData("", "")]
        public void PreferencesForm_SaveReturnsEditedSearchSuffix(string editedText, string expected)
        {
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                var root = Path.Combine(Path.GetTempPath(), $"TimePilotPreferences-{Guid.NewGuid():N}");
                try
                {
                    var settings = AppSettings.Load(Path.Combine(root, "settings.json"));
                    using var form = new PreferencesForm(settings);
                    form.Show();
                    Application.DoEvents();
                    var input = Assert.IsType<TextBox>(
                        Assert.Single(form.Controls.Find("koreanWebSearchSuffixTextBox", true)));
                    input.Text = editedText;
                    var saveButton = Assert.IsType<Button>(
                        Assert.Single(form.Controls.Find("okButton", true)));
                    saveButton.PerformClick();

                    Assert.Equal(DialogResult.OK, form.DialogResult);
                    Assert.Equal(expected, form.KoreanWebSearchSuffix);
                    Assert.Equal("이란", settings.KoreanWebSearchSuffix);
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

        private static void AssertControlsFit(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                Assert.True(control.Left >= 0, $"{control.Name} extends past the left edge.");
                Assert.True(control.Top >= 0, $"{control.Name} extends past the top edge.");
                Assert.True(
                    control.Right <= parent.ClientSize.Width,
                    $"{control.Name} extends past the right edge ({control.Right} > {parent.ClientSize.Width}).");
                Assert.True(
                    control.Bottom <= parent.ClientSize.Height,
                    $"{control.Name} extends past the bottom edge ({control.Bottom} > {parent.ClientSize.Height}).");
            }
        }
    }
}
