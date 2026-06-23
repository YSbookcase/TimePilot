namespace TimePilot.WinForms.Menus
{
    internal sealed record MainMenuControls(
        MenuStrip MenuStrip,
        ToolStripMenuItem File,
        ToolStripMenuItem ExportCsv,
        ToolStripMenuItem ExportRawData,
        ToolStripMenuItem CreateDataBackup,
        ToolStripMenuItem RestoreDataBackup,
        ToolStripMenuItem Exit,
        ToolStripMenuItem Settings,
        ToolStripMenuItem Preferences,
        ToolStripMenuItem AppCategoryManagement,
        ToolStripMenuItem ResetTableSort,
        ToolStripMenuItem Help,
        ToolStripMenuItem RuntimeDiagnostics,
        ToolStripMenuItem Sponsor,
        ToolStripMenuItem About);
}
