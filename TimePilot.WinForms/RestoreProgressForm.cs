using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal sealed class RestoreProgressForm : Form
    {
        private readonly bool isEnglish;
        private readonly int totalSteps;
        private readonly Label titleLabel = new();
        private readonly Label statusLabel = new();
        private readonly TextBox detailTextBox = new();
        private readonly ProgressBar progressBar = new();
        private readonly Button closeButton = new();
        private readonly TaskCompletionSource completionSource = new();
        private bool isCompleted;

        public RestoreProgressForm(UiLanguage language, int totalSteps)
        {
            isEnglish = language == UiLanguage.English;
            this.totalSteps = Math.Max(1, totalSteps);
            InitializeComponent();
        }

        public Task WaitForCloseAsync() => completionSource.Task;

        public void SetStep(int step, string message)
        {
            var currentStep = Math.Clamp(step, 0, totalSteps);
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Maximum = totalSteps;
            progressBar.Value = currentStep;
            statusLabel.Text = isEnglish
                ? $"Step {currentStep} / {totalSteps}"
                : $"{currentStep} / {totalSteps} 단계";
            detailTextBox.Text = message;
        }

        public void ShowCompleted(string message)
        {
            isCompleted = true;
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Maximum = totalSteps;
            progressBar.Value = totalSteps;
            statusLabel.Text = isEnglish ? "Completed" : "완료";
            detailTextBox.Text = message;
            closeButton.Enabled = true;
            closeButton.Focus();
        }

        public void ShowFailed(string message)
        {
            isCompleted = true;
            progressBar.Style = ProgressBarStyle.Blocks;
            statusLabel.Text = isEnglish ? "Failed" : "실패";
            detailTextBox.Text = message;
            closeButton.Enabled = true;
            closeButton.Focus();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = isEnglish ? "TimePilot full restore" : "TimePilot 전체 복원";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            ClientSize = new Size(580, 290);

            titleLabel.Location = new Point(18, 18);
            titleLabel.Size = new Size(520, 26);
            titleLabel.Font = new Font(Font, FontStyle.Bold);
            titleLabel.Text = isEnglish ? "Restoring backup data..." : "백업 데이터를 복원하는 중입니다...";

            statusLabel.Location = new Point(18, 56);
            statusLabel.Size = new Size(520, 24);
            statusLabel.Text = isEnglish ? $"Step 0 / {totalSteps}" : $"0 / {totalSteps} 단계";

            progressBar.Location = new Point(18, 88);
            progressBar.Size = new Size(540, 24);
            progressBar.Minimum = 0;
            progressBar.Maximum = totalSteps;
            progressBar.Value = 0;

            detailTextBox.Location = new Point(18, 126);
            detailTextBox.Size = new Size(540, 114);
            detailTextBox.Multiline = true;
            detailTextBox.ReadOnly = true;
            detailTextBox.ScrollBars = ScrollBars.Vertical;
            detailTextBox.BorderStyle = BorderStyle.FixedSingle;
            detailTextBox.Text = isEnglish
                ? "The app will show each restore step here."
                : "복원 단계가 이곳에 표시됩니다.";

            closeButton.Text = isEnglish ? "OK" : "확인";
            closeButton.Location = new Point(478, 252);
            closeButton.Size = new Size(80, 28);
            closeButton.Enabled = false;
            closeButton.Click += (_, _) => Close();

            Controls.Add(titleLabel);
            Controls.Add(statusLabel);
            Controls.Add(progressBar);
            Controls.Add(detailTextBox);
            Controls.Add(closeButton);

            FormClosing += OnFormClosing;
            FormClosed += (_, _) => completionSource.TrySetResult();

            ResumeLayout(false);
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (!isCompleted)
                e.Cancel = true;
        }
    }
}
