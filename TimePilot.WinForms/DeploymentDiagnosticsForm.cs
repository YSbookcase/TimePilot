using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal sealed class DeploymentDiagnosticsForm : Form
    {
        private readonly TextBox detailsTextBox = new();
        private readonly Button copyButton = new();
        private readonly Button closeButton = new();

        public DeploymentDiagnosticsForm(DeploymentDiagnosticsSnapshot snapshot, UiLanguage language)
        {
            detailsTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            detailsTextBox.BackColor = SystemColors.Window;
            detailsTextBox.Font = new Font(FontFamily.GenericMonospace, 9F);
            detailsTextBox.Location = new Point(12, 12);
            detailsTextBox.Multiline = true;
            detailsTextBox.ReadOnly = true;
            detailsTextBox.ScrollBars = ScrollBars.Both;
            detailsTextBox.Size = new Size(736, 377);
            detailsTextBox.Text = DeploymentDiagnosticsFormatter.Format(snapshot, language);
            detailsTextBox.WordWrap = false;

            copyButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            copyButton.Location = new Point(584, 401);
            copyButton.Size = new Size(78, 29);
            copyButton.Text = UiText.Preferences.CopyInstallationInfo;
            copyButton.UseVisualStyleBackColor = true;
            copyButton.Click += (_, _) => TryCopyDetails();

            closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            closeButton.DialogResult = DialogResult.OK;
            closeButton.Location = new Point(670, 401);
            closeButton.Size = new Size(78, 29);
            closeButton.Text = UiText.Common.Ok;
            closeButton.UseVisualStyleBackColor = true;

            AcceptButton = closeButton;
            CancelButton = closeButton;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(760, 442);
            Controls.Add(detailsTextBox);
            Controls.Add(copyButton);
            Controls.Add(closeButton);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            MinimumSize = new Size(620, 360);
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = UiText.Preferences.InstallationInfoTitle;
        }

        private void TryCopyDetails()
        {
            try
            {
                Clipboard.SetText(detailsTextBox.Text);
            }
            catch (Exception ex)
            {
                CenteredMessageDialog.Show(
                    this,
                    UiText.Preferences.InstallationInfoCopyFailed(ex.Message),
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
