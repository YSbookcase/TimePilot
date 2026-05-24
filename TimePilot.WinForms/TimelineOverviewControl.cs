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
        private static readonly Color DimOverlayColor = Color.FromArgb(220, 255, 255, 255);
        private static readonly Color HighlightTintColor = Color.FromArgb(70, 28, 91, 170);
        private static readonly Color HighlightBorderColor = Color.FromArgb(13, 61, 132);
        private static readonly Color HighlightHatchColor = Color.FromArgb(170, 13, 61, 132);
        private static readonly Color SelectionFillColor = Color.FromArgb(80, 47, 111, 172);
        private static readonly Color SelectionBorderColor = Color.FromArgb(47, 111, 172);
        private static readonly Color AxisColor = Color.FromArgb(190, 196, 204);
        private static readonly Color TextColor = Color.FromArgb(65, 72, 82);
        private static readonly Color[] AppFillPalette =
        [
            Color.FromArgb(137, 176, 211),
            Color.FromArgb(143, 191, 169),
            Color.FromArgb(224, 173, 116),
            Color.FromArgb(188, 166, 216),
            Color.FromArgb(218, 145, 154),
            Color.FromArgb(132, 190, 201),
            Color.FromArgb(198, 184, 123),
            Color.FromArgb(164, 184, 221),
            Color.FromArgb(207, 159, 199),
            Color.FromArgb(151, 198, 142)
        ];
        private static readonly Color[] AppBorderPalette =
        [
            Color.FromArgb(72, 119, 164),
            Color.FromArgb(75, 137, 111),
            Color.FromArgb(168, 112, 54),
            Color.FromArgb(128, 104, 165),
            Color.FromArgb(164, 83, 95),
            Color.FromArgb(62, 130, 143),
            Color.FromArgb(145, 128, 65),
            Color.FromArgb(86, 111, 171),
            Color.FromArgb(150, 93, 140),
            Color.FromArgb(89, 139, 82)
        ];
        private static readonly TimeSpan MinimumViewRange = TimeSpan.FromMinutes(5);
        private const int MinimumDragPixels = 8;
        private const double WheelZoomInFactor = 0.8;
        private const double WheelZoomOutFactor = 1.25;

        private IReadOnlyList<ActivityTimelineRow> rows = Array.Empty<ActivityTimelineRow>();
        private IReadOnlyList<TimelineRange> windowsRuntimeRanges = Array.Empty<TimelineRange>();
        private IReadOnlyList<CategoryTimelineSegment> categorySegments = Array.Empty<CategoryTimelineSegment>();
        private string? highlightedProcessName;
        private string? highlightedActivityType;
        private bool isWindowsHighlighted;
        private DateTime localDate = DateTime.Today;
        private TimeSpan viewStart = TimeSpan.Zero;
        private TimeSpan viewEnd = TimeSpan.FromDays(1);
        private readonly Stack<(TimeSpan Start, TimeSpan End)> viewHistory = new();
        private string? hoverText;
        private bool isDragging;
        private int dragStartX;
        private int dragCurrentX;

        public event EventHandler? ViewRangeChanged;

        public event EventHandler<TimelineActivitySegmentContextEventArgs>? ActivitySegmentContextRequested;

        public bool IsZoomed => viewStart > TimeSpan.Zero || viewEnd < TimeSpan.FromDays(1);

        public bool CanGoBack => viewHistory.Count > 0;

        public bool CanPanPrevious => IsZoomed && viewStart > TimeSpan.Zero;

        public bool CanPanNext => IsZoomed && viewEnd < TimeSpan.FromDays(1);

        public string ViewRangeText => IsZoomed
            ? $"{FormatTimeOfDay(viewStart)}-{FormatTimeOfDay(viewEnd)} ({FormatRangeDuration(viewEnd - viewStart)})"
            : UiText.Main.TimelineFullDay;

        public double ViewStartRatio => viewStart.TotalMilliseconds / TimeSpan.FromDays(1).TotalMilliseconds;

        public double ViewWidthRatio => (viewEnd - viewStart).TotalMilliseconds / TimeSpan.FromDays(1).TotalMilliseconds;

        public TimelineOverviewControl()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            MinimumSize = new Size(240, 150);
            TabStop = true;
        }

        public void SetTimeline(
            DateTime date,
            IReadOnlyList<ActivityTimelineRow> timelineRows,
            IReadOnlyList<TimelineRange> windowsRanges,
            IReadOnlyList<CategoryTimelineSegment> categoryTimelineSegments)
        {
            var dateChanged = date.Date != localDate;
            localDate = date.Date;
            rows = timelineRows.OrderBy(row => row.StartedAt).ToList();
            windowsRuntimeRanges = windowsRanges.OrderBy(range => range.StartedAt).ToList();
            categorySegments = categoryTimelineSegments.OrderBy(segment => segment.StartedAt).ToList();
            hoverText = null;
            if (dateChanged)
            {
                viewHistory.Clear();
                SetViewRange(TimeSpan.Zero, TimeSpan.FromDays(1), addHistory: false);
            }

            Invalidate();
        }

        public void SetHighlightedProcessName(string? processName)
        {
            highlightedProcessName = string.IsNullOrWhiteSpace(processName) ? null : processName;
            hoverText = null;
            Invalidate();
        }

        public void SetHighlightedActivityType(string? activityType)
        {
            highlightedActivityType = string.IsNullOrWhiteSpace(activityType) ? null : activityType;
            isWindowsHighlighted = false;
            hoverText = null;
            Invalidate();
        }

        public void SetWindowsHighlighted(bool isHighlighted)
        {
            highlightedActivityType = null;
            isWindowsHighlighted = isHighlighted;
            hoverText = null;
            Invalidate();
        }

        public void GoBack()
        {
            if (viewHistory.Count == 0)
                return;

            var previous = viewHistory.Pop();
            SetViewRange(previous.Start, previous.End, addHistory: false);
        }

        public void ResetView()
        {
            if (!IsZoomed)
                return;

            viewHistory.Clear();
            SetViewRange(TimeSpan.Zero, TimeSpan.FromDays(1), addHistory: false);
        }

        public void PanPrevious()
        {
            Pan(-GetFinePanRatio());
        }

        public void PanNext()
        {
            Pan(GetFinePanRatio());
        }

        public void ZoomIn()
        {
            Zoom(0.5, centerRatio: 0.5);
        }

        public void ZoomOut()
        {
            Zoom(2, centerRatio: 0.5);
        }

        public void SetViewStartRatio(double ratio)
        {
            if (!IsZoomed)
                return;

            var width = viewEnd - viewStart;
            var maxStart = TimeSpan.FromDays(1) - width;
            var nextStart = TimeSpan.FromTicks((long)(TimeSpan.FromDays(1).Ticks * Math.Clamp(ratio, 0, 1)));
            if (nextStart > maxStart)
                nextStart = maxStart;

            SetViewRange(nextStart, nextStart + width, addHistory: false);
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
            var categoryBounds = GetCategoryBounds(bounds);
            var activityBounds = GetActivityBounds(bounds);
            DrawTrackLabel(graphics, UiText.Main.WindowsRuntimeTrack, windowsBounds);
            DrawTrackLabel(graphics, UiText.Main.CategorySummaryTrack, categoryBounds);
            DrawTrackLabel(graphics, UiText.Main.ActivityTimelineTrack, activityBounds);
            DrawAxis(graphics, activityBounds);
            DrawWindowsRanges(graphics, windowsBounds);
            DrawCategorySegments(graphics, categoryBounds);
            DrawActivityRows(graphics, activityBounds);
            DrawDragSelection(graphics, bounds);

            if (!string.IsNullOrEmpty(hoverText))
                DrawHoverInfo(graphics, hoverText, bounds);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            if (e.Button == MouseButtons.Right)
            {
                if (FindActivityRowAt(e.Location) is { } row)
                    ActivitySegmentContextRequested?.Invoke(this, new TimelineActivitySegmentContextEventArgs(row, e.Location));

                return;
            }

            if (e.Button != MouseButtons.Left || !GetInteractiveBounds(ClientRectangle).Contains(e.Location))
                return;

            isDragging = true;
            dragStartX = e.X;
            dragCurrentX = e.X;
            Capture = true;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (isDragging)
            {
                dragCurrentX = e.X;
                hoverText = null;
                Invalidate();
                return;
            }

            var text = FindHoverTextAt(e.Location);
            if (string.Equals(text, hoverText, StringComparison.Ordinal))
                return;

            hoverText = text;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (!isDragging)
                return;

            isDragging = false;
            Capture = false;

            var startX = dragStartX;
            var endX = e.X;
            dragCurrentX = e.X;
            if (Math.Abs(endX - startX) >= MinimumDragPixels)
                ZoomToDragRange(startX, endX);

            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (isDragging)
                return;

            hoverText = null;
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                var interactiveBounds = GetInteractiveBounds(ClientRectangle);
                var centerRatio = interactiveBounds.Contains(e.Location)
                    ? Math.Clamp((e.X - interactiveBounds.Left) / (double)Math.Max(1, interactiveBounds.Width), 0, 1)
                    : 0.5;
                Zoom(e.Delta > 0 ? WheelZoomInFactor : WheelZoomOutFactor, centerRatio);
                return;
            }

            if ((ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                Pan(e.Delta > 0 ? -GetFinePanRatio() : GetFinePanRatio());
                return;
            }

            base.OnMouseWheel(e);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            var key = keyData & Keys.KeyCode;
            return key is Keys.Left or Keys.Right or Keys.Escape
                || base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            switch (e.KeyCode)
            {
                case Keys.Left:
                    Pan(-GetFinePanRatio());
                    e.Handled = true;
                    break;
                case Keys.Right:
                    Pan(GetFinePanRatio());
                    e.Handled = true;
                    break;
                case Keys.Escape:
                    ResetView();
                    e.Handled = true;
                    break;
            }
        }

        private Rectangle GetWindowsBounds(Rectangle bounds)
        {
            return new Rectangle(bounds.Left + 78, bounds.Top + 16, Math.Max(1, bounds.Width - 90), 18);
        }

        private Rectangle GetActivityBounds(Rectangle bounds)
        {
            return new Rectangle(bounds.Left + 78, bounds.Top + 72, Math.Max(1, bounds.Width - 90), Math.Max(18, bounds.Height - 102));
        }

        private Rectangle GetCategoryBounds(Rectangle bounds)
        {
            return new Rectangle(bounds.Left + 78, bounds.Top + 42, Math.Max(1, bounds.Width - 90), 22);
        }

        private Rectangle GetInteractiveBounds(Rectangle bounds)
        {
            var windowsBounds = GetWindowsBounds(bounds);
            var categoryBounds = GetCategoryBounds(bounds);
            var activityBounds = GetActivityBounds(bounds);
            return Rectangle.Union(Rectangle.Union(windowsBounds, categoryBounds), activityBounds);
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

            foreach (var tick in GetAxisTicks())
            {
                var x = timelineBounds.Left + (int)Math.Round(timelineBounds.Width * GetRatio(tick));
                graphics.DrawLine(axisPen, x, timelineBounds.Top - 5, x, timelineBounds.Bottom + 4);

                var label = FormatAxisLabel(tick);
                var labelBounds = GetAxisLabelBounds(label, x, timelineBounds);
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

                if (!HasHighlight)
                    continue;

                if (isWindowsHighlighted)
                {
                    using var tintBrush = new SolidBrush(HighlightTintColor);
                    using var hatchBrush = new System.Drawing.Drawing2D.HatchBrush(
                        System.Drawing.Drawing2D.HatchStyle.ForwardDiagonal,
                        HighlightHatchColor,
                        Color.Transparent);
                    using var highlightPen = new Pen(HighlightBorderColor, 3);
                    graphics.FillRectangle(tintBrush, segment);
                    graphics.FillRectangle(hatchBrush, segment);
                    graphics.DrawRectangle(highlightPen, segment);
                    continue;
                }

                using var overlayBrush = new SolidBrush(DimOverlayColor);
                graphics.FillRectangle(overlayBrush, segment);
            }
        }

        private void DrawCategorySegments(Graphics graphics, Rectangle categoryBounds)
        {
            using var emptyPen = new Pen(AxisColor);
            graphics.DrawRectangle(emptyPen, categoryBounds);

            foreach (var segment in categorySegments)
            {
                var bounds = GetRangeBounds(segment.StartedAt, segment.EndedAt, categoryBounds);
                if (bounds.Width <= 0)
                    continue;

                using var fillBrush = new SolidBrush(GetCategoryFillColor(segment));
                using var borderPen = new Pen(GetCategoryBorderColor(segment));
                graphics.FillRectangle(fillBrush, bounds);
                graphics.DrawRectangle(borderPen, bounds);

                if (bounds.Width >= 58)
                {
                    TextRenderer.DrawText(
                        graphics,
                        segment.CategoryName,
                        Font,
                        bounds,
                        TextColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                }
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
            var viewStartAt = dayStart + viewStart;
            var viewEndAt = dayStart + viewEnd;
            var fallbackEnd = localDate == DateTime.Today
                ? Min(DateTimeOffset.Now, dayEnd)
                : dayEnd;
            var startedAt = Max(rangeStart, viewStartAt);
            var endedAt = Min(rangeEnd ?? fallbackEnd, Min(viewEndAt, dayEnd));
            if (endedAt <= startedAt)
                return Rectangle.Empty;

            var viewDurationMs = Math.Max(1, (viewEndAt - viewStartAt).TotalMilliseconds);
            var startRatio = (startedAt - viewStartAt).TotalMilliseconds / viewDurationMs;
            var endRatio = (endedAt - viewStartAt).TotalMilliseconds / viewDurationMs;
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

            if (!HasHighlight || IsHighlighted(row))
            {
                if (IsHighlighted(row))
                {
                    using var tintBrush = new SolidBrush(HighlightTintColor);
                    using var hatchBrush = new System.Drawing.Drawing2D.HatchBrush(
                        System.Drawing.Drawing2D.HatchStyle.ForwardDiagonal,
                        HighlightHatchColor,
                        Color.Transparent);
                    using var highlightPen = new Pen(HighlightBorderColor, 4);
                    graphics.FillRectangle(tintBrush, segment);
                    graphics.FillRectangle(hatchBrush, segment);
                    graphics.DrawRectangle(highlightPen, segment);
                }

                return;
            }

            using var overlayBrush = new SolidBrush(DimOverlayColor);
            graphics.FillRectangle(overlayBrush, segment);
        }

        private void DrawDragSelection(Graphics graphics, Rectangle bounds)
        {
            if (!isDragging)
                return;

            var interactiveBounds = GetInteractiveBounds(bounds);
            var left = Math.Clamp(Math.Min(dragStartX, dragCurrentX), interactiveBounds.Left, interactiveBounds.Right);
            var right = Math.Clamp(Math.Max(dragStartX, dragCurrentX), interactiveBounds.Left, interactiveBounds.Right);
            if (right - left < 2)
                return;

            var selectionBounds = new Rectangle(left, interactiveBounds.Top, right - left, interactiveBounds.Height);
            using var fillBrush = new SolidBrush(SelectionFillColor);
            using var borderPen = new Pen(SelectionBorderColor);
            graphics.FillRectangle(fillBrush, selectionBounds);
            graphics.DrawRectangle(borderPen, selectionBounds);
        }

        private string? FindHoverTextAt(Point point)
        {
            return FindWindowsHoverTextAt(point) ?? FindCategoryHoverTextAt(point) ?? FindActivityHoverTextAt(point);
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

        private string? FindCategoryHoverTextAt(Point point)
        {
            var timelineBounds = GetCategoryBounds(ClientRectangle);
            var segment = categorySegments
                .Select(item => new { Segment = item, Bounds = GetRangeBounds(item.StartedAt, item.EndedAt, timelineBounds) })
                .LastOrDefault(x => x.Bounds.Contains(point))
                ?.Segment;

            if (segment is null)
                return null;

            return string.Join(
                " | ",
                UiText.Main.CategorySummaryTrack,
                segment.CategoryName,
                $"{FormatTime(segment.StartedAt)}-{FormatTime(segment.EndedAt)}",
                FormatDuration(segment.StartedAt, segment.EndedAt),
                segment.DetailText);
        }

        private string? FindActivityHoverTextAt(Point point)
        {
            var row = FindActivityRowAt(point);

            return row is null
                ? null
                : $"{row.ActivityType} | {row.DisplayName} | {row.StartedAtText}-{row.EndedAtText} | {row.DurationText}";
        }

        private ActivityTimelineRow? FindActivityRowAt(Point point)
        {
            var timelineBounds = GetActivityBounds(ClientRectangle);
            return rows
                .Select(row => new { Row = row, Bounds = GetSegmentBounds(row, timelineBounds) })
                .LastOrDefault(x => x.Bounds.Contains(point))
                ?.Row;
        }

        private bool IsHighlighted(ActivityTimelineRow row)
        {
            if (isWindowsHighlighted && highlightedProcessName is null)
                return false;

            var processMatches = highlightedProcessName is null
                || string.Equals(row.ProcessName, highlightedProcessName, StringComparison.OrdinalIgnoreCase);
            var activityTypeMatches = highlightedActivityType is null
                || string.Equals(row.ActivityType, highlightedActivityType, StringComparison.Ordinal)
                || (string.Equals(highlightedActivityType, UiText.Main.Untracked, StringComparison.Ordinal)
                    && string.Equals(row.ActivityType, UiText.Main.TimePilotUntracked, StringComparison.Ordinal));

            return processMatches && activityTypeMatches;
        }

        private bool HasHighlight => highlightedProcessName is not null || highlightedActivityType is not null || isWindowsHighlighted;

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

            return AppFillPalette[GetAppPaletteIndex(row.DisplayName)];
        }

        private static Color GetBorderColor(ActivityTimelineRow row)
        {
            if (string.Equals(row.ActivityType, UiText.Main.Idle, StringComparison.Ordinal))
                return IdleBorderColor;

            if (string.Equals(row.ActivityType, UiText.Main.Untracked, StringComparison.Ordinal)
                || string.Equals(row.ActivityType, UiText.Main.TimePilotUntracked, StringComparison.Ordinal))
                return UntrackedBorderColor;

            return AppBorderPalette[GetAppPaletteIndex(row.DisplayName)];
        }

        private static Color GetCategoryFillColor(CategoryTimelineSegment segment)
        {
            return TryParseColor(segment.Color, out var color)
                ? BlendWithWhite(color, 0.35)
                : AppFillPalette[GetAppPaletteIndex(segment.CategoryName)];
        }

        private static Color GetCategoryBorderColor(CategoryTimelineSegment segment)
        {
            return TryParseColor(segment.Color, out var color)
                ? color
                : AppBorderPalette[GetAppPaletteIndex(segment.CategoryName)];
        }

        private static bool TryParseColor(string? value, out Color color)
        {
            color = Color.Empty;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            try
            {
                color = ColorTranslator.FromHtml(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Color BlendWithWhite(Color color, double whiteRatio)
        {
            whiteRatio = Math.Clamp(whiteRatio, 0, 1);
            var colorRatio = 1 - whiteRatio;
            return Color.FromArgb(
                (int)((color.R * colorRatio) + (255 * whiteRatio)),
                (int)((color.G * colorRatio) + (255 * whiteRatio)),
                (int)((color.B * colorRatio) + (255 * whiteRatio)));
        }

        private static DateTimeOffset Min(DateTimeOffset left, DateTimeOffset right)
        {
            return left <= right ? left : right;
        }

        private static DateTimeOffset Max(DateTimeOffset left, DateTimeOffset right)
        {
            return left >= right ? left : right;
        }

        private void ZoomToDragRange(int startX, int endX)
        {
            var interactiveBounds = GetInteractiveBounds(ClientRectangle);
            if (interactiveBounds.Width <= 0)
                return;

            var leftX = Math.Clamp(Math.Min(startX, endX), interactiveBounds.Left, interactiveBounds.Right);
            var rightX = Math.Clamp(Math.Max(startX, endX), interactiveBounds.Left, interactiveBounds.Right);
            var start = XToViewOffset(leftX, interactiveBounds);
            var end = XToViewOffset(rightX, interactiveBounds);
            if (end - start < MinimumViewRange)
                return;

            SetViewRange(start, end, addHistory: true);
        }

        private void Pan(double ratio)
        {
            if (!IsZoomed)
                return;

            var width = viewEnd - viewStart;
            var offset = TimeSpan.FromTicks((long)(width.Ticks * ratio));
            var nextStart = viewStart + offset;
            var nextEnd = viewEnd + offset;
            if (nextStart < TimeSpan.Zero)
            {
                nextStart = TimeSpan.Zero;
                nextEnd = width;
            }
            else if (nextEnd > TimeSpan.FromDays(1))
            {
                nextEnd = TimeSpan.FromDays(1);
                nextStart = nextEnd - width;
            }

            if (nextStart == viewStart && nextEnd == viewEnd)
                return;

            SetViewRange(nextStart, nextEnd, addHistory: false);
        }

        private void Zoom(double factor, double centerRatio)
        {
            var currentWidth = viewEnd - viewStart;
            var nextWidth = TimeSpan.FromTicks((long)(currentWidth.Ticks * factor));
            if (nextWidth < MinimumViewRange)
                nextWidth = MinimumViewRange;
            if (nextWidth > TimeSpan.FromDays(1))
                nextWidth = TimeSpan.FromDays(1);
            if (nextWidth == currentWidth)
                return;

            centerRatio = Math.Clamp(centerRatio, 0, 1);
            var center = viewStart + TimeSpan.FromTicks((long)(currentWidth.Ticks * centerRatio));
            var nextStart = center - TimeSpan.FromTicks((long)(nextWidth.Ticks * centerRatio));
            var nextEnd = nextStart + nextWidth;
            if (nextStart < TimeSpan.Zero)
            {
                nextStart = TimeSpan.Zero;
                nextEnd = nextWidth;
            }
            else if (nextEnd > TimeSpan.FromDays(1))
            {
                nextEnd = TimeSpan.FromDays(1);
                nextStart = nextEnd - nextWidth;
            }

            SetViewRange(nextStart, nextEnd, addHistory: true);
        }

        private double GetFinePanRatio()
        {
            var width = viewEnd - viewStart;
            if (width <= TimeSpan.FromHours(1))
                return 0.1;
            if (width <= TimeSpan.FromHours(3))
                return 0.15;
            if (width <= TimeSpan.FromHours(8))
                return 0.25;

            return 0.5;
        }

        private void SetViewRange(TimeSpan start, TimeSpan end, bool addHistory)
        {
            var normalizedStart = ClampToDay(start);
            var normalizedEnd = ClampToDay(end);
            if (normalizedEnd - normalizedStart < MinimumViewRange)
                return;
            if (normalizedStart == viewStart && normalizedEnd == viewEnd)
                return;

            if (addHistory)
                viewHistory.Push((viewStart, viewEnd));

            viewStart = normalizedStart;
            viewEnd = normalizedEnd;
            ViewRangeChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        private TimeSpan XToViewOffset(int x, Rectangle bounds)
        {
            var ratio = Math.Clamp((x - bounds.Left) / (double)Math.Max(1, bounds.Width), 0, 1);
            var width = viewEnd - viewStart;
            return viewStart + TimeSpan.FromTicks((long)(width.Ticks * ratio));
        }

        private double GetRatio(TimeSpan value)
        {
            var width = viewEnd - viewStart;
            if (width <= TimeSpan.Zero)
                return 0;

            return Math.Clamp((value - viewStart).TotalMilliseconds / width.TotalMilliseconds, 0, 1);
        }

        private IEnumerable<TimeSpan> GetAxisTicks()
        {
            var step = GetAxisStep();
            var firstTick = TimeSpan.FromTicks(((viewStart.Ticks + step.Ticks - 1) / step.Ticks) * step.Ticks);
            if (firstTick > viewStart)
                yield return viewStart;

            for (var tick = firstTick; tick < viewEnd; tick += step)
                yield return tick;

            yield return viewEnd;
        }

        private TimeSpan GetAxisStep()
        {
            var width = viewEnd - viewStart;
            if (width <= TimeSpan.FromHours(1))
                return TimeSpan.FromMinutes(10);
            if (width <= TimeSpan.FromHours(3))
                return TimeSpan.FromMinutes(30);
            if (width <= TimeSpan.FromHours(8))
                return TimeSpan.FromHours(1);

            return TimeSpan.FromHours(3);
        }

        private static TimeSpan ClampToDay(TimeSpan value)
        {
            if (value < TimeSpan.Zero)
                return TimeSpan.Zero;
            if (value > TimeSpan.FromDays(1))
                return TimeSpan.FromDays(1);

            return value;
        }

        private static string FormatAxisLabel(TimeSpan value)
        {
            if (value >= TimeSpan.FromDays(1))
                return "24";

            return string.Format(CultureInfo.CurrentCulture, "{0:00}:{1:00}", (int)value.TotalHours, value.Minutes);
        }

        private static string FormatTimeOfDay(TimeSpan value)
        {
            if (value >= TimeSpan.FromDays(1))
                return "24:00";

            return string.Format(CultureInfo.CurrentCulture, "{0:00}:{1:00}", (int)value.TotalHours, value.Minutes);
        }

        private static string FormatRangeDuration(TimeSpan value)
        {
            var totalHours = (int)value.TotalHours;
            return string.Format(CultureInfo.CurrentCulture, "{0:00}:{1:00}", totalHours, value.Minutes);
        }

        private Rectangle GetAxisLabelBounds(string label, int centerX, Rectangle timelineBounds)
        {
            var labelSize = TextRenderer.MeasureText(label, Font);
            var width = Math.Max(36, labelSize.Width + 6);
            var left = centerX - (width / 2);
            left = Math.Clamp(left, timelineBounds.Left, Math.Max(timelineBounds.Left, timelineBounds.Right - width));

            return new Rectangle(left, timelineBounds.Bottom + 7, width, 16);
        }

        private static int GetAppPaletteIndex(string displayName)
        {
            var hash = 17;
            foreach (var character in displayName)
                hash = unchecked((hash * 31) + char.ToUpperInvariant(character));

            return Math.Abs(hash) % AppFillPalette.Length;
        }
    }
}
