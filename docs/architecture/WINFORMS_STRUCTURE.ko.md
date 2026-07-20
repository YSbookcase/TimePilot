# TimePilot WinForms 구조

이 문서는 TimePilot Community의 현재 WinForms 구성과 새 코드를 배치하는 기준을 설명한다.

## 1. 현재 상태

초기 MVP에서는 `Form1.cs`가 화면 갱신, 추적, 날짜 선택, 데이터 관리, 설정, 메뉴 등 대부분의 책임을 담당했다.

1차 책임 분리 작업 이후 `Form1.cs`는 상태 필드, coordinator 생성, 상위 수준의 조립 순서만 유지한다. 기능 코드는 책임별 partial class와 별도 coordinator, control, service로 이동했다.

이 작업은 **하나의 `Form1` 클래스를 책임별 파일로 정리한 단계**다. partial 파일은 독립 모듈이나 플러그인이 아니며, 컴파일 결과에서는 여전히 하나의 클래스다.

## 2. Form1 partial 책임

| 파일 | 책임 |
| --- | --- |
| `Form1.cs` | 상태 필드, 의존성 생성, 상위 조립 순서 |
| `Form1.Initialization.cs` | UI 초기화, 저장소·추적기 초기화, 이벤트 연결 |
| `Form1.Lifecycle.cs` | 창 종료, 트레이, 창 위치, 샘플 타이머 생명주기 |
| `Form1.SystemEvents.cs` | 시작 안내, 안전 모드, Windows 세션·전원 이벤트 |
| `Form1.ProcessTracking.cs` | 백그라운드 프로세스 스캔과 쓰기 |
| `Form1.Refresh.cs` | 비동기 조회, 캐시, 화면 snapshot 적용 |
| `Form1.Summary.cs` | 요약 기간, 사용 막대, 요약 표 상호작용 |
| `Form1.Detail.cs` | 실행 앱 요약, 실행 구간, 상세 필터 |
| `Form1.Timeline.cs` | 타임라인 조회·강조·확대·시스템 이벤트 |
| `Form1.DateNavigation.cs` | 날짜 선택기, 기록 달력, 자정 전환 |
| `Form1.AppActions.cs` | 앱 분류, 웹 검색, 공통 앱 우클릭 동작 |
| `Form1.Preferences.cs` | 환경 설정 적용과 기록 삭제 |
| `Form1.DataExport.cs` | CSV와 원본 ZIP 내보내기 |
| `Form1.DataBackup.cs` | 전체 백업 생성 |
| `Form1.DataRestore.cs` | 백업 분석, 복원 진행, 저장소 재초기화 |
| `Form1.TableLayout.cs` | 열 배치와 정렬 상태 저장 |
| `Form1.Status.cs` | 상태 표시줄, 대기 커서, 성능 진단 표시 |
| `Form1.Localization.cs` | 실행 중 언어 변경과 UI 문구 적용 |
| `Form1.CoverageSummary.cs` | 기록 상태와 입력 활동 요약 표시 |
| `Form1.HeaderToolTip.cs` | 표 머리글 설명 팝업 |
| `Form1.Menu.cs` | 메인 메뉴 구성 |
| `Form1.SupportActions.cs` | 정보, 후원, 진단, 앱 분류 관리 창 |
| `Form1.Common.cs` | 작고 재사용되는 WinForms 공통 헬퍼 |
| `Form1.DesignPreview.cs` | Visual Studio 디자이너 샘플 데이터 |

## 3. 새 코드 배치 규칙

1. 기존 책임과 일치하면 해당 partial 또는 기존 service에 추가한다.
2. DB 조회, 집계, 정책 판단은 가능한 한 Form partial 밖의 service나 builder에 둔다.
3. partial 간 호출이 늘어나면 공통 service 또는 coordinator 계약을 먼저 검토한다.
4. `Form1.cs`에는 기능 구현을 직접 추가하지 않는다. 상태와 조립 순서만 유지한다.
5. 새 partial은 화면 이벤트를 묶기 위한 수단으로만 사용한다. 도메인 규칙을 숨기는 장소로 사용하지 않는다.
6. UI 스레드를 막을 수 있는 조회와 파일 작업은 기존 비동기 패턴을 따른다.

## 4. 아직 완료되지 않은 구조 작업

- `TimePilot.Core`, `TimePilot.Infrastructure`, `TimePilot.WinForms`의 실제 프로젝트 분리
- WinForms와 저장소 사이의 읽기 전용 분석 계약
- 메뉴, 분석 화면, 설정 섹션을 등록하는 확장 계약
- Community 모듈이 없어도 동작하는 fallback 검증
- private Pro 저장소와 공개 Community 저장소의 빌드·배포 경계

따라서 현재 구조는 확장 계약을 설계하기 쉬워진 상태이지, 동적 플러그인 구조가 완성된 상태는 아니다.

## 5. 첫 Pro 참조 기능

첫 Pro 참조 기능은 **연간 달력 히트맵 기반 사용 통계**로 검토한다.

초기 범위:

- 날짜별 활성 사용 시간의 색상 강도
- 날짜 hover 시 활성 시간, 유휴 기록 시간, 대표 앱, 기록 상태 표시
- 날짜 선택 시 Community의 해당 날짜 요약으로 이동
- 연도 이동
- 기록 없음과 TimePilot 미실행 구분

처음에는 메인 탭에 직접 결합하지 않고 메뉴에서 별도 분석 창을 여는 방식을 우선 검토한다. 이 기능을 기준으로 읽기 전용 일별 통계 공급자, 메뉴 명령 등록, 언어 정보 전달, Pro 모듈 부재 시 fallback 계약을 설계한다.

