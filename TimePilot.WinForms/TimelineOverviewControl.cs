using TimePilot.WinForms.KYS24;
using System.Globalization;

namespace TimePilot.WinForms
{
    internal sealed class TimelineOverviewControl : Control
    {
        private static readonly Color ActiveBorderColor = Color.FromArgb(72, 94, 117);
        private static readonly Color IdleFillColor = Color.FromArgb(226, 229, 233);
        private static readonly Color IdleBorderColor = Color.FromArgb(170, 176, 184);
        private static readonly Color UntrackedFillColor = Color.FromArgb(244, 229, 207);
        private static readonly Color UntrackedBorderColor = Color.FromArgb(211, 162, 92);
        private static readonly Color WindowsFillColor = Color.FromArgb(198, 225, 246);
        private static readonly Color WindowsBorderColor = Color.FromArgb(87, 137, 184);
        private static readonly Color AxisColor = Color.FromArgb(190, 196, 204);
        private static readonly Color TextColor = Color.FromArgb(65, 72, 82);

        private IReadOnlyList<ActivityTimelineRow> rows = Array.Empty<ActivityTimelineRow>();
        private IReadOnlyList<TimelineRange> windowsRuntimeRanges = Array.Empty<TimelineRange>();
        private DateTime localDate = DateTime.Today;
        private string? hoverText;

        public TimelineOverviewControl()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            MinimumSize = new Size(240, 124);
        }

        public void SetTimeline(
            DateTime date,
            IReadOnlyList<ActivityTimelineRow> timelineRows,
            IReadOnlyList<TimelineRange> windowsRanges)
        {
            localDate = date.Date;
            rows = timelineRows.OrderBy(row => row.StartedAt).ToList();
            windowsRuntimeRanges = windowsRanges.OrderBy(range => range.StartedAt).ToList();
            hoverText = null;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var graphics = e.Graphics;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.Clear(BackColor);

            var bounds = ClientRectangle;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            var windowsBounds = GetWindowsBounds(bounds);
            var activityBounds = GetActivityBounds(bounds);
            DrawTrackLabel(graphics, UiText.Main.WindowsRuntimeTrack, windowsBounds);
            DrawTrackLabel(graphics, UiText.Main.ActivityTimelineTrack, activityBounds);
            DrawAxis(graphics, activityBounds);
            DrawWindowsRanges(graphics, windowsBounds);
            DrawActivityRows(graphics, activityBounds);

            if (!string.IsNullOrEmpty(hoverText))
                DrawHoverInfo(graphics, hoverText, bounds);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            var text = FindHoverTextAt(e.Location);
            if (string.Equals(text, hoverText, StringComparison.Ordinal))
                return;

            hoverText = text;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hoverText = null;
            Invalidate();
        }

        private Rectangle GetWindowsBounds(Rectangle bounds)
        {
            return new Rectangle(bounds.Left + 78, bounds.Top + 16, Math.Max(1, bounds.Width - 90), 18);
        }

        private Rectangle GetActivityBounds(Rectangle bounds)
        {
            return new Rectangle(bounds.Left + 78, bounds.Top + 48, Math.Max(1, bounds.Width - 90), Math.Max(18, bounds.Height - 78));
        }

