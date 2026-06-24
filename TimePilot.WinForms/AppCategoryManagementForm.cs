using System.Diagnostics;
using TimePilot.WinForms.KYS24;
using TimePilot.WinForms.Tables;

namespace TimePilot.WinForms
{
    internal sealed class AppCategoryManagementForm : Form
    {
        private readonly TimePilotStorage storage;
        private readonly AppSettings settings;
        private readonly UiLanguage language;
        private readonly AppIconCache appIconCache = new();
        private readonly ComboBox filterComboBox = new();
        private readonly ComboBox filterCategoryComboBox = new();
        private readonly TextBox searchTextBox = new();
        private readonly CheckBox showFileInfoCheckBox = new();
        private readonly ComboBox assignCategoryComboBox = new();
        private readonly Button applyCategoryButton = new();
        private readonly Button clearCategoryButton = new();
        private readonly Button applyRecommendationButton = new();
        private readonly Button manageCategoriesButton = new();
        private readonly Button importantCriteriaButton = new();
        private readonly Button refreshButton = new();
        private readonly Button searchWebButton = new();
        private readonly Button closeButton = new();
        private readonly Label guidanceLabel = new();
        private readonly Label statusLabel = new();
        private readonly ToolTip toolTip = new();
        private readonly DataGridView appsGrid = new();
        private readonly BindingSource appsBindingSource = new();
        private readonly ContextMenuStrip categoryMenu = new();
        private readonly ContextMenuStrip columnMenu = new();

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
        private bool isApplyingColumnVisibility;
        private int? selectionAnchorRowIndex;

        public AppCategoryManagementForm(TimePilotStorage storage, AppSettings settings, UiLanguage language)
        {
            this.storage = storage;
            this.settings = settings;
            this.language = language;

            InitializeComponent();
            LoadData();
        }

        public bool CategoriesChanged { get; private set; }

        private bool IsEnglish => language == UiLanguage.English;

        private string AllText => IsEnglish ? "All" : "전체";

        private string UncategorizedText => IsEnglish ? "Uncategorized only" : "미분류만";

        private string ImportantUnclassifiedText => IsEnglish ? "Important unclassified" : "중요 미분류";

        private string CategorizedText => IsEnglish ? "Categorized only" : "분류됨만";

        private string SpecificCategoryText => IsEnglish ? "Specific category" : "특정 분류";

        private string RecommendationsText => IsEnglish ? "Recommendations only" : "추천 있음";

        private string NoCategoryText => IsEnglish ? "(Uncategorized)" : "(미분류)";

        private string IdentityReviewText => IsEnglish ? "App match check needed" : "앱 구분 확인 필요";

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
            topPanel.Height = 120;
            topPanel.Padding = new Padding(12, 10, 12, 6);
            topPanel.WrapContents = true;

            guidanceLabel.AutoSize = false;
            guidanceLabel.Width = 820;
            guidanceLabel.Height = 38;
            guidanceLabel.Margin = new Padding(0, 0, 0, 4);
            guidanceLabel.ForeColor = SystemColors.GrayText;
            guidanceLabel.TextAlign = ContentAlignment.MiddleLeft;

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
            showFileInfoCheckBox.CheckedChanged += (_, _) =>
            {
                if (!isApplyingColumnVisibility)
                    SetFileInfoColumnVisibility(showFileInfoCheckBox.Checked, save: true);
            };

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
            applyRecommendationButton.Text = IsEnglish ? "Apply suggestion" : "추천 적용";
            applyRecommendationButton.Width = 96;
            applyRecommendationButton.Click += OnApplyRecommendationButtonClick;
            manageCategoriesButton.Text = IsEnglish ? "Categories..." : "분류 관리...";
            manageCategoriesButton.Width = 96;
            manageCategoriesButton.Click += OnManageCategoriesButtonClick;

            importantCriteriaButton.Text = IsEnglish ? "Criteria..." : "중요 기준...";
            importantCriteriaButton.Width = 96;
            importantCriteriaButton.Click += OnImportantCriteriaButtonClick;

            refreshButton.Text = IsEnglish ? "Refresh" : "새로고침";
            refreshButton.Width = 84;
            refreshButton.Click += (_, _) => ReloadAndApplyFilter();

            searchWebButton.Text = IsEnglish ? "Search web" : "웹 검색";
            searchWebButton.Width = 84;
            searchWebButton.Click += OnSearchWebButtonClick;

