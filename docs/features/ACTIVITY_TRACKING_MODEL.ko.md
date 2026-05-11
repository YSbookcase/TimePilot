# 활동 추적 데이터 모델 계획

## 1. 목적

TimePilot은 단순히 "현재 활성 창"만 기록하는 앱이 아니라, 사용자의 컴퓨터 사용 시간을 더 정확히 나누어 보여주는 도구를 목표로 한다.

현재 우선순위는 리소스 사용량이 아니라 시간이다.

따라서 초기 데이터 모델은 다음 네 가지 시간을 분리해서 기록한다.

- TimePilot 앱 자체가 실행되어 기록할 수 있었던 시간
- 사용자가 실제로 보고 있던 활성 창 시간
- 입력 없이 기준 시간 이상 대기한 유휴 시간
- 사용자가 직접 보지 않았더라도 프로그램이 실행되어 있던 시간

CPU, 메모리, 디스크, 네트워크 사용량 추적은 DB 크기와 성능 부담이 커질 수 있으므로 향후 확장으로 보류한다.

---

## 2. 핵심 구분

### 2.1 Foreground Activity

사용자가 실제로 활성 창으로 보고 있던 앱의 시간이다.

예:

```text
10:00 - 10:20 Visual Studio
10:20 - 10:35 Chrome
```

이 데이터는 "내가 어디에 시간을 썼는가"를 분석하는 핵심 기준이다.

### 2.2 Idle Time

키보드나 마우스 입력이 기준 시간 이상 없었던 구간이다.

예:

```text
10:30 - 10:40 Idle
```

활성 창이 Visual Studio였더라도 이 시간은 실제 활동 시간과 분리해서 보아야 한다.

### 2.3 Process Runtime

프로그램이 백그라운드든 포그라운드든 실행되어 있던 시간이다.

예:

```text
Chrome 실행 시간: 09:00 - 18:00
Visual Studio 실행 시간: 10:00 - 17:00
UnknownApp 실행 시간: 14:10 - 15:45
```

이 데이터는 사용자가 모르는 사이 실행된 프로그램을 찾는 데 도움이 된다.

### 2.4 TimePilot App Runtime

TimePilot 앱 자체가 실행되어 활동을 기록할 수 있었던 시간이다.

예:

```text
TimePilot 실행 시간: 09:00 - 18:00
TimePilot 미실행 시간: 18:00 - 20:00
```

이 데이터는 앱이 꺼져 있어서 기록하지 못한 시간을 구분하는 데 필요하다.

---

## 3. 추천 테이블 구조

### 3.1 app_runtime_sessions

TimePilot 앱 자체의 실행 세션이다.

```text
id
started_at
ended_at
duration_ms
last_heartbeat_at
shutdown_reason
app_version
```

이전 실행 세션에 `ended_at`이 없다면 다음 앱 시작 시 비정상 종료로 간주하고, 가능한 경우 마지막 heartbeat 시간을 종료 시간으로 사용한다.

### 3.2 apps

프로그램을 식별하기 위한 마스터 데이터다.

```text
id
process_name
display_name
executable_path
first_seen_at
last_seen_at
user_alias
is_excluded
```

초기에는 `process_name`과 `display_name` 중심으로 사용한다.

`executable_path`는 사용자 이름 등 민감한 경로를 포함할 수 있으므로 저장 여부를 설정으로 분리할 수 있다.

### 3.3 foreground_sessions

사용자가 활성 창으로 보고 있던 구간이다.

```text
id
app_id
started_at
ended_at
duration_ms
```

`window_title`은 문서명, 웹페이지 제목, 대화명 등을 포함할 수 있으므로 기본 저장 대상에서 제외한다.

향후 사용자가 허용하면 선택적으로 추가한다.

### 3.4 idle_sessions

입력 없이 기준 시간 이상 대기한 구간이다.

```text
id
started_at
ended_at
duration_ms
threshold_ms
foreground_app_id
```

`foreground_app_id`는 유휴가 시작될 때 활성 창이 무엇이었는지 분석할 때 사용한다.

### 3.5 process_runtime_sessions

프로그램이 실행되어 있던 구간이다.

```text
id
app_id
process_id
started_at
ended_at
duration_ms
first_observed_at
last_observed_at
```

프로세스 목록을 일정 주기로 스캔하고, 이전 스냅샷과 비교해서 시작/종료를 추정한다.

초기 스캔 주기는 30초 또는 60초 정도로 시작한다.

---

## 4. 계산 방식

실제 활동 시간은 활성 창 시간에서 유휴 시간이 겹치는 부분을 제외해서 계산한다.

```text
actual_active_time = foreground_time - overlapping_idle_time
```

예:

```text
Visual Studio 활성 창 시간: 3시간
Visual Studio 중 유휴 시간: 40분
Visual Studio 실제 활동 시간: 2시간 20분
```

백그라운드 프로그램은 다음 조건으로 찾을 수 있다.

```text
process_runtime_sessions에는 존재
foreground_sessions에는 거의 없음
```

---

## 5. 구현 순서

### Step 1 - TimePilot 실행 세션

- 앱 시작 시 `app_runtime_sessions`를 시작한다.
- 앱 실행 중 heartbeat를 갱신한다.
- 앱 정상 종료 시 runtime session을 종료한다.
- 이전 실행이 비정상 종료된 경우 다음 실행 시 식별한다.

### Step 2 - 활성 창 세션

- 활성 앱 변경을 감지한다.
- 앱이 바뀌면 이전 foreground session을 종료하고 새 session을 시작한다.

### Step 3 - 유휴 세션

- `GetLastInputInfo` 기반으로 유휴 상태를 감지한다.
- 기준 시간 이상 입력이 없으면 idle session을 시작한다.
- 입력이 다시 들어오면 idle session을 종료한다.

### Step 4 - SQLite 저장

- `app_runtime_sessions`
- `apps`
- `foreground_sessions`
- `idle_sessions`

위 네 테이블을 먼저 저장한다.

### Step 5 - 백그라운드 실행 시간

- 주기적으로 실행 중인 프로세스 목록을 스캔한다.
- 이전 스냅샷과 비교해서 `process_runtime_sessions`를 만든다.
- CPU/메모리 같은 리소스 값은 저장하지 않는다.

---

## 6. 보류 항목

다음 기능은 향후 확장으로 둔다.

- CPU 사용량 추적
- 메모리 사용량 추적
- 디스크 I/O 추적
- 네트워크 사용량 추적
- 명령줄 인자 저장
- 창 제목 기본 저장

이 항목들은 개인정보와 DB 크기 측면에서 부담이 있으므로, 사용자가 명시적으로 허용하는 설정과 함께 설계한다.

---

## 7. 설계 원칙

- 시간 기록을 먼저 안정화한다.
- 사용자 활동과 시스템 활동을 섞지 않는다.
- 리소스 기록은 초기 DB 스키마에 넣지 않는다.
- 개인정보가 포함될 수 있는 값은 기본 저장하지 않는다.
- 향후 확장 가능하도록 앱 식별 정보는 `apps`로 분리한다.
