namespace TimePilot.WinForms
{
    internal sealed class OperationProgressForm : Form
    {
        private readonly Label statusLabel;

        public OperationProgressForm(string title, string status)
        {
            statusLabel = new Label
            {
                AutoEllipsis = true,
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(0, 4, 0, 0),
                Text = status,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var progressBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 18,
                MarqueeAnimationSpeed = 28,
                Style = ProgressBarStyle.Marquee
            };

            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 14, 20, 18)
            };
            contentPanel.Controls.Add(progressBar);
            contentPanel.Controls.Add(statusLabel);

            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(440, 98);
            ControlBox = false;
            Controls.Add(contentPanel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = nameof(OperationProgressForm);
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = title;
        }

        public void ShowCentered(Form owner)
        {
            var ownerBounds = owner.WindowState == FormWindowState.Minimized
                ? Screen.FromControl(owner).WorkingArea
                : owner.Bounds;
            var workingArea = Screen.FromRectangle(ownerBounds).WorkingArea;
            var left = ownerBounds.Left + (ownerBounds.Width - Width) / 2;
            var top = ownerBounds.Top + (ownerBounds.Height - Height) / 2;
            left = Math.Clamp(left, workingArea.Left, workingArea.Right - Width);
            top = Math.Clamp(top, workingArea.Top, workingArea.Bottom - Height);
            Location = new Point(left, top);
            Show(owner);
        }

        public void SetStatus(string status)
        {
            statusLabel.Text = status;
            statusLabel.Refresh();
        }
    }
}