            topPanel.Controls.Add(guidanceLabel);
            topPanel.SetFlowBreak(guidanceLabel, true);
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
            topPanel.Controls.Add(applyRecommendationButton);
            topPanel.Controls.Add(manageCategoriesButton);
            topPanel.Controls.Add(importantCriteriaButton);
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
            appsGrid.ColumnHeaderMouseClick += OnAppsGridColumnHeaderContextClick;
            appsGrid.ColumnStateChanged += OnAppsGridColumnStateChanged;
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
                CreateTextColumn(nameof(AppCategoryManagementRow.AutomaticAppName), IsEnglish ? "Automatic name" : "자동 이름", 160),
                CreateTextColumn(nameof(AppCategoryManagementRow.UserAliasText), IsEnglish ? "Custom name" : "사용자 이름", 150, nameof(AppCategoryManagementRow.UserAlias)),
                CreateTextColumn(nameof(AppCategoryManagementRow.ProcessName), IsEnglish ? "Process" : "프로세스", 130),
                CreateTextColumn(nameof(AppCategoryManagementRow.TrackingTypeText), IsEnglish ? "Observed as" : "관측 유형", 110),
                CreateTextColumn(nameof(AppCategoryManagementRow.IdentityStatusText), IsEnglish ? "App match" : "앱 구분", 110, nameof(AppCategoryManagementRow.IdentityStatusText)),
                CreateTextColumn(nameof(AppCategoryManagementRow.FileDescriptionText), IsEnglish ? "Description" : "파일 설명", 180, nameof(AppCategoryManagementRow.FileDescription)),
                CreateTextColumn(nameof(AppCategoryManagementRow.ProductNameText), IsEnglish ? "Product" : "제품명", 160, nameof(AppCategoryManagementRow.ProductName)),
                CreateTextColumn(nameof(AppCategoryManagementRow.CompanyNameText), IsEnglish ? "Company" : "회사", 150, nameof(AppCategoryManagementRow.CompanyName)),
                CreateTextColumn(nameof(AppCategoryManagementRow.CategoryText), IsEnglish ? "Category" : "분류", 130),
                CreateTextColumn(nameof(AppCategoryManagementRow.RecommendedCategoryText), IsEnglish ? "Suggestion" : "추천 분류", 130, nameof(AppCategoryManagementRow.RecommendedCategoryText)),
                CreateTextColumn(nameof(AppCategoryManagementRow.RecommendationReasonText), IsEnglish ? "Suggestion reason" : "추천 근거", 150, nameof(AppCategoryManagementRow.RecommendationReasonText)),
                CreateTextColumn(nameof(AppCategoryManagementRow.LastObservedAtText), IsEnglish ? "Last observed" : "최근 감지", 150, nameof(AppCategoryManagementRow.LastObservedAt)),
                CreateTextColumn(nameof(AppCategoryManagementRow.ActiveUsageTimeText), IsEnglish ? "Active time" : "활성 사용", 110, nameof(AppCategoryManagementRow.ActiveUsageMs)),
                CreateTextColumn(nameof(AppCategoryManagementRow.RuntimeText), IsEnglish ? "Runtime" : "실행 시간", 110, nameof(AppCategoryManagementRow.RuntimeMs)),
                CreateTextColumn(nameof(AppCategoryManagementRow.SwitchCountText), IsEnglish ? "Switches" : "전환", 80, nameof(AppCategoryManagementRow.SwitchCount)),
                CreateTextColumn(nameof(AppCategoryManagementRow.RuntimeSegmentCountText), IsEnglish ? "Segments" : "구간", 80, nameof(AppCategoryManagementRow.RuntimeSegmentCount)),
                CreateTextColumn(nameof(AppCategoryManagementRow.ExecutablePath), IsEnglish ? "Path" : "실행 경로", 320));

            appsGrid.Columns[nameof(AppCategoryManagementRow.TrackingTypeText) + "Column"].ToolTipText = IsEnglish
                ? "Shows whether the app was observed as a visible app, user process, or all-process item."
                : "앱이 화면 앱, 사용자 프로세스, 전체 프로세스 항목 중 어떤 방식으로 관측되었는지 보여줍니다.";
            appsGrid.Columns[nameof(AppCategoryManagementRow.IdentityStatusText) + "Column"].ToolTipText = IsEnglish
                ? "Shows whether TimePilot can safely treat records with this name as the same app."
                : "TimePilot이 이 이름의 기록을 같은 앱으로 보아도 되는지 확인할 수 있게 보여줍니다.";
            appsGrid.Columns[nameof(AppCategoryManagementRow.RecommendedCategoryText) + "Column"].ToolTipText = IsEnglish
                ? "Shows a conservative local category suggestion for unclassified apps."
                : "미분류 앱에 대해 보수적인 로컬 분류 추천을 보여줍니다.";

