namespace TimePilot.WinForms.KYS24
{
    internal sealed record SummaryPeriodRange(
        DateTimeOffset Start,
        DateTimeOffset End,
        bool ShowDateInTimestamps);
}
