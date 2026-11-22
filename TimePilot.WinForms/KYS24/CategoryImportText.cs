namespace TimePilot.WinForms.KYS24;

internal sealed class CategoryImportText(UiLanguage language)
{
    private bool English => language == UiLanguage.English;
    public string Title => English ? "Import app categories..." : "앱 분류만 가져오기...";
    public string Warning => English
        ? "Only categories and assignments to existing apps are imported. Current assignments, usage records, times and settings are kept. Apps with different executable paths are skipped. A safety backup is required before applying."
        : "분류와 현재 PC에 있는 앱의 분류 지정만 가져옵니다. 현재 지정된 분류, 사용 기록·시간·설정은 유지합니다. 실행 경로가 다른 앱은 건너뜁니다. 적용 전 안전 백업을 생성합니다.";
    public string Analyzing => English ? "Reading categories from the backup..." : "백업의 분류를 확인하는 중...";
    public string Applying => English ? "Creating safety backup and applying categories..." : "안전 백업 생성 및 분류 적용 중...";
    public string Apply => English ? "Create safety backup and apply" : "안전 백업 후 적용";
    public string Cancel => English ? "Cancel" : "취소";
    public string Close => English ? "Close" : "닫기";
    public string App => English ? "App / category" : "앱 / 분류";
    public string Current => English ? "Current category" : "현재 분류";
    public string Backup => English ? "Backup category" : "백업 분류";
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
        ? $"New categories: {plan.AddedCategories} | App assignments: {plan.AssignedApps}. Review kept and skipped entries below."
        : $"추가할 분류: {plan.AddedCategories}개 | 지정할 앱: {plan.AssignedApps}개. 아래의 유지·건너뛰기 항목도 확인하세요.";
    public string Completed(string safetyPath) => English
        ? $"Category import completed. Usage records and settings were not restored.\nSafety backup: {safetyPath}"
        : $"분류 가져오기 완료. 사용 기록과 설정은 복원하지 않았습니다.\n안전 백업: {safetyPath}";
    public string RefreshFailed(string reason) => English
        ? $"Categories were imported, but the view could not refresh. Reopen the app.\n{reason}"
        : $"분류는 반영했지만 화면을 갱신하지 못했습니다. 앱을 다시 실행하세요.\n{reason}";
    public string Failed(string reason) => English
        ? $"Import failed. No category changes were committed. Reopen the backup to try again.\n{reason}"
        : $"가져오기에 실패하여 분류 변경을 반영하지 않았습니다. 백업을 다시 열어 시도하세요.\n{reason}";
    public string Action(CategoryImportAction action) => (English, action) switch
    {
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
