using Microsoft.Data.Sqlite;
using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal sealed class AppCategoryEditorForm : Form
    {
        private static readonly IReadOnlyList<CategoryColorOption> ColorOptions =
        [
            new("#2563EB", "Blue"),
            new("#7C3AED", "Purple"),
            new("#0891B2", "Cyan"),
            new("#DB2777", "Pink"),
            new("#F59E0B", "Amber"),
            new("#16A34A", "Green"),
            new("#DC2626", "Red"),
            new("#EA580C", "Orange"),
            new("#475569", "Slate"),
            new("#64748B", "Gray")
        ];

        private readonly TimePilotStorage storage;
        private readonly UiLanguage language;
        private readonly DataGridView categoriesGrid = new();
        private readonly BindingSource categoriesBindingSource = new();
        private readonly TextBox nameTextBox = new();
        private readonly ComboBox colorComboBox = new();
        private readonly Button addButton = new();
        private readonly Button updateButton = new();
        private readonly Button deleteButton = new();
        private readonly Button closeButton = new();
        private readonly Label statusLabel = new();
        private IReadOnlyList<AppCategoryEditorRow> rows = Array.Empty<AppCategoryEditorRow>();

        public AppCategoryEditorForm(TimePilotStorage storage, UiLanguage language)
        {
            this.storage = storage;
            this.language = language;

            InitializeComponent();
            LoadRows();
        }

        public bool CategoriesChanged { get; private set; }

        private bool IsEnglish => language == UiLanguage.English;

        private void InitializeComponent()
        {
            var topPanel = new FlowLayoutPanel();
            var nameLabel = new Label();
            var colorLabel = new Label();
            var bottomPanel = new FlowLayoutPanel();

            SuspendLayout();

            Text = IsEnglish ? "Manage Categories" : "분류 관리";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(620, 420);
            Size = new Size(720, 520);

            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 74;
            topPanel.Padding = new Padding(12, 10, 12, 6);
            topPanel.WrapContents = true;

            nameLabel.AutoSize = true;
            nameLabel.Margin = new Padding(0, 5, 6, 0);
            nameLabel.Text = IsEnglish ? "Name" : "이름";

            nameTextBox.Width = 180;

            colorLabel.AutoSize = true;
            colorLabel.Margin = new Padding(12, 5, 6, 0);
            colorLabel.Text = IsEnglish ? "Color" : "색상";

            colorComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            colorComboBox.Width = 120;
            colorComboBox.Items.AddRange(ColorOptions.Cast<object>().ToArray());
            colorComboBox.SelectedIndex = 0;

            addButton.Text = IsEnglish ? "Add" : "추가";
            addButton.Width = 72;
            addButton.Click += OnAddButtonClick;

            updateButton.Text = IsEnglish ? "Update" : "수정";
            updateButton.Width = 72;
            updateButton.Click += OnUpdateButtonClick;

            deleteButton.Text = IsEnglish ? "Delete" : "삭제";
            deleteButton.Width = 72;
            deleteButton.Click += OnDeleteButtonClick;

            statusLabel.AutoSize = true;
            statusLabel.ForeColor = SystemColors.GrayText;
            statusLabel.Margin = new Padding(0, 7, 0, 0);
            statusLabel.Text = IsEnglish
                ? "Built-in categories cannot be edited. Custom categories are used as one main category per app."
                : "기본 분류는 수정할 수 없습니다. 사용자 분류는 앱당 하나의 대표 분류로 사용됩니다.";

            topPanel.Controls.Add(nameLabel);
            topPanel.Controls.Add(nameTextBox);
            topPanel.Controls.Add(colorLabel);
            topPanel.Controls.Add(colorComboBox);
            topPanel.Controls.Add(addButton);
            topPanel.Controls.Add(updateButton);
            topPanel.Controls.Add(deleteButton);
            topPanel.Controls.Add(statusLabel);

            categoriesGrid.AllowUserToAddRows = false;
            categoriesGrid.AllowUserToDeleteRows = false;
            categoriesGrid.AllowUserToResizeRows = false;
            categoriesGrid.AutoGenerateColumns = false;
            categoriesGrid.BackgroundColor = SystemColors.Window;
            categoriesGrid.BorderStyle = BorderStyle.None;
            categoriesGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            categoriesGrid.Dock = DockStyle.Fill;
            categoriesGrid.MultiSelect = false;
            categoriesGrid.ReadOnly = true;
            categoriesGrid.RowHeadersVisible = false;
            categoriesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            categoriesGrid.DataSource = categoriesBindingSource;
            categoriesGrid.SelectionChanged += OnCategoriesGridSelectionChanged;
            categoriesGrid.Columns.AddRange(
                CreateTextColumn(nameof(AppCategoryEditorRow.DisplayName), IsEnglish ? "Category" : "분류", 180),
                CreateTextColumn(nameof(AppCategoryEditorRow.ColorText), IsEnglish ? "Color" : "색상", 90),
                CreateTextColumn(nameof(AppCategoryEditorRow.AppCount), IsEnglish ? "Apps" : "앱 수", 80),
                CreateTextColumn(nameof(AppCategoryEditorRow.IsBuiltin), IsEnglish ? "Built-in" : "기본", 80));

            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.FlowDirection = FlowDirection.RightToLeft;
            bottomPanel.Height = 48;
            bottomPanel.Padding = new Padding(12, 8, 12, 8);

            closeButton.Text = IsEnglish ? "Close" : "닫기";
            closeButton.Width = 84;
            closeButton.DialogResult = DialogResult.OK;
            bottomPanel.Controls.Add(closeButton);

            Controls.Add(categoriesGrid);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);

            ResumeLayout(false);
        }

        private static DataGridViewTextBoxColumn CreateTextColumn(string propertyName, string headerText, int width)
        {
            return new DataGridViewTextBoxColumn
            {
                DataPropertyName = propertyName,
                HeaderText = headerText,
                MinimumWidth = Math.Min(width, 70),
                Name = propertyName + "Column",
                ReadOnly = true,
                Width = width
            };
        }

        private void LoadRows()
        {
            rows = storage.GetAppCategoryEditorRows();
            categoriesBindingSource.DataSource = rows;
            UpdateEditorFromSelection();
        }

        private void OnCategoriesGridSelectionChanged(object? sender, EventArgs e)
        {
            UpdateEditorFromSelection();
        }

        private void UpdateEditorFromSelection()
        {
            if (GetSelectedRow() is not { } row)
            {
                updateButton.Enabled = false;
                deleteButton.Enabled = false;
                return;
            }

            nameTextBox.Text = row.IsBuiltin ? row.DisplayName : row.Name;
            SelectColor(row.Color);
            updateButton.Enabled = !row.IsBuiltin;
            deleteButton.Enabled = !row.IsBuiltin;
        }

        private void SelectColor(string? color)
        {
            var selected = ColorOptions.FirstOrDefault(option =>
                string.Equals(option.Hex, color, StringComparison.OrdinalIgnoreCase));
            colorComboBox.SelectedItem = selected ?? ColorOptions[0];
        }

        private AppCategoryEditorRow? GetSelectedRow()
        {
            return categoriesGrid.CurrentRow?.DataBoundItem as AppCategoryEditorRow;
        }

        private void OnAddButtonClick(object? sender, EventArgs e)
        {
            TrySaveCategory(() =>
            {
                storage.CreateCustomAppCategory(nameTextBox.Text, GetSelectedColor());
                CategoriesChanged = true;
                LoadRows();
                statusLabel.Text = IsEnglish ? "Category added." : "분류를 추가했습니다.";
            });
        }

        private void OnUpdateButtonClick(object? sender, EventArgs e)
        {
            if (GetSelectedRow() is not { IsBuiltin: false } row)
                return;

            TrySaveCategory(() =>
            {
                storage.UpdateCustomAppCategory(row.Id, nameTextBox.Text, GetSelectedColor());
                CategoriesChanged = true;
                LoadRows();
                statusLabel.Text = IsEnglish ? "Category updated." : "분류를 수정했습니다.";
            });
        }

        private void OnDeleteButtonClick(object? sender, EventArgs e)
        {
            if (GetSelectedRow() is not { IsBuiltin: false } row)
                return;

            var message = IsEnglish
                ? $"Delete {row.Name}? Apps using it will become uncategorized."
                : $"{row.Name} 분류를 삭제할까요? 이 분류를 사용 중인 앱은 미분류로 변경됩니다.";
            var result = CenteredMessageDialog.Show(
                this,
                message,
                Text,
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (result != DialogResult.OK)
                return;

            storage.DeleteCustomAppCategory(row.Id);
            CategoriesChanged = true;
            LoadRows();
            statusLabel.Text = IsEnglish ? "Category deleted." : "분류를 삭제했습니다.";
        }

        private void TrySaveCategory(Action action)
        {
            try
            {
                action();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                statusLabel.Text = IsEnglish
                    ? "A category with the same name already exists."
                    : "같은 이름의 분류가 이미 있습니다.";
            }
            catch (ArgumentException)
            {
                statusLabel.Text = IsEnglish ? "Enter a category name." : "분류 이름을 입력하세요.";
            }
        }

        private string? GetSelectedColor()
        {
            return colorComboBox.SelectedItem is CategoryColorOption option ? option.Hex : null;
        }

        private sealed record CategoryColorOption(string Hex, string Name)
        {
            public override string ToString() => $"{Name} {Hex}";
        }
    }
}
