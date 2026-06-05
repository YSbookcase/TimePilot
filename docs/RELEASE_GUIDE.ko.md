# TimePilot 릴리즈 가이드

## 버전 규칙

TimePilot은 SemVer 형식을 사용한다.

```text
MAJOR.MINOR.PATCH
```

- `MAJOR`: 호환성이 크게 깨지는 구조 변경
- `MINOR`: 사용자에게 보이는 기능 추가
- `PATCH`: 버그 수정, 성능 개선, 문구 수정, 안정화

현재 핫픽스 릴리즈는 `0.2.1`을 기준으로 준비한다.

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
.\scripts\build-release.ps1 -Version 0.2.1
```

PowerShell 실행 정책 때문에 스크립트가 막히면 다음 명령을 사용한다.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-release.ps1 -Version 0.2.1
```

portable zip만 만들려면 다음 명령을 사용한다.

```powershell
.\scripts\build-release.ps1 -Version 0.2.1 -SkipInstaller
```

산출물은 `artifacts/release` 아래에 생성된다.

## GitHub Release 작성

태그 이름은 다음 형식을 사용한다.

```text
v0.2.1
```

릴리즈 제목은 다음 형식을 사용한다.

```text
TimePilot v0.2.1
```

## v0.2.1 릴리즈 설명 초안

## TimePilot v0.2.1

TimePilot v0.2.1은 v0.2.0 공개 후 확인된 타임라인/상세 탭 디스크 읽기 사용량 문제를 완화하는 핫픽스 버전입니다.

## 변경 사항

- 타임라인/상세 탭에서 날짜 전체 데이터를 매초 다시 읽지 않도록 개선
- 앱 전환, 유휴/활성 상태 변경, 프로세스 시작/종료가 있을 때는 즉시 다시 조회하도록 보강
- 진행 중 실행 시간은 매초 표시되도록 유지
- 마지막 감지는 실제 감지 주기 기준으로 표시되도록 유지
- 앱 아이콘 조회 실패 결과를 캐시해 반복 파일 접근을 줄임
- 타임라인 첫 진입 시 일시적으로 미실행 구간이 표시될 수 있는 현상 완화

## 업데이트 권장

v0.2.0을 사용 중이라면 v0.2.1로 업데이트하는 것을 권장합니다.

## 다운로드

- `TimePilot-0.2.1-Setup.exe`: Windows 설치 파일
- `TimePilot-0.2.1-win-x64-portable.zip`: 설치 없이 실행할 수 있는 무설치 압축 파일

## 알려진 제한사항

- 현재는 Windows 전용입니다.
- 코드 서명이 아직 적용되지 않아 Windows SmartScreen 경고가 표시될 수 있습니다.
- 백업 복원은 현재 전체 복원 중심이며, 병합 복원은 아직 제공하지 않습니다.
- 브라우저 방문 기록 같은 상세 웹 사용 기록 연동은 아직 제공하지 않습니다.

## 전체 변경 내역

https://github.com/YSbookcase/TimePilot/compare/v0.2.0...v0.2.1
