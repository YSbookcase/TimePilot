# ActiveLogbook Microsoft Store 제출 기록 (v0.2.3)

이 문서는 2026-09-12에 진행한 ActiveLogbook의 첫 MSIX Microsoft Store 제출 상태와, 이후 개발을 재개할 때 확인할 항목을 기록한다.

## 현재 상태

- Store 제품명: `ActiveLogbook`
- 배포 형식: MSIX
- 제출 버전: `0.2.3.0` (`x64`)
- Partner Center 상태: 인증 중, 사전 처리 단계
- 게시 방식: 인증 통과 후 자동 게시
- 가격: 전 세계 시장, 무료 (`USD 0` 기준 가격)
- 대상: Windows 10/11 Desktop
- 제품 공개 및 Store 검색: 사용

인증이 통과하면 Store가 MSIX 패키지를 다시 서명하고 배포한다. 일반 사용자는 `.msixupload` 파일, 테스트 인증서, `Install.ps1`을 사용하지 않고 Microsoft Store에서 설치한다.

## 제출에 사용한 패키지

- 업로드 파일: `artifacts/msix/TimePilot.Packaging_0.2.3.0_x64.msixupload`
- 로컬 테스트 폴더: `artifacts/msix/TimePilot.Packaging_0.2.3.0_x64_Test`
- 패키지 Identity: `YSBookcase.ActiveLogbook`
- 표시 이름: `ActiveLogbook`
- 게시자 표시 이름: `YS Bookcase`
- 제한 기능: `runFullTrust`

`runFullTrust`는 패키지된 WinForms/Win32 데스크톱 프로세스를 일반 사용자 권한(medium integrity)으로 실행하기 위해 선언했다. 관리자 권한, 드라이버, 서비스 설치를 요구하지 않는다. 제출 옵션에는 이 용도를 설명했으며, 사용 기록은 로컬 앱 데이터 폴더에만 저장되고 개발자 서버로 전송하지 않는다고 명시했다.

## Store 등록 정보

- 지원 페이지: https://ys-bookcase.com/active-logbook/support/
- 지원 이메일: support@ys-bookcase.com
- 개인정보처리방침: https://ys-bookcase.com/active-logbook/privacy-policy/
- 언어: 영어(미국), 한국어(한국)
- 각 언어에 Store 설명, 릴리스 노트, 기능 목록, 키워드 및 스크린샷 4장을 입력했다.
- 1:1 Microsoft Store Box art를 업로드했다.
- 트레일러, Xbox 자산, 9:16 포스터, 16:9 슈퍼히어로 아트는 이번 제출에서 사용하지 않았다.
- 연령 등급: IARC 설문 완료, 전 연령/3세 이상 수준으로 생성됨.

## 로컬 MSIX 설치 검증

다음 명령으로 테스트 패키지 설치가 성공했다.

```powershell
cd "E:\Program_Study\TimePilotWorkspace\TimePilot\artifacts\msix\TimePilot.Packaging_0.2.3.0_x64_Test"
powershell -ExecutionPolicy Bypass -File ".\Install.ps1"
```

설치 스크립트는 테스트 인증서를 현재 PC에 설치한 뒤 패키지를 설치한다. 이 인증서는 로컬 테스트에만 필요하며 Store 배포와는 관계없다.

Windows 설정의 설치된 앱에는 기존 EXE 설치본과 MSIX 테스트 설치본이 함께 표시될 수 있다.

- `ActiveLogbook 0.2.2`, 약 167MB: 기존 Inno Setup EXE 설치본
- `ActiveLogbook`, `YS Bookcase`, 2026-09-12, 약 164MB: MSIX 테스트 설치본으로 판단
- 작은 용량의 이전 항목이 남아 있으면, 새 MSIX 실행 검증이 끝나기 전에는 제거하지 않는다.

## 남은 검증 순서

Store 인증 중에도 다음 검증은 계속 진행한다. 문제를 발견하면 인증을 취소하고 수정 패키지로 다시 제출한다.

