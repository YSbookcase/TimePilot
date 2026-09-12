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
7. 시작 메뉴와 Windows 설정에서 MSIX 아이콘이 빈 기본 아이콘으로 계속 표시되는지 확인한다. 재로그인 또는 재시작 후에도 같다면 다음 패키지 빌드 전에 패키지 아이콘 자산과 캐시 동작을 점검한다.

## 다음 개발 작업의 우선순위

1. 위 MSIX 실제 설치 검증을 완료하고 결과를 이 문서에 추가한다.
2. Store 인증 결과와 실패 사유가 있으면 기록하고, 필요 시 `0.2.3.1` 패치 릴리즈를 만든다.
3. Store 아이콘과 설치 목록 중복 항목을 확인한다. 기존 EXE와 MSIX의 공존은 전환 과정에서는 허용되지만, 최종 사용 흐름은 Store 설치본 기준으로 정리한다.
4. 제품 기능은 `docs/PROJECT_PLAN.ko.md`의 Phase 4 항목(주간/월간 통계, TOP 앱 분석, 기록 커버리지, 타임라인 개선)을 우선 검토한다.

## 소스 관리 메모

- 현재 기준 브랜치: `develop`
- Store association 병합: PR #318, 커밋 `bbf2c53`
- `0.2.3` 릴리즈 준비 병합: PR #317, 커밋 `c7b422e`
- MSIX 로고 자산 변경: 커밋 `ea2d08a`, 브랜치 `codex/update-msix-logo-assets`

로고 자산 변경 커밋은 이 기록 작성 시점에 `develop`에 병합되어 있지 않다. 다음 MSIX 패키지를 다시 만들기 전에는 해당 변경을 검토하고 병합 여부를 결정해야 한다.

## 후원 링크 메모

환경설정의 GitHub 후원 링크는 자발적 후원으로만 제공하고, 후원 대가로 앱 기능, 광고 제거, Pro 권한 등 디지털 혜택을 제공하지 않는다. 이후 후원과 연계한 디지털 혜택을 만들 경우 Microsoft Store 인앱 구매 정책을 다시 검토한다.
