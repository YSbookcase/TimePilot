namespace TimePilot.WinForms.KYS24
{
    internal sealed record AppExecutableMetadata(
        string? FileDescription,
        string? ProductName,
        string? CompanyName,
        bool HasDistinctAssociatedIcon);
}
