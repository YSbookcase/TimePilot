namespace TimePilot.WinForms.KYS24;

internal enum RestoreAnalysisStage { ExtractBackup, SnapshotCurrent, ReadBackup, ReadCurrent, Compare, Classify, PrepareMapping }

// Units are completed table passes within a stage, not a percentage of elapsed time.
internal sealed record RestoreAnalysisProgress(RestoreAnalysisStage Stage, int Completed = 0, int? Total = null);