1. 시작 메뉴에서 새 MSIX `ActiveLogbook`을 실행한다.
2. 기존 EXE와 동시에 실행하지 않은 상태에서 활성 창 기록, 유휴 시간, 요약, 타임라인, 상세 화면을 확인한다.
3. 앱을 종료하고 다시 실행해 기존 로컬 데이터가 유지되는지 확인한다.
4. 트레이 상주, 단일 실행 방지, Windows 시작 시 자동 실행을 확인한다.
5. CSV/원시 데이터 내보내기, 백업, 복원, 로컬 데이터 삭제를 확인한다.
6. 새 MSIX가 정상 동작한다고 판단한 뒤에만 기존 `0.2.2` EXE를 제거한다.
7. 시작 메뉴와 Windows 설정에서 MSIX 아이콘을 확인한다. 2026-09-13 확인 결과, 인증 중인 `0.2.3.0` 테스트 패키지에는 회색 기본 아이콘 자산이 포함되어 있어 빈 아이콘처럼 표시된다.

## 다음 개발 작업의 우선순위

1. 위 MSIX 실제 설치 검증을 완료하고 결과를 이 문서에 추가한다.
2. Store 인증 결과와 실패 사유가 있으면 기록하고, 필요 시 `0.2.3.1` 패치 릴리즈를 만든다.
3. 아이콘 수정이 필요한 경우 앱/패키지 버전을 올린 새 MSIX를 만들고, 테스트 설치 후 인증을 취소하거나 다음 제출로 교체한다. 이미 인증 중인 `0.2.3.0` 패키지는 수정할 수 없다.
4. Store 아이콘과 설치 목록 중복 항목을 확인한다. 기존 EXE와 MSIX의 공존은 전환 과정에서는 허용되지만, 최종 사용 흐름은 Store 설치본 기준으로 정리한다.
5. 제품 기능은 `docs/PROJECT_PLAN.ko.md`의 Phase 4 항목(주간/월간 통계, TOP 앱 분석, 기록 커버리지, 타임라인 개선)을 우선 검토한다.

## 소스 관리 메모

- 현재 기준 브랜치: `develop`
- Store association 병합: PR #318, 커밋 `bbf2c53`
- `0.2.3` 릴리즈 준비 병합: PR #317, 커밋 `c7b422e`
- MSIX 로고 자산 수정 병합: 커밋 `c9839c9`

`c9839c9`은 패키지의 `Square44x44Logo`, `Square150x150Logo`, Store 로고 등 기본 템플릿 이미지를 ActiveLogbook 나침반 이미지로 교체했다. 수정된 `Square44x44Logo`가 실제로 나침반 이미지인 것을 확인했다. 이 변경은 이미 만들어 인증 중인 `0.2.3.0` 패키지에는 반영되지 않는다.

## 아이콘 수정 패키지 (v0.2.4)

2026-09-13에 아이콘 수정과 함께 앱 및 MSIX 패키지 버전을 `0.2.4.0`으로 올렸다.

- 테스트 패키지: `artifacts/msix/TimePilot.Packaging_0.2.4.0_x64_Test/TimePilot.Packaging_0.2.4.0_x64.msix`
- Store 업로드 패키지: `artifacts/msix/TimePilot.Packaging_0.2.4.0_x64.msixupload`
- 빌드 결과: 경고 0개, 오류 0개
- 패키지 검사: 매니페스트 버전 `0.2.4.0`, `Square44x44Logo` 및 Store 로고에 수정된 아이콘 자산 포함

이 패키지는 아직 로컬 설치 및 기능 검증 전이며 Partner Center에 업로드하지 않았다. 검증이 끝난 뒤에만 `0.2.3.0` 인증을 취소하고 `0.2.4.0`을 제출한다.

`0.2.4`에서는 첫 실행 시 Windows 표시 언어가 `ko` 계열이면 한국어를, 그 외에는 영어를 기본 UI 언어로 선택하도록 변경했다. 사용자가 환경설정에서 직접 선택한 언어는 기존과 같이 설정 파일에 저장되어 다음 실행에도 유지된다.

