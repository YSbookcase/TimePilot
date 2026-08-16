using System.Diagnostics;

namespace TimePilot.WinForms
{
    public partial class Form1
    {
        private string GetAppCategoryManagementMenuText()
        {
            return settings.UiLanguage == UiLanguage.English
                ? "App Categories..."
                : "앱 분류 관리...";
        }

        private void OnAppCategoryManagementMenuItemClick(
            object? sender,
            EventArgs e)
        {
            if (storage is null)
                return;

            using var form = new AppCategoryManagementForm(
                storage,
                settings,
                settings.UiLanguage);
            form.Icon = Icon;
            if (form.ShowDialog(this) == DialogResult.OK && form.CategoriesChanged)
            {
                InvalidateCategoryDependentViewCaches();
                RefreshViews(DateTimeOffset.UtcNow);
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
                BuildAboutMessage(),
                UiText.Main.AboutTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private string BuildAboutMessage()
        {
            var isEnglish = settings.UiLanguage == UiLanguage.English;
            return string.Join(
                Environment.NewLine,
                "TimePilot " + Application.ProductVersion,
                string.Empty,
                UiText.Main.SponsorAboutMessage,
                string.Empty,
                isEnglish ? "Website: " + ExternalLinks.HomeUrl : "공식 페이지: " + ExternalLinks.HomeUrl,
                isEnglish ? "Support: " + ExternalLinks.SupportUrl : "지원: " + ExternalLinks.SupportUrl,
                isEnglish ? "Privacy policy: " + ExternalLinks.PrivacyPolicyUrl : "개인정보처리방침: " + ExternalLinks.PrivacyPolicyUrl,
                isEnglish ? "Email: " + ExternalLinks.SupportEmail : "이메일: " + ExternalLinks.SupportEmail);
        }

        private void OnSponsorMenuItemClick(object? sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(ExternalLinks.SponsorUrl)
                {
                    UseShellExecute = true
                });
                SetStatusText(UiText.Main.SponsorOpened);
            }
            catch (Exception ex)
            {
                CenteredMessageDialog.Show(
                    this,
                    UiText.Main.SponsorOpenFailed(ex.Message),
                    UiText.Main.Sponsor,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void OnRuntimeDiagnosticsMenuItemClick(object? sender, EventArgs e)
        {
            if (storage is null)
                return;

            var sessions = storage.GetRecentRuntimeSessionDiagnostics(10);
            var systemEvents = storage.GetRecentSystemEventDiagnostics(5);
            var message =
                RuntimeDiagnosticsMessageBuilder.BuildMessage(sessions, systemEvents);
            CenteredMessageDialog.Show(
                this,
                message,
                UiText.Main.RuntimeDiagnosticsTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
