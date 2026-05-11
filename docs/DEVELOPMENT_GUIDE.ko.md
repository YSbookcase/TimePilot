# 개발 가이드 - TimePilot

이 문서는 TimePilot 프로젝트의 개발 규칙을 정의한다.

목표는 WinForms MVP에서 시작한 프로젝트가 점진적으로 계층화된 애플리케이션으로 성장하더라도, 코드와 문서와 커밋 이력이 계속 읽기 쉽고 관리하기 쉬운 상태를 유지하는 것이다.

---

## 1. 커밋 메시지 규칙

Conventional Commits 형식을 사용한다.

형식:

```text
<type>: <짧은 설명>
```

타입은 소문자로 작성하고, 설명은 짧고 명확하게 쓴다.

예:

```text
docs: 활동 추적 모델 문서 추가
feat: 활성 창 감지 기능 추가
fix: 깨진 한글 UI 문구 수정
refactor: 사용 시간 로직을 Core로 이동
chore: gitignore 업데이트
```

### 타입

| Type | 의미 |
|---|---|
| `feat` | 새 기능 |
| `fix` | 버그 수정 |
| `docs` | 문서 변경 |
| `refactor` | 동작 변경 없는 코드 구조 개선 |
| `chore` | 설정, 의존성, 기타 유지보수 작업 |
| `test` | 테스트 변경 |
| `style` | 포맷팅만 변경 |
| `perf` | 성능 개선 |
| `build` | 빌드 시스템 또는 프로젝트 파일 변경 |
| `ci` | CI 설정 변경 |

### 커밋 본문

커밋 본문은 선택 사항이다.

제목만으로 이유, 의도, 구현 방식이 충분히 드러나지 않을 때 작성한다.

예:

```text
feat: 유휴 세션 감지 추가

- 활성 창 사용 시간과 유휴 시간을 분리해서 추적한다.
- Windows 마지막 입력 시간을 초기 유휴 판단 기준으로 사용한다.
```

---

## 2. 브랜치 규칙

짧은 소문자 브랜치 이름을 사용한다.

형식:

```text
<category>/<topic>
```

권장 category:

```text
feature
fix
docs
refactor
chore
```

예:

```text
feature/foreground-app-info
docs/activity-tracking-model
fix/korean-ui-labels
refactor/core-usage-tracking
```

주요 브랜치:

```text
main      안정 브랜치
develop   프로젝트가 커질 때 사용할 수 있는 통합 브랜치
```

작은 문서 수정은 현재 브랜치에서 바로 작업해도 된다. 큰 코드 변경이나 위험한 리팩터링은 별도 브랜치에서 진행한다.

---

## 3. 이슈 규칙

규모가 있는 작업은 이슈를 사용한다.

이슈 제목에는 커밋 메시지와 같은 타입 이름을 사용한다.

권장 제목 형식:

```text
<type>: <짧은 작업 설명>
```

예:

```text
feat: 활성 앱 표시 이름 추가
docs: SQLite 스키마 정의
refactor: 추적 로직을 Core 프로젝트로 이동
fix: 깨진 한글 UI 문구 수정
```

권장 이슈 본문:

```md
## 목표

## 작업 목록

## 참고 사항

## 완료 기준
```

---

## 4. 코드 규칙

C# 표준 네이밍 규칙을 따른다.

| 대상 | 규칙 | 예 |
|---|---|---|
| 클래스, record, struct | PascalCase | `ForegroundAppInfo` |
| 메서드 | PascalCase | `TryGetForegroundAppInfo` |
| 속성 | PascalCase | `ProcessName` |
| 지역 변수 | camelCase | `processName` |
| 파라미터 | camelCase | `idleThresholdMs` |
| private 필드 | `_camelCase` | `_usageAccumulator` |
| 상수 | PascalCase | `SampleIntervalMs` |
| 인터페이스 | `I` 접두어 + PascalCase | `IUsageRepository` |

bool 값은 의미가 잘 드러나는 이름을 사용한다.

예:

```csharp
isIdle
canTrack
hasWindowTitle
shouldStoreWindowTitle
```

클래스는 가능한 한 작은 책임을 갖도록 유지한다.

코드가 더 이상 단순하지 않다면 UI 코드, Windows API 접근, 저장소 코드, 분석 규칙을 한 클래스에 섞지 않는다.

---

## 5. 프로젝트 구조 규칙

기획된 구조를 점진적으로 따른다.

목표 구조:

```text
TimePilot.Core
TimePilot.Infrastructure
TimePilot.WinForms
```

책임:

```text
TimePilot.Core
- 도메인 모델
- 세션 로직
- 시간 집계
- 분석 규칙

TimePilot.Infrastructure
- Windows API reader
- SQLite 저장소
- 파일 시스템 또는 OS 연동

TimePilot.WinForms
- UI
- 사용자 상호작용
- 화면 표시용 포맷팅
```

현재 WinForms MVP에는 임시 구현 클래스가 포함될 수 있다. 구조가 명확해지는 시점에 점진적으로 이동한다.

---

## 6. 문서 규칙

영어 문서는 에이전트와 구현 작업자가 참고하는 기준 문서로 사용한다.

한국어 문서는 자연어 논의, 기획 의도, 원본 맥락을 보존하기 위한 문서로 사용한다.

예:

```text
docs/PROJECT_PLAN.md
docs/PROJECT_PLAN.ko.md
docs/features/ACTIVITY_TRACKING_MODEL.md
docs/features/ACTIVITY_TRACKING_MODEL.ko.md
```

세부 기능 기획은 다음 위치에 둔다.

```text
docs/features/
```

데이터베이스 기획은 다음 위치에 둔다.

```text
docs/database/
```

영어판과 한국어판이 함께 있을 때 역할은 다음과 같다.

- 영어판: 구현 기준 및 에이전트 참조 문서
- 한국어판: 기획 원본 및 논의하기 쉬운 설명 문서

---

## 7. 개인정보 규칙

TimePilot은 로컬 우선 앱이며, 기본적으로 개인정보성이 높은 데이터를 저장하지 않는다.

민감한 데이터 예:

```text
window_title
command_line
전체 실행 파일 경로
웹페이지 제목
문서명
```

이 값들은 사용자가 명시적으로 허용하는 설정이 추가된 뒤에만 저장한다.

기본 추적 대상은 다음에 집중한다.

```text
process_name
display_name
활성 창 시간
유휴 시간
프로세스 실행 시간
```

CPU, 메모리, 디스크, 네트워크 같은 리소스 지표는 시간 모델이 안정화된 뒤 향후 확장으로 다룬다.
