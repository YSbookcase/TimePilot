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

        public string EditabilityText => IsBuiltin
            ? UiText.CurrentLanguage == UiLanguage.English ? "Name/delete locked" : "이름/삭제 불가"
            : UiText.CurrentLanguage == UiLanguage.English ? "Editable" : "수정 가능";
    }
}
