using TimePilot.WinForms.KYS24;

namespace TimePilot.WinForms;

internal sealed class KeepLaterRestorePreparationForm : Form
{
    private KeepLaterRestorePreparation plan;
    private readonly bool english;
    private readonly TextBox details = new();

    internal KeepLaterRestorePreparationForm(KeepLaterRestorePreparation preparation, UiLanguage language)
    {
        plan = preparation;
        english = language == UiLanguage.English;
        Text = english ? "Restore preparation: no data changes" : "복원 적용 준비: 데이터 변경 없음";
        Name = nameof(KeepLaterRestorePreparationForm);
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(940, 640);
        MinimumSize = new Size(880, 560);
        ShowIcon = false;
        ShowInTaskbar = false;
        MinimizeBox = false;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 4 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.Controls.Add(new Label { Dock = DockStyle.Fill, Text = english
            ? "Preparation only. This checks mapping candidates and unresolved policies, not a validated output database. No restore, backup or settings save runs here."
            : "적용 준비 점검만 수행합니다. 연결 후보와 미해결 정책을 확인하며 완성된 복원 DB는 만들지 않습니다. 복원·백업·설정 저장은 실행하지 않습니다." }, 0, 0);
        var options = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        options.Controls.Add(new Label { AutoSize = true, Text = english ? "Settings in proposed plan" : "계획의 설정 기준", Margin = new Padding(0, 6, 8, 0) });
        var choice = new ComboBox { Name = "restoreSettingsChoice", DropDownStyle = ComboBoxStyle.DropDownList, Width = 420 };
        choice.Items.AddRange(english ? new object[] { "Not selected", "Keep current settings", "Use backup settings (validation pending)" }
            : new object[] { "미선택", "현재 설정 유지", "백업 설정 사용 (검증 미완료)" });
        choice.SelectedIndex = (int)plan.SettingsChoice;
        choice.SelectedIndexChanged += (_, _) => { plan = plan.WithSettingsChoice((RestoreSettingsChoice)choice.SelectedIndex); RefreshDetails(); };
        options.Controls.Add(choice);
        layout.Controls.Add(options, 0, 1);
        details.Name = "restorePreparationDetails";
        details.Dock = DockStyle.Fill;
        details.Multiline = true;
        details.ReadOnly = true;
        details.ScrollBars = ScrollBars.Both;
        details.WordWrap = false;
        layout.Controls.Add(details, 0, 2);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var close = new Button { Name = "closePreparation", Text = english ? "Close" : "닫기", Size = new Size(90, 30), DialogResult = DialogResult.Cancel };
        footer.Controls.Add(close);
        layout.Controls.Add(footer, 0, 3);
        Controls.Add(layout);
        AcceptButton = close;
        CancelButton = close;
        ActiveControl = close;
        RefreshDetails();
    }

    private void RefreshDetails()
    {
        var lines = new List<string>
        {
            english ? $"Unresolved issues: {plan.Blockers.Count} (no apply action in this phase)" : $"미해결 차단 사유: {plan.Blockers.Count}개 (이 단계는 적용 불가)",
            $"Policy: {KeepLaterRestorePreparation.PolicyVersion}",
            $"Plan SHA-256: {plan.PlanSha256}",
            $"Backup snapshot SHA-256: {plan.BackupSha256}",
            $"Current snapshot SHA-256: {plan.CurrentSha256}", "",
            english ? "Blocking reasons" : "차단 사유"
        };
        lines.AddRange(plan.Blockers.Select(b => "- " + Describe(b)));
        lines.Add(english ? "Warnings" : "주의 사항");
        lines.AddRange(plan.Warnings.Select(w => "- " + Describe(w)));
        lines.Add("");
        lines.Add(english ? "Record candidates (not durations or an executable selection)" : "기록 후보 (시간 합산이나 실행 가능한 선택 목록 아님)");
        foreach (var row in plan.Records)
            lines.Add(english ? $"{row.Table}: backup before={row.BackupBefore}, current after={row.CurrentAtOrAfter}, current before replaced={row.CurrentBeforeReplaced}, backup after omitted={row.BackupAtOrAfterOmitted}"
                : $"{row.Table}: 백업 이전={row.BackupBefore}, 현재 이후={row.CurrentAtOrAfter}, 현재 이전 대체={row.CurrentBeforeReplaced}, 백업 이후 제외={row.BackupAtOrAfterOmitted}");
        lines.Add(english ? "Current category -> backup-base identity candidates" : "현재 분류 → 백업 기준 연결 후보");
        lines.AddRange(plan.Categories.Select(c => $"{c.CurrentId} {c.Name} → {c.TargetKey ?? "?"} [{c.Action}]"));
        lines.Add(english ? "Current app -> backup-base identity candidates" : "현재 앱 → 백업 기준 연결 후보");
        lines.AddRange(plan.Apps.Select(a => $"{a.CurrentId} {a.Name} → {a.TargetKey ?? "?"} [{a.Action}]"));
        details.Text = string.Join("\r\n", lines);
    }

    private string Describe(string code) => (english, code) switch
    {
        (false, "reference-unconfirmed") => "백업 기준 시각을 확정할 수 없음",
        (false, "backup-after-reference") => "백업에 기준 시각 이후 관측이 있어 처리 정책 필요",
        (false, "crossing-sessions") => "경계를 걸치는 세션 처리 필요 (자동 분할/제외하지 않음)",
        (false, "open-sessions") => "진행 중 세션 처리 필요 (관측을 정상 종료로 바꾸지 않음)",
        (false, "invalid-records") => "비정상 기록 검토 필요",
        (false, "missing-record-schema") => "필수 기록 테이블/시간 열 누락",
        (false, "settings-choice-required") => "설정 기준 선택 필요",
        (false, "backup-settings-missing") => "백업 설정 파일 없음",
        (false, "backup-settings-unvalidated") => "백업 설정의 내용/PC 종속 옵션 검증 미완료",
        (false, "snapshot-hash-missing") => "입력 스냅샷 해시 없음",
        (false, "identity-schema-unavailable") => "앱/분류 스키마를 읽을 수 없음",
        (false, "category-identity-conflict") => "분류 식별 충돌 (자동 병합하지 않음)",
        (false, "app-category-unmapped") => "앱의 분류 연결을 결정할 수 없음",
        (false, "app-path-conflict") => "같은 프로세스명의 실행 경로 충돌",
        (false, "user-category-conflict") => "현재 사용자 지정 분류와 백업 지정 충돌 (승인 정책 필요)",
        (false, "category-color-backup-priority") => "동일 분류 색상은 백업 우선 후보 (실제 변경 없음)",
        (false, "app-category-backup-priority") => "동일 앱의 비사용자 지정 분류는 백업 우선 후보",
        (false, "app-path-unknown") => "앱 경로가 없어 프로세스명만으로 연결하는 후보 있음",
        _ => code.Replace('-', ' ')
    };
}
