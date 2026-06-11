using System.Globalization;
using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal enum SummaryUsageBarMode
    {
        App,
        Category
    }

    internal sealed class SummaryUsageBarsControl : Control
    {
        private const int MaxNamedSegments = 7;
        private static readonly Color[] SegmentColors =
        [
            Color.FromArgb(37, 99, 235),
            Color.FromArgb(22, 163, 74),
            Color.FromArgb(234, 88, 12),
            Color.FromArgb(124, 58, 237),
            Color.FromArgb(8, 145, 178),
            Color.FromArgb(219, 39, 119),
            Color.FromArgb(245, 158, 11)
        ];

        private IReadOnlyList<Segment> segments = Array.Empty<Segment>();
        private long? highlightedAppId;
        private string? highlightedProcessName;

        public SummaryUsageBarsControl()
        {
            DoubleBuffered = true;
            MinimumSize = new Size(160, 82);
        }

        public void SetRows(
            IReadOnlyList<UsageSummaryRow> usageRows,
            UsageSummaryRow? highlightedRow,
            SummaryUsageBarMode mode)
        {
            highlightedAppId = highlightedRow?.AppId;
            highlightedProcessName = highlightedRow?.ProcessName;
            var namedRows = BuildSegments(usageRows, highlightedRow, mode);

            if (namedRows.Count == 0)
            {
                segments = Array.Empty<Segment>();
                Visible = false;
                Invalidate();
                return;
            }

            var topRows = namedRows.Take(MaxNamedSegments).ToList();
            var builtSegments = topRows
                .Select((segment, index) => segment with { Color = SegmentColors[index % SegmentColors.Length] })
                .ToList();

            var otherRatio = namedRows.Skip(MaxNamedSegments).Sum(row => row.Ratio);
            if (otherRatio > 0)
            {
                builtSegments.Add(new Segment(
                    UiText.CurrentLanguage == UiLanguage.English ? "Other" : "\uAE30\uD0C0",
                    otherRatio,
                    Color.FromArgb(148, 163, 184),
                    false));
            }

            segments = builtSegments;
            Visible = true;
            Invalidate();
        }

        private List<Segment> BuildSegments(
            IReadOnlyList<UsageSummaryRow> usageRows,
            UsageSummaryRow? highlightedRow,
            SummaryUsageBarMode mode)
        {
            if (mode == SummaryUsageBarMode.Category)
            {
                var highlightedCategory = highlightedRow?.CategoryText;
                return usageRows
                    .Where(row => row.ActiveUsageMs > 0)
                    .GroupBy(row => row.CategoryText)
                    .Select(group => new Segment(
                        group.Key,
                        group.Sum(row => row.UsageRatio),
                        Color.Empty,
                        !string.IsNullOrWhiteSpace(highlightedCategory)
                            && string.Equals(group.Key, highlightedCategory, StringComparison.CurrentCulture)))
                    .OrderByDescending(segment => segment.Ratio)
                    .ThenBy(segment => segment.Name, StringComparer.CurrentCulture)
                    .ToList();
            }

            return usageRows
                .Where(row => row.ActiveUsageMs > 0)
                .OrderByDescending(row => row.UsageRatio)
                .ThenBy(row => row.AppName, StringComparer.CurrentCulture)
                .Select(row => new Segment(
                    row.AppName,
                    row.UsageRatio,
                    Color.Empty,
                    IsHighlightedRow(row)))
                .ToList();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (segments.Count == 0 || ClientSize.Width <= 0 || ClientSize.Height <= 0)
                return;

            e.Graphics.Clear(BackColor);

            var barHeight = Math.Min(38, Math.Max(28, ClientSize.Height / 2));
            var barBounds = new Rectangle(0, 6, ClientSize.Width - 1, barHeight);
            using (var trackBrush = new SolidBrush(Color.FromArgb(229, 231, 235)))
                e.Graphics.FillRectangle(trackBrush, barBounds);

            var x = barBounds.Left;
            var remainingWidth = barBounds.Width;
            for (var i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                var width = i == segments.Count - 1
                    ? remainingWidth
                    : Math.Clamp((int)Math.Round(barBounds.Width * segment.Ratio), 0, remainingWidth);
                if (width <= 0)
                    continue;

                var segmentBounds = new Rectangle(x, barBounds.Top, width, barBounds.Height);
                using (var brush = new SolidBrush(segment.Color))
                    e.Graphics.FillRectangle(brush, segmentBounds);

                if (segment.IsHighlighted)
                {
                    using var pen = new Pen(Color.FromArgb(15, 23, 42), 2);
                    e.Graphics.DrawRectangle(pen, segmentBounds.Left, segmentBounds.Top, segmentBounds.Width - 1, segmentBounds.Height - 1);
                }

                if (width >= 76)
                {
                    TextRenderer.DrawText(
                        e.Graphics,
                        FormatRatio(segment.Ratio),
                        Font,
                        segmentBounds,
                        Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                }

                x += width;
                remainingWidth = Math.Max(0, barBounds.Right - x);
            }

            using (var borderPen = new Pen(Color.FromArgb(203, 213, 225)))
                e.Graphics.DrawRectangle(borderPen, barBounds);

            DrawLegend(e.Graphics, barBounds.Bottom + 8);
        }

        private void DrawLegend(Graphics graphics, int top)
        {
            var x = 0;
            var y = top;
            var lineHeight = Font.Height + 4;
            foreach (var segment in segments)
            {
                var text = $"{segment.Name} {FormatRatio(segment.Ratio)}";
                var textSize = TextRenderer.MeasureText(graphics, text, Font);
                var itemWidth = 14 + 4 + textSize.Width + 12;
                if (x > 0 && x + itemWidth > ClientSize.Width)
                {
                    x = 0;
                    y += lineHeight;
                    if (y + lineHeight > ClientSize.Height)
                        return;
                }

                var swatchBounds = new Rectangle(x, y + 4, 10, 10);
                using (var brush = new SolidBrush(segment.Color))
                    graphics.FillRectangle(brush, swatchBounds);
                if (segment.IsHighlighted)
                {
                    using var pen = new Pen(Color.FromArgb(15, 23, 42));
                    graphics.DrawRectangle(pen, swatchBounds);
                }

                var textBounds = new Rectangle(x + 14, y, Math.Min(textSize.Width + 4, ClientSize.Width - x - 14), lineHeight);
                TextRenderer.DrawText(
                    graphics,
                    text,
                    Font,
                    textBounds,
                    segment.IsHighlighted ? Color.FromArgb(29, 78, 216) : ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                x += itemWidth;
            }
        }

        private bool IsHighlightedRow(UsageSummaryRow row)
        {
            if (highlightedAppId is { } appId && row.AppId == appId)
                return true;

            return !string.IsNullOrWhiteSpace(highlightedProcessName)
                && string.Equals(row.ProcessName, highlightedProcessName, StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatRatio(double ratio)
        {
            return ratio.ToString("P1", CultureInfo.CurrentCulture);
        }

        private sealed record Segment(
            string Name,
            double Ratio,
            Color Color,
            bool IsHighlighted);
    }
}
