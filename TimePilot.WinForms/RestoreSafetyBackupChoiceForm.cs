using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal sealed class RestoreSafetyBackupChoiceForm : Form
    {
        private readonly bool isEnglish;
        private readonly Button createBackupButton = new();
        private readonly Button restoreWithoutBackupButton = new();
        private readonly Button cancelButton = new();

        public RestoreSafetyBackupChoiceForm(UiLanguage language)
        {
            isEnglish = language == UiLanguage.English;
            InitializeComponent();
        }

        public RestoreSafetyBackupChoice Choice { get; private set; } = RestoreSafetyBackupChoice.Cancel;

        private void InitializeComponent()
        {
            var iconBox = new PictureBox();
            var messageLabel = new Label();
            var buttonPanel = new FlowLayoutPanel();

            SuspendLayout();

            Text = isEnglish ? "ActiveLogbook full restore" : "ActiveLogbook 전체 복원";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            ClientSize = new Size(520, 210);

            iconBox.Location = new Point(18, 22);
            iconBox.Size = new Size(32, 32);
            iconBox.SizeMode = PictureBoxSizeMode.CenterImage;
            iconBox.Image = SystemIcons.Warning.ToBitmap();

            messageLabel.Location = new Point(66, 18);
            messageLabel.Size = new Size(440, 100);
            messageLabel.Text = isEnglish
                ? "Create a safety backup before restore?\n\nA safety backup lets you return to the current state before applying the restore. Creating it can take time if you have a large database."
                : "복원 전에 현재 데이터를 안전 백업으로 저장할까요?\n\n안전 백업을 만들면 복원 직전 상태로 되돌릴 수 있습니다. 데이터가 많으면 백업 생성에 시간이 걸릴 수 있습니다.";

            buttonPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonPanel.Location = new Point(18, 140);
            buttonPanel.Size = new Size(500, 40);

            createBackupButton.Text = isEnglish ? "Create backup and restore" : "안전 백업 만들고 복원";
            createBackupButton.AutoSize = true;
            createBackupButton.DialogResult = DialogResult.OK;
            createBackupButton.Click += (_, _) => Choice = RestoreSafetyBackupChoice.CreateSafetyBackup;

            restoreWithoutBackupButton.Text = isEnglish ? "Restore without backup" : "백업 없이 복원";
            restoreWithoutBackupButton.AutoSize = true;
            restoreWithoutBackupButton.DialogResult = DialogResult.OK;
            restoreWithoutBackupButton.Click += (_, _) => Choice = RestoreSafetyBackupChoice.SkipSafetyBackup;

            cancelButton.Text = isEnglish ? "Cancel" : "취소";
            cancelButton.Width = 84;
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Click += (_, _) => Choice = RestoreSafetyBackupChoice.Cancel;

            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(restoreWithoutBackupButton);
            buttonPanel.Controls.Add(createBackupButton);

            AcceptButton = createBackupButton;
            CancelButton = cancelButton;
            Controls.Add(iconBox);
            Controls.Add(messageLabel);
            Controls.Add(buttonPanel);

            ResumeLayout(false);
        }
    }

    internal enum RestoreSafetyBackupChoice
    {
        Cancel,
        CreateSafetyBackup,
        SkipSafetyBackup
    }
}
