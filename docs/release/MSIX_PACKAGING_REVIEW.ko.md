# ActiveLogbook MSIX 패키징 검토

이 문서는 Microsoft Store 제출 과정에서 EXE 설치 파일 코드 서명 차단 항목이 확인된 뒤, MSIX 전환 가능성을 검토하기 위한 기록이다.

## 배경

2026-09-02 Microsoft Store 인증에서 EXE 설치 파일 제출이 `10.2.9 Security - Package Submissions` 항목으로 반려되었다.

- 제출 파일: `ActiveLogbook-0.2.2-Setup.exe`
- 인증 리포트 판단: `Unsigned`
- 로컬 확인: `Get-AuthenticodeSignature` 결과 `NotSigned`
- 요구 사항: EXE/MSI 경로를 유지하려면 설치 파일과 포함된 PE 파일이 SHA256 이상 Authenticode 코드 서명되어야 한다.

## MSIX를 검토하는 이유

Microsoft Store의 MSIX 제출은 Store가 패키지를 다시 서명하고 배포하는 흐름을 사용할 수 있다. 따라서 EXE/MSI 제출처럼 개발자가 별도의 신뢰된 코드 서명 인증서를 바로 준비해야 하는 부담을 줄일 수 있다.

다만 MSIX는 기존 Inno Setup 설치 모델과 다르므로 설치, 실행, 시작 프로그램, 제거, 데이터 보존 정책을 다시 검증해야 한다.

## 현재 프로젝트 상태

- 앱 프로젝트: `TimePilot.WinForms/TimePilot.WinForms.csproj`
- 대상 프레임워크: `net8.0-windows`
- UI: WinForms
- 출력 이름: `ActiveLogbook.exe`
- 설치 방식: Inno Setup 기반 EXE
- 로컬 데이터 위치: `%LocalAppData%\TimePilot`
- 시작 프로그램 등록: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`에 `Application.ExecutablePath --tray` 저장
- 단일 실행 제어: `TimePilot.SingleInstance` mutex
- 종료 요청: `--shutdown` 인수와 shutdown event 사용

## 로컬 도구 상태

Windows SDK 도구는 설치되어 있다.

- `makeappx.exe`: Windows Kits `10.0.19041.0`, `10.0.26100.0` 아래 존재
- `signtool.exe`: Windows Kits `10.0.19041.0`, `10.0.26100.0` 아래 존재
- `.NET SDK`: 10.0.400

명령줄로 MSIX를 만들 수 있는 기반은 있으나, Store 제출용 패키지 생성을 안정적으로 하려면 Visual Studio의 Windows Application Packaging Project 검토가 우선이다.

## 예상 확인 항목

- [ ] Visual Studio에서 Windows Application Packaging Project를 추가할 수 있는지 확인한다.
- [ ] Partner Center의 ActiveLogbook 제품과 패키징 프로젝트를 연결할 수 있는지 확인한다.
- [ ] Store용 `.msixupload` 또는 `.msixbundle` 산출물을 만들 수 있는지 확인한다.
- [ ] MSIX 설치 후 앱 실행이 정상인지 확인한다.
- [ ] `%LocalAppData%\TimePilot` 기존 데이터가 유지되는지 확인한다.
- [ ] 트레이 상주 모드가 정상 동작하는지 확인한다.
- [ ] Windows 시작 시 자동 실행 설정이 MSIX 환경에서 정상 동작하는지 확인한다.
- [ ] 단일 실행 mutex가 정상 동작하는지 확인한다.
- [ ] 제거 후 앱 파일과 로컬 데이터 보존/삭제 정책이 의도와 맞는지 확인한다.
- [ ] Store 제출 제품명이 기존 Win32 EXE 제출 제품과 충돌하지 않는지 확인한다.

## 주의할 점

### 시작 프로그램 등록

현재 구현은 `Application.ExecutablePath`를 기준으로 `HKCU\...\Run` 값을 직접 등록한다. MSIX 환경에서는 패키지 설치 경로와 실행 경로가 일반 EXE 설치 방식과 달라질 수 있으므로 반드시 실제 설치 후 확인해야 한다.

필요하면 MSIX 전용 시작 작업 방식 또는 패키지 실행 alias/activation 방식을 별도로 검토한다.

### 앱 데이터 위치

현재 앱은 `%LocalAppData%\TimePilot`을 직접 사용한다. 이 경로는 기존 사용자 데이터와 호환성을 유지하기에 유리하지만, MSIX 패키지 앱의 데이터 격리/리디렉션 동작과 충돌하지 않는지 확인해야 한다.

### 기존 Win32 제출과 제품명

현재 Partner Center에는 ActiveLogbook 이름으로 Win32 EXE 제출 흐름을 시작한 상태다. Microsoft 인증 리포트는 같은 이름을 MSIX packaged app으로 쓰려면 기존 Win32 앱 이름을 삭제해야 할 수 있다고 안내했다.

MSIX로 전환하기 전에 다음 중 하나를 결정한다.

- 기존 제출을 취소하거나 삭제하고 같은 이름으로 MSIX 앱을 새로 만든다.
- EXE/MSI 경로를 유지하고 코드 서명 인증서를 준비한다.

## 판단

현재 단계에서는 코드 서명 인증서 비용과 개인 개발자 운영 부담을 줄이기 위해 MSIX 전환 가능성을 먼저 확인하는 것이 합리적이다.

단, MSIX 패키징이 시작 프로그램, 트레이, 로컬 데이터 정책에 문제를 일으키거나 Partner Center 제품명 재사용이 복잡하면 EXE/MSI 코드 서명 경로를 다시 검토한다.
