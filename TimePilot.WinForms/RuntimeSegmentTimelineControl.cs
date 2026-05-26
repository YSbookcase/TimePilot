using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms
{
    internal sealed class RuntimeSegmentTimelineControl : Control
    {
        private static readonly Color SegmentFillColor = Color.FromArgb(137, 176, 211);
        private static readonly Color SegmentBorderColor = Color.FromArgb(72, 119, 164);
        private static readonly Color RunningFillColor = Color.FromArgb(143, 191, 169);
        private static readonly Color RunningBorderColor = Color.FromArgb(75, 137, 111);
        private static readonly Color AxisColor = Color.FromArgb(190, 196, 204);
        private static readonly Color TextColor = Color.FromArgb(65, 72, 82);
        private static readonly Color EmptyTextColor = Color.FromArgb(115, 122, 132);
        private static readonly Color SelectionFillColor = Color.FromArgb(80, 47, 111, 172);
        private static readonly Color SelectionBorderColor = Color.FromArgb(47, 111, 172);
        private static readonly Color HighlightFillColor = Color.FromArgb(238, 189, 78);
        private static readonly Color HighlightBorderColor = Color.FromArgb(90, 58, 12);
        private static readonly TimeSpan MinimumViewRange = TimeSpan.FromMinutes(5);
        private const int MinimumDragPixels = 8;
        private const double WheelZoomInFactor = 0.8;
        private const double WheelZoomOutFactor = 1.25;

        private ProcessRuntimeSummaryRow? summary;
        private IReadOnlyList<ProcessRuntimeSegmentRow> segments = Array.Empty<ProcessRuntimeSegmentRow>();
        private DateTime localDate = DateTime.Today;
        private TimeSpan viewStart = TimeSpan.Zero;
        private TimeSpan viewEnd = TimeSpan.FromDays(1);
        private string? hoverText;
        private ProcessRuntimeSegmentRow? highlightedSegment;
        private RuntimeSegmentKey? highlightedSegmentKey;
        private bool isDragging;
        private int dragStartX;
        private int dragCurrentX;

        public event EventHandler? ViewRangeChanged;

        public bool IsZoomed => viewStart > TimeSpan.Zero || viewEnd < TimeSpan.FromDays(1);

        public bool CanPanPrevious => IsZoomed && viewStart > TimeSpan.Zero;

        public bool CanPanNext => IsZoomed && viewEnd < TimeSpan.FromDays(1);

        public double ViewStartRatio => viewStart.TotalMilliseconds / TimeSpan.FromDays(1).TotalMilliseconds;

        public double ViewWidthRatio => (viewEnd - viewStart).TotalMilliseconds / TimeSpan.FromDays(1).TotalMilliseconds;

        public string ViewRangeText => IsZoomed
            ? $"{FormatTimeOfDay(viewStart)}-{FormatTimeOfDay(viewEnd)}"
            : UiText.CurrentLanguage == UiLanguage.English ? "Full day" : "하루 전체";

        public RuntimeSegmentTimelineControl()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            MinimumSize = new Size(240, 104);
            TabStop = true;
        }

        public void SetSegments(
            DateTime date,
            ProcessRuntimeSummaryRow? selectedSummary,
            IReadOnlyList<ProcessRuntimeSegmentRow> runtimeSegments)
        {
            var dateChanged = localDate.Date != date.Date;
            localDate = date.Date;
            summary = selectedSummary;
            segments = runtimeSegments.OrderBy(x => x.StartedAt).ToList();
            highlightedSegment = highlightedSegmentKey is null
                ? null
                : segments.FirstOrDefault(segment => highlightedSegmentKey.Value.Matches(segment));
            hoverText = null;
            if (dateChanged)
            {
                highlightedSegment = null;
                highlightedSegmentKey = null;
                ResetView();
            }

            Invalidate();
        }

        public void SetHighlightedSegment(ProcessRuntimeSegmentRow? segment)
        {
            RuntimeSegmentKey? key = segment is null ? null : RuntimeSegmentKey.From(segment);
            if (highlightedSegmentKey == key)
                return;

            highlightedSegment = segment;
            highlightedSegmentKey = key;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var graphics = e.Graphics;
            graphics.Clear(BackColor);

            var bounds = ClientRectangle;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            var summaryBounds = new Rectangle(8, 2, Math.Max(1, bounds.Width - 16), 38);
            var timelineBounds = new Rectangle(92, 52, Math.Max(1, bounds.Width - 104), 18);
            DrawSummary(graphics, summaryBounds);
            DrawTrackLabel(graphics, timelineBounds);
            DrawAxis(graphics, timelineBounds);
            DrawSegments(graphics, timelineBounds);
            DrawDragSelection(graphics, timelineBounds);

            if (!string.IsNullOrWhiteSpace(hoverText))
                DrawHoverInfo(graphics, hoverText, bounds);
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

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (isDragging)
                return;

            hoverText = null;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            if (e.Button != MouseButtons.Left)
                return;

            var timelineBounds = GetTimelineBounds(ClientRectangle);
            if (!timelineBounds.Contains(e.Location))
                return;

            isDragging = true;
            dragStartX = e.X;
            dragCurrentX = e.X;
            Capture = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (!isDragging)
                return;

            isDragging = false;
            Capture = false;
            dragCurrentX = e.X;

            if (Math.Abs(e.X - dragStartX) >= MinimumDragPixels)
                ZoomToDragRange(dragStartX, e.X);

            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                var timelineBounds = GetTimelineBounds(ClientRectangle);
                var centerRatio = timelineBounds.Contains(e.Location)
                    ? Math.Clamp((e.X - timelineBounds.Left) / (double)Math.Max(1, timelineBounds.Width), 0, 1)
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

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Escape:
                    ResetView();
                    return true;
                case Keys.Left:
                    Pan(-GetFinePanRatio());
                    return true;
                case Keys.Right:
                    Pan(GetFinePanRatio());
                    return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
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

            SetViewRange(nextStart, nextStart + width);
        }

        private void DrawSummary(Graphics graphics, Rectangle bounds)
        {
            if (summary is null)
            {
                TextRenderer.DrawText(
                    graphics,
                    GetRuntimeNoSelectionText(),
                    Font,
                    bounds,
                    EmptyTextColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                return;
            }

            TextRenderer.DrawText(
                graphics,
                summary.AppName,
                Font,
                new Rectangle(bounds.Left, bounds.Top, bounds.Width, 18),
                TextColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(
                graphics,
                GetSummaryDetailsText(summary, segments),
                Font,
                new Rectangle(bounds.Left, bounds.Top + 18, bounds.Width, 18),
                EmptyTextColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private void DrawTrackLabel(Graphics graphics, Rectangle trackBounds)
        {
            var labelBounds = new Rectangle(6, trackBounds.Top - 1, 82, trackBounds.Height + 2);
            TextRenderer.DrawText(
                graphics,
                GetRuntimeTrackText(),
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
                var x = timelineBounds.Left + (int)Math.Round(timelineBounds.Width * GetViewRatio(tick));
                graphics.DrawLine(axisPen, x, timelineBounds.Top - 4, x, timelineBounds.Bottom + 3);
                var label = FormatTimeOfDay(tick);
                var labelBounds = new Rectangle(x - 18, timelineBounds.Bottom + 4, 36, 18);
                TextRenderer.DrawText(
                    graphics,
                    label,
                    Font,
                    labelBounds,
                    TextColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawSegments(Graphics graphics, Rectangle timelineBounds)
        {
            foreach (var segment in segments)
            {
                if (IsHighlighted(segment))
                    continue;

                DrawSegment(graphics, timelineBounds, segment, isHighlighted: false);
            }

            foreach (var segment in segments)
            {
                if (IsHighlighted(segment))
                    DrawSegment(graphics, timelineBounds, segment, isHighlighted: true);
            }
        }

        private void DrawSegment(Graphics graphics, Rectangle timelineBounds, ProcessRuntimeSegmentRow segment, bool isHighlighted)
        {
            var bounds = GetSegmentBounds(segment, timelineBounds);
            if (bounds.Width <= 0)
                return;

            var isRunning = segment.EndedAt is null;
            using var fillBrush = new SolidBrush(isHighlighted ? HighlightFillColor : isRunning ? RunningFillColor : SegmentFillColor);
            using var borderPen = new Pen(
                isHighlighted ? HighlightBorderColor : isRunning ? RunningBorderColor : SegmentBorderColor,
                isHighlighted ? 3 : isRunning ? 2 : 1);
            graphics.FillRectangle(fillBrush, bounds);
            graphics.DrawRectangle(borderPen, bounds);

            if (!isHighlighted)
                return;

            using var innerPen = new Pen(Color.FromArgb(255, 255, 255), 1);
            graphics.DrawRectangle(innerPen, Rectangle.Inflate(bounds, -2, -2));
        }

        private bool IsHighlighted(ProcessRuntimeSegmentRow segment)
        {
            return highlightedSegmentKey?.Matches(segment) == true;
        }

        private string? FindHoverTextAt(Point point)
        {
            var timelineBounds = GetTimelineBounds(ClientRectangle);
            var segment = segments
                .Select(row => new { Row = row, Bounds = GetSegmentBounds(row, timelineBounds) })
                .LastOrDefault(x => x.Bounds.Contains(point))
                ?.Row;

            if (segment is null)
                return null;

            return string.Join(
                " | ",
                $"{segment.StartedAtText}-{segment.EndedAtText}",
                segment.DurationText,
                segment.StatusText,
                segment.ObservationTypeText);
        }

        private Rectangle GetSegmentBounds(ProcessRuntimeSegmentRow segment, Rectangle timelineBounds)
        {
            var dayStart = new DateTimeOffset(localDate, TimeZoneInfo.Local.GetUtcOffset(localDate));
            var viewStartAt = dayStart + viewStart;
            var viewEndAt = dayStart + viewEnd;
            var effectiveStart = Max(segment.StartedAt, viewStartAt);
            var effectiveEnd = Min(segment.EndedAt ?? DateTimeOffset.UtcNow, viewEndAt);
            if (effectiveEnd <= effectiveStart)
                return Rectangle.Empty;

            var viewDurationMs = Math.Max(1, (viewEndAt - viewStartAt).TotalMilliseconds);
            var startRatio = (effectiveStart - viewStartAt).TotalMilliseconds / viewDurationMs;
            var endRatio = (effectiveEnd - viewStartAt).TotalMilliseconds / viewDurationMs;
            var left = timelineBounds.Left + (int)Math.Round(timelineBounds.Width * startRatio);
            var right = timelineBounds.Left + (int)Math.Round(timelineBounds.Width * endRatio);
            return new Rectangle(left, timelineBounds.Top + 3, Math.Max(2, right - left), Math.Max(8, timelineBounds.Height - 6));
        }

        private void DrawDragSelection(Graphics graphics, Rectangle timelineBounds)
        {
            if (!isDragging)
                return;

            var left = Math.Clamp(Math.Min(dragStartX, dragCurrentX), timelineBounds.Left, timelineBounds.Right);
            var right = Math.Clamp(Math.Max(dragStartX, dragCurrentX), timelineBounds.Left, timelineBounds.Right);
            if (right - left < 2)
                return;

            var selectionBounds = new Rectangle(left, timelineBounds.Top, right - left, timelineBounds.Height);
            using var fillBrush = new SolidBrush(SelectionFillColor);
            using var borderPen = new Pen(SelectionBorderColor);
            graphics.FillRectangle(fillBrush, selectionBounds);
            graphics.DrawRectangle(borderPen, selectionBounds);
        }

        private void ZoomToDragRange(int startX, int endX)
        {
            var timelineBounds = GetTimelineBounds(ClientRectangle);
            var leftRatio = Math.Clamp((Math.Min(startX, endX) - timelineBounds.Left) / (double)Math.Max(1, timelineBounds.Width), 0, 1);
            var rightRatio = Math.Clamp((Math.Max(startX, endX) - timelineBounds.Left) / (double)Math.Max(1, timelineBounds.Width), 0, 1);
            var currentRange = viewEnd - viewStart;
            SetViewRange(
                viewStart + TimeSpan.FromTicks((long)(currentRange.Ticks * leftRatio)),
                viewStart + TimeSpan.FromTicks((long)(currentRange.Ticks * rightRatio)));
        }

        private void Zoom(double factor, double centerRatio)
        {
            var range = viewEnd - viewStart;
            var nextRange = TimeSpan.FromTicks((long)(range.Ticks * factor));
            if (nextRange < MinimumViewRange)
                nextRange = MinimumViewRange;
            if (nextRange > TimeSpan.FromDays(1))
                nextRange = TimeSpan.FromDays(1);

            var center = viewStart + TimeSpan.FromTicks((long)(range.Ticks * Math.Clamp(centerRatio, 0, 1)));
            var nextStart = center - TimeSpan.FromTicks((long)(nextRange.Ticks * Math.Clamp(centerRatio, 0, 1)));
            SetViewRange(nextStart, nextStart + nextRange);
        }

        private void Pan(double ratio)
        {
            if (!IsZoomed)
                return;

            if (viewStart <= TimeSpan.Zero && ratio < 0)
                return;
            if (viewEnd >= TimeSpan.FromDays(1) && ratio > 0)
                return;

            var range = viewEnd - viewStart;
            var offset = TimeSpan.FromTicks((long)(range.Ticks * ratio));
            SetViewRange(viewStart + offset, viewEnd + offset);
        }

        public void ResetView()
        {
            if (!IsZoomed)
                return;

            SetViewRange(TimeSpan.Zero, TimeSpan.FromDays(1));
        }

        private void SetViewRange(TimeSpan start, TimeSpan end)
        {
            var fullDay = TimeSpan.FromDays(1);
            if (end - start < MinimumViewRange)
                end = start + MinimumViewRange;
            if (start < TimeSpan.Zero)
            {
                end -= start;
                start = TimeSpan.Zero;
            }
            if (end > fullDay)
            {
                var overflow = end - fullDay;
                start -= overflow;
                end = fullDay;
            }
            if (start < TimeSpan.Zero)
                start = TimeSpan.Zero;

            viewStart = start;
            viewEnd = end;
            hoverText = null;
            Invalidate();
            ViewRangeChanged?.Invoke(this, EventArgs.Empty);
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

        private double GetViewRatio(TimeSpan value)
        {
            var durationMs = Math.Max(1, (viewEnd - viewStart).TotalMilliseconds);
            return (value - viewStart).TotalMilliseconds / durationMs;
        }

        private IEnumerable<TimeSpan> GetAxisTicks()
        {
            var range = viewEnd - viewStart;
            var interval = range.TotalHours <= 1
                ? TimeSpan.FromMinutes(15)
                : range.TotalHours <= 4
                    ? TimeSpan.FromHours(1)
                    : TimeSpan.FromHours(6);
            var firstTick = TimeSpan.FromTicks((long)(Math.Ceiling(viewStart.Ticks / (double)interval.Ticks) * interval.Ticks));
            for (var tick = firstTick; tick <= viewEnd; tick += interval)
                yield return tick;
        }

        private static Rectangle GetTimelineBounds(Rectangle bounds)
        {
            return new Rectangle(92, 52, Math.Max(1, bounds.Width - 104), 18);
        }

        private static string FormatTimeOfDay(TimeSpan value)
        {
            var totalHours = (int)Math.Floor(value.TotalHours);
            return $"{totalHours:00}:{value.Minutes:00}";
        }

        private string GetViewRangeText()
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? $"view {FormatTimeOfDay(viewStart)}-{FormatTimeOfDay(viewEnd)}"
                : $"보기 {FormatTimeOfDay(viewStart)}-{FormatTimeOfDay(viewEnd)}";
        }

        private void DrawHoverInfo(Graphics graphics, string text, Rectangle bounds)
        {
            var textSize = TextRenderer.MeasureText(text, Font, new Size(Math.Max(120, bounds.Width - 24), 0), TextFormatFlags.SingleLine);
            var width = Math.Min(bounds.Width - 16, textSize.Width + 18);
            var height = textSize.Height + 10;
            var box = new Rectangle(bounds.Right - width - 8, 6, width, height);

            using var fillBrush = new SolidBrush(Color.FromArgb(250, 255, 255, 255));
            using var borderPen = new Pen(Color.FromArgb(180, 190, 196, 204));
            graphics.FillRectangle(fillBrush, box);
            graphics.DrawRectangle(borderPen, box);
            TextRenderer.DrawText(
                graphics,
                text,
                Font,
                new Rectangle(box.Left + 8, box.Top + 5, box.Width - 16, box.Height - 8),
                TextColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private string GetSummaryDetailsText(ProcessRuntimeSummaryRow summary, IReadOnlyList<ProcessRuntimeSegmentRow> segments)
        {
            var longest = segments.Count == 0 ? 0 : segments.Max(x => x.DurationMs);
            var isEnglish = UiText.CurrentLanguage == UiLanguage.English;
            var text = isEnglish
                ? $"runtime {summary.RuntimeText} | active {summary.ActiveUsageTimeText} | idle {summary.IdleRecordedTimeText} | actual {summary.ActualUsageRatioText} | segments {summary.RuntimeSegmentCountText} | longest {FormatDuration(longest)} | first {summary.FirstObservedAtText} | last {summary.LastObservedAtText}"
                : $"실행 {summary.RuntimeText} | 활성 {summary.ActiveUsageTimeText} | 유휴 {summary.IdleRecordedTimeText} | 실사용 {summary.ActualUsageRatioText} | 구간 {summary.RuntimeSegmentCountText} | 최장 {FormatDuration(longest)} | 첫 감지 {summary.FirstObservedAtText} | 마지막 감지 {summary.LastObservedAtText}";

            return IsZoomed ? $"{text} | {GetViewRangeText()}" : text;
        }

        private static string GetRuntimeNoSelectionText()
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? "Select an app to view runtime segments."
                : "앱을 선택하면 실행 구간을 시각화합니다.";
        }

        private static string GetRuntimeTrackText()
        {
            return UiText.CurrentLanguage == UiLanguage.English ? "App runtime" : "앱 실행";
        }

        private static string GetCleanSummaryText(ProcessRuntimeSummaryRow summary, IReadOnlyList<ProcessRuntimeSegmentRow> segments)
        {
            var longest = segments.Count == 0 ? 0 : segments.Max(x => x.DurationMs);
            var isEnglish = UiText.CurrentLanguage == UiLanguage.English;
            return isEnglish
                ? $"{summary.AppName} | runtime {summary.RuntimeText} | active {summary.ActiveUsageTimeText} | idle {summary.IdleRecordedTimeText} | actual {summary.ActualUsageRatioText} | segments {summary.RuntimeSegmentCountText} | longest {FormatDuration(longest)} | first {summary.FirstObservedAtText} | last {summary.LastObservedAtText}"
                : $"{summary.AppName} | 실행 {summary.RuntimeText} | 활성 {summary.ActiveUsageTimeText} | 유휴 {summary.IdleRecordedTimeText} | 실사용 {summary.ActualUsageRatioText} | 구간 {summary.RuntimeSegmentCountText} | 최장 {FormatDuration(longest)} | 첫 감지 {summary.FirstObservedAtText} | 마지막 감지 {summary.LastObservedAtText}";
        }

        private static string GetCleanNoSelectionText()
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? "Select an app to view runtime segments."
                : "앱을 선택하면 실행 구간을 시각화합니다.";
        }

        private static string GetCleanRuntimeTrackLabel()
        {
            return UiText.CurrentLanguage == UiLanguage.English ? "App runtime" : "앱 실행";
        }

        private static string GetSummaryText(ProcessRuntimeSummaryRow summary, IReadOnlyList<ProcessRuntimeSegmentRow> segments)
        {
            var longest = segments.Count == 0 ? 0 : segments.Max(x => x.DurationMs);
            var isEnglish = UiText.CurrentLanguage == UiLanguage.English;
            return isEnglish
                ? $"{summary.AppName} · runtime {summary.RuntimeText} · active {summary.ActiveUsageTimeText} · idle {summary.IdleRecordedTimeText} · actual {summary.ActualUsageRatioText} · segments {summary.RuntimeSegmentCountText} · longest {FormatDuration(longest)} · first {summary.FirstObservedAtText} · last {summary.LastObservedAtText}"
                : $"{summary.AppName} · 실행 {summary.RuntimeText} · 활성 {summary.ActiveUsageTimeText} · 유휴 {summary.IdleRecordedTimeText} · 실사용 {summary.ActualUsageRatioText} · 구간 {summary.RuntimeSegmentCountText} · 최장 {FormatDuration(longest)} · 첫 감지 {summary.FirstObservedAtText} · 마지막 감지 {summary.LastObservedAtText}";
        }

        private static string GetNoSelectionText()
        {
            return UiText.CurrentLanguage == UiLanguage.English
                ? "Select an app to view runtime segments."
                : "앱을 선택하면 실행 구간을 시각화합니다.";
        }

        private static string GetRuntimeTrackLabel()
        {
            return UiText.CurrentLanguage == UiLanguage.English ? "App runtime" : "앱 실행";
        }

        private static string FormatDuration(long durationMs)
        {
            var span = TimeSpan.FromMilliseconds(Math.Max(0, durationMs));
            return $"{(int)span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}";
        }

        private static DateTimeOffset Max(DateTimeOffset left, DateTimeOffset right)
        {
            return left >= right ? left : right;
        }

        private static DateTimeOffset Min(DateTimeOffset left, DateTimeOffset right)
        {
            return left <= right ? left : right;
        }

        private readonly record struct RuntimeSegmentKey(
            DateTimeOffset StartedAt,
            DateTimeOffset? EndedAt,
            int ProcessId,
            bool HasMainWindow,
            bool IsCurrentSessionProcess)
        {
            public static RuntimeSegmentKey From(ProcessRuntimeSegmentRow segment)
            {
                return new RuntimeSegmentKey(
                    segment.StartedAt,
                    segment.EndedAt,
                    segment.ProcessId,
                    segment.HasMainWindow,
                    segment.IsCurrentSessionProcess);
            }

            public bool Matches(ProcessRuntimeSegmentRow segment)
            {
                return StartedAt == segment.StartedAt
                    && EndedAt == segment.EndedAt
                    && ProcessId == segment.ProcessId
                    && HasMainWindow == segment.HasMainWindow
                    && IsCurrentSessionProcess == segment.IsCurrentSessionProcess;
            }
        }
    }
}
