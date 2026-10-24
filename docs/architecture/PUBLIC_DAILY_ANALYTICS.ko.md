# 공개 일별 분석 계약 인계

#349의 첫 구현이다. 상세 계약과 예제·오류 정책은
`PUBLIC_DAILY_ANALYTICS.md`를 기준으로 한다.

## 이번에 제공하는 것

- `TimePilot.Analytics.Contracts`: .NET 8 공개 DTO/인터페이스, 계약 버전 1.0.0.
- `TimePilot.Analytics.Sqlite`: SQLite 내부 형식과 WinForms를 공개 계약에 노출하지 않는 Windows용 조회 어댑터.
- `TimePilot.Analytics.ConsumerTests`: WinForms 참조·InternalsVisibleTo 없이 공개 API만 사용하는 소비자 테스트.
- 날짜·시간대·관측 시각을 명시한 일별 앱/분류 활성 시간, 유휴 시간, 대표 앱, 기록 실행 시간.
- 실제 Community 저장소가 만든 격리 DB를 읽을 수 있는 호환성 테스트.

설치 앱이나 SharedStoreData를 실행할 필요가 없다. NuGet 배포는 아직 없으며
Pro는 병합 후 전체 SHA를 고정하고 두 공개 프로젝트를 참조한다.

## 중요한 제한

**실시간 기록 중인 원본 DB에 직접 연결하는 기능이 아니다.** 쓰기가 끝난 별도
rollback-journal 스냅샷을 명시해야 한다. Windows 파일 공유 제한으로 조회 중 쓰기를
막으며, 쓰기 핸들이 이미 열려 있으면 실패한다. 조회가 끝나면 반드시 Dispose한다.

WAL은 읽기 연결도 보조 파일에 영향을 줄 수 있어 첫 버전에서는 거부한다.
WAL 헤더 또는 WAL/SHM/journal 파일이 발견되면 SQLite 연결 전에 실패하고
자동 checkpoint·journal 전환·복구를 하지 않는다. 실제 DB를 raw copy하거나
immutable이라고 속여 우회하지 않는다. 스냅샷 준비는 기존 명시적 백업 등 별도 단계다.

`Mode=ReadOnly`와 연결 단위 query_only, 캐시 비공유·풀링 해제로 조회한다.
경로 자동 탐색·fallback, DB 생성/마이그레이션, 설정 저장, 자동실행 등록,
전역 데이터 위치 변경, 안전 백업 자동 생성은 없다. 실제 개인 DB는 테스트에 쓰지 않는다.

## 지표 해석

- 기간은 시작일 포함/종료일 제외이며 최대 366일. DST와 시간대별 자정을 사용한다.
- 활성 시간은 기존 Community처럼 foreground 구간을 합산한다. 겹친 활성 기록은
  합산되며 유휴 시간을 다시 빼지 않는다. 앱 ID가 다르면 표시 이름이 같아도 구분한다.
- 분류는 조회 시점 DB의 현재 기본 분류다. 과거 분류 이력을 복원하거나 대표 앱으로
  대표 분류를 추정하지 않는다. 미분류는 null ID로 별도 제공한다.
- 진행 중 활성 시간은 마지막 관측까지, 기록 실행 시간은 heartbeat까지 인정한다.
  진행 중 유휴는 요청 관측 시각까지다. 모든 값은 요청 날짜/관측 시각으로 잘린다.
- **Coverage는 달력상 관측 시간 중 기록기가 실행된 비율**이다. 기존 UI의 Windows
  기록 가능 시간 대비 커버리지와는 다르다. 절전·잠금·부팅 시각 추정은 제공하지 않는다.
  빈 구간을 무활동·PC 종료로 단정하면 안 된다.

## 검증과 다음 단계

```powershell
dotnet test TimePilot.Analytics.ConsumerTests/TimePilot.Analytics.ConsumerTests.csproj -c Release
dotnet test TimePilot.Tests/TimePilot.Tests.csproj -c Release -p:OutputPath=bin/CodexPublicAnalyticsVerify/
```

fixture v1은 임시 경로에서 합성 앱·분류·시간 구간을 생성한다. 반복 조회 전후
DB/설정 해시·파일 목록·논리 스키마 비교, 쓰기 거부, WAL 거부, 자정/연도/윤년/DST,
빈 기록/부분 기록/취소/조회 한도를 확인한다. 1만 구간의 연간 조회 회귀 예산은 15초다.

병합 후 #349에 PR, 전체 병합 SHA, 실제 테스트 결과를 남기고 Pro에서 같은 fixture로
독립 검증한다. 원본 실시간 접근이 필수라면 offline-only 제한의 후속 설계가 필요하다.
#350 구간 계약과 #351 UI 진입 계약은 이 작업에 포함하지 않는다.
