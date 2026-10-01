using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms.Timeline
{
    internal readonly record struct RuntimeSegmentSelectionKey(
        DateTimeOffset StartedAt,
        DateTimeOffset? EndedAt,
        int ProcessId,
        bool HasMainWindow,
        bool IsCurrentSessionProcess)
    {
        public static RuntimeSegmentSelectionKey From(ProcessRuntimeSegmentRow segment)
        {
            return new RuntimeSegmentSelectionKey(
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