## MSIX 자동 시작 수정 패키지 (v0.2.5)

`0.2.4.0` MSIX 테스트에서 기존 EXE용 `HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run` 등록이 패키지 환경의 Windows 시작 프로그램으로 동작하지 않는 것을 확인했다. MSIX 앱은 `windows.startupTask` 매니페스트 확장과 `StartupTask` API를 사용해야 한다.

- 패키지된 MSIX에서는 Windows `StartupTask`를 사용한다.
- 기존 EXE와 무설치 버전에서는 기존 Run 레지스트리 등록을 계속 사용한다.
- 환경 설정의 `Windows 시작 시 실행` 값이 켜지면 MSIX 시작 작업을 사용 설정하고, 꺼지면 해제한다.
- Task Manager 또는 Windows 설정에서 사용자가 시작 작업을 껐다면 앱이 이를 강제로 다시 켜지 않는다.
- 새 테스트 및 Store 제출 버전은 `0.2.5.0`이다.

## MSIX 자동 시작 재수정 패키지 (v0.2.6)

`0.2.5.0` 테스트에서 Windows 시작 앱을 수동으로 `사용`으로 바꿔도 다시 `사용 안 함`으로 돌아가고, 재시작 후 자동 실행되지 않는 현상을 확인했다. `desktop:StartupTask` 매니페스트의 `Enabled` 값이 `false`여서 코드의 활성화 요청에만 의존하고 있었다.

- `desktop:StartupTask Enabled`를 `true`로 변경했다. 패키지된 데스크톱 앱은 이 설정으로 첫 실행 시 Windows 시작 작업을 기본 활성 상태로 등록한다.
- 사용자가 앱 환경설정에서 `Windows 시작 시 실행`을 끄면 앱의 기존 코드가 시작 작업을 해제한다.
- 새 테스트 패키지는 `artifacts/msix/TimePilot.Packaging_0.2.6.0_x64_Test/TimePilot.Packaging_0.2.6.0_x64.msix`이다.
- Store 제출 파일은 이 자동 시작 검증이 성공한 뒤에만 생성·업로드한다.
- `0.2.5.0`은 Partner Center에 업로드하지 않는다.

## MSIX 자동 시작 작업 재등록 패키지 (v0.2.7)

`0.2.6.0`에서도 기존 MSIX 시작 작업이 Windows 시작 앱에서 즉시 `사용 안 함`으로 되돌아가는 현상이 지속되었다. 이 작업은 이전 테스트 패키지에서 이미 해제된 상태를 유지할 수 있으므로, 새 작업 ID로 다시 등록해 이전 상태를 승계하지 않게 했다.

- 시작 작업 ID: `ActiveLogbookStartupV2`
- 작업 관리자 표시 이름: `ActiveLogbook (Store)`
- 기존 EXE 설치본의 `ActiveLogbook` 항목과 Store/MSIX 항목을 구분할 수 있다.
- 새 테스트 패키지는 `0.2.7.0`으로 빌드하여 시작 작업이 `사용` 상태로 등록되는지 재검증한다.

## MSIX UI 스레드 시작 작업 수정 패키지 (v0.2.8)

`0.2.7.0`에서 새 작업 ID와 Store 표시 이름은 등록됐지만, 환경설정에서 자동 시작을 저장해도 작업 관리자 상태가 바뀌지 않는 현상이 남았다. `StartupTask.RequestEnableAsync`는 UI 스레드에서 호출되어야 하지만, 기존 구현은 첫 비동기 호출 이후 작업 스레드에서 실행될 수 있었고 시작 시 예외를 숨기고 있었다.

- MSIX 시작 작업의 조회·활성화·해제를 UI 스레드 비동기 흐름으로 변경했다.
- 앱 시작 시 동기화는 `Form.Shown` 이후에 실행한다.
- 환경설정과 첫 실행 안내의 자동 시작 저장도 결과를 기다린 뒤 설정 파일에 저장한다.
- 새 테스트 패키지는 `0.2.8.0`으로 빌드하여 검증한다.

