namespace TimePilot.WinForms.KYS24;

internal sealed class BackupRemovalText(UiLanguage language)
{
    private bool English => language == UiLanguage.English;
    public string Title => English ? "Backup and removal preparation" : "백업 및 제거 준비";
    public string Warning => English
        ? "Uninstalling or resetting the Store app may delete local records and settings. Create an external backup first. Windows removal started outside this app cannot be intercepted. This window does not uninstall or delete anything. Unsaved preferences are not included in the backup."
        : "Store 앱을 제거하거나 초기화하면 로컬 기록과 설정이 삭제될 수 있습니다. 먼저 외부 백업을 만드세요. Windows에서 직접 시작한 제거를 가로챌 수는 없습니다. 이 창은 앱을 제거하거나 데이터를 삭제하지 않습니다. 저장하지 않은 환경설정은 백업에 포함되지 않습니다.";
    public string Create => English ? "Create backup for removal..." : "백업 후 제거 준비...";
    public string OpenSettings => English ? "Open installed apps" : "설치된 앱 열기";
    public string Close => English ? "Close" : "닫기";
    public string AwaitingBackup => English ? "A new, verified external backup is required before proceeding." : "새 외부 백업을 생성하고 검증해야 다음 단계로 진행할 수 있습니다.";
    public string Busy => English ? "A data operation is already running. Try again when it finishes." : "다른 데이터 작업이 진행 중입니다. 완료 후 다시 시도하세요.";
    public string Opening => English ? "Rechecking the backup..." : "백업을 다시 확인하는 중...";
    public string Ready => English
        ? "Backup verified. Records made after this backup are not included. Keep the ZIP outside the app folders; it may contain sensitive data and is not encrypted."
        : "백업 검증 완료. 백업 이후의 기록은 포함되지 않습니다. 민감한 정보가 들어 있는 암호화되지 않은 ZIP이므로 앱 폴더 밖에 안전하게 보관하세요.";
    public string SettingsOpened => English
        ? "Windows installed apps opened. Close ActiveLogbook from its tray menu before selecting Uninstall. External backups remain and must be deleted separately when no longer needed."
        : "Windows 설치된 앱을 열었습니다. 제거를 선택하기 전에 트레이 메뉴에서 ActiveLogbook을 종료하세요. 외부 백업은 남으므로 필요 없을 때 별도로 삭제해야 합니다.";
    public string HistoryUnknown => English ? "No previous external backup success recorded." : "이전 외부 백업 성공 기록이 없습니다.";
    public string History(DateTimeOffset at, string path) => English
        ? $"Last recorded external backup: {at.ToLocalTime():yyyy-MM-dd HH:mm}\n{path}\nHistorical result only; not revalidated."
        : $"최근 외부 백업 성공 기록: {at.ToLocalTime():yyyy-MM-dd HH:mm}\n{path}\n과거 성공 기록이며 현재 파일은 재검증하지 않았습니다.";
    public string Failed(string reason) => English ? $"Could not prepare or verify the backup.\n{reason}" : $"백업 준비 또는 검증에 실패했습니다.\n{reason}";
    public string VerifiedHistory(DateTimeOffset at, string path) => English
        ? $"External backup verified: {at.ToLocalTime():yyyy-MM-dd HH:mm}\n{path}"
        : $"외부 백업 생성 및 검증 완료: {at.ToLocalTime():yyyy-MM-dd HH:mm}\n{path}";
    public string OpenFailed(string reason) => English ? $"Could not open Windows installed apps. Try again.\n{reason}" : $"Windows 설치된 앱을 열지 못했습니다. 다시 시도하세요.\n{reason}";
    public string HistorySaveFailed => English ? "Backup is verified, but its history could not be saved." : "백업 검증은 완료했지만 성공 이력을 저장하지 못했습니다.";
}
