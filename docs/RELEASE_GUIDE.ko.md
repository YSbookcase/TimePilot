# TimePilot 릴리즈 가이드

## 버전 규칙

TimePilot은 SemVer 형식을 사용한다.

```text
MAJOR.MINOR.PATCH
```

초기 프로토타입 배포 버전은 `0.1.0`으로 시작한다.

- `MAJOR`: 호환성이 크게 깨지는 구조 변경
- `MINOR`: 사용자에게 보이는 기능 추가
- `PATCH`: 버그 수정, 문구 수정, 작은 안정화

버전은 `TimePilot.WinForms/TimePilot.WinForms.csproj`의 다음 속성에 반영한다.

- `Version`
- `FileVersion`
- `AssemblyVersion`

## 릴리즈 산출물

릴리즈에는 다음 파일을 첨부한다.

- `TimePilot-<version>-win-x64-portable.zip`
- `TimePilot-<version>-Setup.exe`

Inno Setup이 설치되어 있지 않은 환경에서는 portable zip만 생성된다.

## 빌드 명령

```powershell
.\scripts\build-release.ps1 -Version 0.1.0
```

PowerShell 실행 정책 때문에 스크립트가 막히면 다음 명령을 사용한다.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-release.ps1 -Version 0.1.0
```

portable zip만 만들려면 다음 명령을 사용한다.

```powershell
.\scripts\build-release.ps1 -Version 0.1.0 -SkipInstaller
```

산출물은 `artifacts/release` 아래에 생성된다.

## GitHub Release 작성

태그 이름은 다음 형식을 사용한다.

```text
v0.1.0
```

릴리즈 제목은 다음 형식을 사용한다.

```text
TimePilot v0.1.0
```

릴리즈 설명에는 다음 내용을 포함한다.

- 주요 기능
- 설치 방법
- 데이터 저장 위치
- 알려진 제한 사항
- 후원 링크

## 초기 릴리즈 설명 초안

```md
## TimePilot v0.1.0

TimePilot은 Windows PC 사용 시간을 로컬에서 기록하고, 어떤 앱에 시간이 쓰였는지 확인할 수 있게 도와주는 데스크톱 앱입니다.

### 주요 기능

- 전경 앱 사용 시간 기록
- 유휴 시간 분리
- 실행 중인 앱 세션 추적
- 일 단위 요약/상세/타임라인 확인
- 트레이 상주 모드
- Windows 시작 시 자동 실행 설정

### 설치

`TimePilot-0.1.0-Setup.exe`를 다운로드해 설치합니다.

설치 없이 실행하려면 `TimePilot-0.1.0-win-x64-portable.zip`을 다운로드해 압축을 풀고 `TimePilot.WinForms.exe`를 실행합니다.

### 데이터 저장 위치

TimePilot은 데이터를 로컬에 저장합니다.

```text
%LocalAppData%\TimePilot
```

### 알려진 제한 사항

- 현재 통계는 주로 일 단위 확인에 초점을 둡니다.
- 성능 진단 문구는 후속 버전에서 설정 또는 진단 모드로 분리할 예정입니다.
- 설치 제거 시 사용자 데이터 삭제 정책은 후속 작업에서 정리할 예정입니다.

### 후원

TimePilot 개발을 응원하고 싶다면 PayPal 후원을 사용할 수 있습니다.

PayPal: <후원 링크 입력>
```