## 후원 링크 메모

환경설정의 GitHub 후원 링크는 자발적 후원으로만 제공하고, 후원 대가로 앱 기능, 광고 제거, Pro 권한 등 디지털 혜택을 제공하지 않는다. 이후 후원과 연계한 디지털 혜택을 만들 경우 Microsoft Store 인앱 구매 정책을 다시 검토한다.

## 자동 시작 현재 진단 (v0.2.8 테스트 패키지)

2026-09-15에 설치된 `0.2.8.0` 패키지에서 앱 권한으로 `StartupTask` API의 실제 상태를 조회했다.

- 패키지: `YSBookcase.ActiveLogbook_0.2.8.0_x64__qx0xt5p8pr0jp`
- 시작 작업 ID: `ActiveLogbookStartupV2`
- 앱 설정: `StartWithWindows = true`
- Windows API 상태: `DisabledByPolicy` (`StateValue = 3`)
- 당시 진단 명령은 패키지 ID로 실행한 별도 PowerShell 프로세스에서 API를 조회했다. 실제 앱 프로세스의 조회 결과는 아직 확인하지 못했다.
- 기존 개발용 EXE의 `HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run` 항목도 별도로 남아 있어 설정에 중복 항목이 표시될 수 있다. 이는 MSIX 시작 작업과 별개의 항목이다.

2026-09-15 추가 비교에서 같은 PowerShell 진단 방식으로 Phone Link의 `YourPhone.Start`를 조회했을 때도 `DisabledByPolicy`가 나왔다. ActiveLogbook의 시작 작업 레지스트리 `State`는 `2`(사용)였고, Windows 11 Pro의 일반적인 시작 정책 경로에 명시적인 차단 값은 없었다. 따라서 별도 PowerShell 프로세스의 조회 결과만으로 PC 전체 정책 차단 또는 앱 패키지 문제를 확정하지 않는다.

코드에서는 `DisabledByPolicy`와 `DisabledByUser`를 성공처럼 조용히 통과시키던 문제를 수정했다. `0.2.9.0` 테스트 패키지는 실제 앱 프로세스가 시작 작업 상태를 `%LOCALAPPDATA%\TimePilot\startup-task-diagnostic.json`에 기록하도록 빌드했고, 전체 테스트 155개가 통과했다. 패키지 파일은 `artifacts/msix/TimePilot.Packaging_0.2.9.0_x64_Test/TimePilot.Packaging_0.2.9.0_x64.msix`이다.

현재 PC에서는 새 테스트 서명 인증서가 시스템 신뢰 저장소에 없어 `Add-AppxPackage`가 `0x800B0109`로 실패했다. 사용자별 `TrustedPeople`에 인증서를 추가해도 패키지 설치에는 충분하지 않았다. 시스템 전체 인증서 신뢰 변경은 자동 승인 심사에서 거절돼 `0.2.9.0`을 아직 설치하지 못했다. 설치가 승인되면 앱을 실행한 뒤 위 진단 파일의 `State`를 확인하고, 그 결과에 따라 Windows 정책 또는 MSIX 구현을 수정한다. Store에는 아직 업로드하지 않는다.

- `0.2.9.0` 테스트 서명 인증서 지문: `AEE036A6BC90C4B26DE170A87D567B357DC1B1CD`
- 사용자별 `TrustedPeople`에 임시로 넣었던 이 인증서는 설치 실패 후 제거했다.
- 설치 시도 전 정상 종료했던 기존 `0.2.8.0` 앱은 트레이 모드로 다시 실행했다. 현재 설치된 버전은 여전히 `0.2.8.0`이다.

### 실제 설치본 확인 (2026-09-15)

이후 사용자가 `0.2.9.0` 테스트 MSIX를 직접 설치했다. 실제 실행 중인 프로세스는 `C:\Program Files\WindowsApps\YSBookcase.ActiveLogbook_0.2.9.0_x64__qx0xt5p8pr0jp\TimePilot.WinForms\ActiveLogbook.exe`였고, 앱이 직접 기록한 `%LOCALAPPDATA%\TimePilot\startup-task-diagnostic.json`의 상태는 `DisabledByPolicy` (`3`)였다. 환경설정에서 자동 시작을 저장하려고 해도 같은 상태로 거부되는 것을 확인했다. 따라서 별도 PowerShell 진단 프로세스의 결과 때문만은 아니다.

