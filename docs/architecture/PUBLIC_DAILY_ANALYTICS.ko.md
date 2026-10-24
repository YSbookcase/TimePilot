# 공개 일별 분석 계약 인계

#349의 첫 구현이다. 상세 계약과 예제·오류 정책은
`PUBLIC_DAILY_ANALYTICS.md`를 기준으로 한다.

## 이번에 제공하는 것

- `TimePilot.Analytics.Contracts`: .NET 8 공개 DTO/인터페이스, 계약 버전 2.0.0.
- `TimePilot.Analytics.Sqlite`: SQLite 내부 형식과 WinForms를 공개 계약에 노출하지 않는 Windows용 조회 어댑터.
- `TimePilot.Analytics.ConsumerTests`: WinForms 참조·InternalsVisibleTo 없이 공개 API만 사용하는 소비자 테스트.
- 날짜·시간대·관측 시각을 명시한 일별 앱/분류 활성 시간, 유휴 시간, 대표 앱, 기록 실행 시간.
- 실제 Community 저장소가 만든 격리 DB를 읽을 수 있는 호환성 테스트.

설치 앱이나 SharedStoreData를 실행할 필요가 없다. NuGet 배포는 아직 없으며
Pro는 병합 후 전체 SHA를 고정하고 두 공개 프로젝트를 참조한다.

미병합 초안 `57b2093`의 1.0.0에서 공개 API 이름과 시각 전달 계약이 바뀌어
2.0.0으로 올렸다. ActiveLogbook 앱/Store 버전 변경은 아니다.
DB 구조 프로필은 `community-daily-v1`, 스냅샷 메타데이터 형식은 1이다.

## 중요한 제한

**실시간 기록 중인 원본 DB에 직접 연결하는 기능이 아니다.** 쓰기가 끝난 별도
rollback-journal 스냅샷을 명시해야 한다. Windows 파일 공유 제한으로 조회 중 쓰기를
막으며, 쓰기 핸들이 이미 열려 있으면 실패한다. 조회가 끝나면 반드시 Dispose한다.

WAL은 읽기 연결도 보조 파일에 영향을 줄 수 있어 첫 버전에서는 거부한다.
WAL 헤더 또는 WAL/SHM/journal 파일이 발견되면 SQLite 연결 전에 실패하고
자동 checkpoint·journal 전환·복구를 하지 않는다. 실제 DB를 raw copy하거나
immutable이라고 속여 우회하지 않는다. 스냅샷 준비는 아래 명시적 별도 단계다.

`Mode=ReadOnly`와 연결 단위 query_only, 캐시 비공유·풀링 해제로 조회한다.
경로 자동 탐색·fallback, DB 생성/마이그레이션, 설정 저장, 자동실행 등록,
전역 데이터 위치 변경, 안전 백업 자동 생성은 없다. 실제 개인 DB는 테스트에 쓰지 않는다.

## 원본 비변경 스냅샷 생성 → 조회

1. 기록 앱을 정상 종료한 뒤 기존 DB의 절대 경로를 명시한다. 정상 종료 자체의 쓰기는
   기록기의 동작이며, 스냅샷 준비 API는 프로세스 종료/복구/checkpoint를 하지 않는다.
2. 원본 데이터 폴더 밖의 새 출력 폴더를 지정한다. 출력 폴더는 없어야 하고 부모는
   이미 존재해야 한다. 기존 출력 덮어쓰기, 원본 내부 출력, junction/symlink 경로는 거부한다.
3. `OfflineAnalyticsSnapshot.Create(sourcePath, destinationDirectory, token)`을 호출한다.
   원본 쓰기를 막는 lease와 스키마 검증 후 UTC `SourceCapturedAt`을 기록한다.
   원본이 고정된 상태에서만 main DB를 복사하므로 live WAL raw copy와 다르다.
4. 별도 staging에 `analytics.db`와 `snapshot.json`을 만든다. 메타데이터는 형식/계약/
   스키마 버전, 생성 기준 시각, DB SHA-256을 포함하고 원본 경로·설정은 포함하지 않는다.
   공개 Open API로 검증한 뒤 완성된 폴더를 한 번에 게시한다. 실패·취소 시 부분 결과는
   게시하지 않는다. 정리 권한이 없으면 미게시 staging이 남을 수 있다.
5. 반환된 `DirectoryPath`의 **두 파일 전체**를 전달하고
   `OfflineAnalyticsSnapshot.Open(directoryPath)`으로 연다. 해시와 메타데이터가 검증되며
   새 프로세스에서도 `reader.SourceCapturedAt`이 동일하게 제공된다.

