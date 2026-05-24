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
        private IReadOnlySet<long> rowsToRestoreAfterFilter = new HashSet<long>();
        private CategoryChangeUndo? lastCategoryChange;
        private IReadOnlySet<long>? pendingRightClickSelectionAppIds;
        private IReadOnlyList<AppCategoryManagementRow>? contextMenuRows;
        private IReadOnlySet<long> lastStableMultiSelectionAppIds = new HashSet<long>();
        private IReadOnlySet<long> visualSelectionAppIds = new HashSet<long>();
        private int pendingRightClickRowIndex = -1;
        private int pendingRightClickColumnIndex = -1;
        private bool isRightClickInProgress;
        private int? selectionAnchorRowIndex;

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
            appsGrid.MultiSelect = true;
            appsGrid.ReadOnly = true;
            appsGrid.RowHeadersVisible = false;
            appsGrid.ScrollBars = ScrollBars.Both;
            appsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            appsGrid.DataSource = appsBindingSource;
            appsGrid.ColumnHeaderMouseClick += OnAppsGridColumnHeaderMouseClick;
            appsGrid.MouseDown += OnAppsGridMouseDown;
            appsGrid.MouseUp += OnAppsGridMouseUp;
            appsGrid.CellMouseDown += OnAppsGridCellMouseDown;
            appsGrid.SelectionChanged += OnAppsGridSelectionChanged;
            appsGrid.RowPrePaint += OnAppsGridRowPrePaint;
            categoryMenu.Closed += (_, _) =>
            {
                visualSelectionAppIds = GetSelectedRows().Select(row => row.AppId).ToHashSet();
                appsGrid.Invalidate();
            };
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
            if (assignCategoryComboBox.SelectedItem is not CategorySelectionOption option)
                return;

            SetSelectedCategories(option.Id);
        }

        private void OnClearCategoryButtonClick(object? sender, EventArgs e)
        {
            SetSelectedCategories(null);
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
                CenteredMessageDialog.Show(
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

        private void SetSelectedCategories(long? categoryId)
        {
            var rows = GetSelectedRows();
            if (rows.Count == 0)
                return;

            SetCategories(rows, categoryId);
        }

        private void SetCategory(AppCategoryManagementRow row, long? categoryId, bool recordUndo = true)
        {
            SetCategories([row], categoryId, recordUndo);
        }

        private void SetCategories(
            IReadOnlyList<AppCategoryManagementRow> rows,
            long? categoryId,
            bool recordUndo = true)
        {
            var rowsToChange = rows
                .Where(row => row.PrimaryCategoryId != categoryId)
                .GroupBy(row => row.AppId)
                .Select(group => group.First())
                .ToList();
            if (rowsToChange.Count == 0)
                return;

            if (rowsToChange.Count > 1 && !ConfirmBulkCategoryChange(rowsToChange.Count, categoryId))
                return;

            if (recordUndo)
            {
                lastCategoryChange = new CategoryChangeUndo(
                    rowsToChange
                        .Select(row => new CategoryChangeUndoItem(row.AppId, row.PrimaryCategoryId))
                        .ToList());
            }

            foreach (var row in rowsToChange)
                storage.SetAppPrimaryCategory(row.AppId, categoryId);
            CategoriesChanged = true;
            rowsToRestoreAfterFilter = rowsToChange.Select(row => row.AppId).ToHashSet();
            allRows = AddIcons(storage.GetAppCategoryManagementRows(DateTimeOffset.UtcNow));
            UpdateVisibleRows(rowsToRestoreAfterFilter);
            if (recordUndo)
                statusLabel.Text = IsEnglish
                    ? $"{rowsToChange.Count:N0} app category changed. Press Ctrl+Z to undo the last change."
                    : $"{rowsToChange.Count:N0}개 앱의 분류를 변경했습니다. Ctrl+Z로 직전 변경 1회를 되돌릴 수 있습니다.";
        }

        private void UndoLastCategoryChange()
        {
            if (lastCategoryChange is not { } undo)
                return;

            if (undo.Items.Count == 0)
                return;

            lastCategoryChange = null;
            foreach (var item in undo.Items)
                storage.SetAppPrimaryCategory(item.AppId, item.PreviousCategoryId);

            CategoriesChanged = true;
            rowsToRestoreAfterFilter = undo.Items.Select(item => item.AppId).ToHashSet();
            allRows = AddIcons(storage.GetAppCategoryManagementRows(DateTimeOffset.UtcNow));
            UpdateVisibleRows(rowsToRestoreAfterFilter);
            statusLabel.Text = IsEnglish
                ? $"{undo.Items.Count:N0} category change undone."
                : $"{undo.Items.Count:N0}개 앱의 분류 변경을 되돌렸습니다.";
        }

        private bool ConfirmBulkCategoryChange(int count, long? categoryId)
        {
            var categoryName = categoryId is null
                ? NoCategoryText
                : categories.FirstOrDefault(category => category.Id == categoryId) is { } category
                    ? GetCategoryDisplayName(category)
                    : NoCategoryText;
            var message = IsEnglish
                ? $"Change category for {count:N0} selected apps to {categoryName}?"
                : $"선택한 {count:N0}개 앱의 분류를 {categoryName}(으)로 변경할까요?";

            return CenteredMessageDialog.Show(
                this,
                message,
                Text,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private void UpdateVisibleRows(IReadOnlySet<long> appIds)
        {
            var updatedRows = allRows
                .Where(row => appIds.Contains(row.AppId))
                .ToDictionary(row => row.AppId);
            if (updatedRows.Count == 0)
            {
                ApplyFilter();
                return;
            }

            var firstDisplayedRowIndex = GetFirstDisplayedRowIndex();
            var firstDisplayedColumnIndex = GetFirstDisplayedColumnIndex();
            var horizontalOffset = GetHorizontalScrollingOffset();
            visibleRows = visibleRows
                .Select(row => updatedRows.TryGetValue(row.AppId, out var updatedRow) ? updatedRow : row)
                .ToList();
            appsBindingSource.DataSource = visibleRows;
            UpdateSortGlyphs();
            RestoreSelection(visibleRows);
            RestoreScrollPosition(firstDisplayedRowIndex, firstDisplayedColumnIndex, horizontalOffset);
        }

        private void RestoreSelection(IReadOnlyList<AppCategoryManagementRow> rows)
        {
            if (rowsToRestoreAfterFilter.Count == 0)
                return;

            var rowIndexes = rows
                .Select((row, index) => new { row, index })
                .Where(x => rowsToRestoreAfterFilter.Contains(x.row.AppId))
                .Select(x => x.index)
                .ToList();
            if (rowIndexes.Count == 0)
                return;

            appsGrid.ClearSelection();
            foreach (var rowIndex in rowIndexes)
                appsGrid.Rows[rowIndex].Selected = true;

            appsGrid.CurrentCell = appsGrid.Rows[rowIndexes[0]].Cells[Math.Max(GetFirstDisplayedColumnIndex(), 0)];
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

        private void OnAppsGridMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            isRightClickInProgress = true;
            pendingRightClickSelectionAppIds = null;
            pendingRightClickRowIndex = -1;
            pendingRightClickColumnIndex = -1;

            var hit = appsGrid.HitTest(e.X, e.Y);
            if (hit.Type != DataGridViewHitTestType.Cell || hit.RowIndex < 0)
                return;

            pendingRightClickRowIndex = hit.RowIndex;
            pendingRightClickColumnIndex = hit.ColumnIndex;
            if (appsGrid.Rows[hit.RowIndex].DataBoundItem is not AppCategoryManagementRow row)
                return;

            var currentSelectionAppIds = GetSelectedRows().Select(selectedRow => selectedRow.AppId).ToHashSet();
            if (currentSelectionAppIds.Count > 1 && currentSelectionAppIds.Contains(row.AppId))
            {
                pendingRightClickSelectionAppIds = currentSelectionAppIds;
                return;
            }

            if (lastStableMultiSelectionAppIds.Count > 1 && lastStableMultiSelectionAppIds.Contains(row.AppId))
                pendingRightClickSelectionAppIds = lastStableMultiSelectionAppIds;
        }

        private void OnAppsGridMouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            var hit = appsGrid.HitTest(e.X, e.Y);
            if (hit.Type != DataGridViewHitTestType.Cell || hit.RowIndex < 0)
            {
                ClearPendingRightClickState();
                isRightClickInProgress = false;
                return;
            }

            RestorePendingRightClickSelectionOrSelectRow(hit.RowIndex, hit.ColumnIndex);
            if (appsGrid.Rows[hit.RowIndex].DataBoundItem is AppCategoryManagementRow row)
                ShowCategoryMenu(row, e.Location);
            isRightClickInProgress = false;
        }

        private void OnAppsGridCellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.Button != MouseButtons.Left)
                return;

            var isCtrlPressed = (ModifierKeys & Keys.Control) == Keys.Control;
            var isShiftPressed = (ModifierKeys & Keys.Shift) == Keys.Shift;
            if (!isShiftPressed)
            {
                selectionAnchorRowIndex = e.RowIndex;
                return;
            }

            if (!isCtrlPressed)
                return;

            var anchorRowIndex = selectionAnchorRowIndex ?? GetCurrentSelectedRowIndex() ?? e.RowIndex;
            var selectedAppIds = GetSelectedRows()
                .Select(row => row.AppId)
                .ToHashSet();

            BeginInvoke(new Action(() =>
            {
                SelectAdditionalRange(anchorRowIndex, e.RowIndex, e.ColumnIndex, selectedAppIds);
                selectionAnchorRowIndex = e.RowIndex;
            }));
        }

        private int? GetCurrentSelectedRowIndex()
        {
            return appsGrid.CurrentRow?.Index;
        }

        private void SelectAdditionalRange(
            int anchorRowIndex,
            int rowIndex,
            int columnIndex,
            IReadOnlySet<long> existingSelectionAppIds)
        {
            if (appsGrid.Rows.Count == 0)
                return;

            var appIds = existingSelectionAppIds.ToHashSet();
            var start = Math.Clamp(Math.Min(anchorRowIndex, rowIndex), 0, appsGrid.Rows.Count - 1);
            var end = Math.Clamp(Math.Max(anchorRowIndex, rowIndex), 0, appsGrid.Rows.Count - 1);
            for (var index = start; index <= end; index++)
            {
                if (appsGrid.Rows[index].DataBoundItem is AppCategoryManagementRow row)
                    appIds.Add(row.AppId);
            }

            RestoreSelectionByAppIds(appIds, rowIndex, columnIndex);
            lastStableMultiSelectionAppIds = appIds.Count > 1 ? appIds : new HashSet<long>();
            visualSelectionAppIds = appIds;
            appsGrid.Invalidate();
        }

        private void RestorePendingRightClickSelectionOrSelectRow(int rowIndex, int columnIndex)
        {
            contextMenuRows = null;
            if (pendingRightClickSelectionAppIds is { Count: > 1 } appIds
                && rowIndex == pendingRightClickRowIndex)
            {
                RestoreSelectionByAppIds(appIds, rowIndex, pendingRightClickColumnIndex >= 0 ? pendingRightClickColumnIndex : columnIndex);
                contextMenuRows = GetRowsByAppIds(appIds);
            }
            else
            {
                SelectGridRowsForRightClick(rowIndex, columnIndex);
            }

            ClearPendingRightClickState();
        }

        private void ClearPendingRightClickState()
        {
            pendingRightClickSelectionAppIds = null;
            pendingRightClickRowIndex = -1;
            pendingRightClickColumnIndex = -1;
        }

        private void OnAppsGridSelectionChanged(object? sender, EventArgs e)
        {
            if (!isRightClickInProgress)
            {
                var selectedAppIds = GetSelectedRows()
                    .Select(row => row.AppId)
                    .ToHashSet();
                lastStableMultiSelectionAppIds = selectedAppIds.Count > 1
                    ? selectedAppIds
                    : new HashSet<long>();
                visualSelectionAppIds = selectedAppIds;
            }

            UpdateSelectionStatus();
            appsGrid.Invalidate();
        }

        private void OnAppsGridRowPrePaint(object? sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= appsGrid.Rows.Count)
                return;

            var gridRow = appsGrid.Rows[e.RowIndex];
            if (gridRow.DataBoundItem is not AppCategoryManagementRow row)
                return;

            var shouldShowVisualSelection = visualSelectionAppIds.Count > 1
                && visualSelectionAppIds.Contains(row.AppId);
            gridRow.DefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;
            gridRow.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            gridRow.DefaultCellStyle.ForeColor = shouldShowVisualSelection
                ? SystemColors.HighlightText
                : SystemColors.WindowText;
            gridRow.DefaultCellStyle.BackColor = shouldShowVisualSelection
                ? SystemColors.Highlight
                : SystemColors.Window;
        }

        private void SelectGridRowsForRightClick(int rowIndex, int columnIndex)
        {
            if (!appsGrid.Rows[rowIndex].Selected)
                SelectSingleGridRow(rowIndex, columnIndex);
            else if (appsGrid.SelectedRows.Count <= 1)
                SetCurrentGridCell(rowIndex, columnIndex);
        }

        private void RestoreSelectionByAppIds(IReadOnlySet<long> appIds, int currentRowIndex, int columnIndex)
        {
            if (appIds.Count == 0 || appsGrid.Rows.Count == 0)
                return;

            var safeRowIndex = Math.Clamp(currentRowIndex, 0, appsGrid.Rows.Count - 1);
            var safeColumnIndex = Math.Clamp(columnIndex >= 0 ? columnIndex : 0, 0, appsGrid.Columns.Count - 1);
            appsGrid.CurrentCell = appsGrid.Rows[safeRowIndex].Cells[safeColumnIndex];
            appsGrid.ClearSelection();
            foreach (DataGridViewRow gridRow in appsGrid.Rows)
            {
                if (gridRow.DataBoundItem is AppCategoryManagementRow row && appIds.Contains(row.AppId))
                    gridRow.Selected = true;
            }

            UpdateSelectionStatus();
        }

        private IReadOnlyList<AppCategoryManagementRow> GetRowsByAppIds(IReadOnlySet<long> appIds)
        {
            return visibleRows
                .Where(row => appIds.Contains(row.AppId))
                .ToList();
        }

        private void RestoreSelectionByAppIds(IReadOnlySet<long> appIds, long currentAppId)
        {
            if (appIds.Count == 0 || appsGrid.Rows.Count == 0)
                return;

            var currentRowIndex = 0;
            var currentColumnIndex = GetFirstDisplayedColumnIndex();
            for (var index = 0; index < appsGrid.Rows.Count; index++)
            {
                if (appsGrid.Rows[index].DataBoundItem is AppCategoryManagementRow row
                    && row.AppId == currentAppId)
                {
                    currentRowIndex = index;
                    break;
                }
            }

            RestoreSelectionByAppIds(appIds, currentRowIndex, currentColumnIndex);
        }

        private void SelectSingleGridRow(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || rowIndex >= appsGrid.Rows.Count)
                return;

            appsGrid.ClearSelection();
            SetCurrentGridCell(rowIndex, columnIndex);
            appsGrid.Rows[rowIndex].Selected = true;
            UpdateSelectionStatus();
        }

        private void SetCurrentGridCell(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || rowIndex >= appsGrid.Rows.Count)
                return;

            var targetColumnIndex = Math.Clamp(columnIndex >= 0 ? columnIndex : 0, 0, appsGrid.Columns.Count - 1);
            appsGrid.CurrentCell = appsGrid.Rows[rowIndex].Cells[targetColumnIndex];
        }

        private void ShowCategoryMenu(AppCategoryManagementRow row, Point location)
        {
            categoryMenu.Items.Clear();
            var selectedRows = GetSelectedRows();
            if (contextMenuRows is { Count: > 0 } rowsForMenu)
                selectedRows = rowsForMenu;
            if (selectedRows.Count == 0)
                selectedRows = [row];
            visualSelectionAppIds = selectedRows.Select(row => row.AppId).ToHashSet();
            appsGrid.Invalidate();
            var isBulkSelection = selectedRows.Count > 1;

            var clearItem = new ToolStripMenuItem(isBulkSelection
                ? IsEnglish ? "Clear selected categories" : "선택 항목 분류 해제"
                : IsEnglish ? "Clear category" : "분류 해제")
            {
                Checked = !isBulkSelection && row.PrimaryCategoryId is null
            };
            clearItem.Click += (_, _) => SetCategories(selectedRows, null);
            categoryMenu.Items.Add(clearItem);

            if (categories.Count > 0)
                categoryMenu.Items.Add(new ToolStripSeparator());

            foreach (var category in categories)
            {
                var categoryItem = new ToolStripMenuItem(GetCategoryDisplayName(category))
                {
                    Checked = !isBulkSelection && row.PrimaryCategoryId == category.Id
                };
                categoryItem.Click += (_, _) => SetCategories(selectedRows, category.Id);
                categoryMenu.Items.Add(categoryItem);
            }

            categoryMenu.Items.Add(new ToolStripSeparator());
            var searchWebItem = new ToolStripMenuItem(IsEnglish ? "Search web" : "웹에서 검색");
            searchWebItem.Click += (_, _) => OpenWebSearch(row);
            categoryMenu.Items.Add(searchWebItem);

            categoryMenu.Show(appsGrid, location);
            if (selectedRows.Count > 1)
                BeginInvoke(new Action(() => RestoreSelectionByAppIds(selectedRows.Select(row => row.AppId).ToHashSet(), row.AppId)));
            contextMenuRows = null;
        }

        private IReadOnlyList<AppCategoryManagementRow> GetSelectedRows()
        {
            var rows = appsGrid.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(row => row.DataBoundItem)
                .OfType<AppCategoryManagementRow>()
                .OrderBy(row => visibleRows.ToList().FindIndex(x => x.AppId == row.AppId))
                .ToList();

            if (rows.Count > 0)
                return rows;

            return appsGrid.CurrentRow?.DataBoundItem is AppCategoryManagementRow currentRow
                ? [currentRow]
                : [];
        }

        private void UpdateSelectionStatus()
        {
            var selectedCount = appsGrid.SelectedRows.Count;
            if (selectedCount > 1)
            {
                statusLabel.Text = IsEnglish
                    ? $"{selectedCount:N0} apps selected."
                    : $"{selectedCount:N0}개 앱을 선택했습니다.";
            }
        }

        private sealed record CategorySelectionOption(long? Id, string Name)
        {
            public override string ToString()
            {
                return Name;
            }
        }

        private sealed record CategoryChangeUndo(IReadOnlyList<CategoryChangeUndoItem> Items);

        private sealed record CategoryChangeUndoItem(long AppId, long? PreviousCategoryId);
    }
}
