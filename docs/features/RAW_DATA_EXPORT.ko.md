# 원본 데이터 내보내기

원본 데이터 내보내기는 TimePilot의 내부 SQLite 테이블을 CSV 파일로 추출해 ZIP 파일로 묶는 기능이다.

현재 1차 구현은 **기간 제한 없이 전체 원본 데이터를 내보낸다.** 특정 기간만 내보내는 기능은 대용량 내보내기 UX와 함께 후속 작업으로 검토한다.

일반 `CSV 내보내기`는 사용자가 보기 쉽게 가공된 요약, 타임라인, 실행 구간 데이터를 내보낸다. 반면 원본 데이터 내보내기는 내부 테이블 구조에 가까운 컬럼과 값을 유지한다.

## 용도

- 사용자가 자신의 데이터를 외부 분석 도구에서 확인
- 개발 또는 문제 진단 시 실제 저장 데이터 검증
- 향후 통계와 시각화 기능의 원본 데이터 확인

이 기능은 백업/복원 기능이 아니다. 복원 가능한 백업은 별도 기능에서 SQLite DB와 설정 파일을 대상으로 다룬다.

## 다른 내보내기와의 차이

- **CSV 내보내기**: 기간을 골라 요약·타임라인 등을 사람이 읽거나 엑셀에서 확인하기 위한 기능.
- **원본 데이터 전체 내보내기**: 전체 기간의 내부 테이블 CSV를 외부 분석 도구에서 조회하거나 저장 문제를 진단하기 위한 고급 기능. 현재 앱으로 다시 가져오는 복원 파일이 아니다.
- **백업 만들기**: SQLite DB와 설정을 포함해 앱의 전체 복원에 사용할 ZIP을 만드는 기능.
- **앱 분류 내보내기**: 분류와 앱별 지정을 JSON으로 옮기는 별도 기능. 사용 기록·설정·실행 경로·별칭은 제외하지만 분류명과 분류된 앱의 프로세스명은 포함한다. `앱 분류만 가져오기`에서 읽는다.

원본 내보내기는 #70에서 외부 분석·진단을 위해 도입했다. 일반 사용자에게 필수인 절차는
아니지만 백업과 목적이 다르므로 유지한다. ZIP의 `README.txt`가 실제 포함 파일과 컬럼을
나열한다. 현재는 분류, 앱, 앱 식별 관측, 앱 실행, 전경, 유휴, 프로세스 실행, 시스템 이벤트
테이블을 내보내며, 아래 목록은 주요 데이터 설명이다. 추가된 분류 `source`도 진단 CSV에 포함한다.

## 개인정보 주의

내보낸 ZIP 파일에는 전체 기간의 앱 이름, 프로세스 이름, 실행 파일 경로, 사용 시간, 실행 구간 등 개인 사용 기록이 포함될 수 있다.

공유하거나 외부 저장소에 보관할 때 주의해야 한다.

## 포함 파일

### `apps.csv`

앱 식별과 표시 정보.

- `id`
- `process_name`
- `display_name`
- `executable_path`
- `first_seen_at`
- `last_seen_at`
- `user_alias`
- `is_excluded`

### `app_runtime_sessions.csv`

TimePilot 자체 실행 세션과 종료 사유.

- `id`
- `started_at`
- `ended_at`
- `duration_ms`
- `last_heartbeat_at`
- `shutdown_reason`
- `system_booted_at`
- `app_version`

### `foreground_sessions.csv`

사용자가 실제 foreground에서 사용한 앱 세션.

- `id`
- `app_id`
- `started_at`
- `ended_at`
- `duration_ms`
- `last_observed_at`

### `idle_sessions.csv`

유휴 상태로 판단된 구간.

- `id`
- `started_at`
- `ended_at`
- `duration_ms`
- `threshold_ms`
- `foreground_app_id`

### `process_runtime_sessions.csv`

백그라운드 앱 추적으로 감지한 프로세스 실행 세션.

- `id`
- `app_id`
- `process_id`
- `started_at`
- `ended_at`
- `duration_ms`
- `first_observed_at`
- `last_observed_at`
- `tracking_scope`
- `has_main_window`
- `is_current_session_process`
