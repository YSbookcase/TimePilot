# [Bug] 기록 삭제 직후 화면 갱신 지연과 재실행 후 기록 표시 확인

GitHub Issue: https://github.com/YSbookcase/TimePilot/issues/320

Labels: `bug`, `priority: Medium`

### 작업 개요

다른 PC의 ActiveLogbook `0.2.10.0` 테스트 MSIX에서 기록 삭제 직후 화면이 즉시 갱신되지 않고, 잠시 뒤 기록이 사라지는 현상을 확인했다. 앱 재실행 후 데이터가 보였으나 삭제 전 기록이 재등장한 것인지, 재실행 후 새로 수집된 기록인지는 아직 확인되지 않았다.

### 작업 배경

환경설정 저장 흐름은 `RefreshViews`로 비동기 화면 조회를 시작한 뒤 `ClearUsageData`를 호출한다. 삭제 전 조회 결과가 삭제 직후 화면에 적용될 수 있고, 진행 중인 조회가 있으면 다른 갱신 요청을 건너뛰는 코드가 있다. 이는 가능한 원인이지만 아직 해당 PC에서 재현 검증되지 않았다. 현재 증거만으로 DB 저장 실패나 삭제 실패를 단정하지 않는다.

### 작업 내용

- [x] 다른 PC에서 실제 `timepilot.db`와 `settings.json`의 경로 및 패키지별 AppData 리디렉션을 확인했다. 폴더 열기 수정은 GitHub Issue #321에서 처리한다.
- [ ] 삭제 전, 삭제 직후, 앱 재실행 후 같은 날짜 범위의 화면과 DB 기록 건수를 비교한다. 재실행 후 보이는 행의 시간으로 기존 기록과 새 기록을 구분한다.
- [ ] 삭제 실행 전 비동기 화면 조회, 삭제 중 조회, 삭제 후 화면 적용 순서를 재현한다.
- [ ] 삭제 전 조회 결과가 삭제 후 화면이나 캐시에 다시 적용되지 않도록 하고, 삭제 직후 모든 관련 화면이 일관된 빈 상태를 보여 주도록 개선한다.
- [ ] 삭제와 화면 갱신이 겹치는 경우, 새 활동이 다시 수집되는 경우를 포함해 회귀 테스트를 추가한다.

### 완료 기준

- 기록 삭제 직후 이전 사용 기록이 화면에 다시 나타나지 않는다.
- 앱 재실행 후 삭제 전 기록이 복원되지 않고, 새로 수집된 기록만 표시된다.
- 저장 위치 및 폴더 열기 문제는 GitHub Issue #321에서 별도로 처리한다.

### 관련 파일 또는 시스템

- `TimePilot.WinForms/Form1.Preferences.cs`: 환경설정 저장, 기록 삭제, 화면 초기화
- `TimePilot.WinForms/Form1.Refresh.cs`: 비동기 조회 및 화면 갱신
- `TimePilot.WinForms/KYS24/TimePilotStorage.cs`: SQLite 기록 삭제
- `TimePilot.WinForms/KYS24/AppDataPaths.cs`, `TimePilot.WinForms/PreferencesForm.cs`: 저장 경로와 폴더 열기
- Windows MSIX AppData 저장/리디렉션

### 참고 사항

이 문서는 GitHub Issue #320의 로컬 기록이다. 다른 PC에서 재실행 후 보인 행이 삭제 전 기록인지 새 기록인지 확인되면 결과를 Issue에 추가한다. 저장 위치 문제는 GitHub Issue #321을 참고한다.
