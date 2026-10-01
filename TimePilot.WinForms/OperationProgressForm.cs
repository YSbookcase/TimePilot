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
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Text = status,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var progressBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 8, 0, 0),
                MarqueeAnimationSpeed = 28,
                Style = ProgressBarStyle.Marquee
            };

            var contentLayout = new TableLayoutPanel
            {
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 16, 20, 22),
                RowCount = 2
            };
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));
            contentLayout.Controls.Add(statusLabel, 0, 0);
            contentLayout.Controls.Add(progressBar, 0, 1);

            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(460, 126);
            ControlBox = false;
            Controls.Add(contentLayout);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(460, 154);
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
