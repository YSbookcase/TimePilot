namespace TimePilot.WinForms.KYS24
{
    internal sealed record RawDataExportTable(
        string TableName,
        string FileName,
        IReadOnlyList<string> Columns,
        IReadOnlyList<IReadOnlyList<string>> Rows);
}