        private void DrawTrackLabel(Graphics graphics, string text, Rectangle trackBounds)
        {
            var labelBounds = new Rectangle(6, trackBounds.Top - 1, 66, trackBounds.Height + 2);
            TextRenderer.DrawText(
                graphics,
                text,
                Font,
                labelBounds,
                TextColor,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private void DrawAxis(Graphics graphics, Rectangle timelineBounds)
        {
            using var axisPen = new Pen(AxisColor);
            graphics.DrawRectangle(axisPen, timelineBounds);

            for (var hour = 0; hour <= 24; hour += 3)
            {
                var x = timelineBounds.Left + (int)Math.Round(timelineBounds.Width * (hour / 24.0));
                graphics.DrawLine(axisPen, x, timelineBounds.Top - 5, x, timelineBounds.Bottom + 4);

                var label = hour == 24 ? "24" : $"{hour:00}";
                var labelBounds = new Rectangle(x - 18, timelineBounds.Bottom + 7, 36, 16);
                TextRenderer.DrawText(
                    graphics,
                    label,
                    Font,
                    labelBounds,
                    TextColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawWindowsRanges(Graphics graphics, Rectangle windowsBounds)
        {
            using var fillBrush = new SolidBrush(WindowsFillColor);
            using var borderPen = new Pen(WindowsBorderColor);

            foreach (var range in windowsRuntimeRanges)
            {
                var segment = GetRangeBounds(range.StartedAt, range.EndedAt, windowsBounds);
                if (segment.Width <= 0)
                    continue;

                graphics.FillRectangle(fillBrush, segment);
                graphics.DrawRectangle(borderPen, segment);
            }
        }

        private void DrawActivityRows(Graphics graphics, Rectangle activityBounds)
        {
            if (rows.Count == 0)
            {
                TextRenderer.DrawText(
                    graphics,
                    UiText.Main.NoData,
                    Font,
                    activityBounds,
                    SystemColors.GrayText,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            foreach (var row in rows)
            {
                var segment = GetSegmentBounds(row, activityBounds);
                if (segment.Width <= 0)
                    continue;

                DrawSegment(graphics, row, segment);
            }
        }

        private Rectangle GetSegmentBounds(ActivityTimelineRow row, Rectangle timelineBounds)
        {
            return GetRangeBounds(row.StartedAt, row.EndedAt, timelineBounds);
        }

        private Rectangle GetRangeBounds(DateTimeOffset rangeStart, DateTimeOffset? rangeEnd, Rectangle timelineBounds)
        {
            var dayStart = new DateTimeOffset(localDate, TimeZoneInfo.Local.GetUtcOffset(localDate));
            var dayEndDate = localDate.AddDays(1);
            var dayEnd = new DateTimeOffset(dayEndDate, TimeZoneInfo.Local.GetUtcOffset(dayEndDate));
            var fallbackEnd = localDate == DateTime.Today
                ? Min(DateTimeOffset.Now, dayEnd)
                : dayEnd;
            var startedAt = Max(rangeStart, dayStart);
            var endedAt = Min(rangeEnd ?? fallbackEnd, dayEnd);
            if (endedAt <= startedAt)
                return Rectangle.Empty;

            var dayDurationMs = Math.Max(1, (dayEnd - dayStart).TotalMilliseconds);
            var startRatio = (startedAt - dayStart).TotalMilliseconds / dayDurationMs;
            var endRatio = (endedAt - dayStart).TotalMilliseconds / dayDurationMs;
            var left = timelineBounds.Left + (int)Math.Round(timelineBounds.Width * startRatio);
            var right = timelineBounds.Left + (int)Math.Round(timelineBounds.Width * endRatio);
            var width = Math.Max(2, right - left);

            return new Rectangle(left, timelineBounds.Top + 4, width, Math.Max(10, timelineBounds.Height - 8));
        }

        private void DrawSegment(Graphics graphics, ActivityTimelineRow row, Rectangle segment)
        {
            using var fillBrush = new SolidBrush(GetFillColor(row));
            using var borderPen = new Pen(GetBorderColor(row));
            graphics.FillRectangle(fillBrush, segment);
            graphics.DrawRectangle(borderPen, segment);
        }

        private string? FindHoverTextAt(Point point)
        {
            return FindWindowsHoverTextAt(point) ?? FindActivityHoverTextAt(point);
        }

        private string? FindWindowsHoverTextAt(Point point)
        {
            var timelineBounds = GetWindowsBounds(ClientRectangle);
            var range = windowsRuntimeRanges
                .Select(item => new { Range = item, Bounds = GetRangeBounds(item.StartedAt, item.EndedAt, timelineBounds) })
                .LastOrDefault(x => x.Bounds.Contains(point))
                ?.Range;

            if (range is null)
                return null;

            return string.Join(
                " | ",
                UiText.Main.WindowsRuntimeTrack,
                $"{FormatTime(range.StartedAt)}-{FormatTime(range.EndedAt)}",
                FormatDuration(range.StartedAt, range.EndedAt),
                UiText.Main.Runtime);
        }

        private string? FindActivityHoverTextAt(Point point)
        {
            var timelineBounds = GetActivityBounds(ClientRectangle);
            var row = rows
                .Select(row => new { Row = row, Bounds = GetSegmentBounds(row, timelineBounds) })
                .LastOrDefault(x => x.Bounds.Contains(point))
                ?.Row;

            return row is null
                ? null
                : $"{row.ActivityType} | {row.DisplayName} | {row.StartedAtText}-{row.EndedAtText} | {row.DurationText}";
        }

        private void DrawHoverInfo(Graphics graphics, string text, Rectangle bounds)
        {
            var infoBounds = new Rectangle(78, bounds.Top + 2, Math.Max(1, bounds.Width - 90), 14);
            TextRenderer.DrawText(
                graphics,
                text,
                Font,
                infoBounds,
                TextColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private static string FormatTime(DateTimeOffset value)
        {
            return value.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture);
        }

        private static string FormatDuration(DateTimeOffset startedAt, DateTimeOffset endedAt)
        {
            var duration = endedAt - startedAt;
            if (duration < TimeSpan.Zero)
                duration = TimeSpan.Zero;

            var totalHours = (int)duration.TotalHours;
            return string.Format(
                CultureInfo.CurrentCulture,
                "{0:00}:{1:00}:{2:00}",
                totalHours,
                duration.Minutes,
                duration.Seconds);
        }

        private static Color GetFillColor(ActivityTimelineRow row)
        {
            if (string.Equals(row.ActivityType, UiText.Main.Idle, StringComparison.Ordinal))
                return IdleFillColor;

            if (string.Equals(row.ActivityType, UiText.Main.Untracked, StringComparison.Ordinal)
                || string.Equals(row.ActivityType, UiText.Main.TimePilotUntracked, StringComparison.Ordinal))
                return UntrackedFillColor;

            var hash = unchecked((uint)StringComparer.OrdinalIgnoreCase.GetHashCode(row.DisplayName));
            var hue = (int)(hash % 360);
            return FromHsl(hue, 0.52, 0.72);
        }

        private static Color GetBorderColor(ActivityTimelineRow row)
        {
            if (string.Equals(row.ActivityType, UiText.Main.Idle, StringComparison.Ordinal))
                return IdleBorderColor;

            if (string.Equals(row.ActivityType, UiText.Main.Untracked, StringComparison.Ordinal)
                || string.Equals(row.ActivityType, UiText.Main.TimePilotUntracked, StringComparison.Ordinal))
                return UntrackedBorderColor;

            return ActiveBorderColor;
        }

        private static Color FromHsl(int hue, double saturation, double lightness)
        {
            var c = (1 - Math.Abs(2 * lightness - 1)) * saturation;
            var x = c * (1 - Math.Abs((hue / 60.0) % 2 - 1));
            var m = lightness - c / 2;
            var (r, g, b) = hue switch
            {
                < 60 => (c, x, 0.0),
                < 120 => (x, c, 0.0),
                < 180 => (0.0, c, x),
                < 240 => (0.0, x, c),
                < 300 => (x, 0.0, c),
                _ => (c, 0.0, x)
            };

            return Color.FromArgb(
                ToRgbComponent(r + m),
                ToRgbComponent(g + m),
                ToRgbComponent(b + m));
        }

        private static int ToRgbComponent(double value)
        {
            return Math.Clamp((int)Math.Round(value * 255), 0, 255);
        }

        private static DateTimeOffset Min(DateTimeOffset left, DateTimeOffset right)
        {
            return left <= right ? left : right;
        }

        private static DateTimeOffset Max(DateTimeOffset left, DateTimeOffset right)
        {
            return left >= right ? left : right;
        }
    }
}
