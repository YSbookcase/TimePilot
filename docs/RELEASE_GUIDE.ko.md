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

## 배포 채널 정책

일반 사용자에게는 Microsoft Store를 기본 설치 경로로 안내한다.

```text
https://apps.microsoft.com/detail/9NWXBR051GLM
```

GitHub Release의 EXE 설치 파일과 portable zip은 Microsoft Store를 사용할 수 없는 환경,
오프라인 설치, 테스트를 위한 보조·레거시 경로로 취급한다. 신규 사용자를 GitHub EXE로 먼저
유도하지 않는다.

Store, EXE, portable 빌드는 서로 다른 데이터 폴더와 자동 시작 등록을 사용할 수 있다. 배포판
전환 전에는 전체 백업을 만들고 새 설치에서 데이터를 확인한 뒤 이전 설치를 제거하도록 안내한다.
두 배포판의 자동 시작을 동시에 켜도록 안내하지 않는다.

Store에 게시된 버전과 Git 태그 및 GitHub Release는 같은 소스 커밋을 가리켜야 한다. Store
인증과 게시가 확인되기 전에는 해당 버전을 GitHub의 최신 안정 버전으로 선언하지 않는다.

## 릴리즈 산출물

GitHub Release에 보조 배포 파일을 제공하는 경우 다음 파일을 첨부할 수 있다.

- `ActiveLogbook-<version>-Setup.exe`
- `ActiveLogbook-<version>-win-x64-portable.zip`

릴리스 본문에는 Microsoft Store 링크를 먼저 표시하고, EXE와 portable 파일은 보조·레거시
배포이며 Store 버전과 동시에 사용하지 않아야 한다는 주의를 포함한다.

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
