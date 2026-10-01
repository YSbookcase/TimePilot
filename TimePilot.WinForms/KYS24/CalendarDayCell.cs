namespace TimePilot.WinForms.KYS24
{
    internal sealed record CalendarDayCell(
        DateTime Date,
        bool IsCurrentMonth,
        bool IsToday,
        bool IsSelected,
        bool HasRecord);
}
