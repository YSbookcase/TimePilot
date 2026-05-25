namespace TimePilot.WinForms.KYS24
{
    internal sealed record AppCategoryEditorRow(
        long Id,
        string Name,
        string? Color,
        int SortOrder,
        bool IsBuiltin,
        int AppCount)
    {
        public string DisplayName => AppCategoryDisplay.GetDisplayName(Name, SortOrder);

        public string ColorText => string.IsNullOrWhiteSpace(Color) ? "" : Color;
    }
}
