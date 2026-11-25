using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms;

internal sealed class KeepLaterRestorePreviewForm : Form
{
    internal KeepLaterRestorePreviewForm(KeepLaterRestorePreview plan, UiLanguage language)
    {
        var english = language == UiLanguage.English;
        Text = english ? "Keep later records: preview only" : "이후 기록 유지: 미리보기 전용";
        Name = nameof(KeepLaterRestorePreviewForm);
        ClientSize = new Size(1000, 650);
        MinimumSize = new Size(900, 620);
        AutoScaleMode = AutoScaleMode.Font;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        ShowIcon = false;
        MinimizeBox = false;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 4 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 125));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        string Time(DateTimeOffset? at) => at?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz") ?? "-";
        var source = plan.ReferenceSource switch
        {
            RestoreReferenceSource.Metadata => english ? "Backup metadata" : "백업 메타데이터",
            RestoreReferenceSource.RuntimeHeartbeat => english ? "Inferred: runtime heartbeat" : "추정: 앱 실행 heartbeat",
            RestoreReferenceSource.LastObservation => english ? "Inferred: last observation" : "추정: 마지막 관측",
            _ => english ? "Unavailable" : "결정 불가"
        };
        var heading = english
            ? $"No restore is performed in this window.\r\nReference (local): {Time(plan.ReferenceAt)} | {source}\r\nSnapshot inspected: {Time(plan.ObservedAt)}\r\nBackup last observation: {Time(plan.BackupLastObservation)}\r\nCurrent before-reference replacement candidates: {plan.CurrentBefore}; at/after-reference preservation candidates: {plan.CurrentAfter}."
            : $"이 창에서는 복원을 실행하지 않습니다.\r\n기준 시각(현지): {Time(plan.ReferenceAt)} | {source}\r\n스냅샷 조회: {Time(plan.ObservedAt)}\r\n백업 마지막 관측: {Time(plan.BackupLastObservation)}\r\n현재 기준 이전 대체 후보 {plan.CurrentBefore}개, 기준 이후 보존 후보 {plan.CurrentAfter}개입니다.";
        layout.Controls.Add(new TextBox { Name = "restorePreviewHeading", Dock = DockStyle.Fill, ReadOnly = true, Multiline = true, ScrollBars = ScrollBars.Vertical, Text = heading }, 0, 0);
        var grid = new DataGridView { Name = "restorePreviewGrid", Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
            AllowUserToDeleteRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells };
        grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        var headers = english ? new[] { "Source", "Table", "Before", "At/after", "Crossing", "Open", "Invalid", "No cutoff", "Observed range" }
            : new[] { "출처", "기록 종류", "기준 이전", "기준 이후", "경계 걸침", "진행 중", "비정상", "기준 없음", "관측 범위" };
        var widths = new[] { 8F, 18F, 9F, 9F, 9F, 8F, 8F, 8F, 23F };
        for (var i = 0; i < headers.Length; i++) grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = headers[i], FillWeight = widths[i], SortMode = DataGridViewColumnSortMode.NotSortable });
        foreach (var table in plan.Tables)
        {
            var tableName = english ? table.Name : table.Name switch
            {
                "foreground_sessions" => "전경 사용", "idle_sessions" => "유휴", "app_runtime_sessions" => "앱 실행",
                "process_runtime_sessions" => "프로세스 실행", "system_events" => "시스템 이벤트", _ => table.Name
            };
            foreach (var (label, counts, available) in new[] { (english ? "Backup" : "백업", table.Backup, table.BackupAvailable), (english ? "Current" : "현재", table.Current, table.CurrentAvailable) })
                grid.Rows.Add(label, available ? tableName : tableName + (english ? " (unavailable)" : " (테이블 없음)"),
                    counts.Before, counts.AtOrAfter, counts.Crossing, counts.Open, counts.Invalid, counts.Unclassified,
                    $"{Time(counts.Earliest)}\n{Time(counts.Latest)}");
        }
        layout.Controls.Add(grid, 0, 1);
        var warnings = new List<string> { english
            ? "Preview only. No apply action is available. These are row counts, not summed usage durations. App/category mapping and settings policies still require validation."
            : "미리보기 전용이며 적용 기능은 없습니다. 레코드 개수이지 합산 사용시간이 아닙니다. 앱·분류 매핑과 설정 정책은 추가 검증이 필요합니다." };
        warnings.Add(english ? "Proposed direction: use backup rows before the reference and current rows at/after it; crossing/open rows remain unresolved."
            : "정책 초안: 기준 이전은 백업, 기준 이후는 현재 기록을 우선합니다. 경계 걸침·진행 중 기록은 아직 미결정입니다.");
        if (plan.ReferenceSource != RestoreReferenceSource.Metadata) warnings.Add(english ? "Reference time is inferred or missing; it is not a confirmed backup creation time." : "기준 시각이 추정 또는 누락되어 실제 백업 생성 시각으로 확정할 수 없습니다.");
        if (plan.MetadataAt > plan.ObservedAt) warnings.Add(english ? "Future metadata time was not used." : "미래로 기록된 메타데이터 시각은 사용하지 않았습니다.");
        if (plan.BackupBeyondReference) warnings.Add(english ? "Backup observations extend past the reference time; review snapshot timing." : "백업에 기준 시각 이후 관측이 있습니다. 스냅샷 생성 시차를 검토해야 합니다.");
        if (plan.Crossing + plan.Open + plan.Invalid > 0) warnings.Add(english
            ? $"Needs review: crossing {plan.Crossing}, open {plan.Open}, invalid {plan.Invalid}. No sessions are silently split, closed or discarded."
            : $"검토 필요: 경계 걸침 {plan.Crossing}개, 진행 중 {plan.Open}개, 비정상 {plan.Invalid}개. 세션을 임의로 분할·종료·삭제하지 않습니다.");
        if (plan.MissingTables) warnings.Add(english ? "Some tables/required time columns are unavailable; zero is not proof of no records." : "일부 테이블/시간 열이 없어 분석할 수 없습니다. 표시된 0개를 기록 없음으로 해석하면 안 됩니다.");
        layout.Controls.Add(new TextBox { Name = "restorePreviewWarnings", Dock = DockStyle.Fill, ReadOnly = true, Multiline = true,
            ScrollBars = ScrollBars.Vertical, Text = string.Join("\r\n", warnings) }, 0, 2);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var close = new Button { Name = "restorePreviewClose", Text = english ? "Close" : "닫기", Size = new Size(90, 30), DialogResult = DialogResult.Cancel };
        footer.Controls.Add(close);
        layout.Controls.Add(footer, 0, 3);
        Controls.Add(layout);
        CancelButton = close;
        AcceptButton = close;
        ActiveControl = close;
    }
}
