using System.Diagnostics;
using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal sealed class AppCategoryManagementForm : Form
    {
        private readonly TimePilotStorage storage;
        private readonly UiLanguage language;
        private readonly AppIconCache appIconCache = new();
        private readonly ComboBox filterComboBox = new();
        private readonly ComboBox filterCategoryComboBox = new();
        private readonly TextBox searchTextBox = new();
        private readonly CheckBox showFileInfoCheckBox = new();
        private readonly ComboBox assignCategoryComboBox = new();
        private readonly Button applyCategoryButton = new();
        private readonly Button clearCategoryButton = new();
        private readonly Button refreshButton = new();
        private readonly Button searchWebButton = new();
        private readonly Button closeButton = new();
        private readonly Label statusLabel = new();
        private readonly DataGridView appsGrid = new();
        private readonly BindingSource appsBindingSource = new();
        private readonly ContextMenuStrip categoryMenu = new();

        private IReadOnlyList<AppCategoryOption> categories = Array.Empty<AppCategoryOption>();
        private IReadOnlyList<AppCategoryManagementRow> allRows = Array.Empty<AppCategoryManagementRow>();
        private IReadOnlyList<AppCategoryManagementRow> visibleRows = Array.Empty<AppCategoryManagementRow>();
        private string sortProperty = nameof(AppCategoryManagementRow.LastObservedAt);
        private SortOrder sortOrder = SortOrder.Descending;
        private long? rowToRestoreAfterFilter;
        private CategoryChangeUndo? lastCategoryChange;

        public AppCategoryManagementForm(TimePilotStorage storage, UiLanguage language)
        {
            this.storage = storage;
            this.language = language;

            InitializeComponent();
            LoadData();
        }

        public bool CategoriesChanged { get; private set; }

        private bool IsEnglish => language == UiLanguage.English;

        private string AllText => IsEnglish ? "All" : "전체";

        private string UncategorizedText => IsEnglish ? "Uncategorized only" : "미분류만";

        private string CategorizedText => IsEnglish ? "Categorized only" : "분류됨만";

        private string SpecificCategoryText => IsEnglish ? "Specific category" : "특정 분류";

        private string NoCategoryText => IsEnglish ? "(Uncategorized)" : "(미분류)";

        private void InitializeComponent()
        {
            var topPanel = new FlowLayoutPanel();
            var filterLabel = new Label();
            var searchLabel = new Label();
            var assignLabel = new Label();
            var bottomPanel = new FlowLayoutPanel();

            SuspendLayout();

            Text = IsEnglish ? "App Category Management" : "앱 분류 관리";
            KeyPreview = true;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(860, 520);
            Size = new Size(1040, 640);

            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 72;
            topPanel.Padding = new Padding(12, 10, 12, 6);
            topPanel.WrapContents = true;

            filterLabel.AutoSize = true;
            filterLabel.Margin = new Padding(0, 5, 6, 0);
            filterLabel.Text = IsEnglish ? "Filter" : "필터";

            filterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            filterComboBox.Width = 130;
            filterComboBox.SelectedIndexChanged += (_, _) => ApplyFilter();

            filterCategoryComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            filterCategoryComboBox.Width = 160;
            filterCategoryComboBox.SelectedIndexChanged += (_, _) => ApplyFilter();

            searchLabel.AutoSize = true;
            searchLabel.Margin = new Padding(12, 5, 6, 0);
            searchLabel.Text = IsEnglish ? "Search" : "검색";

            searchTextBox.Width = 180;
            searchTextBox.TextChanged += (_, _) => ApplyFilter();

            showFileInfoCheckBox.AutoSize = true;
            showFileInfoCheckBox.Margin = new Padding(12, 4, 0, 0);
            showFileInfoCheckBox.Text = IsEnglish ? "Show file info" : "파일 정보 표시";
            showFileInfoCheckBox.CheckedChanged += (_, _) => UpdateFileInfoColumnVisibility();

            assignLabel.AutoSize = true;
            assignLabel.Margin = new Padding(12, 5, 6, 0);
            assignLabel.Text = IsEnglish ? "Set to" : "분류 지정";

            assignCategoryComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            assignCategoryComboBox.Width = 160;

            applyCategoryButton.Text = IsEnglish ? "Apply" : "적용";
            applyCategoryButton.Width = 72;
            applyCategoryButton.Click += OnApplyCategoryButtonClick;

            clearCategoryButton.Text = IsEnglish ? "Clear category" : "분류 해제";
            clearCategoryButton.Width = 72;
            clearCategoryButton.Click += OnClearCategoryButtonClick;

            refreshButton.Text = IsEnglish ? "Refresh" : "새로고침";
            refreshButton.Width = 84;
            refreshButton.Click += (_, _) => ReloadAndApplyFilter();

            searchWebButton.Text = IsEnglish ? "Search web" : "웹 검색";
            searchWebButton.Width = 84;
            searchWebButton.Click += OnSearchWebButtonClick;

            topPanel.Controls.Add(filterLabel);
            topPanel.Controls.Add(filterComboBox);
            topPanel.Controls.Add(filterCategoryComboBox);
            topPanel.Controls.Add(searchLabel);
            topPanel.Controls.Add(searchTextBox);
            topPanel.Controls.Add(showFileInfoCheckBox);
            topPanel.Controls.Add(assignLabel);
            topPanel.Controls.Add(assignCategoryComboBox);
            topPanel.Controls.Add(applyCategoryButton);
            topPanel.Controls.Add(clearCategoryButton);
            topPanel.Controls.Add(searchWebButton);
            topPanel.Controls.Add(refreshButton);

            appsGrid.AllowUserToAddRows = false;
            appsGrid.AllowUserToDeleteRows = false;
            appsGrid.AllowUserToOrderColumns = true;
            appsGrid.AllowUserToResizeRows = false;
            appsGrid.AutoGenerateColumns = false;
            appsGrid.BackgroundColor = SystemColors.Window;
            appsGrid.BorderStyle = BorderStyle.None;
            appsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            appsGrid.Dock = DockStyle.Fill;
            appsGrid.MultiSelect = false;
            appsGrid.ReadOnly = true;
            appsGrid.RowHeadersVisible = false;
            appsGrid.ScrollBars = ScrollBars.Both;
            appsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            appsGrid.DataSource = appsBindingSource;
            appsGrid.ColumnHeaderMouseClick += OnAppsGridColumnHeaderMouseClick;
            appsGrid.CellMouseDown += OnAppsGridCellMouseDown;
            appsGrid.CellDoubleClick += OnAppsGridCellDoubleClick;
            appsGrid.Columns.AddRange(
                CreateIconColumn(),
                CreateTextColumn(nameof(AppCategoryManagementRow.AppName), IsEnglish ? "App" : "앱", 180),
                CreateTextColumn(nameof(AppCategoryManagementRow.ProcessName), IsEnglish ? "Process" : "프로세스", 130),
                CreateTextColumn(nameof(AppCategoryManagementRow.FileDescriptionText), IsEnglish ? "Description" : "파일 설명", 180, nameof(AppCategoryManagementRow.FileDescription)),
                CreateTextColumn(nameof(AppCategoryManagementRow.ProductNameText), IsEnglish ? "Product" : "제품명", 160, nameof(AppCategoryManagementRow.ProductName)),
                CreateTextColumn(nameof(AppCategoryManagementRow.CompanyNameText), IsEnglish ? "Company" : "회사", 150, nameof(AppCategoryManagementRow.CompanyName)),
                CreateTextColumn(nameof(AppCategoryManagementRow.CategoryText), IsEnglish ? "Category" : "분류", 130),
                CreateTextColumn(nameof(AppCategoryManagementRow.LastObservedAtText), IsEnglish ? "Last observed" : "최근 감지", 150, nameof(AppCategoryManagementRow.LastObservedAt)),
                CreateTextColumn(nameof(AppCategoryManagementRow.ActiveUsageTimeText), IsEnglish ? "Active time" : "활성 사용", 110, nameof(AppCategoryManagementRow.ActiveUsageMs)),
                CreateTextColumn(nameof(AppCategoryManagementRow.RuntimeText), IsEnglish ? "Runtime" : "실행 시간", 110, nameof(AppCategoryManagementRow.RuntimeMs)),
                CreateTextColumn(nameof(AppCategoryManagementRow.SwitchCountText), IsEnglish ? "Switches" : "전환", 80, nameof(AppCategoryManagementRow.SwitchCount)),
                CreateTextColumn(nameof(AppCategoryManagementRow.RuntimeSegmentCountText), IsEnglish ? "Segments" : "구간", 80, nameof(AppCategoryManagementRow.RuntimeSegmentCount)),
                CreateTextColumn(nameof(AppCategoryManagementRow.ExecutablePath), IsEnglish ? "Path" : "실행 경로", 320));

            UpdateFileInfoColumnVisibility();

            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.FlowDirection = FlowDirection.LeftToRight;
            bottomPanel.Height = 48;
            bottomPanel.Padding = new Padding(12, 8, 12, 8);

            statusLabel.AutoSize = true;
            statusLabel.Margin = new Padding(0, 8, 12, 0);
            statusLabel.ForeColor = SystemColors.GrayText;
            statusLabel.Text = "";

            closeButton.Text = IsEnglish ? "Close" : "닫기";
            closeButton.Width = 84;
            closeButton.DialogResult = DialogResult.OK;
            closeButton.Anchor = AnchorStyles.Right;
            bottomPanel.Controls.Add(statusLabel);
            bottomPanel.Controls.Add(closeButton);

            Controls.Add(appsGrid);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);

            ResumeLayout(false);
        }

        private static DataGridViewTextBoxColumn CreateTextColumn(
            string propertyName,
            string headerText,
            int width,
            string? sortPropertyName = null)
        {
            return new DataGridViewTextBoxColumn
            {
                DataPropertyName = propertyName,
                HeaderText = headerText,
                MinimumWidth = Math.Min(width, 80),
                Name = propertyName + "Column",
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.Programmatic,
                Tag = sortPropertyName ?? propertyName,
                Width = width
            };
        }

        private DataGridViewImageColumn CreateIconColumn()
        {
            return new DataGridViewImageColumn
            {
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                DataPropertyName = nameof(AppCategoryManagementRow.AppIcon),
                HeaderText = IsEnglish ? "Icon" : "아이콘",
                ImageLayout = DataGridViewImageCellLayout.Zoom,
                MinimumWidth = 52,
                Name = "appIconColumn",
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.Programmatic,
                Tag = nameof(AppCategoryManagementRow.HasAppIcon),
                Width = 56
            };
        }

        private void UpdateFileInfoColumnVisibility()
        {
            var showFileInfo = showFileInfoCheckBox.Checked;
            SetColumnVisible(nameof(AppCategoryManagementRow.FileDescriptionText), showFileInfo);
            SetColumnVisible(nameof(AppCategoryManagementRow.ProductNameText), showFileInfo);
            SetColumnVisible(nameof(AppCategoryManagementRow.CompanyNameText), showFileInfo);
            SetColumnVisible(nameof(AppCategoryManagementRow.ExecutablePath), showFileInfo);
        }

        private void SetColumnVisible(string propertyName, bool visible)
        {
            var columnName = propertyName + "Column";
            if (appsGrid.Columns.Contains(columnName))
                appsGrid.Columns[columnName].Visible = visible;
        }

        private void LoadData()
        {
            categories = storage.GetAppCategoryOptions();
            allRows = AddIcons(storage.GetAppCategoryManagementRows(DateTimeOffset.UtcNow));
            RefreshFilterOptions();
            RefreshAssignCategoryOptions();
            ApplyFilter();
        }

        private void ReloadAndApplyFilter()
        {
            allRows = AddIcons(storage.GetAppCategoryManagementRows(DateTimeOffset.UtcNow));
            ApplyFilter();
        }

        private IReadOnlyList<AppCategoryManagementRow> AddIcons(IReadOnlyList<AppCategoryManagementRow> rows)
        {
            return rows
                .Select(row =>
                {
                    var metadata = GetFileMetadata(row.ExecutablePath);
                    return row with
                    {
                        FileDescription = metadata.FileDescription,
                        ProductName = metadata.ProductName,
                        CompanyName = metadata.CompanyName,
                        CategoryDisplayName = GetCategoryDisplayName(row.PrimaryCategoryId, row.CategoryName),
                        HasExtractedAppIcon = HasDistinctAssociatedIcon(row.ExecutablePath),
                        AppIcon = appIconCache.GetIcon(row.ExecutablePath)
                    };
                })
                .ToList();
        }

        private static bool HasDistinctAssociatedIcon(string? executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                return false;

            try
            {
                using var icon = Icon.ExtractAssociatedIcon(executablePath);
                if (icon is null)
                    return false;

                using var extractedBitmap = icon.ToBitmap();
                using var defaultBitmap = SystemIcons.Application.ToBitmap();
                return !AreBitmapsEqual(extractedBitmap, defaultBitmap);
            }
            catch
            {
                return false;
            }
        }

        private static bool AreBitmapsEqual(Bitmap left, Bitmap right)
        {
            if (left.Size != right.Size)
                return false;

            for (var y = 0; y < left.Height; y++)
            {
                for (var x = 0; x < left.Width; x++)
                {
                    if (left.GetPixel(x, y) != right.GetPixel(x, y))
                        return false;
                }
            }

            return true;
        }

        private static (string? FileDescription, string? ProductName, string? CompanyName) GetFileMetadata(string? executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                return (null, null, null);

            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(executablePath);
                return (
                    NormalizeMetadata(versionInfo.FileDescription),
                    NormalizeMetadata(versionInfo.ProductName),
                    NormalizeMetadata(versionInfo.CompanyName));
            }
            catch
            {
                return (null, null, null);
            }
        }

        private static string? NormalizeMetadata(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private string? GetCategoryDisplayName(long? categoryId, string? fallbackName)
        {
            if (categoryId is null)
                return null;

            var category = categories.FirstOrDefault(x => x.Id == categoryId);
            return category is null ? fallbackName : GetCategoryDisplayName(category);
        }

        private string GetCategoryDisplayName(AppCategoryOption category)
        {
            return AppCategoryDisplay.GetDisplayName(category);
        }

        private void RefreshFilterOptions()
        {
            var selectedFilter = filterComboBox.SelectedItem as string;
            filterComboBox.BeginUpdate();
            try
            {
                filterComboBox.Items.Clear();
                filterComboBox.Items.AddRange(new object[]
                {
                    AllText,
                    UncategorizedText,
                    CategorizedText,
                    SpecificCategoryText
                });
                filterComboBox.SelectedItem = selectedFilter is not null && filterComboBox.Items.Contains(selectedFilter)
                    ? selectedFilter
                    : UncategorizedText;
            }
            finally
            {
                filterComboBox.EndUpdate();
            }

            var selectedCategoryId = (filterCategoryComboBox.SelectedItem as CategorySelectionOption)?.Id;
            filterCategoryComboBox.BeginUpdate();
            try
            {
                filterCategoryComboBox.Items.Clear();
                foreach (var category in categories)
                    filterCategoryComboBox.Items.Add(new CategorySelectionOption(category.Id, GetCategoryDisplayName(category)));

                filterCategoryComboBox.SelectedItem = filterCategoryComboBox.Items
                    .Cast<CategorySelectionOption>()
                    .FirstOrDefault(x => x.Id == selectedCategoryId)
                    ?? filterCategoryComboBox.Items.Cast<CategorySelectionOption>().FirstOrDefault();
            }
            finally
            {
                filterCategoryComboBox.EndUpdate();
            }
        }

        private void RefreshAssignCategoryOptions()
        {
            var selectedCategoryId = (assignCategoryComboBox.SelectedItem as CategorySelectionOption)?.Id;
            assignCategoryComboBox.BeginUpdate();
            try
            {
                assignCategoryComboBox.Items.Clear();
                assignCategoryComboBox.Items.Add(new CategorySelectionOption(null, NoCategoryText));
                foreach (var category in categories)
                    assignCategoryComboBox.Items.Add(new CategorySelectionOption(category.Id, GetCategoryDisplayName(category)));

                assignCategoryComboBox.SelectedItem = assignCategoryComboBox.Items
                    .Cast<CategorySelectionOption>()
                    .FirstOrDefault(x => x.Id == selectedCategoryId)
                    ?? assignCategoryComboBox.Items.Cast<CategorySelectionOption>().First();
            }
            finally
            {
                assignCategoryComboBox.EndUpdate();
            }
        }

        private void ApplyFilter()
        {
            var rows = allRows.AsEnumerable();
            var selectedFilter = filterComboBox.SelectedItem as string ?? UncategorizedText;
            filterCategoryComboBox.Enabled = selectedFilter == SpecificCategoryText;

            if (selectedFilter == UncategorizedText)
                rows = rows.Where(x => x.PrimaryCategoryId is null);
            else if (selectedFilter == CategorizedText)
                rows = rows.Where(x => x.PrimaryCategoryId is not null);
            else if (selectedFilter == SpecificCategoryText
                && filterCategoryComboBox.SelectedItem is CategorySelectionOption category)
                rows = rows.Where(x => x.PrimaryCategoryId == category.Id);

            var searchText = searchTextBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                rows = rows.Where(x =>
                    x.AppName.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
                    || x.ProcessName.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
                    || x.CategoryText.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
                    || (x.FileDescription?.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ?? false)
                    || (x.ProductName?.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ?? false)
                    || (x.CompanyName?.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ?? false)
                    || (x.ExecutablePath?.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ?? false));
            }

            visibleRows = SortRows(rows)
                .ThenBy(x => x.AppName)
                .ToList();
            appsBindingSource.DataSource = visibleRows;
            UpdateSortGlyphs();
            RestoreSelection(visibleRows);
        }

        private IOrderedEnumerable<AppCategoryManagementRow> SortRows(IEnumerable<AppCategoryManagementRow> rows)
        {
            return sortProperty switch
            {
                nameof(AppCategoryManagementRow.AppName) => OrderRows(rows, x => x.AppName),
                nameof(AppCategoryManagementRow.ProcessName) => OrderRows(rows, x => x.ProcessName),
                nameof(AppCategoryManagementRow.HasAppIcon) => OrderRows(rows, x => x.HasAppIcon),
                nameof(AppCategoryManagementRow.FileDescription) => OrderRows(rows, x => x.FileDescription ?? ""),
                nameof(AppCategoryManagementRow.ProductName) => OrderRows(rows, x => x.ProductName ?? ""),
                nameof(AppCategoryManagementRow.CompanyName) => OrderRows(rows, x => x.CompanyName ?? ""),
                nameof(AppCategoryManagementRow.CategoryText) => OrderRows(rows, x => x.CategoryText),
                nameof(AppCategoryManagementRow.ActiveUsageMs) => OrderRows(rows, x => x.ActiveUsageMs),
                nameof(AppCategoryManagementRow.RuntimeMs) => OrderRows(rows, x => x.RuntimeMs),
                nameof(AppCategoryManagementRow.SwitchCount) => OrderRows(rows, x => x.SwitchCount),
                nameof(AppCategoryManagementRow.RuntimeSegmentCount) => OrderRows(rows, x => x.RuntimeSegmentCount),
                nameof(AppCategoryManagementRow.ExecutablePath) => OrderRows(rows, x => x.ExecutablePath ?? ""),
                _ => OrderRows(rows, x => x.LastObservedAt)
            };
        }

        private IOrderedEnumerable<AppCategoryManagementRow> OrderRows<TKey>(
            IEnumerable<AppCategoryManagementRow> rows,
            Func<AppCategoryManagementRow, TKey> keySelector)
        {
            return sortOrder == SortOrder.Ascending
                ? rows.OrderBy(keySelector)
                : rows.OrderByDescending(keySelector);
        }

        private void OnAppsGridColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0)
                return;

            if (appsGrid.Columns[e.ColumnIndex].SortMode == DataGridViewColumnSortMode.NotSortable)
                return;

            var propertyName = appsGrid.Columns[e.ColumnIndex].Tag as string
                ?? appsGrid.Columns[e.ColumnIndex].DataPropertyName;
            if (string.IsNullOrWhiteSpace(propertyName))
                return;

            sortOrder = string.Equals(sortProperty, propertyName, StringComparison.Ordinal)
                ? ToggleSortOrder(sortOrder)
                : SortOrder.Descending;
            sortProperty = propertyName;
            SortVisibleRowsPreservingView();
        }

        private void SortVisibleRowsPreservingView()
        {
            var firstDisplayedRowIndex = GetFirstDisplayedRowIndex();
            var firstDisplayedColumnIndex = GetFirstDisplayedColumnIndex();
            var horizontalOffset = GetHorizontalScrollingOffset();
            visibleRows = SortRows(visibleRows)
                .ThenBy(x => x.AppName)
                .ToList();
            appsBindingSource.DataSource = visibleRows;
            UpdateSortGlyphs();
            SelectFirstVisibleRow();
            RestoreScrollPosition(firstDisplayedRowIndex, firstDisplayedColumnIndex, horizontalOffset);
        }

        private void SelectFirstVisibleRow()
        {
            if (appsGrid.Rows.Count == 0)
                return;

            appsGrid.ClearSelection();
            appsGrid.CurrentCell = appsGrid.Rows[0].Cells[0];
            appsGrid.Rows[0].Selected = true;

            try
            {
                appsGrid.FirstDisplayedScrollingRowIndex = 0;
            }
            catch
            {
            }

            try
            {
                appsGrid.FirstDisplayedScrollingColumnIndex = 0;
                appsGrid.HorizontalScrollingOffset = 0;
            }
            catch
            {
            }
        }

        private void UpdateSortGlyphs()
        {
            foreach (DataGridViewColumn column in appsGrid.Columns)
            {
                if (column.SortMode == DataGridViewColumnSortMode.NotSortable)
                {
                    column.HeaderCell.SortGlyphDirection = SortOrder.None;
                    continue;
                }

                var columnSortProperty = column.Tag as string ?? column.DataPropertyName;
                column.HeaderCell.SortGlyphDirection = string.Equals(columnSortProperty, sortProperty, StringComparison.Ordinal)
                    ? sortOrder
                    : SortOrder.None;
            }
        }

        private static SortOrder ToggleSortOrder(SortOrder current)
        {
            return current == SortOrder.Descending ? SortOrder.Ascending : SortOrder.Descending;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Z) && ActiveControl is not TextBoxBase)
            {
                UndoLastCategoryChange();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void OnApplyCategoryButtonClick(object? sender, EventArgs e)
        {
            if (appsGrid.CurrentRow?.DataBoundItem is not AppCategoryManagementRow row
                || assignCategoryComboBox.SelectedItem is not CategorySelectionOption option)
                return;

            SetCategory(row, option.Id);
        }

        private void OnClearCategoryButtonClick(object? sender, EventArgs e)
        {
            if (appsGrid.CurrentRow?.DataBoundItem is not AppCategoryManagementRow row)
                return;

            SetCategory(row, null);
        }

        private void OnSearchWebButtonClick(object? sender, EventArgs e)
        {
            if (appsGrid.CurrentRow?.DataBoundItem is not AppCategoryManagementRow row)
                return;

            OpenWebSearch(row);
        }

        private void OpenWebSearch(AppCategoryManagementRow row)
        {
            var query = BuildWebSearchQuery(row);
            if (string.IsNullOrWhiteSpace(query))
                return;

            if (!IsEnglish)
                query += " 이란";

            var url = "https://www.google.com/search?q=" + Uri.EscapeDataString(query);
            try
            {
                Process.Start(new ProcessStartInfo(url)
                {
                    UseShellExecute = true
                });
            }
            catch
            {
                MessageBox.Show(
                    this,
                    IsEnglish ? "Unable to open the browser." : "브라우저를 열 수 없습니다.",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private static string BuildWebSearchQuery(AppCategoryManagementRow row)
        {
            var parts = new[]
                {
                    row.AppName,
                    row.ProductName,
                    row.CompanyName,
                    row.ProcessName
                }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim())
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .Take(4);

            return string.Join(" ", parts);
        }

        private void SetCategory(AppCategoryManagementRow row, long? categoryId, bool recordUndo = true)
        {
            if (row.PrimaryCategoryId == categoryId)
                return;

            if (recordUndo)
                lastCategoryChange = new CategoryChangeUndo(row.AppId, row.PrimaryCategoryId);

            storage.SetAppPrimaryCategory(row.AppId, categoryId);
            CategoriesChanged = true;
            if (recordUndo)
                statusLabel.Text = IsEnglish
                    ? "Category changed. Press Ctrl+Z to undo the last change."
                    : "분류를 변경했습니다. Ctrl+Z로 직전 변경 1회를 되돌릴 수 있습니다.";
            rowToRestoreAfterFilter = row.AppId;
            allRows = AddIcons(storage.GetAppCategoryManagementRows(DateTimeOffset.UtcNow));
            UpdateVisibleRow(row.AppId);
        }

        private void UndoLastCategoryChange()
        {
            if (lastCategoryChange is not { } undo)
                return;

            var row = allRows.FirstOrDefault(x => x.AppId == undo.AppId)
                ?? visibleRows.FirstOrDefault(x => x.AppId == undo.AppId);
            if (row is null)
                return;

            lastCategoryChange = null;
            SetCategory(row, undo.PreviousCategoryId, recordUndo: false);
            statusLabel.Text = IsEnglish ? "Category change undone." : "분류 변경을 되돌렸습니다.";
        }

        private void UpdateVisibleRow(long appId)
        {
            var updatedRow = allRows.FirstOrDefault(x => x.AppId == appId);
            if (updatedRow is null)
            {
                ApplyFilter();
                return;
            }

            var firstDisplayedRowIndex = GetFirstDisplayedRowIndex();
            var firstDisplayedColumnIndex = GetFirstDisplayedColumnIndex();
            var horizontalOffset = GetHorizontalScrollingOffset();
            visibleRows = visibleRows
                .Select(row => row.AppId == appId ? updatedRow : row)
                .ToList();
            appsBindingSource.DataSource = visibleRows;
            UpdateSortGlyphs();
            RestoreSelection(visibleRows);
            RestoreScrollPosition(firstDisplayedRowIndex, firstDisplayedColumnIndex, horizontalOffset);
        }

        private void RestoreSelection(IReadOnlyList<AppCategoryManagementRow> rows)
        {
            if (rowToRestoreAfterFilter is not { } appId)
                return;

            var rowIndex = rows.ToList().FindIndex(x => x.AppId == appId);
            if (rowIndex < 0)
                return;

            appsGrid.ClearSelection();
            appsGrid.CurrentCell = appsGrid.Rows[rowIndex].Cells[0];
            appsGrid.Rows[rowIndex].Selected = true;
        }

        private int GetFirstDisplayedRowIndex()
        {
            try
            {
                return Math.Max(appsGrid.FirstDisplayedScrollingRowIndex, 0);
            }
            catch
            {
                return 0;
            }
        }

        private int GetFirstDisplayedColumnIndex()
        {
            try
            {
                return Math.Max(appsGrid.FirstDisplayedScrollingColumnIndex, 0);
            }
            catch
            {
                return 0;
            }
        }

        private int GetHorizontalScrollingOffset()
        {
            try
            {
                return Math.Max(appsGrid.HorizontalScrollingOffset, 0);
            }
            catch
            {
                return 0;
            }
        }

        private void RestoreScrollPosition(
            int firstDisplayedRowIndex,
            int firstDisplayedColumnIndex,
            int horizontalOffset)
        {
            if (appsGrid.Rows.Count == 0)
                return;

            try
            {
                appsGrid.FirstDisplayedScrollingRowIndex = Math.Clamp(firstDisplayedRowIndex, 0, appsGrid.Rows.Count - 1);
            }
            catch
            {
            }

            try
            {
                appsGrid.FirstDisplayedScrollingColumnIndex = Math.Clamp(firstDisplayedColumnIndex, 0, appsGrid.Columns.Count - 1);
            }
            catch
            {
            }

            try
            {
                appsGrid.HorizontalScrollingOffset = horizontalOffset;
            }
            catch
            {
            }
        }

        private void OnAppsGridCellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0)
                return;

            SelectGridRow(e.RowIndex, e.ColumnIndex);
            if (appsGrid.Rows[e.RowIndex].DataBoundItem is AppCategoryManagementRow row)
                ShowCategoryMenu(row, appsGrid.PointToClient(Cursor.Position));
        }

        private void OnAppsGridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            SelectGridRow(e.RowIndex, e.ColumnIndex);
            if (appsGrid.Rows[e.RowIndex].DataBoundItem is AppCategoryManagementRow row)
                ShowCategoryMenu(row, appsGrid.GetCellDisplayRectangle(Math.Max(e.ColumnIndex, 0), e.RowIndex, true).Location);
        }

        private void SelectGridRow(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || rowIndex >= appsGrid.Rows.Count)
                return;

            appsGrid.ClearSelection();
            var targetColumnIndex = Math.Clamp(columnIndex >= 0 ? columnIndex : 0, 0, appsGrid.Columns.Count - 1);
            appsGrid.CurrentCell = appsGrid.Rows[rowIndex].Cells[targetColumnIndex];
            appsGrid.Rows[rowIndex].Selected = true;
        }

        private void ShowCategoryMenu(AppCategoryManagementRow row, Point location)
        {
            categoryMenu.Items.Clear();

            var clearItem = new ToolStripMenuItem(IsEnglish ? "Clear category" : "분류 해제")
            {
                Checked = row.PrimaryCategoryId is null
            };
            clearItem.Click += (_, _) => SetCategory(row, null);
            categoryMenu.Items.Add(clearItem);

            if (categories.Count > 0)
                categoryMenu.Items.Add(new ToolStripSeparator());

            foreach (var category in categories)
            {
                var categoryItem = new ToolStripMenuItem(GetCategoryDisplayName(category))
                {
                    Checked = row.PrimaryCategoryId == category.Id
                };
                categoryItem.Click += (_, _) => SetCategory(row, category.Id);
                categoryMenu.Items.Add(categoryItem);
            }

            categoryMenu.Items.Add(new ToolStripSeparator());
            var searchWebItem = new ToolStripMenuItem(IsEnglish ? "Search web" : "웹에서 검색");
            searchWebItem.Click += (_, _) => OpenWebSearch(row);
            categoryMenu.Items.Add(searchWebItem);

            categoryMenu.Show(appsGrid, location);
        }

        private sealed record CategorySelectionOption(long? Id, string Name)
        {
            public override string ToString()
            {
                return Name;
            }
        }

        private sealed record CategoryChangeUndo(long AppId, long? PreviousCategoryId);
    }
}
