namespace TimePilot.WinForms.KYS24;

internal sealed class CategoryImportText(UiLanguage language)
{
    private bool English => language == UiLanguage.English;
    public string Title => English ? "Import app categories..." : "앱 분류만 가져오기...";
    public string ExportTitle => English ? "Export app categories..." : "앱 분류 내보내기...";
    public string ImportFilter => English ? "Category JSON or backup ZIP (*.json;*.zip)|*.json;*.zip" : "분류 JSON 또는 백업 ZIP (*.json;*.zip)|*.json;*.zip";
    public string ExportFilter => English ? "Category JSON (*.json)|*.json" : "분류 JSON (*.json)|*.json";
    public string ExportWarning => English
        ? "Export categories, colors and assignments to a category-only JSON file. Usage records, settings, executable paths and aliases are excluded. Category names and classified app process names are included and may reveal private information. Review the file before sharing. This is not a full backup. Continue?"
        : "분류·색상·앱별 분류 지정을 전용 JSON 파일로 내보냅니다. 사용 기록·설정·실행 경로·별칭은 제외합니다. 분류명과 분류된 앱의 프로세스명은 포함되므로 개인 정보가 드러날 수 있습니다. 공유 전에 내용을 확인하세요. 전체 백업 파일은 아닙니다. 계속할까요?";
    public string Exporting => English ? "Exporting app categories..." : "앱 분류 내보내기 중...";
    public string ExportCompleted(string path) => English ? $"Category export completed.\n{path}" : $"분류 내보내기 완료.\n{path}";
    public string ExportFailed(string reason) => English ? $"Could not export categories.\n{reason}" : $"분류 내보내기에 실패했습니다.\n{reason}";
    public string ProcessNameOnlyWarning => English
        ? "This category-only file excludes executable paths. Apps are matched by process name only. Missing entries never clear current assignments. This is not a complete category restore."
        : "분류 전용 파일에는 실행 경로가 없어 프로세스명으로만 앱을 맞춥니다. 파일에 없는 항목은 초기화하지 않습니다. 완전한 분류 복원은 아닙니다.";
    public string Warning => English
        ? "Keep current assignments by default, or choose file priority. Usage records and settings remain unchanged. Missing or unclassified entries never clear current assignments. Known path conflicts are skipped. A safety backup is recommended."
        : "기본값은 현재 분류 유지이며 파일 우선도 선택할 수 있습니다. 사용 기록·시간·설정은 변경하지 않습니다. 파일에 없거나 미분류인 항목은 초기화하지 않으며 실행 경로 충돌은 건너뜁니다. 안전 백업 생성을 권장합니다.";
    public string Priority => English ? "Priority" : "적용 기준";
    public string CurrentPriority => English ? "Keep current assignments (default)" : "현재 분류 유지 (기본)";
    public string FilePriority => English ? "Use file assignments" : "파일 분류 우선";
    public string ReplaceColors => English ? "Use file colors for existing categories" : "기존 분류 색상도 파일 값으로 변경";
    public string CurrentColor => English ? "Current color" : "현재 색상";
    public string FileColor => English ? "File color" : "파일 색상";
    public string CategoryRow => English ? "Category" : "분류";
    public string ColorValue(string? value) => string.IsNullOrWhiteSpace(value) ? English ? "Default" : "기본색" : value;
    public string OverwriteConfirmation(CategoryImportPlan plan) => English
        ? $"Replace {plan.ReplacedApps} app assignments, including {plan.ReplacedUserApps} user assignments, and change {plan.ChangedColors} existing category colors? Color changes affect all apps using those categories. Approve the changes shown in the preview? Usage records and settings will not be restored."
        : $"앱 지정 {plan.ReplacedApps}개(사용자 지정 {plan.ReplacedUserApps}개 포함)를 교체하고 기존 분류 색상 {plan.ChangedColors}개를 변경할까요? 색상 변경은 해당 분류를 사용하는 모든 앱에 영향을 줍니다. 미리보기에 표시된 변경을 승인하면 적용합니다. 사용 기록과 설정은 복원하지 않습니다.";
    public string Analyzing => English ? "Reading the category or backup file..." : "분류 또는 백업 파일을 확인하는 중...";
    public string Applying => English ? "Creating safety backup and applying categories..." : "안전 백업 생성 및 분류 적용 중...";
    public string ApplyingWithoutBackup => English ? "Applying categories without a safety backup..." : "안전 백업 없이 분류 적용 중...";
    public string ApplyWithBackup => English ? "Create safety backup and apply" : "안전 백업 후 적용";
    public string ApplyWithoutBackup => English ? "Apply without backup" : "백업 없이 적용";
    public string CreateBackup => English ? "Create safety backup before applying (recommended)" : "적용 전 안전 백업 만들기 (권장)";
    public string SkipBackupConfirmation => English
        ? "Apply without creating a safety backup? Usage records and settings will remain, but there will be no new recovery backup for category changes."
        : "안전 백업을 만들지 않고 적용할까요? 사용 기록과 설정은 유지하지만, 분류 변경을 되돌릴 새 복구용 백업은 생성되지 않습니다.";
    public string NoChanges => English
        ? "There are no changes to apply. Review kept or skipped entries, then close this window. No backup or import will run."
        : "현재 데이터에 적용할 변경이 없습니다. 유지·건너뛰기 항목을 확인하고 닫으세요. 백업이나 가져오기는 실행하지 않습니다.";
    public string Cancel => English ? "Cancel" : "취소";
    public string Close => English ? "Close" : "닫기";
    public string App => English ? "App / category" : "앱 / 분류";
    public string Current => English ? "Current category" : "현재 분류";
    public string Backup => English ? "File category" : "파일 분류";
    public string Result => English ? "Action" : "처리";
    public string Category(string? name)
    {
        if (name is null) return "-";
        var builtin = AppCategoryDisplay.BuiltinCategories.FirstOrDefault(c =>
            string.Equals(c.CanonicalName, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(c.KoreanName, name, StringComparison.OrdinalIgnoreCase));
        return builtin is null ? name : English ? builtin.CanonicalName : builtin.KoreanName;
    }
    public string Summary(CategoryImportPlan plan) => English
        ? $"New categories: {plan.AddedCategories} | New assignments: {plan.AssignedApps} | Replaced assignments: {plan.ReplacedApps} (user: {plan.ReplacedUserApps}) | Colors: {plan.ChangedColors}"
        : $"분류 추가: {plan.AddedCategories}개 | 새 지정: {plan.AssignedApps}개 | 지정 교체: {plan.ReplacedApps}개(사용자 {plan.ReplacedUserApps}개) | 색상 변경: {plan.ChangedColors}개";
    public string Completed(string? safetyPath) => safetyPath is null
        ? English ? "Category import completed. Usage records and settings were not restored.\nNo safety backup was created by your choice."
            : "분류 가져오기 완료. 사용 기록과 설정은 복원하지 않았습니다.\n사용자 선택에 따라 안전 백업을 생성하지 않았습니다."
        : English ? $"Category import completed. Usage records and settings were not restored.\nSafety backup: {safetyPath}"
            : $"분류 가져오기 완료. 사용 기록과 설정은 복원하지 않았습니다.\n안전 백업: {safetyPath}";
    public string RefreshFailed(string reason) => English
        ? $"Categories were imported, but the view could not refresh. Reopen the app.\n{reason}"
        : $"분류는 반영했지만 화면을 갱신하지 못했습니다. 앱을 다시 실행하세요.\n{reason}";
    public string Failed(string reason) => English
        ? $"Import failed. No category changes were committed. Reopen the file to try again.\n{reason}"
        : $"가져오기에 실패하여 분류 변경을 반영하지 않았습니다. 파일을 다시 열어 시도하세요.\n{reason}";
    public string Action(CategoryImportAction action) => (English, action) switch
    {
        (true, CategoryImportAction.Replace) => "Replace with file assignment",
        (false, CategoryImportAction.Replace) => "파일 분류로 교체",
        (true, CategoryImportAction.ReplaceUser) => "Replace user assignment (approval)",
        (false, CategoryImportAction.ReplaceUser) => "사용자 지정을 파일 값으로 교체 (승인)",
        (true, CategoryImportAction.UpdateColor) => "Change shared category color (approval)",
        (false, CategoryImportAction.UpdateColor) => "공유 분류 색상 변경 (승인)",
        (true, CategoryImportAction.AddCategory) => "Add category (import)",
        (false, CategoryImportAction.AddCategory) => "분류 추가 (import)",
        (true, CategoryImportAction.Assign) => "Assign (import)",
        (false, CategoryImportAction.Assign) => "분류 지정 (import)",
        (true, CategoryImportAction.KeepUser) => "Keep user assignment",
        (false, CategoryImportAction.KeepUser) => "사용자 지정 유지",
        (true, CategoryImportAction.KeepCurrent) => "Keep current assignment",
        (false, CategoryImportAction.KeepCurrent) => "현재 분류 유지",
        (true, CategoryImportAction.PathConflict) => "Skip: different executable path",
        (false, CategoryImportAction.PathConflict) => "건너뜀: 실행 경로 다름",
        (true, CategoryImportAction.MissingApp) => "Skip: app not on this PC",
        (false, CategoryImportAction.MissingApp) => "건너뜀: 현재 PC에 없는 앱",
        (true, CategoryImportAction.MatchCategory) => "Use existing category / color",
        (false, CategoryImportAction.MatchCategory) => "기존 분류·색상 사용",
        (true, _) => "Skip: category cannot be matched",
        _ => "건너뜀: 분류 매칭 불가"
    };
}
