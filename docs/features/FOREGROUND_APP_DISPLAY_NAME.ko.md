# 활성 앱 표시 이름 개선 계획

## 1. 목적

현재 TimePilot은 활성 창의 프로그램을 식별할 때 프로세스 이름(`Process.ProcessName`)을 사용한다.

예:

- `chrome`
- `devenv`
- `Code`
- `msedge`

이 값은 내부 식별자로는 유용하지만, 사용자가 보기에는 작업 관리자에 표시되는 이름과 달라 직관성이 떨어진다.

이 개선 작업의 목적은 UI에는 사람이 이해하기 쉬운 앱 이름을 표시하고, 내부 추적에는 안정적인 프로세스 이름을 함께 유지하는 것이다.

---

## 2. 현재 상태

현재 관련 코드는 다음 위치에 있다.

- `TimePilot.WinForms/KYS24/ForegroundWindowReader.cs`
- `TimePilot.WinForms/Form1.cs`
- `TimePilot.WinForms/KYS24/UsageAccumulator.cs`

현재 흐름:

1. `ForegroundWindowReader.TryGetForegroundProcessName()` 호출
2. 활성 창의 프로세스 ID 조회
3. `Process.GetProcessById(...)` 호출
4. `process.ProcessName` 반환
5. `Form1`에서 해당 이름을 UI와 누적 집계에 사용

문제점:

- UI 표시 이름과 내부 식별자가 하나로 섞여 있다.
- 작업 관리자에 가까운 이름을 보여주지 못한다.
- 향후 SQLite 저장 시 `process_name`, `display_name`, `window_title` 같은 데이터를 분리하기 어렵다.

---

## 3. 목표 동작

활성 앱 정보를 다음처럼 분리한다.

```csharp
public sealed class ForegroundAppInfo
{
    public string ProcessName { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string? WindowTitle { get; init; }
}
```

UI 표시에는 `DisplayName`을 우선 사용한다.

분석 및 저장 기준에는 `ProcessName`을 유지한다.

예:

| ProcessName | DisplayName | WindowTitle |
|---|---|---|
| `chrome` | `Google Chrome` | 선택 저장 |
| `devenv` | `Microsoft Visual Studio 2022` | 선택 저장 |
| `Code` | `Visual Studio Code` | 선택 저장 |
| `msedge` | `Microsoft Edge` | 선택 저장 |

---

## 4. 표시 이름 결정 우선순위

표시 이름은 다음 우선순위로 결정한다.

1. `FileVersionInfo.FileDescription`
2. `FileVersionInfo.ProductName`
3. `Process.ProcessName`

권한 문제, 시스템 프로세스, 종료된 프로세스 등으로 인해 `MainModule` 접근이 실패할 수 있으므로 예외를 허용하고 fallback을 사용한다.

```text
FileDescription 실패
→ ProductName 실패
→ ProcessName 사용
```

---

## 5. 개인정보 고려

`WindowTitle`은 사용자가 실제로 보고 있는 문서명, 웹페이지 제목, 메신저 대화명 등을 포함할 수 있다.

따라서 초기 MVP에서는 다음 정책을 따른다.

- 내부 모델에는 `WindowTitle` 필드를 둘 수 있다.
- UI 표시나 저장에는 기본적으로 사용하지 않는다.
- 향후 설정에서 사용자가 허용한 경우에만 저장한다.

---

## 6. 구현 순서

### Step 1 - 앱 정보 모델 추가

`ForegroundAppInfo` 클래스를 추가한다.

초기 위치 후보:

- 단기: `TimePilot.WinForms/KYS24/ForegroundAppInfo.cs`
- 구조 분리 이후: `TimePilot.Core` 또는 `TimePilot.Infrastructure`

현재 프로젝트 단계에서는 단기 위치에 추가하고, 이후 Core/Infrastructure 분리 시 이동한다.

### Step 2 - ForegroundWindowReader 반환값 변경

기존 메서드:

```csharp
TryGetForegroundProcessName()
```

개선 메서드:

```csharp
TryGetForegroundAppInfo()
```

반환 타입:

```csharp
ForegroundAppInfo?
```

### Step 3 - 표시 이름 추출 로직 추가

`Process.MainModule?.FileVersionInfo`에서 다음 값을 읽는다.

- `FileDescription`
- `ProductName`

값이 비어 있거나 예외가 발생하면 `ProcessName`을 사용한다.

### Step 4 - UsageAccumulator 입력 기준 정리

집계 기준은 우선 `DisplayName`이 아니라 `ProcessName`을 사용한다.

이유:

- 같은 앱이 여러 표시 이름을 가질 가능성을 줄인다.
- SQLite 저장 시 안정적인 앱 식별자로 사용하기 좋다.

단, UI에는 `DisplayName`을 보여주기 위해 snapshot 구조 개선이 필요할 수 있다.

초기 MVP에서는 다음 중 하나를 선택한다.

- 간단한 방식: `UsageAccumulator`는 `DisplayName` 기준으로 집계한다.
- 권장 방식: `ProcessName` 기준으로 집계하고, 마지막으로 확인된 `DisplayName`을 별도 보관한다.

권장 방식이 향후 저장 구조와 더 잘 맞는다.

### Step 5 - Form1 UI 반영

`Form1`에서 상태 표시 라벨은 `DisplayName`을 사용한다.

예:

```text
현재 창: Google Chrome - 활성
현재 창: Microsoft Visual Studio 2022 - 활성
```

목록 표시도 가능하면 `DisplayName`을 보여준다.

### Step 6 - 깨진 한글 문자열 정리

현재 `Form1.cs`에는 인코딩이 깨진 UI 문자열이 있다.

예:

```csharp
var idleText = isIdle ? "?좏쑕" : "?쒖꽦";
```

이 작업과 함께 다음처럼 정리한다.

```csharp
var idleText = isIdle ? "유휴" : "활성";
```

---

## 7. 검증 방법

다음 앱을 활성화하면서 UI 표시를 확인한다.

- Visual Studio
- Visual Studio Code
- Chrome 또는 Edge
- 파일 탐색기
- 작업 관리자

확인할 것:

- 앱이 종료되거나 접근 권한이 부족해도 예외로 앱이 죽지 않는다.
- 표시 이름이 비어 있으면 프로세스 이름으로 fallback된다.
- 유휴 상태에서는 사용 시간이 누적되지 않는다.
- 기존 1초 주기 추적 동작이 유지된다.

---

## 8. 이후 확장

SQLite 저장 단계에서는 다음 컬럼 구성을 고려한다.

```text
process_name
display_name
window_title
started_at
ended_at
duration_ms
```

향후 사용자가 앱 이름을 직접 별칭으로 지정할 수 있게 할 수도 있다.

예:

```text
devenv → Visual Studio
chrome → Chrome
Code → VS Code
```
