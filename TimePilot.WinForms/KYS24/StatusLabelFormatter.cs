namespace TimePilot.WinForms.KYS24
{
    internal static class StatusLabelFormatter
    {
        public static string Format(
            string? status,
            string? exportStatus,
            string? viewRefreshStatus,
            string? performanceStatus)
        {
            return string.Join(
                " | ",
                new[] { status, exportStatus, viewRefreshStatus, performanceStatus }
                    .Where(part => !string.IsNullOrWhiteSpace(part)));
        }
    }
}
