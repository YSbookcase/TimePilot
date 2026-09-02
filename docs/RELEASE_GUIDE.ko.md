# TimePilot 릴리즈 가이드

## 버전 규칙

TimePilot은 SemVer 형식을 사용한다.

```text
MAJOR.MINOR.PATCH
```

- `MAJOR`: 호환성이 크게 깨지는 구조 변경
- `MINOR`: 사용자에게 보이는 기능 추가
- `PATCH`: 버그 수정, 성능 개선, 문구 수정, 안정화

현재 패치 릴리즈는 `0.2.3`을 기준으로 준비한다.

버전은 `TimePilot.WinForms/TimePilot.WinForms.csproj`의 다음 속성에 반영한다.

- `Version`
- `FileVersion`
- `AssemblyVersion`

## 릴리즈 산출물

GitHub Release에는 다음 파일을 첨부한다.

- `TimePilot-<version>-Setup.exe`
- `TimePilot-<version>-win-x64-portable.zip`

Inno Setup이 설치되어 있지 않은 환경에서는 portable zip만 생성될 수 있다.

## 빌드 명령

```powershell
.\scripts\build-release.ps1 -Version 0.2.3
```

PowerShell 실행 정책 때문에 스크립트가 막히면 다음 명령을 사용한다.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-release.ps1 -Version 0.2.3
```

portable zip만 만들려면 다음 명령을 사용한다.

```powershell
.\scripts\build-release.ps1 -Version 0.2.3 -SkipInstaller
```

산출물은 `artifacts/release` 아래에 생성된다.

## GitHub Release 작성

태그 이름은 다음 형식을 사용한다.

```text
v0.2.3
```

릴리즈 제목은 다음 형식을 사용한다.

```text
ActiveLogbook v0.2.3
```

## v0.2.3 릴리즈 설명 초안

## ActiveLogbook v0.2.3

ActiveLogbook v0.2.3은 Microsoft Store 제출 준비 과정에서 확인된 설치, 표시 이름, 요약/타임라인 사용성 문제를 다듬은 패치 버전입니다.

## 변경 사항

- 앱 표시 이름을 ActiveLogbook 기준으로 정리
- 설치, 업데이트, 제거 중 실행 중인 앱 처리 흐름 개선
- 요약 탭의 활성/유휴 시간 표시와 기록 상태 표시 정리
- 타임라인 그래프 확대/축소와 좁은 창 배치 개선
- 앱 분류 관리 화면의 로딩 응답성 개선
- 요약 탭의 특정 날짜 선택에서 달력 드롭다운 사용 가능
- Microsoft Store 제출을 위한 MSIX 패키징 초안 추가

## 업데이트 권장

v0.2.2를 사용 중이라면 v0.2.3으로 업데이트하는 것을 권장합니다.

## 다운로드

- `ActiveLogbook-0.2.3-Setup.exe`: Windows 설치 파일
- `ActiveLogbook-0.2.3-win-x64-portable.zip`: 설치 없이 실행할 수 있는 무설치 압축 파일

## 알려진 제한사항

- 현재는 Windows 전용입니다.
- 코드 서명이 아직 적용되지 않아 Windows SmartScreen 경고가 표시될 수 있습니다.
- 백업 복원은 현재 전체 복원 중심이며, 병합 복원은 아직 제공하지 않습니다.
- 브라우저 방문 기록 같은 상세 웹 사용 기록 연동은 아직 제공하지 않습니다.

## 전체 변경 내역

https://github.com/YSbookcase/TimePilot/compare/v0.2.2...v0.2.3
