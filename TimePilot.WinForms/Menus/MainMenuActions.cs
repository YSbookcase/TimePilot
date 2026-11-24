namespace TimePilot.WinForms.Menus
{
    internal sealed record MainMenuActions(
        EventHandler ExportCsv,
        EventHandler ExportRawData,
        EventHandler CreateDataBackup,
        EventHandler RestoreDataBackup,
        EventHandler ImportAppCategories,
        EventHandler ExportAppCategories,
        EventHandler Exit,
        EventHandler Preferences,
        EventHandler AppCategoryManagement,
        EventHandler ResetTableSort,
        EventHandler RuntimeDiagnostics,
        EventHandler Sponsor,
        EventHandler About);
}
