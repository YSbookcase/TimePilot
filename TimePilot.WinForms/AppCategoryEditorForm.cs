using Microsoft.Data.Sqlite;
using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal sealed class AppCategoryEditorForm : Form
    {
        private readonly TimePilotStorage storage;
        private readonly UiLanguage language;
        private readonly DataGridView categoriesGrid = new();
        private readonly BindingSource categoriesBindingSource = new();
        private readonly TextBox nameTextBox = new();
        private readonly Panel colorPreviewPanel = new();
        private readonly TextBox colorTextBox = new();
        private readonly Button chooseColorButton = new();
        private readonly Button addButton = new();
        private readonly Button updateButton = new();
        private readonly Button deleteButton = new();
        private readonly Button closeButton = new();
        private readonly Label statusLabel = new();
        private readonly ContextMenuStrip categoriesContextMenu = new();
        private readonly ToolStripMenuItem editColorMenuItem = new();
        private readonly ToolStripMenuItem deleteCategoryMenuItem = new();
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
            var toolTip = new ToolTip();

            SuspendLayout();

            Text = IsEnglish ? "Manage Categories" : "분류 관리";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(620, 420);
            Size = new Size(720, 520);

            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 100;
            topPanel.Padding = new Padding(12, 10, 12, 6);
            topPanel.WrapContents = true;

            nameLabel.AutoSize = true;
            nameLabel.Margin = new Padding(0, 5, 6, 0);
            nameLabel.Text = IsEnglish ? "Name" : "이름";

            nameTextBox.Width = 180;

            colorLabel.AutoSize = true;
            colorLabel.Margin = new Padding(12, 5, 6, 0);
            colorLabel.Text = IsEnglish ? "Color" : "색상";

            colorPreviewPanel.BorderStyle = BorderStyle.FixedSingle;
            colorPreviewPanel.Cursor = Cursors.Hand;
            colorPreviewPanel.Margin = new Padding(0, 3, 6, 0);
            colorPreviewPanel.Size = new Size(24, 23);
            colorPreviewPanel.Click += OnChooseColorButtonClick;

            colorTextBox.Width = 90;
            colorTextBox.Text = "#2563EB";
            colorTextBox.TextChanged += (_, _) => UpdateColorPreview();

            chooseColorButton.Text = IsEnglish ? "Color..." : "색상...";
            chooseColorButton.Width = 96;
            chooseColorButton.Click += OnChooseColorButtonClick;

            addButton.Text = IsEnglish ? "Add new" : "새 분류 추가";
            addButton.Margin = new Padding(16, 0, 3, 0);
            addButton.Width = 104;
            addButton.Click += OnAddButtonClick;

            updateButton.Text = IsEnglish ? "Save selected" : "선택 저장";
            updateButton.Width = 104;
            updateButton.Click += OnUpdateButtonClick;

            deleteButton.Text = IsEnglish ? "Delete selected" : "선택 삭제";
            deleteButton.Width = 104;
            deleteButton.Click += OnDeleteButtonClick;

            toolTip.SetToolTip(nameTextBox, IsEnglish ? "Enter the category name to add or edit." : "추가하거나 수정할 분류 이름을 입력합니다.");
            toolTip.SetToolTip(chooseColorButton, IsEnglish ? "Choose a category color." : "분류 색상을 선택합니다.");
            toolTip.SetToolTip(addButton, IsEnglish ? "Create a new custom category from the current name and color." : "현재 이름과 색상으로 새 사용자 분류를 추가합니다.");
            toolTip.SetToolTip(updateButton, IsEnglish ? "Save changes to the selected category." : "선택한 분류의 변경 사항을 저장합니다.");
            toolTip.SetToolTip(deleteButton, IsEnglish ? "Delete the selected custom category." : "선택한 사용자 분류를 삭제합니다.");

            statusLabel.AutoSize = true;
            statusLabel.ForeColor = SystemColors.GrayText;
            statusLabel.Margin = new Padding(0, 7, 0, 0);
            statusLabel.Text = IsEnglish
                ? "Built-in category names and deletion are locked. Colors can be changed."
                : "기본 분류는 이름 변경과 삭제가 불가능합니다. 색상은 변경할 수 있습니다.";

            topPanel.Controls.Add(nameLabel);
            topPanel.Controls.Add(nameTextBox);
            topPanel.Controls.Add(colorLabel);
            topPanel.Controls.Add(colorPreviewPanel);
            topPanel.Controls.Add(colorTextBox);
            topPanel.Controls.Add(chooseColorButton);
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
            categoriesGrid.MouseDown += OnCategoriesGridMouseDown;
            categoriesGrid.CellPainting += OnCategoriesGridCellPainting;
            categoriesGrid.Columns.AddRange(
                CreateTextColumn(nameof(AppCategoryEditorRow.DisplayName), IsEnglish ? "Category" : "분류", 180),
                CreateTextColumn(nameof(AppCategoryEditorRow.ColorText), IsEnglish ? "Color" : "색상", 120),
                CreateTextColumn(nameof(AppCategoryEditorRow.AppCount), IsEnglish ? "Apps" : "앱 수", 80),
                CreateTextColumn(nameof(AppCategoryEditorRow.EditabilityText), IsEnglish ? "Edit policy" : "수정 정책", 130));

            editColorMenuItem.Text = IsEnglish ? "Edit color..." : "색상 수정...";
            editColorMenuItem.Click += OnEditColorMenuItemClick;
            deleteCategoryMenuItem.Text = IsEnglish ? "Delete category" : "분류 삭제";
            deleteCategoryMenuItem.Click += OnDeleteCategoryMenuItemClick;
            categoriesContextMenu.Items.Add(editColorMenuItem);
            categoriesContextMenu.Items.Add(deleteCategoryMenuItem);
            categoriesContextMenu.Opening += OnCategoriesContextMenuOpening;
            categoriesGrid.ContextMenuStrip = categoriesContextMenu;

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

        private void OnCategoriesGridMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            var hit = categoriesGrid.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0 || hit.ColumnIndex < 0)
                return;

            categoriesGrid.CurrentCell = categoriesGrid.Rows[hit.RowIndex].Cells[hit.ColumnIndex];
            categoriesGrid.Rows[hit.RowIndex].Selected = true;
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
            nameTextBox.Enabled = true;
            SelectColor(row.Color);
            updateButton.Enabled = true;
            deleteButton.Enabled = !row.IsBuiltin;
        }

        private void SelectColor(string? color)
        {
            colorTextBox.Text = string.IsNullOrWhiteSpace(color) ? "#2563EB" : color;
            UpdateColorPreview();
        }

        private AppCategoryEditorRow? GetSelectedRow()
        {
            return categoriesGrid.CurrentRow?.DataBoundItem as AppCategoryEditorRow;
        }

        private void OnAddButtonClick(object? sender, EventArgs e)
        {
            TrySaveCategory(() =>
            {
                storage.CreateCustomAppCategory(nameTextBox.Text, colorTextBox.Text);
                CategoriesChanged = true;
                LoadRows();
                statusLabel.Text = IsEnglish ? "Category added." : "분류를 추가했습니다.";
            });
        }

        private void OnUpdateButtonClick(object? sender, EventArgs e)
        {
            if (GetSelectedRow() is not { } row)
                return;

            if (!HasCategoryChanges(row))
            {
                statusLabel.Text = IsEnglish ? "No changes to save." : "저장할 변경 사항이 없습니다.";
                return;
            }

            TrySaveCategory(() =>
            {
                if (row.IsBuiltin)
                    storage.UpdateAppCategoryColor(row.Id, colorTextBox.Text);
                else
                    storage.UpdateCustomAppCategory(row.Id, nameTextBox.Text, colorTextBox.Text);
                CategoriesChanged = true;
                LoadRows();
                statusLabel.Text = IsEnglish ? "Category updated." : "분류를 수정했습니다.";
            });
        }

        private bool HasCategoryChanges(AppCategoryEditorRow row)
        {
            if (!IsSameColor(row.Color, colorTextBox.Text))
                return true;

            return !row.IsBuiltin
                && !string.Equals(row.Name.Trim(), nameTextBox.Text.Trim(), StringComparison.Ordinal);
        }

        private static bool IsSameColor(string? left, string? right)
        {
            return string.Equals(NormalizeColorForComparison(left), NormalizeColorForComparison(right), StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeColorForComparison(string? value)
        {
            if (!TryParseColor(value, out var color))
                return string.Empty;

            return ColorTranslator.ToHtml(Color.FromArgb(color.R, color.G, color.B));
        }

        private void OnDeleteButtonClick(object? sender, EventArgs e)
        {
            DeleteSelectedCustomCategory();
        }

        private void DeleteSelectedCustomCategory()
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
            catch (InvalidOperationException)
            {
                statusLabel.Text = IsEnglish
                    ? "A category with the same display name already exists."
                    : "같은 표시 이름의 분류가 이미 있습니다.";
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                statusLabel.Text = IsEnglish
                    ? "A category with the same name already exists."
                    : "같은 이름의 분류가 이미 있습니다.";
            }
            catch (ArgumentException)
            {
                statusLabel.Text = IsEnglish
                    ? "Enter a category name and a valid color such as #2563EB."
                    : "분류 이름과 #2563EB 같은 올바른 색상 값을 입력하세요.";
            }
        }

        private void OnChooseColorButtonClick(object? sender, EventArgs e)
        {
            var selectedColor = ChooseColor(colorTextBox.Text);
            if (selectedColor is null)
                return;

            colorTextBox.Text = selectedColor;
        }

        private void OnEditColorMenuItemClick(object? sender, EventArgs e)
        {
            if (GetSelectedRow() is not { } row)
                return;

            var selectedColor = ChooseColor(row.Color);
            if (selectedColor is null)
                return;

            TrySaveCategory(() =>
            {
                storage.UpdateAppCategoryColor(row.Id, selectedColor);
                CategoriesChanged = true;
                LoadRows();
                statusLabel.Text = IsEnglish ? "Category color updated." : "분류 색상을 변경했습니다.";
            });
        }

        private void OnDeleteCategoryMenuItemClick(object? sender, EventArgs e)
        {
            DeleteSelectedCustomCategory();
        }

        private void OnCategoriesContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            deleteCategoryMenuItem.Visible = GetSelectedRow() is { IsBuiltin: false };
        }

        private string? ChooseColor(string? initialColor)
        {
            using var dialog = new CategoryColorPickerForm(initialColor, language, rows.Select(x => x.Color));
            return dialog.ShowDialog(this) == DialogResult.OK
                ? dialog.SelectedColorHex
                : null;
        }

        private void UpdateColorPreview()
        {
            colorPreviewPanel.BackColor = TryParseColor(colorTextBox.Text, out var color)
                ? color
                : SystemColors.Control;
        }

        private void OnCategoriesGridCellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0
                || categoriesGrid.Columns[e.ColumnIndex].DataPropertyName != nameof(AppCategoryEditorRow.ColorText)
                || categoriesGrid.Rows[e.RowIndex].DataBoundItem is not AppCategoryEditorRow row)
                return;

            e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);
            if (e.Graphics is null)
                return;

            var chipBounds = new Rectangle(e.CellBounds.Left + 6, e.CellBounds.Top + 6, 16, Math.Max(8, e.CellBounds.Height - 12));
            if (TryParseColor(row.Color, out var color))
            {
                using var brush = new SolidBrush(color);
                using var pen = new Pen(SystemColors.ControlDark);
                e.Graphics.FillRectangle(brush, chipBounds);
                e.Graphics.DrawRectangle(pen, chipBounds);
            }

            var textBounds = new Rectangle(e.CellBounds.Left + 28, e.CellBounds.Top, Math.Max(1, e.CellBounds.Width - 30), e.CellBounds.Height);
            TextRenderer.DrawText(
                e.Graphics,
                row.ColorText,
                categoriesGrid.Font,
                textBounds,
                e.CellStyle?.ForeColor ?? categoriesGrid.ForeColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            e.Handled = true;
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