            ApplySavedColumnVisibility();

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

        private void SetFileInfoColumnVisibility(bool visible, bool save)
        {
            SetColumnVisible(nameof(AppCategoryManagementRow.FileDescriptionText), visible, save);
            SetColumnVisible(nameof(AppCategoryManagementRow.ProductNameText), visible, save);
            SetColumnVisible(nameof(AppCategoryManagementRow.CompanyNameText), visible, save);
            SetColumnVisible(nameof(AppCategoryManagementRow.ExecutablePath), visible, save);
            SyncFileInfoCheckBox();
        }

        private void ApplySavedColumnVisibility()
        {
            isApplyingColumnVisibility = true;
            try
            {
                foreach (DataGridViewColumn column in appsGrid.Columns)
                {
                    column.Visible = GetSavedColumnVisibility(column);
                }

                EnsureRequiredColumnsVisible();
                SyncFileInfoCheckBox();
            }
            finally
            {
                isApplyingColumnVisibility = false;
            }
        }

        private bool GetSavedColumnVisibility(DataGridViewColumn column)
        {
            if (settings.AppCategoryManagementColumnVisibility.TryGetValue(column.Name, out var visible))
                return visible || IsRequiredColumn(column);

            return !IsFileInfoColumn(column);
        }

        private void SetColumnVisible(string propertyName, bool visible, bool save)
        {
            var columnName = propertyName + "Column";
            if (!appsGrid.Columns.Contains(columnName))
                return;

            SetColumnVisible(appsGrid.Columns[columnName], visible, save);
        }

        private void SetColumnVisible(DataGridViewColumn column, bool visible, bool save)
        {
            if (IsRequiredColumn(column))
                visible = true;

            column.Visible = visible;
            if (!save)
                return;

            settings.AppCategoryManagementColumnVisibility[column.Name] = visible;
            settings.Save();
            SyncFileInfoCheckBox();
        }

        private void SyncFileInfoCheckBox()
        {
            isApplyingColumnVisibility = true;
            try
            {
                showFileInfoCheckBox.Checked = GetFileInfoColumns().Any(column => column.Visible);
            }
            finally
            {
                isApplyingColumnVisibility = false;
            }
        }

        private IEnumerable<DataGridViewColumn> GetFileInfoColumns()
        {
            return new[]
                {
                    nameof(AppCategoryManagementRow.FileDescriptionText) + "Column",
                    nameof(AppCategoryManagementRow.ProductNameText) + "Column",
                    nameof(AppCategoryManagementRow.CompanyNameText) + "Column",
                    nameof(AppCategoryManagementRow.ExecutablePath) + "Column"
                }
                .Where(name => appsGrid.Columns.Contains(name))
                .Select(name => appsGrid.Columns[name]);
        }

        private static bool IsFileInfoColumn(DataGridViewColumn column)
        {
            return column.DataPropertyName is nameof(AppCategoryManagementRow.FileDescriptionText)
                or nameof(AppCategoryManagementRow.ProductNameText)
                or nameof(AppCategoryManagementRow.CompanyNameText)
                or nameof(AppCategoryManagementRow.ExecutablePath);
        }

        private static bool IsRequiredColumn(DataGridViewColumn column)
        {
            return column.DataPropertyName == nameof(AppCategoryManagementRow.AppName);
        }

