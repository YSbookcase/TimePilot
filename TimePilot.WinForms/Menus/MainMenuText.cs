namespace TimePilot.WinForms.Menus
{
    internal sealed record MainMenuText(
        string File,
        string ExportCsv,
        string ExportRawData,
        string CreateDataBackup,
        string RestoreDataBackup,
        string Exit,
        string Settings,
        string Preferences,
        string AppCategoryManagement,
        string ResetTableSort,
        string Help,
        string RuntimeDiagnostics,
        string Sponsor,
        string About);
}