Create의 쓰기는 지정한 출력 부모에만 일어나며 원본 DB·설정·파일 목록은 그대로다.
복사 후 원본 lease를 해제하므로 기록기를 다시 실행할 수 있다. 자동 보관 정리나
업로드는 없고 복사본에도 개인정보가 있으므로 별도로 관리해야 한다.
메타데이터 해시는 DB 쌍의 불일치 검출용이지 서명·위변조 방지 보장은 아니다.

별도 백업에서 시각 근거를 신뢰할 수 있다면
`SqliteDailyAnalyticsReader.Open(path, sourceCapturedAt)`도 가능하다.
시각은 필수이며 현재 시각·파일 수정 시각·압축 해제 시각으로 대체하면 안 된다.
출처가 불명확하면 시각을 추정하지 말고 지원되는 새 스냅샷을 생성한다.

## 생성 시각과 ObservedAt

`SourceCapturedAt`은 복사 완료 시각이 아니라 원본 고정 후 확보한 스냅샷 기준 시각이다.
`ObservedAt`은 계산을 어디까지 자를지 정하는 조회 상한이며, 기본 사용은
`reader.SourceCapturedAt`을 그대로 전달하는 것이다. 그보다 늦은 값은 오류로 거부한다.
더 이른 값으로 기간을 자를 수 있지만 과거 시점의 분류/별칭까지 복원하는 조회는 아니다.
기존 메타데이터는 조회 때문에 바뀌지 않으므로 재개봉 후 진행 중 유휴 시간이 늘지 않는다.

```csharp
var prepared = OfflineAnalyticsSnapshot.Create(sourcePath, destinationDirectory, cancellationToken);
using var reader = OfflineAnalyticsSnapshot.Open(prepared.DirectoryPath);
var snapshot = reader.Read(new DailyQuery(
    new DateOnly(2024, 1, 1), new DateOnly(2025, 1, 1),
    TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time"),
    reader.SourceCapturedAt), cancellationToken);
```

## 지표 해석

- 기간은 시작일 포함/종료일 제외이며 최대 366일. DST와 시간대별 자정을 사용한다.
- 활성 시간은 기존 Community처럼 foreground 구간을 합산한다. 겹친 활성 기록은
  합산되며 유휴 시간을 다시 빼지 않는다. 앱 ID가 다르면 표시 이름이 같아도 구분한다.
- 분류는 조회 시점 DB의 현재 기본 분류다. 과거 분류 이력을 복원하거나 대표 앱으로
  대표 분류를 추정하지 않는다. 미분류는 null ID로 별도 제공한다.
- 진행 중 활성 시간은 마지막 관측까지, 기록 실행 시간은 heartbeat까지 인정한다.
  진행 중 유휴는 요청 관측 시각까지다. 모든 값은 요청 날짜/관측 시각으로 잘린다.
- **`RecorderCalendarCoverage`는 달력상 관측 시간 중 기록기가 실행된 비율**이다. 기존 UI의 Windows
  기록 가능 시간 대비 커버리지와는 다르다. 절전·잠금·부팅 시각 추정은 제공하지 않는다.
  빈 구간을 무활동·PC 종료로 단정하면 안 된다.
- 필드도 `ElapsedCalendarMilliseconds`, `RecorderRunningMilliseconds`,
  `CalendarGapMilliseconds`, `RecorderToCalendarRatio`로 구분했다. 기존의 모호한
  `Coverage`/`Ratio` 명칭은 제거했다. 분모가 0이면 비율도 0이다.

## 검증과 다음 단계

```powershell
dotnet test TimePilot.Analytics.ConsumerTests/TimePilot.Analytics.ConsumerTests.csproj -c Release
dotnet test TimePilot.Tests/TimePilot.Tests.csproj -c Release -p:OutputPath=bin/CodexSnapshotReviewVerify/
```

fixture v1은 임시 경로에서 합성 앱·분류·시간 구간을 생성한다. 반복 조회 전후
DB/설정 해시·파일 목록·논리 스키마 비교, 쓰기 거부, WAL 거부, 자정/연도/윤년/DST,
빈 기록/부분 기록/취소/조회 한도를 확인한다. 1만 구간의 연간 조회 회귀 예산은 15초다.
생성→공개 API 조회, 생성 시각 유지, 이후 ObservedAt 거부, 원본 비변경과
같은 DB에서 기존 UI 커버리지와 다른 계산 결과도 검증한다.

Pro 검토 보완 결과: Windows에서 독립 소비자 35개, Community 303개 테스트 통과.
실제 개인 DB·설정·백업은 사용하지 않았다.

병합 후 #349에 PR, 전체 병합 SHA, 실제 테스트 결과를 남기고 Pro에서 같은 fixture로
독립 검증한다. 원본 실시간 접근이 필수라면 offline-only 제한의 후속 설계가 필요하다.
#350 구간 계약과 #351 UI 진입 계약은 이 작업에 포함하지 않는다.
