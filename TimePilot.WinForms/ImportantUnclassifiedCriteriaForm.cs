using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal sealed class ImportantUnclassifiedCriteriaForm : Form
    {
        private readonly UiLanguage language;
        private readonly ComboBox activeMinutesComboBox = new();
        private readonly ComboBox switchCountComboBox = new();
        private readonly CheckBox includeRecommendationsCheckBox = new();
        private readonly CheckBox visibleAppsOnlyCheckBox = new();
        private readonly CheckBox excludeBackgroundOnlyCheckBox = new();
        private readonly Button resetButton = new();
        private readonly Button okButton = new();
        private readonly Button cancelButton = new();

        public ImportantUnclassifiedCriteriaForm(AppSettings settings, UiLanguage language)
        {
            this.language = language;
            InitializeComponent();
            SetValues(
                settings.ImportantUnclassifiedActiveMinutes,
                settings.ImportantUnclassifiedSwitchCount,
                settings.ImportantUnclassifiedIncludeRecommendations,
                settings.ImportantUnclassifiedVisibleAppsOnly,
                settings.ImportantUnclassifiedExcludeBackgroundOnly);
        }

        public int ActiveMinutes => (activeMinutesComboBox.SelectedItem as CriteriaOption)?.Value
            ?? AppSettings.DefaultImportantUnclassifiedActiveMinutes;

        public int SwitchCount => (switchCountComboBox.SelectedItem as CriteriaOption)?.Value
            ?? AppSettings.DefaultImportantUnclassifiedSwitchCount;

        public bool IncludeRecommendations => includeRecommendationsCheckBox.Checked;

        public bool VisibleAppsOnly => visibleAppsOnlyCheckBox.Checked;

        public bool ExcludeBackgroundOnly => excludeBackgroundOnlyCheckBox.Checked;

        private bool IsEnglish => language == UiLanguage.English;

        private void InitializeComponent()
        {
            var root = new TableLayoutPanel();
            var activeLabel = new Label();
            var switchLabel = new Label();
            var descriptionLabel = new Label();
            var buttonPanel = new FlowLayoutPanel();

            SuspendLayout();

            Text = IsEnglish ? "Important Criteria" : "중요 미분류 기준";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(420, 292);

            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(16);
            root.ColumnCount = 2;
            root.RowCount = 7;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            descriptionLabel.AutoSize = false;
            descriptionLabel.Dock = DockStyle.Fill;
            descriptionLabel.ForeColor = SystemColors.GrayText;
            descriptionLabel.Text = IsEnglish
                ? "Controls which unclassified apps are highlighted as important."
                : "미분류 앱 중 어떤 항목을 중요하게 볼지 정합니다.";
            root.Controls.Add(descriptionLabel, 0, 0);
            root.SetColumnSpan(descriptionLabel, 2);

            activeLabel.AutoSize = true;
            activeLabel.TextAlign = ContentAlignment.MiddleLeft;
            activeLabel.Dock = DockStyle.Fill;
            activeLabel.Text = IsEnglish ? "Active time" : "활성 사용";
            root.Controls.Add(activeLabel, 0, 1);

            activeMinutesComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            activeMinutesComboBox.Dock = DockStyle.Fill;
            activeMinutesComboBox.Items.AddRange(new object[]
            {
                new CriteriaOption(1, IsEnglish ? "1 minute" : "1분"),
                new CriteriaOption(5, IsEnglish ? "5 minutes (default)" : "5분 (기본)"),
                new CriteriaOption(10, IsEnglish ? "10 minutes" : "10분"),
                new CriteriaOption(30, IsEnglish ? "30 minutes" : "30분")
            });
            root.Controls.Add(activeMinutesComboBox, 1, 1);

            switchLabel.AutoSize = true;
            switchLabel.TextAlign = ContentAlignment.MiddleLeft;
            switchLabel.Dock = DockStyle.Fill;
            switchLabel.Text = IsEnglish ? "Switch count" : "전환 횟수";
            root.Controls.Add(switchLabel, 0, 2);

            switchCountComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            switchCountComboBox.Dock = DockStyle.Fill;
            switchCountComboBox.Items.AddRange(new object[]
            {
                new CriteriaOption(1, IsEnglish ? "1 switch" : "1회"),
                new CriteriaOption(3, IsEnglish ? "3 switches (default)" : "3회 (기본)"),
                new CriteriaOption(5, IsEnglish ? "5 switches" : "5회"),
                new CriteriaOption(10, IsEnglish ? "10 switches" : "10회")
            });
            root.Controls.Add(switchCountComboBox, 1, 2);

            includeRecommendationsCheckBox.AutoSize = true;
            includeRecommendationsCheckBox.Text = IsEnglish
                ? "Treat apps with suggestions as important"
                : "추천 분류가 있으면 중요 항목으로 보기";
            root.Controls.Add(includeRecommendationsCheckBox, 0, 3);
            root.SetColumnSpan(includeRecommendationsCheckBox, 2);

            visibleAppsOnlyCheckBox.AutoSize = true;
            visibleAppsOnlyCheckBox.Text = IsEnglish
                ? "Only count visible/user-used apps"
                : "화면에 보였거나 사용한 앱만 기준에 포함";
            root.Controls.Add(visibleAppsOnlyCheckBox, 0, 4);
            root.SetColumnSpan(visibleAppsOnlyCheckBox, 2);

            excludeBackgroundOnlyCheckBox.AutoSize = true;
            excludeBackgroundOnlyCheckBox.Text = IsEnglish
                ? "Exclude background-only processes"
                : "백그라운드로만 관측된 프로세스 제외";
            root.Controls.Add(excludeBackgroundOnlyCheckBox, 0, 5);
            root.SetColumnSpan(excludeBackgroundOnlyCheckBox, 2);

            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonPanel.Padding = new Padding(0, 10, 0, 0);

            okButton.Text = IsEnglish ? "Apply" : "적용";
            okButton.Width = 84;
            okButton.DialogResult = DialogResult.OK;

            cancelButton.Text = IsEnglish ? "Cancel" : "취소";
            cancelButton.Width = 84;
            cancelButton.DialogResult = DialogResult.Cancel;

            resetButton.Text = IsEnglish ? "Defaults" : "기본값";
            resetButton.Width = 84;
            resetButton.Click += (_, _) => SetValues(
                AppSettings.DefaultImportantUnclassifiedActiveMinutes,
                AppSettings.DefaultImportantUnclassifiedSwitchCount,
                AppSettings.DefaultImportantUnclassifiedIncludeRecommendations,
                AppSettings.DefaultImportantUnclassifiedVisibleAppsOnly,
                AppSettings.DefaultImportantUnclassifiedExcludeBackgroundOnly);

            buttonPanel.Controls.Add(okButton);
            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(resetButton);
            root.Controls.Add(buttonPanel, 0, 6);
            root.SetColumnSpan(buttonPanel, 2);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            Controls.Add(root);

            ResumeLayout(false);
        }

        private void SetValues(
            int activeMinutes,
            int switchCount,
            bool includeRecommendations,
            bool visibleAppsOnly,
            bool excludeBackgroundOnly)
        {
            SelectOption(activeMinutesComboBox, activeMinutes);
            SelectOption(switchCountComboBox, switchCount);
            includeRecommendationsCheckBox.Checked = includeRecommendations;
            visibleAppsOnlyCheckBox.Checked = visibleAppsOnly;
            excludeBackgroundOnlyCheckBox.Checked = excludeBackgroundOnly;
        }

        private static void SelectOption(ComboBox comboBox, int value)
        {
            var option = comboBox.Items.Cast<CriteriaOption>().FirstOrDefault(item => item.Value == value)
                ?? comboBox.Items.Cast<CriteriaOption>().First();
            comboBox.SelectedItem = option;
        }

        private sealed record CriteriaOption(int Value, string Text)
        {
            public override string ToString()
            {
                return Text;
            }
        }
    }
}