        private void EnsureRequiredColumnsVisible()
        {
            foreach (DataGridViewColumn column in appsGrid.Columns)
            {
                if (IsRequiredColumn(column))
                    column.Visible = true;
            }
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

        private void OnManageCategoriesButtonClick(object? sender, EventArgs e)
        {
            using var form = new AppCategoryEditorForm(storage, language);
            form.Icon = Icon;
            form.ShowDialog(this);
            if (!form.CategoriesChanged)
                return;

            CategoriesChanged = true;
            LoadData();
        }

        private void OnImportantCriteriaButtonClick(object? sender, EventArgs e)
        {
            using var form = new ImportantUnclassifiedCriteriaForm(settings, language);
            form.Icon = Icon;
            if (form.ShowDialog(this) != DialogResult.OK)
                return;

            settings.SetImportantUnclassifiedCriteria(
                form.ActiveMinutes,
                form.SwitchCount,
                form.IncludeRecommendations,
                form.VisibleAppsOnly,
                form.ExcludeBackgroundOnly);
            ApplyFilter();
            statusLabel.Text = IsEnglish
                ? "Important unclassified criteria updated."
                : "중요 미분류 기준을 변경했습니다.";
        }

        private IReadOnlyList<AppCategoryManagementRow> AddIcons(IReadOnlyList<AppCategoryManagementRow> rows)
        {
            return rows
                .Select(row =>
                {
                    var metadata = GetFileMetadata(row.ExecutablePath);
                    var enrichedRow = row with
                    {
                        FileDescription = metadata.FileDescription,
                        ProductName = metadata.ProductName,
                        CompanyName = metadata.CompanyName,
                        CategoryDisplayName = GetCategoryDisplayName(row.PrimaryCategoryId, row.CategoryName),
                        HasExtractedAppIcon = HasDistinctAssociatedIcon(row.ExecutablePath),
                        AppIcon = appIconCache.GetIcon(row.ExecutablePath)
                    };

                    var recommendation = AppCategoryRecommendationService.Recommend(enrichedRow, categories, language);
                    return recommendation is null
                        ? enrichedRow
                        : enrichedRow with
                        {
                            RecommendedCategoryId = recommendation.CategoryId,
                            RecommendedCategoryName = recommendation.CategoryName,
                            RecommendationReason = recommendation.Reason
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
                    ImportantUnclassifiedText,
                    CategorizedText,
                    RecommendationsText,
                    IdentityReviewText,
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
            else if (selectedFilter == ImportantUnclassifiedText)
                rows = rows.Where(IsImportantUnclassified);
            else if (selectedFilter == CategorizedText)
                rows = rows.Where(x => x.PrimaryCategoryId is not null);
            else if (selectedFilter == RecommendationsText)
                rows = rows.Where(x => x.HasRecommendation);
            else if (selectedFilter == IdentityReviewText)
                rows = rows.Where(x => x.NeedsIdentityReview);
            else if (selectedFilter == SpecificCategoryText
                && filterCategoryComboBox.SelectedItem is CategorySelectionOption category)
                rows = rows.Where(x => x.PrimaryCategoryId == category.Id);

            var searchText = searchTextBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                rows = rows.Where(x =>
                    x.AppName.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
                    || x.AutomaticAppName.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
                    || (x.UserAlias?.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ?? false)
                    || x.ProcessName.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
                    || x.CategoryText.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
                    || x.IdentityStatusText.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
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
            UpdateGuidanceSummary();
            RestoreSelection(visibleRows);
        }

        private void UpdateGuidanceSummary()
        {
            var unclassifiedCount = allRows.Count(row => row.PrimaryCategoryId is null);
            var importantUnclassifiedCount = allRows.Count(IsImportantUnclassified);
            var recommendationCount = allRows.Count(row => row.PrimaryCategoryId is null && row.HasRecommendation);
            var criteriaText = GetImportantCriteriaText();
            guidanceLabel.Text = IsEnglish
                ? $"Unclassified {unclassifiedCount:N0} · Important {importantUnclassifiedCount:N0} · Suggestions {recommendationCount:N0}{Environment.NewLine}{criteriaText}"
                : $"미분류 {unclassifiedCount:N0}개 · 중요 미분류 {importantUnclassifiedCount:N0}개 · 추천 가능 {recommendationCount:N0}개{Environment.NewLine}{criteriaText}";
            toolTip.SetToolTip(guidanceLabel, criteriaText);
            toolTip.SetToolTip(importantCriteriaButton, criteriaText);
        }

        private string GetImportantCriteriaText()
        {
            var targetText = settings.ImportantUnclassifiedVisibleAppsOnly
                ? IsEnglish ? "visible apps" : "화면 앱"
                : IsEnglish ? "all tracked apps" : "전체 관측 앱";
            var recommendationText = settings.ImportantUnclassifiedIncludeRecommendations
                ? IsEnglish ? "suggestions included" : "추천 포함"
                : IsEnglish ? "suggestions ignored" : "추천 제외";
            var backgroundText = settings.ImportantUnclassifiedExcludeBackgroundOnly
                ? IsEnglish ? "background-only excluded" : "백그라운드 단독 제외"
                : IsEnglish ? "background-only included" : "백그라운드 단독 포함";

            return IsEnglish
                ? $"Criteria: {recommendationText} · {targetText} · {settings.ImportantUnclassifiedActiveMinutes}m active/{settings.ImportantUnclassifiedSwitchCount} switches · {backgroundText}"
                : $"기준: {recommendationText} · {targetText} · 활성 {settings.ImportantUnclassifiedActiveMinutes}분/전환 {settings.ImportantUnclassifiedSwitchCount}회 · {backgroundText}";
        }

        private bool IsImportantUnclassified(AppCategoryManagementRow row)
        {
            if (row.PrimaryCategoryId is not null)
                return false;

            if (settings.ImportantUnclassifiedIncludeRecommendations && row.HasRecommendation)
                return true;

            var isVisibleOrUsed = row.HasForegroundActivity || row.HasMainWindow;
            if (settings.ImportantUnclassifiedVisibleAppsOnly && !isVisibleOrUsed)
                return false;

            if (settings.ImportantUnclassifiedExcludeBackgroundOnly && !isVisibleOrUsed)
                return false;

            return row.ActiveUsageMs >= settings.ImportantUnclassifiedActiveMs
                || row.SwitchCount >= settings.ImportantUnclassifiedSwitchCount;
        }

        private IOrderedEnumerable<AppCategoryManagementRow> SortRows(IEnumerable<AppCategoryManagementRow> rows)
        {
            return sortProperty switch
            {
                nameof(AppCategoryManagementRow.AppName) => OrderRows(rows, x => x.AppName),
                nameof(AppCategoryManagementRow.AutomaticAppName) => OrderRows(rows, x => x.AutomaticAppName),
                nameof(AppCategoryManagementRow.UserAlias) => OrderRows(rows, x => x.UserAlias ?? ""),
                nameof(AppCategoryManagementRow.ProcessName) => OrderRows(rows, x => x.ProcessName),
                nameof(AppCategoryManagementRow.TrackingTypeText) => OrderRows(rows, x => x.TrackingTypeText),
                nameof(AppCategoryManagementRow.IdentityStatusText) => OrderRows(rows, x => x.IdentityStatusText),
                nameof(AppCategoryManagementRow.HasAppIcon) => OrderRows(rows, x => x.HasAppIcon),
                nameof(AppCategoryManagementRow.FileDescription) => OrderRows(rows, x => x.FileDescription ?? ""),
                nameof(AppCategoryManagementRow.ProductName) => OrderRows(rows, x => x.ProductName ?? ""),
                nameof(AppCategoryManagementRow.CompanyName) => OrderRows(rows, x => x.CompanyName ?? ""),
                nameof(AppCategoryManagementRow.CategoryText) => OrderRows(rows, x => x.CategoryText),
                nameof(AppCategoryManagementRow.RecommendedCategoryText) => OrderRows(rows, x => x.RecommendedCategoryText),
                nameof(AppCategoryManagementRow.RecommendationReasonText) => OrderRows(rows, x => x.RecommendationReasonText),
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
            return GridRowOrderer.OrderRows(rows, keySelector, sortOrder);
        }

        private void OnAppsGridColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            if (e.ColumnIndex < 0)
                return;

            if (appsGrid.Columns[e.ColumnIndex].SortMode == DataGridViewColumnSortMode.NotSortable)
                return;

            var propertyName = appsGrid.Columns[e.ColumnIndex].Tag as string
                ?? appsGrid.Columns[e.ColumnIndex].DataPropertyName;
            if (string.IsNullOrWhiteSpace(propertyName))
                return;

            sortOrder = string.Equals(sortProperty, propertyName, StringComparison.Ordinal)
                ? GridSortOrderHelper.Toggle(sortOrder)
                : SortOrder.Descending;
            sortProperty = propertyName;
            SortVisibleRowsPreservingView();
        }

        private void OnAppsGridColumnHeaderContextClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.ColumnIndex < 0)
                return;

            ShowColumnMenu(appsGrid.Columns[e.ColumnIndex], appsGrid.PointToClient(Cursor.Position));
        }

        private void ShowColumnMenu(DataGridViewColumn clickedColumn, Point location)
        {
            columnMenu.Items.Clear();

            var hideItem = new ToolStripMenuItem(IsEnglish ? "Hide this column" : "이 열 숨기기")
            {
                Enabled = clickedColumn.Visible && !IsRequiredColumn(clickedColumn)
            };
            hideItem.Click += (_, _) => SetColumnVisible(clickedColumn, false, save: true);
            columnMenu.Items.Add(hideItem);
            columnMenu.Items.Add(new ToolStripSeparator());

            var columnsItem = new ToolStripMenuItem(IsEnglish ? "Visible columns" : "표시할 열");
            foreach (DataGridViewColumn column in appsGrid.Columns.Cast<DataGridViewColumn>().OrderBy(column => column.DisplayIndex))
            {
                var item = new ToolStripMenuItem(column.HeaderText)
                {
                    Checked = column.Visible,
                    Enabled = !IsRequiredColumn(column)
                };
                item.Click += (_, _) => SetColumnVisible(column, !column.Visible, save: true);
                columnsItem.DropDownItems.Add(item);
            }

            columnMenu.Items.Add(columnsItem);
            columnMenu.Items.Add(new ToolStripSeparator());

            var resetItem = new ToolStripMenuItem(IsEnglish ? "Reset columns" : "기본값으로 되돌리기");
            resetItem.Click += (_, _) => ResetColumnVisibility();
            columnMenu.Items.Add(resetItem);

            columnMenu.Show(appsGrid, location);
        }

        private void ResetColumnVisibility()
        {
            settings.AppCategoryManagementColumnVisibility.Clear();
            settings.Save();
            ApplySavedColumnVisibility();
        }

        private void OnAppsGridColumnStateChanged(object? sender, DataGridViewColumnStateChangedEventArgs e)
        {
            if (isApplyingColumnVisibility || e.StateChanged != DataGridViewElementStates.Visible)
                return;

            if (IsRequiredColumn(e.Column) && !e.Column.Visible)
            {
                e.Column.Visible = true;
                return;
            }

            settings.AppCategoryManagementColumnVisibility[e.Column.Name] = e.Column.Visible;
            settings.Save();
            SyncFileInfoCheckBox();
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

        private void OnApplyRecommendationButtonClick(object? sender, EventArgs e)
        {
            ApplyRecommendations(GetSelectedRows());
        }

        private void ApplyRecommendations(IReadOnlyList<AppCategoryManagementRow> rows)
        {
            var rowsToChange = rows
                .Where(row => row.PrimaryCategoryId is null && row.RecommendedCategoryId is not null)
                .GroupBy(row => row.AppId)
                .Select(group => group.First())
                .ToList();
            if (rowsToChange.Count == 0)
            {
                statusLabel.Text = IsEnglish
                    ? "No selected unclassified apps have a recommendation."
                    : "선택한 미분류 앱 중 적용할 추천이 없습니다.";
                return;
            }

            if (rowsToChange.Count > 1 && !ConfirmBulkRecommendationApply(rowsToChange.Count))
                return;

            lastCategoryChange = new CategoryChangeUndo(
                rowsToChange
                    .Select(row => new CategoryChangeUndoItem(row.AppId, row.PrimaryCategoryId))
                    .ToList());

            foreach (var row in rowsToChange)
                storage.SetAppPrimaryCategory(row.AppId, row.RecommendedCategoryId, AppCategorySource.Recommendation);

            CategoriesChanged = true;
            rowsToRestoreAfterFilter = rowsToChange.Select(row => row.AppId).ToHashSet();
            allRows = AddIcons(storage.GetAppCategoryManagementRows(DateTimeOffset.UtcNow));
            UpdateVisibleRows(rowsToRestoreAfterFilter);
            statusLabel.Text = IsEnglish
                ? $"{rowsToChange.Count:N0} recommendations applied. Press Ctrl+Z to undo the last change."
                : $"{rowsToChange.Count:N0}개 앱에 추천 분류를 적용했습니다. Ctrl+Z로 직전 변경 1회를 되돌릴 수 있습니다.";
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

        private bool ConfirmBulkRecommendationApply(int count)
        {
            var message = IsEnglish
                ? $"Apply recommendations to {count:N0} selected unclassified apps?"
                : $"선택한 미분류 앱 {count:N0}개에 추천 분류를 적용할까요?";

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

            if (!isBulkSelection)
            {
                var editNameItem = new ToolStripMenuItem(IsEnglish ? "Edit custom name..." : "사용자 이름 수정...");
                editNameItem.Click += (_, _) => EditCustomName(row);
                categoryMenu.Items.Add(editNameItem);

                var clearNameItem = new ToolStripMenuItem(IsEnglish ? "Clear custom name" : "사용자 이름 초기화")
                {
                    Enabled = !string.IsNullOrWhiteSpace(row.UserAlias)
                };
                clearNameItem.Click += (_, _) => SetCustomName(row, null);
                categoryMenu.Items.Add(clearNameItem);
                categoryMenu.Items.Add(new ToolStripSeparator());
            }
            else
            {
                var clearSelectedNamesItem = new ToolStripMenuItem(IsEnglish
                    ? "Clear selected custom names"
                    : "선택 항목 사용자 이름 초기화")
                {
                    Enabled = selectedRows.Any(selectedRow => !string.IsNullOrWhiteSpace(selectedRow.UserAlias))
                };
                clearSelectedNamesItem.Click += (_, _) => ClearCustomNames(selectedRows);
                categoryMenu.Items.Add(clearSelectedNamesItem);
                categoryMenu.Items.Add(new ToolStripSeparator());
            }

            var clearItem = new ToolStripMenuItem(isBulkSelection
                ? IsEnglish ? "Clear selected categories" : "선택 항목 분류 해제"
                : IsEnglish ? "Clear category" : "분류 해제")
            {
                Checked = !isBulkSelection && row.PrimaryCategoryId is null
            };
            clearItem.Click += (_, _) => SetCategories(selectedRows, null);
            categoryMenu.Items.Add(clearItem);

            var recommendationRows = selectedRows
                .Where(selectedRow => selectedRow.PrimaryCategoryId is null && selectedRow.RecommendedCategoryId is not null)
                .ToList();
            if (recommendationRows.Count > 0)
            {
                var recommendationItem = new ToolStripMenuItem(isBulkSelection
                    ? IsEnglish ? "Apply selected recommendations" : "선택 항목 추천 적용"
                    : IsEnglish ? "Apply recommendation" : "추천 적용");
                recommendationItem.Click += (_, _) => ApplyRecommendations(recommendationRows);
                categoryMenu.Items.Add(recommendationItem);
            }

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
            var identityItem = new ToolStripMenuItem(IsEnglish ? "Show app match info" : "앱 구분 정보 보기")
            {
                Enabled = !isBulkSelection
            };
            identityItem.Click += (_, _) => ShowIdentityInfo(row);
            categoryMenu.Items.Add(identityItem);
            categoryMenu.Items.Add(new ToolStripSeparator());
            var searchWebItem = new ToolStripMenuItem(IsEnglish ? "Search web" : "웹에서 검색");
            searchWebItem.Click += (_, _) => OpenWebSearch(row);
            categoryMenu.Items.Add(searchWebItem);

            categoryMenu.Show(appsGrid, location);
            if (selectedRows.Count > 1)
                BeginInvoke(new Action(() => RestoreSelectionByAppIds(selectedRows.Select(row => row.AppId).ToHashSet(), row.AppId)));
            contextMenuRows = null;
        }

        private void ShowIdentityInfo(AppCategoryManagementRow row)
        {
            var observations = storage.GetAppIdentityObservations(row.AppId);
            var observationText = observations.Count == 0
                ? IsEnglish ? "No detailed observations yet." : "아직 상세 관측 정보가 없습니다."
                : string.Join(
                    Environment.NewLine,
                    observations.Select(observation =>
                        $"- {observation.DisplayName} / {observation.ProcessName} / {observation.ExecutablePath ?? (IsEnglish ? "(no path)" : "(경로 없음)")} / {observation.LastSeenAt.ToLocalTime():yyyy-MM-dd HH:mm:ss} / {observation.ObservedCount:N0}"));
            var message = string.Join(
                Environment.NewLine + Environment.NewLine,
                IsEnglish
                    ? $"App match: {row.IdentityStatusText}"
                    : $"앱 구분: {row.IdentityStatusText}",
                IsEnglish
                    ? "This is a reference signal for checking whether records with the same name can be treated as the same app. In this version, records with the same name are still grouped as one app. You can set a category or use web search to identify the app. Splitting records into separate apps will be considered in a later version."
                    : "이 표시는 같은 이름의 기록을 같은 앱으로 보아도 되는지 확인하기 위한 참고 정보입니다. 현재 버전에서는 같은 이름의 기록이 하나의 앱으로 묶입니다. 필요하면 분류를 지정하거나 웹 검색으로 앱을 확인할 수 있습니다. 서로 다른 앱으로 분리하는 기능은 이후 버전에서 검토합니다.",
                row.IdentityDetailText,
                IsEnglish
                    ? $"Process: {row.ProcessName}"
                    : $"프로세스: {row.ProcessName}",
                IsEnglish
                    ? $"Path: {row.ExecutablePath ?? "(none)"}"
                    : $"경로: {row.ExecutablePath ?? "(없음)"}",
                IsEnglish ? $"Observed entries:{Environment.NewLine}{observationText}" : $"관측 항목:{Environment.NewLine}{observationText}");

            MessageBox.Show(
                this,
                message,
                IsEnglish ? "App match information" : "앱 구분 정보",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void EditCustomName(AppCategoryManagementRow row)
        {
            var alias = ShowCustomNameDialog(row);
            if (alias is null)
                return;

            SetCustomName(row, alias);
        }

        private void SetCustomName(AppCategoryManagementRow row, string? alias)
        {
            var normalizedAlias = string.IsNullOrWhiteSpace(alias) ? null : alias.Trim();
            if (string.Equals(row.UserAlias, normalizedAlias, StringComparison.Ordinal))
                return;

            if (normalizedAlias is not null && HasDuplicateAlias(row.AppId, normalizedAlias) && !ConfirmDuplicateAlias(normalizedAlias))
                return;

            storage.SetAppUserAlias(row.AppId, normalizedAlias);
            CategoriesChanged = true;
            rowsToRestoreAfterFilter = new HashSet<long> { row.AppId };
            allRows = AddIcons(storage.GetAppCategoryManagementRows(DateTimeOffset.UtcNow));
            UpdateVisibleRows(rowsToRestoreAfterFilter);
            statusLabel.Text = normalizedAlias is null
                ? IsEnglish ? "Custom name cleared." : "사용자 이름을 초기화했습니다."
                : IsEnglish ? "Custom name updated." : "사용자 이름을 수정했습니다.";
        }

        private void ClearCustomNames(IReadOnlyList<AppCategoryManagementRow> rows)
        {
            var rowsToClear = rows
                .Where(row => !string.IsNullOrWhiteSpace(row.UserAlias))
                .GroupBy(row => row.AppId)
                .Select(group => group.First())
                .ToList();
            if (rowsToClear.Count == 0)
                return;

            foreach (var row in rowsToClear)
            {
                storage.SetAppUserAlias(row.AppId, null);
            }

            CategoriesChanged = true;
            rowsToRestoreAfterFilter = rowsToClear.Select(row => row.AppId).ToHashSet();
            allRows = AddIcons(storage.GetAppCategoryManagementRows(DateTimeOffset.UtcNow));
            UpdateVisibleRows(rowsToRestoreAfterFilter);
            statusLabel.Text = IsEnglish
                ? $"Cleared custom names for {rowsToClear.Count:N0} selected app(s)."
                : $"선택 항목 {rowsToClear.Count:N0}개의 사용자 이름을 초기화했습니다.";
        }

        private bool HasDuplicateAlias(long currentAppId, string alias)
        {
            return allRows.Any(row =>
                row.AppId != currentAppId
                && string.Equals(row.UserAlias?.Trim(), alias, StringComparison.CurrentCultureIgnoreCase));
        }

        private bool ConfirmDuplicateAlias(string alias)
        {
            var message = IsEnglish
                ? $"Another app already uses the custom name \"{alias}\".\n\nMultiple apps with the same display name can be harder to distinguish in Summary and Timeline. Continue?"
                : $"이미 \"{alias}\" 사용자 이름을 사용하는 앱이 있습니다.\n\n여러 앱이 같은 이름으로 표시되면 요약과 타임라인에서 구분하기 어려울 수 있습니다. 계속할까요?";

            return CenteredMessageDialog.Show(
                this,
                message,
                IsEnglish ? "Duplicate Custom Name" : "사용자 이름 중복",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning) == DialogResult.OK;
        }

        private string? ShowCustomNameDialog(AppCategoryManagementRow row)
        {
            using var dialog = new Form();
            var descriptionLabel = new Label();
            var nameTextBox = new TextBox();
            var saveButton = new Button();
            var cancelButton = new Button();

            dialog.SuspendLayout();

            descriptionLabel.AutoSize = false;
            descriptionLabel.Location = new Point(16, 16);
            descriptionLabel.Size = new Size(396, 52);
            descriptionLabel.Text = IsEnglish
                ? $"Automatic name: {row.AutomaticAppName}\nProcess: {row.ProcessName}"
                : $"자동 이름: {row.AutomaticAppName}\n프로세스: {row.ProcessName}";

            nameTextBox.Location = new Point(16, 76);
            nameTextBox.Size = new Size(396, 23);
            nameTextBox.Text = row.UserAlias ?? "";

            saveButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            saveButton.DialogResult = DialogResult.OK;
            saveButton.Location = new Point(256, 116);
            saveButton.Size = new Size(75, 27);
            saveButton.Text = IsEnglish ? "Save" : "저장";

            cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(337, 116);
            cancelButton.Size = new Size(75, 27);
            cancelButton.Text = IsEnglish ? "Cancel" : "취소";

            dialog.AcceptButton = saveButton;
            dialog.CancelButton = cancelButton;
            dialog.ClientSize = new Size(428, 160);
            dialog.Controls.Add(descriptionLabel);
            dialog.Controls.Add(nameTextBox);
            dialog.Controls.Add(saveButton);
            dialog.Controls.Add(cancelButton);
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            dialog.MaximizeBox = false;
            dialog.MinimizeBox = false;
            dialog.ShowIcon = false;
            dialog.ShowInTaskbar = false;
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.Text = IsEnglish ? "Edit Custom Name" : "사용자 이름 수정";

            dialog.ResumeLayout(false);
            dialog.PerformLayout();

            return dialog.ShowDialog(this) == DialogResult.OK ? nameTextBox.Text : null;
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