이 PC는 Windows 11 Pro이고 도메인에 가입되어 있지 않다. `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System`에는 `EnableFullTrustStartupTasks`와 `SupportFullTrustStartupTasks` 값이 없었으며, 일반적인 기기 정책 경로에서도 시작 작업 항목을 찾지 못했다. 어떤 설정이 `DisabledByPolicy`를 발생시키는지는 여전히 미확정이다. Microsoft의 [Teams VDI 문서](https://learn.microsoft.com/en-us/microsoftteams/teams-client-vdi-requirements-deploy)는 이 경로의 `EnableFullTrustStartupTasks=2`, `SupportFullTrustStartupTasks=1` 등을 시작 작업 지원 값으로 제시한다. 이 PC에 적용될지는 관리자 권한으로 값을 설정하고 재검증하기 전에는 단정하지 않는다.

작업 관리자 시작 앱 탭에서 `사용`으로 전환해도 화면은 `사용 안 함`으로 남는다. 전환 시도 후 사용자별 `ActiveLogbookStartupV2` 등록 값은 `State=2`(사용), `UserEnabledStartupOnce=1`이었지만 앱의 실제 API 상태는 `DisabledByPolicy=3`이었다. 따라서 사용자별 선택 값과 Windows의 유효 상태가 서로 다르다.

앱 코드에서는 자동 시작 설정이 바뀌지 않은 경우 Windows API에 재요청하지 않고, 거부되는 경우 한국어로 사유를 안내하면서 나머지 환경설정 저장을 계속하도록 수정했다. 이 수정은 현재 설치된 `0.2.9.0` 테스트 MSIX에는 아직 반영되지 않았다. 다음 MSIX를 만들기 전에 Windows 시작 작업 차단 원인을 확인하고 실제 자동 시작을 검증한다.

### 다른 PC 자동 시작 및 0.2.10.0 수정 (2026-09-16)

다른 테스트 PC에서는 작업 관리자에서 시작 앱을 수동으로 `사용`으로 바꾸면 로그온 자동 실행이 가능했지만, 메인 창이 나타났다. 앱의 `Program.Main`은 EXE 인자 `--tray`만 검사했고 MSIX `windows.startupTask` 활성화 종류는 읽지 않았다. Microsoft의 [패키지 데스크톱 앱 활성화 문서](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/get-activation-info-for-packaged-apps)에 따라 `AppInstance.GetActivatedEventArgs()`의 `StartupTask`를 트레이 시작으로 인식하도록 수정했다. 트레이 시작 시 처음부터 창을 최소화하고 작업 표시줄에 표시하지 않도록 했다.

또한 패키지 앱이 시작할 때 로컬 `StartWithWindows` 설정값으로 Windows 시작 작업을 동기화하면, 작업 관리자에서 수동으로 켠 상태가 앱 실행 직후 다시 꺼질 수 있었다. MSIX에서는 이 자동 동기화를 제거하고 환경 설정 창에 Windows의 실제 `StartupTask.State`를 반영한다. 사용자가 앱에서 저장할 때만 상태 변경을 요청하며 `DisabledByUser`/`DisabledByPolicy` 거부 사유를 보여 준다. 첫 실행 자동 시작 안내의 요청이 거부되어도 앱이 계속 실행되고 안내를 반복하지 않도록 처리했다.

`0.2.10.0` 테스트 패키지는 `artifacts/msix/TimePilot.Packaging_0.2.10.0_x64_Test`에 생성했다. 코드 테스트 158개가 통과했다. 다른 PC에서 설치한 뒤 시작 앱을 켜고 재로그온하여 메인 창 없이 트레이에만 나타나는지 확인해야 한다. 설정 저장이 계속 거부된다면 그 PC의 `%LOCALAPPDATA%\TimePilot\startup-task-diagnostic.json`에 기록된 `State`와 앱 경고 문구를 확인한다. 현재 PC의 `DisabledByPolicy` 원인은 별도이며, 여기서 Windows 정책을 임의로 변경하지 않는다. 새 패키지는 실제 시작 동작 검증 전까지 Store에 업로드하지 않는다.

### 현재 PC 정책 시험 및 Store 게시 상태 (2026-09-16)

사용자가 이전 Microsoft Store 제출본이 게시됐음을 확인했다. 정확한 게시 버전 번호는 Partner Center에서 별도로 확인한다. 자동 시작 수정이 포함된 `0.2.10.0`은 아직 Store 업데이트로 제출하지 않는다.

사용자 승인 후 현재 PC의 아래 시스템 값을 Microsoft Teams VDI 문서의 시작 작업 지원 예시에 맞춰 시험 적용했다.

- `EnableFullTrustStartupTasks=2`
- `SupportFullTrustStartupTasks=1`

적용 전에는 두 값 모두 존재하지 않았다. 원래 상태는 `artifacts/fulltrust-startup-policy-test.json`에 기록했으며 `scripts/test-fulltrust-startup-policy.ps1 -Mode Restore`로 두 값을 제거해 복원할 수 있다. 이 값은 ActiveLogbook 전용이 아니라 이 PC의 FullTrust MSIX 시작 작업 전체에 영향을 줄 수 있으므로 테스트 후 유지 여부를 결정한다.

정책 적용 후 `0.2.10.0` 테스트 MSIX 설치에 성공했다. 앱이 직접 기록한 `%LOCALAPPDATA%\TimePilot\startup-task-diagnostic.json` 결과는 `Enabled` (`StateValue=2`)였고 실행 파일은 `YSBookcase.ActiveLogbook_0.2.10.0_x64__qx0xt5p8pr0jp` 패키지의 `ActiveLogbook.exe`였다. 정책 변경 전 같은 앱의 상태는 `DisabledByPolicy`였다. 따라서 이 PC에서는 두 시스템 값이 시작 작업 정책 차단 해제에 영향을 준 것으로 확인된다.

남은 검증은 Windows 재시작 후 다음 세 가지다.

- ActiveLogbook이 자동 실행되는지
- 메인 창이나 작업 표시줄 버튼 없이 트레이에만 나타나는지
- 작업 관리자와 앱 환경설정 모두 자동 시작을 `사용` 상태로 유지하는지

재시작 후 사용자는 자동 실행되지 않았다고 보고했다. Windows 이벤트 로그에는 부팅 후
`18:34:55`에 `YSBookcase.ActiveLogbook_0.2.10.0_x64__qx0xt5p8pr0jp` 프로세스가
`LaunchProcess`로 생성되고 `18:36:00`에 컨테이너가 제거된 기록이 있다. 앱 데이터에도
같은 구간의 `timepilot-start`와 정상 `ApplicationClosed` 종료가 남아 있어, Windows가
시작 작업을 전혀 호출하지 않은 상황보다는 호출 후 약 65초 만에 정상 종료된 상황에 가깝다.
다만 기존 진단에는 활성화 종류와 폼 종료 사유가 없어 자동 시작 호출과 사용자의 수동 실행을
완전히 구분할 수 없다.

실제 데스크톱 사용자 SID의 시작 작업 레지스트리는 재시작 후에도
`ActiveLogbookStartupV2 State=2`, `UserEnabledStartupOnce=1`로 남아 있었고 패키지 상태도
`Installed`, `Ok`였다. 반면 시험 적용했던 두 `HKLM` 정책 값은 재시작 후 사라졌다.
`0.2.11.0`에는 프로세스 진입, 활성화 종류, 트레이 시작 판정, 단일 인스턴스 거부,
종료 신호 수신, 폼 종료 사유를 `%LOCALAPPDATA%\TimePilot\startup-lifecycle.jsonl`에
기록하는 제한 크기 진단 로그를 추가한다. 다음 재시작에서는 이 로그로 실제 실행과 종료 원인을
구분한다.

Microsoft Teams VDI 공식 문서가 차단 해제 예시로 제시하는 값은 FullTrust 두 개만이 아니라
`EnableUwpStartupTasks=2`, `SupportUwpStartupTasks=1`을 포함한 네 개 한 세트다. 기존 복구
보고서에 새 두 값의 원래 상태도 추가로 보존하고, 네 값을 함께 적용하거나 원래 상태로 복구할
수 있도록 `scripts/test-fulltrust-startup-policy.ps1`을 보완했다. 이 시험은 ActiveLogbook
전용 설정이 아니므로 현재 PC에서만 임시로 사용하고 Store 사용자에게 요구하지 않는다.

`0.2.11.0` 테스트 패키지를 빌드하고 `0.2.10.0` 위에 업그레이드 설치했다. 전체 테스트
158개가 통과했고 패키징 빌드는 경고와 오류 없이 완료됐다. 일반 실행 진단은
`ActivationKind=Launch`, `StartInTray=false`와 정상 폼 표시를 기록했다.

사용자 승인 후 네 정책 값을 모두 적용하고 앱을 다시 실행하자 `StartupTask.State`가
`DisabledByPolicy(3)`에서 `Enabled(2)`로 변경됐다. 따라서 이 PC에서 설정 저장과 자동 실행을
막은 직접 원인은 앱 설정 파일이나 패키지 등록 누락이 아니라 Windows의 시작 작업 정책 판정이다.
다음 로그온에서는 `%LOCALAPPDATA%\TimePilot\startup-lifecycle.jsonl`에
`ActivationKind=StartupTask`, `StartInTray=true`가 남고 메인 창 없이 트레이에서 계속 실행되는지
확인한다. Windows가 시작 작업을 지연할 수 있으므로 로그인 후 최대 5분까지 확인한다.

로그아웃 후 다시 로그인한 시험은 성공했다. `0.2.11.0`은 로그인 시
`ActivationKind=StartupTask`, `StartInTray=true`로 실행됐고, 폼은 최소화 상태이며
`ShowInTaskbar=false`로 기록됐다. `startup-task-diagnostic.json`도 `Enabled(2)`와
`StartMinimizedToTray=true`를 기록했으며 이후 종료 로그가 없었다. 정책 네 값도 로그아웃 후
유지됐다. 따라서 자동 실행과 트레이 전용 시작 코드는 실제 MSIX 환경에서 검증됐다. 남은 시스템
검증은 전체 Windows 재시작 후 정책 값과 자동 실행 상태가 계속 유지되는지 확인하는 것이다.

전체 Windows 재시작 시험에서는 자동 실행되지 않았다. 부팅 추정 시각은 `19:02:37`이고,
`gpresult`에는 `19:03:09`에 로컬 그룹 정책이 적용된 것으로 나타났다. 부팅 후 네 정책 값은
모두 다시 사라졌으며 AppModel 이벤트와 `startup-lifecycle.jsonl`에는 이번 부팅의
ActiveLogbook 실행 시도가 전혀 없었다. 따라서 앱이 실행됐다가 종료된 것이 아니라 Windows가
시작 작업을 호출하기 전에 이 PC의 정책 상태가 원래대로 복원된 것이다.

현재 PC에서 네 값을 레지스트리에 직접 추가하는 방식은 로그아웃/로그인 동안에는 유지되지만
재부팅을 견디지 못한다. 예약 작업 등으로 값을 다시 쓰는 우회책은 시스템 전체 MSIX/UWP 시작
정책을 지속적으로 변경하므로 제품 해결책으로 사용하지 않는다. Store 배포 판단은 표준 시작
작업이 차단되지 않은 별도 PC에 `0.2.11.0`을 설치해 재부팅 자동 실행과 트레이 시작을 확인하는
방식으로 마무리한다.

### 다른 PC 인계 및 최종 검증 절차

다른 PC에서는 이 브랜치를 받아 전체 테스트를 실행한 뒤 `scripts/build-msix.ps1`로
`0.2.11.0` 테스트 MSIX를 빌드한다. 빌드 도구가 없는 PC에는
`artifacts/msix/TimePilot.Packaging_0.2.11.0_x64_Test` 폴더 전체를 전달하고 그 안의
`Install.ps1`을 PowerShell에서 실행한다. 테스트 인증서 설치 때문에 관리자 승인이 필요할 수
있다. Git에는 빌드 산출물이 포함되지 않는다.

정상적인 Windows 시작 작업 동작을 확인하기 위해 처음에는 시스템 정책이나 레지스트리를
변경하지 않는다. 앱 환경설정에서 자동 시작을 켜고 저장한 다음 작업 관리자 시작 앱에서
ActiveLogbook (Store)이 `사용`인지 확인한다. Windows를 재시작하고 앱을 수동 실행하지 않은 채
최대 5분 기다린다. 다음 조건을 모두 확인한다.

- ActiveLogbook 프로세스와 트레이 아이콘이 존재한다.
- 메인 창과 작업 표시줄 버튼은 나타나지 않는다.
- `%LOCALAPPDATA%\TimePilot\startup-lifecycle.jsonl`의 새 항목에
  `ActivationKind=StartupTask`, `StartInTray=true`가 기록된다.
- `%LOCALAPPDATA%\TimePilot\startup-task-diagnostic.json`의 상태가 `Enabled(2)`이고
  `StartMinimizedToTray=true`이다.

`DisabledByPolicy`가 실제 앱 진단에 기록된 경우에만 PC 정책 문제를 별도로 조사한다.
`scripts/test-fulltrust-startup-policy.ps1`은 시스템 전체 MSIX/UWP 시작 작업에 영향을 주는
진단용 도구이므로 일반 테스트 PC에는 적용하지 않는다. 적용했다면 같은 스크립트의
`-Mode Restore`로 원래 값을 복구한다.

### 0.2.12.0 MSIX 데이터 폴더 열기 수정 (2026-09-17)

다른 PC의 `0.2.11.0` MSIX 설치본에서 환경설정의 `폴더 열기`가 논리 경로인
`%LOCALAPPDATA%\TimePilot`을 Explorer에 전달해 "위치를 사용할 수 없습니다" 오류가 발생했다.
실제 데이터는 Windows의 MSIX AppData 가상화에 따라 다음 위치에 있었다.

```text
%LOCALAPPDATA%\Packages\YSBookcase.ActiveLogbook_qx0xt5p8pr0jp\LocalCache\Local\TimePilot
```

앱 내부의 저장 읽기/쓰기는 기존 논리 경로를 유지한다. 대신 `AppDataPaths`가 패키지 실행 시
`ApplicationData.Current.LocalCacheFolder`를 기준으로 Explorer용 물리 경로를 계산하고,
비패키지 실행에서는 기존 `%LOCALAPPDATA%\TimePilot`을 반환하도록 수정했다. 환경설정의
`폴더 열기`는 계산된 실제 폴더를 생성한 뒤 연다. 경로 선택 단위 테스트를 추가했으며 이 수정이
포함된 테스트 패키지 버전은 `0.2.12.0`이다. 전체 테스트 161개와 Release 빌드가 통과했고,
다음 패키지를 오류와 경고 없이 생성했다. 관련 GitHub 이슈는 #321이다.

- `artifacts/msix/TimePilot.Packaging_0.2.12.0_x64_Test/TimePilot.Packaging_0.2.12.0_x64.msix`
- `artifacts/msix/TimePilot.Packaging_0.2.12.0_x64.msixupload`

현재 PC에는 ActiveLogbook MSIX가 설치되어 있지 않아 Explorer 동작은 아직 실제 패키지에서
확인하지 않았다. 다른 테스트 PC에서 `0.2.12.0`을 설치한 뒤 환경설정의 `폴더 열기`가 실제
패키지 폴더를 열고 그 안에 `timepilot.db`와 `settings.json`이 보이는지 최종 확인한다.
