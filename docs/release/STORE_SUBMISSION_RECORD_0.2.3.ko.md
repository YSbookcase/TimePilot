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
