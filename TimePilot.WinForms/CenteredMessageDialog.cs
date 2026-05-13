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

            SuspendLayout();

            iconBox.Location = new Point(20, 22);
            iconBox.Name = "iconBox";
            iconBox.Size = new Size(32, 32);
            iconBox.SizeMode = PictureBoxSizeMode.CenterImage;
            iconBox.Image = GetIconBitmap(icon);

            messageLabel.Location = new Point(68, 20);
            messageLabel.Name = "messageLabel";
            messageLabel.Size = new Size(344, 82);
            messageLabel.Text = message;

            primaryButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            primaryButton.Location = new Point(256, 118);
            primaryButton.Name = "primaryButton";
            primaryButton.Size = new Size(75, 27);

            secondaryButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            secondaryButton.Location = new Point(337, 118);
            secondaryButton.Name = "secondaryButton";
            secondaryButton.Size = new Size(75, 27);

            if (buttons == MessageBoxButtons.YesNo)
            {
                primaryButton.DialogResult = DialogResult.Yes;
                primaryButton.Text = "예";
                secondaryButton.DialogResult = DialogResult.No;
                secondaryButton.Text = "아니오";
                CancelButton = secondaryButton;
                Controls.Add(secondaryButton);
            }
            else
            {
                primaryButton.DialogResult = DialogResult.OK;
                primaryButton.Text = "확인";
                primaryButton.Location = secondaryButton.Location;
                CancelButton = primaryButton;
            }

            AcceptButton = primaryButton;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(432, 165);
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
