# ActiveLogbook Microsoft Store 업데이트 기록 (v0.2.16)

## 목적

`0.2.16.0`은 비정상 종료 후 Store 데이터베이스에 SQLite 롤백 저널이 남았을 때
저장 위치 검사가 실패하고 과거 EXE 데이터 폴더로 되돌아가던 문제를 수정하는 안정화
업데이트다.

## 포함 변경

- 저장 위치 검사 전에 SQLite가 남은 롤백 저널을 정상 복구할 수 있도록 한다.
- Store 데이터 이전이 완료된 경우 `LocalState\TimePilot`을 기준 저장 위치로 유지한다.
- 일시적인 데이터베이스 검사 실패가 발생해도 과거 EXE 데이터 폴더로 후퇴하여 두
  데이터베이스에 기록이 나뉘지 않도록 한다.
- 롤백 저널 복구와 이전 완료 후 저장 위치 선택에 대한 회귀 테스트를 추가한다.

## 버전

- 앱 버전: `0.2.16`
- 파일/어셈블리 버전: `0.2.16.0`
- MSIX Identity 버전: `0.2.16.0`
- 아키텍처: `x64`
- 최소 Windows 버전: `10.0.19041.0`

## 자동 검증

- Release 빌드: 경고 `0`, 오류 `0`
- Release 테스트: `217`개 통과
- MSIX 패키징: 경고 `0`, 오류 `0`
- 패키지 매니페스트: Identity `0.2.16.0`, `x64`, 게시자와 패키지 이름 확인
- Store 업로드 패키지 SHA-256: `75658021F7C617FE8309CDB07886212AB9D3B6A34208E4E61E414C1A08FDD760`
- 개발 서명 테스트 MSIX SHA-256: `B284D3F99E20D2F9CD2C2DEA60C5A395D85144B5251812829048C6EAAB657832`
- 개발 서명 인증서 지문: `E8DB0437C5DD244A4866876C4CC01B8B79851F5D`

## 산출물

- 설치 테스트 폴더: `artifacts/msix/TimePilot.Packaging_0.2.16.0_x64_Test`
- 설치 테스트 MSIX: `artifacts/msix/TimePilot.Packaging_0.2.16.0_x64_Test/TimePilot.Packaging_0.2.16.0_x64.msix`
- 설치 테스트 인증서: `artifacts/msix/TimePilot.Packaging_0.2.16.0_x64_Test/TimePilot.Packaging_0.2.16.0_x64.cer`
- Store 업로드 후보: `artifacts/msix/TimePilot.Packaging_0.2.16.0_x64.msixupload`

개발 서명 인증서는 로컬 설치 검증에만 사용한다. 일반 사용자는 인증서나 테스트 설치
스크립트를 사용하지 않으며, Partner Center 인증 후 Microsoft Store가 다시 서명한
패키지를 받는다.

## 로컬 업데이트 설치 검증

- [x] 기존 Store/MSIX `0.2.15.0` 위에 개발 서명 `0.2.16.0`을 업데이트 설치했다.
- [x] 설치 후 패키지 상태가 `Ok`이고 서명이 유효한 것을 확인했다.
- [x] 설치 전후 데이터베이스 경로가 `LocalState\TimePilot\timepilot.db`로 유지됐다.
- [x] 기존 데이터베이스와 백업 파일 12개가 유지됐다.
- [x] 설정 내용이 유지됐다. 종료 시 창 위치 좌표만 현재 위치로 갱신됐다.
- [x] 실행 진단에서 `StorageDecision=UseExistingTarget`과 Store `LocalState` 선택을 확인했다.
- [x] MSIX 시작 작업 상태가 `Enabled`로 유지됐다.

## 제출 전 수동 확인

- [x] Windows 재시작 후 `StartupTask`로 트레이 자동 시작되는 것을 확인했다. 로그인 후
  Windows의 시작 앱 지연 처리로 약 3분 뒤 실행됐다.
- [x] 재시작 후 Store `LocalState` 데이터베이스만 계속 갱신되고, 과거 EXE 데이터베이스는
  갱신되지 않는 것을 확인했다.
- [ ] 일반 실행, 기록 조회, 환경설정 저장, 수동 백업이 정상 동작하는지 확인한다.
- [ ] 패키지와 시작 메뉴 아이콘이 정상 표시되는지 확인한다.

## Store 목록 업데이트 설명

### 한국어

비정상 종료 후 Microsoft Store 데이터 복구 안정성을 개선했습니다. ActiveLogbook은 저장소
검사 전에 남은 SQLite 저널을 복구하며, 이전이 완료된 LocalState 데이터를 기준 위치로
유지해 과거 EXE 데이터 폴더로 되돌아가지 않습니다.

### English

Improves Microsoft Store data recovery after an interrupted shutdown. ActiveLogbook now recovers
pending SQLite journals before validation and keeps migrated LocalState data as the authoritative
storage location instead of falling back to an older EXE data folder.
