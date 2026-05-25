using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal sealed class CenteredMessageDialog : Form
    {
        private CenteredMessageDialog(
            string title,
            string message,
            MessageBoxButtons buttons,
            MessageBoxIcon icon)
        {
            var iconBox = new PictureBox();
            var messageLabel = new Label();
            var primaryButton = new Button();
            var secondaryButton = new Button();
            const int messageWidth = 344;
            const int minMessageHeight = 82;
            const int maxMessageHeight = 360;
            var measuredMessage = TextRenderer.MeasureText(
                message,
                messageLabel.Font,
                new Size(messageWidth, int.MaxValue),
                TextFormatFlags.WordBreak);
            var messageHeight = Math.Clamp(measuredMessage.Height + 8, minMessageHeight, maxMessageHeight);
            var buttonTop = 20 + messageHeight + 16;

            SuspendLayout();

            iconBox.Location = new Point(20, 22);
            iconBox.Name = "iconBox";
            iconBox.Size = new Size(32, 32);
            iconBox.SizeMode = PictureBoxSizeMode.CenterImage;
            iconBox.Image = GetIconBitmap(icon);

            messageLabel.Location = new Point(68, 20);
            messageLabel.Name = "messageLabel";
            messageLabel.Size = new Size(messageWidth, messageHeight);
            messageLabel.Text = message;

            primaryButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            primaryButton.Location = new Point(256, buttonTop);
            primaryButton.Name = "primaryButton";
            primaryButton.Size = new Size(75, 27);

            secondaryButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            secondaryButton.Location = new Point(337, buttonTop);
            secondaryButton.Name = "secondaryButton";
            secondaryButton.Size = new Size(75, 27);

            if (buttons == MessageBoxButtons.YesNo)
            {
                primaryButton.DialogResult = DialogResult.Yes;
                primaryButton.Text = UiText.Common.Yes;
                secondaryButton.DialogResult = DialogResult.No;
                secondaryButton.Text = UiText.Common.No;
                CancelButton = secondaryButton;
                Controls.Add(secondaryButton);
            }
            else if (buttons == MessageBoxButtons.OKCancel)
            {
                primaryButton.DialogResult = DialogResult.OK;
                primaryButton.Text = UiText.Common.Ok;
                secondaryButton.DialogResult = DialogResult.Cancel;
                secondaryButton.Text = UiText.Common.Cancel;
                CancelButton = secondaryButton;
                Controls.Add(secondaryButton);
            }
            else
            {
                primaryButton.DialogResult = DialogResult.OK;
                primaryButton.Text = UiText.Common.Ok;
                primaryButton.Location = secondaryButton.Location;
                CancelButton = primaryButton;
            }

            AcceptButton = primaryButton;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(432, buttonTop + 47);
            Controls.Add(iconBox);
            Controls.Add(messageLabel);
            Controls.Add(primaryButton);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "CenteredMessageDialog";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = title;

            ResumeLayout(false);
        }

        public static DialogResult Show(
            IWin32Window owner,
            string message,
            string title,
            MessageBoxButtons buttons,
            MessageBoxIcon icon)
        {
            using var dialog = new CenteredMessageDialog(title, message, buttons, icon);
            return dialog.ShowDialog(owner);
        }

        private static Bitmap? GetIconBitmap(MessageBoxIcon icon)
        {
            return icon switch
            {
                MessageBoxIcon.Question => SystemIcons.Question.ToBitmap(),
                MessageBoxIcon.Warning => SystemIcons.Warning.ToBitmap(),
                MessageBoxIcon.Error => SystemIcons.Error.ToBitmap(),
                MessageBoxIcon.Information => SystemIcons.Information.ToBitmap(),
                _ => null
            };
        }
    }
}
