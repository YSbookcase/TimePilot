using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal sealed class CategoryColorPickerForm : Form
    {
        private static readonly string[] Palette =
        [
            "#2563EB", "#7C3AED", "#0891B2", "#DB2777", "#F59E0B",
            "#16A34A", "#DC2626", "#EA580C", "#475569", "#64748B",
            "#0F766E", "#4F46E5", "#9333EA", "#BE123C", "#B45309",
            "#15803D", "#0369A1", "#6D28D9", "#A21CAF", "#334155"
        ];

        private readonly UiLanguage language;
        private readonly IReadOnlyList<string> usedPaletteColors;
        private readonly IReadOnlyList<string> paletteColors;
        private readonly TextBox hexTextBox = new();
        private readonly Panel previewPanel = new();
        private readonly TrackBar redTrackBar = new();
        private readonly TrackBar greenTrackBar = new();
        private readonly TrackBar blueTrackBar = new();
        private readonly NumericUpDown redValueInput = new();
        private readonly NumericUpDown greenValueInput = new();
        private readonly NumericUpDown blueValueInput = new();
        private readonly Button windowsPaletteButton = new();
        private readonly Button okButton = new();
        private readonly Button cancelButton = new();
        private bool isUpdatingColorControls;

        public CategoryColorPickerForm(string? initialColor, UiLanguage language, IEnumerable<string?>? usedColors = null)
        {
            this.language = language;
            SelectedColorHex = NormalizeColorOrDefault(initialColor);
            usedPaletteColors = BuildDistinctColors(usedColors);
            paletteColors = BuildPaletteColors(SelectedColorHex, usedPaletteColors);
            InitializeComponent();
        }

        public string SelectedColorHex { get; private set; }

        private bool IsEnglish => language == UiLanguage.English;

        private void InitializeComponent()
        {
            var mainPanel = new TableLayoutPanel();
            var usedLabel = new Label();
            var usedPanel = new FlowLayoutPanel();
            var paletteLabel = new Label();
            var palettePanel = new FlowLayoutPanel();
            var inputPanel = new FlowLayoutPanel();
            var hexLabel = new Label();
            var hexHelpLabel = new Label();
            var slidersPanel = new TableLayoutPanel();
            var buttonPanel = new FlowLayoutPanel();

            SuspendLayout();

            Text = IsEnglish ? "Choose Color" : "색상 선택";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(420, 420);

            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Padding = new Padding(12);
            mainPanel.RowCount = 7;
            mainPanel.ColumnCount = 1;
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            usedLabel.Dock = DockStyle.Fill;
            usedLabel.Text = IsEnglish ? "Used colors (thick border)" : "사용 중인 색상 (두꺼운 테두리)";
            usedLabel.TextAlign = ContentAlignment.MiddleLeft;

            usedPanel.Dock = DockStyle.Fill;
            usedPanel.WrapContents = true;

            foreach (var color in usedPaletteColors)
                usedPanel.Controls.Add(CreateColorButton(color, isUsed: true));

            if (usedPaletteColors.Count == 0)
            {
                usedPanel.Controls.Add(new Label
                {
                    AutoSize = true,
                    ForeColor = SystemColors.GrayText,
                    Margin = new Padding(3, 8, 0, 0),
                    Text = IsEnglish ? "No saved colors yet." : "아직 저장된 색상이 없습니다."
                });
            }

            paletteLabel.Dock = DockStyle.Fill;
            paletteLabel.Text = IsEnglish ? "Suggested colors" : "추천 색상";
            paletteLabel.TextAlign = ContentAlignment.MiddleLeft;

            palettePanel.Dock = DockStyle.Fill;
            palettePanel.WrapContents = true;

            foreach (var color in paletteColors)
                palettePanel.Controls.Add(CreateColorButton(color, usedPaletteColors.Contains(color, StringComparer.OrdinalIgnoreCase)));

            hexLabel.AutoSize = true;
            hexLabel.Margin = new Padding(0, 8, 8, 0);
            hexLabel.Text = IsEnglish ? "Hex RGB" : "RGB 색상값";

            previewPanel.BorderStyle = BorderStyle.FixedSingle;
            previewPanel.Margin = new Padding(0, 5, 8, 0);
            previewPanel.Size = new Size(28, 24);

            hexTextBox.Width = 96;
            hexTextBox.Text = SelectedColorHex;
            hexTextBox.TextChanged += OnHexTextBoxTextChanged;

            hexHelpLabel.AutoSize = true;
            hexHelpLabel.ForeColor = SystemColors.GrayText;
            hexHelpLabel.Margin = new Padding(8, 8, 8, 0);
            hexHelpLabel.Text = IsEnglish ? "#RRGGBB format" : "#RRGGBB 형식";

            windowsPaletteButton.Text = IsEnglish ? "Windows palette..." : "Windows 색상표...";
            windowsPaletteButton.Width = 126;
            windowsPaletteButton.Click += OnWindowsPaletteButtonClick;

            inputPanel.Dock = DockStyle.Fill;
            inputPanel.Controls.Add(hexLabel);
            inputPanel.Controls.Add(previewPanel);
            inputPanel.Controls.Add(hexTextBox);
            inputPanel.Controls.Add(hexHelpLabel);

            slidersPanel.Dock = DockStyle.Fill;
            slidersPanel.ColumnCount = 3;
            slidersPanel.RowCount = 3;
            slidersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 24));
            slidersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            slidersPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
            slidersPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            slidersPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            slidersPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            AddSliderRow(slidersPanel, 0, "R", redTrackBar, redValueInput);
            AddSliderRow(slidersPanel, 1, "G", greenTrackBar, greenValueInput);
            AddSliderRow(slidersPanel, 2, "B", blueTrackBar, blueValueInput);

            okButton.Text = IsEnglish ? "OK" : "확인";
            okButton.Width = 84;
            okButton.Click += OnOkButtonClick;

            cancelButton.Text = IsEnglish ? "Cancel" : "취소";
            cancelButton.Width = 84;
            cancelButton.DialogResult = DialogResult.Cancel;

            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(okButton);
            buttonPanel.Controls.Add(windowsPaletteButton);

            mainPanel.Controls.Add(usedLabel, 0, 0);
            mainPanel.Controls.Add(usedPanel, 0, 1);
            mainPanel.Controls.Add(paletteLabel, 0, 2);
            mainPanel.Controls.Add(palettePanel, 0, 3);
            mainPanel.Controls.Add(inputPanel, 0, 4);
            mainPanel.Controls.Add(slidersPanel, 0, 5);
            mainPanel.Controls.Add(buttonPanel, 0, 6);

            AcceptButton = okButton;
            CancelButton = cancelButton;
            Controls.Add(mainPanel);

            UpdatePreviewAndSliders();
            ResumeLayout(false);
        }

        private Button CreateColorButton(string color, bool isUsed)
        {
            var button = new Button
            {
                BackColor = ParseColor(color),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(3),
                Size = new Size(28, 28),
                Tag = color,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderColor = isUsed ? Color.Black : SystemColors.ControlDark;
            button.FlatAppearance.BorderSize = isUsed ? 3 : 1;
            button.Click += (_, _) =>
            {
                hexTextBox.Text = color;
            };
            return button;
        }

        private void AddSliderRow(TableLayoutPanel panel, int rowIndex, string labelText, TrackBar trackBar, NumericUpDown valueInput)
        {
            var label = new Label
            {
                Dock = DockStyle.Fill,
                Text = labelText,
                TextAlign = ContentAlignment.MiddleLeft
            };

            trackBar.Dock = DockStyle.Fill;
            trackBar.Maximum = 255;
            trackBar.TickFrequency = 51;
            trackBar.TickStyle = TickStyle.None;
            trackBar.ValueChanged += OnRgbTrackBarValueChanged;

            valueInput.Dock = DockStyle.Fill;
            valueInput.Maximum = 255;
            valueInput.Minimum = 0;
            valueInput.TextAlign = HorizontalAlignment.Right;
            valueInput.ValueChanged += OnRgbValueInputChanged;

            panel.Controls.Add(label, 0, rowIndex);
            panel.Controls.Add(trackBar, 1, rowIndex);
            panel.Controls.Add(valueInput, 2, rowIndex);
        }

        private void OnOkButtonClick(object? sender, EventArgs e)
        {
            if (!TryParseColor(hexTextBox.Text, out var color))
            {
                hexTextBox.Focus();
                hexTextBox.SelectAll();
                return;
            }

            SelectedColorHex = ColorTranslator.ToHtml(Color.FromArgb(color.R, color.G, color.B));
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnHexTextBoxTextChanged(object? sender, EventArgs e)
        {
            if (isUpdatingColorControls)
                return;

            UpdatePreviewAndSliders();
        }

        private void OnRgbTrackBarValueChanged(object? sender, EventArgs e)
        {
            if (isUpdatingColorControls)
                return;

            isUpdatingColorControls = true;
            var color = Color.FromArgb(redTrackBar.Value, greenTrackBar.Value, blueTrackBar.Value);
            hexTextBox.Text = ColorTranslator.ToHtml(color);
            previewPanel.BackColor = color;
            UpdateRgbValueLabels();
            isUpdatingColorControls = false;
        }

        private void OnRgbValueInputChanged(object? sender, EventArgs e)
        {
            if (isUpdatingColorControls)
                return;

            isUpdatingColorControls = true;
            redTrackBar.Value = (int)redValueInput.Value;
            greenTrackBar.Value = (int)greenValueInput.Value;
            blueTrackBar.Value = (int)blueValueInput.Value;
            var color = Color.FromArgb(redTrackBar.Value, greenTrackBar.Value, blueTrackBar.Value);
            hexTextBox.Text = ColorTranslator.ToHtml(color);
            previewPanel.BackColor = color;
            isUpdatingColorControls = false;
        }

        private void OnWindowsPaletteButtonClick(object? sender, EventArgs e)
        {
            using var dialog = new ColorDialog
            {
                AllowFullOpen = true,
                AnyColor = true,
                FullOpen = true
            };

            if (TryParseColor(hexTextBox.Text, out var color))
                dialog.Color = color;

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            hexTextBox.Text = ColorTranslator.ToHtml(Color.FromArgb(dialog.Color.R, dialog.Color.G, dialog.Color.B));
        }

        private void UpdatePreviewAndSliders()
        {
            if (!TryParseColor(hexTextBox.Text, out var color))
            {
                previewPanel.BackColor = SystemColors.Control;
                return;
            }

            isUpdatingColorControls = true;
            previewPanel.BackColor = color;
            redTrackBar.Value = color.R;
            greenTrackBar.Value = color.G;
            blueTrackBar.Value = color.B;
            UpdateRgbValueInputs();
            isUpdatingColorControls = false;
        }

        private void UpdateRgbValueLabels()
        {
            UpdateRgbValueInputs();
        }

        private void UpdateRgbValueInputs()
        {
            redValueInput.Value = redTrackBar.Value;
            greenValueInput.Value = greenTrackBar.Value;
            blueValueInput.Value = blueTrackBar.Value;
        }

        private static string NormalizeColorOrDefault(string? value)
        {
            return TryParseColor(value, out var color)
                ? ColorTranslator.ToHtml(Color.FromArgb(color.R, color.G, color.B))
                : "#2563EB";
        }

        private static IReadOnlyList<string> BuildPaletteColors(string selectedColor, IReadOnlyList<string> usedColors)
        {
            var colors = new List<string>();
            AddColor(colors, selectedColor);

            foreach (var color in usedColors)
                AddColor(colors, color);

            foreach (var color in Palette)
                AddColor(colors, color);

            return colors;
        }

        private static IReadOnlyList<string> BuildDistinctColors(IEnumerable<string?>? colors)
        {
            var distinctColors = new List<string>();
            if (colors is null)
                return distinctColors;

            foreach (var color in colors)
                AddColor(distinctColors, color);

            return distinctColors;
        }

        private static void AddColor(ICollection<string> colors, string? value)
        {
            if (!TryParseColor(value, out var color))
                return;

            var normalized = ColorTranslator.ToHtml(Color.FromArgb(color.R, color.G, color.B));
            if (!colors.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                colors.Add(normalized);
        }

        private static Color ParseColor(string value)
        {
            return ColorTranslator.FromHtml(value);
        }

        private static bool TryParseColor(string? value, out Color color)
        {
            color = Color.Empty;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            try
            {
                color = ColorTranslator.FromHtml(value.Trim());
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
