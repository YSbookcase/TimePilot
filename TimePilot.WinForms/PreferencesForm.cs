using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal sealed class PreferencesForm : Form
    {
        private static readonly IReadOnlyList<IdleThresholdOption> IdleThresholdOptions =
        [
            new("1분", 1),
            new("2분", 2),
            new("5분", 5),
            new("10분", 10),
            new("15분", 15),
            new("사용자 지정", null)
        ];

        private readonly ComboBox idleThresholdComboBox = new();
        private readonly NumericUpDown customIdleThresholdNumeric = new();
        private readonly Label customIdleThresholdUnitLabel = new();
        private readonly Button okButton = new();
        private readonly Button cancelButton = new();

        public PreferencesForm(int idleThresholdMinutes)
        {
            IdleThresholdMinutes = Math.Clamp(
                idleThresholdMinutes,
                AppSettings.MinIdleThresholdMinutes,
                AppSettings.MaxIdleThresholdMinutes);

            InitializeComponent();
            ConfigureIdleThresholdControls();
        }

        public int IdleThresholdMinutes { get; private set; }

        private void InitializeComponent()
        {
            var idleThresholdLabel = new Label();

            SuspendLayout();

            idleThresholdLabel.AutoSize = true;
            idleThresholdLabel.Location = new Point(20, 22);
            idleThresholdLabel.Name = "idleThresholdLabel";
            idleThresholdLabel.Size = new Size(126, 15);
            idleThresholdLabel.Text = "유휴 판단 대기시간";

            idleThresholdComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            idleThresholdComboBox.FormattingEnabled = true;
            idleThresholdComboBox.Location = new Point(20, 48);
            idleThresholdComboBox.Name = "idleThresholdComboBox";
            idleThresholdComboBox.Size = new Size(160, 23);
            idleThresholdComboBox.SelectedIndexChanged += OnIdleThresholdSelectionChanged;

            customIdleThresholdNumeric.Location = new Point(196, 48);
            customIdleThresholdNumeric.Maximum = AppSettings.MaxIdleThresholdMinutes;
            customIdleThresholdNumeric.Minimum = AppSettings.MinIdleThresholdMinutes;
            customIdleThresholdNumeric.Name = "customIdleThresholdNumeric";
            customIdleThresholdNumeric.Size = new Size(72, 23);

            customIdleThresholdUnitLabel.AutoSize = true;
            customIdleThresholdUnitLabel.Location = new Point(274, 52);
            customIdleThresholdUnitLabel.Name = "customIdleThresholdUnitLabel";
            customIdleThresholdUnitLabel.Size = new Size(19, 15);
            customIdleThresholdUnitLabel.Text = "분";

            okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            okButton.DialogResult = DialogResult.OK;
            okButton.Location = new Point(171, 110);
            okButton.Name = "okButton";
            okButton.Size = new Size(75, 27);
            okButton.Text = "저장";
            okButton.Click += OnOkButtonClick;

            cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(252, 110);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(75, 27);
            cancelButton.Text = "취소";

            AcceptButton = okButton;
            CancelButton = cancelButton;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(347, 157);
            Controls.Add(idleThresholdLabel);
            Controls.Add(idleThresholdComboBox);
            Controls.Add(customIdleThresholdNumeric);
            Controls.Add(customIdleThresholdUnitLabel);
            Controls.Add(okButton);
            Controls.Add(cancelButton);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PreferencesForm";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "환경 설정";

            ResumeLayout(false);
            PerformLayout();
        }

        private void ConfigureIdleThresholdControls()
        {
            idleThresholdComboBox.DataSource = IdleThresholdOptions.ToList();
            idleThresholdComboBox.DisplayMember = nameof(IdleThresholdOption.Label);
            idleThresholdComboBox.ValueMember = nameof(IdleThresholdOption.Minutes);
            customIdleThresholdNumeric.Value = IdleThresholdMinutes;

            var selectedOption = IdleThresholdOptions.FirstOrDefault(option => option.Minutes == IdleThresholdMinutes)
                ?? IdleThresholdOptions.First(option => option.Minutes is null);
            idleThresholdComboBox.SelectedItem = selectedOption;
            UpdateCustomIdleThresholdVisibility();
        }

        private void OnIdleThresholdSelectionChanged(object? sender, EventArgs e)
        {
            UpdateCustomIdleThresholdVisibility();
        }

        private void OnOkButtonClick(object? sender, EventArgs e)
        {
            if (idleThresholdComboBox.SelectedItem is IdleThresholdOption { Minutes: { } minutes })
            {
                IdleThresholdMinutes = minutes;
                return;
            }

            IdleThresholdMinutes = (int)customIdleThresholdNumeric.Value;
        }

        private void UpdateCustomIdleThresholdVisibility()
        {
            var isCustom = idleThresholdComboBox.SelectedItem is IdleThresholdOption { Minutes: null };
            customIdleThresholdNumeric.Visible = isCustom;
            customIdleThresholdUnitLabel.Visible = isCustom;
        }

        private sealed record IdleThresholdOption(string Label, int? Minutes);
    }
}
