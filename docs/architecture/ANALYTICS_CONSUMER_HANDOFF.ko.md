# Pro 외부 솔루션 빌드 인계

상세 조건·프로젝트 참조 XML·명령은 `ANALYTICS_CONSUMER_HANDOFF.md`를 기준으로 한다.
아직 병합하지 않았으며 private `TimePilot.Pro.sln` 자체를 검증한 것은 아니다.

## 참조와 환경

- 같은 고정 Community 전체 SHA의 `TimePilot.Analytics.Contracts`와
  `TimePilot.Analytics.Sqlite` csproj를 참조한다. 두 프로젝트 모두 net8.0, 버전 2.0.0.
- Community WinForms/Tests/Packaging 또는 메인 TimePilot.sln은 참조하지 않는다.
  전체 앱 실행·Store 설치·실제 DB·설정·Windows SDK 패키징 도구가 없어도 된다.
- .NET 8/C# 12 지원 SDK(8 이상)와 실행용 .NET 8 런타임이 필요하다.
  검증 호스트는 Windows x64, SDK 10.0.402, 런타임 8.0.32다. SDK 8과 다른 아키텍처는
  여기서 별도로 검증하지 않았다. 실제 어댑터 실행은 Windows 전용이다.
- Microsoft.Data.Sqlite 10.0.7 및 전이 SQLitePCLRaw 2.1.11 네이티브 자산을 복원한다.
  별도 SQLite 서버/명령 프로그램은 필요 없다. 테스트 의존성은 영문 문서에 명시했다.
- 재현 스크립트에는 Git과 Windows PowerShell 5.1 또는 PowerShell 7이 필요하다.

## 고정 커밋과 솔루션

Pro의 작업 폴더와 Community의 활성 개발 폴더를 공유하지 말고 별도 고정 checkout을 둔다.
병합 후 #349에 기록하는 전체 merge SHA가 기준이다. 지금의 PR head SHA는 검토용이다.
새 SHA에는 이번에 추가한 samples와 scripts도 포함되어야 한다.

Pro 프로젝트는 `$(CommunityRoot)`를 기준으로 두 csproj를 ProjectReference하고 다음처럼
자기 솔루션을 빌드한다. 개인 절대 경로는 커밋하지 않는다.

```powershell
dotnet build TimePilot.Pro.sln -c Release "-p:CommunityRoot=C:\absolute\CommunityPinned"
```

이 명령은 Pro 측 구성 예시이며 이번 작업이 private Pro 저장소를 변경/빌드하지 않는다.

## 한 명령 재현

고정 checkout에서 #349의 전체 SHA와 새 절대 출력 경로를 지정한다.
출력 부모는 미리 존재해야 하며 출력 폴더는 없어야 한다.

```powershell
$communitySha = '<#349에 기록된 전체 40자리 SHA>'
.\scripts\verify-analytics-handoff.ps1 `
  -CommunityCommit $communitySha `
  -OutputDirectory 'C:\absolute\scratch\analytics-handoff-01'
```

해당 커밋의 공개 프로젝트·독립 테스트·샘플만 git archive로 분리한다.
다른 위치의 `ExternalConsumer/Analytics.HandoffConsumer.sln`을 빌드하며 다음을 수행한다.

1. 새 sandbox에 합성 fixture v1 생성. 사용자 DB 경로를 받지 않는다.
2. 원본 DB·설정 해시/파일 목록 저장 → 명시적 스냅샷 준비.
3. 공개 API 조회 → 활성 2시간/유휴 1시간/기록기 3시간/달력 24시간 확인.
4. 원본 비변경 및 메타데이터 재개봉 시 생성 기준 시각 유지 확인.
5. 독립 소비자 테스트 35개 실행. 실패 시 nonzero 종료.

정확한 SHA·SDK는 input.json, 테스트는 TestResults/*.trx로 남기고 출력물을 자동 삭제하지
않는다. 기존 출력 폴더는 덮어쓰지 않는다. 현재 Community 브랜치나 미커밋 파일은
입력에 섞이지 않으며 전체 앱이나 패키징 솔루션은 빌드하지 않는다.

## 버전과 승인

- 계약/어댑터: 2.0.0 (NuGet 배포 아님)
- 스키마 프로필: community-daily-v1 (user_version=0)
- 스냅샷 형식: 1 (`analytics.db`, `snapshot.json` 전체 전달)
- 독립 fixture: v1 (`DailyFixture.cs`), smoke 시나리오: v1 (샘플 Program.cs)
- ObservedAt 기본 사용: reader.SourceCapturedAt. 생성 기준 시각 이후는 거부한다.
- RecorderCalendarCoverage는 기존 UI의 Windows 기록 가능 커버리지와 다른 지표다.

실시간 DB/WAL과 #350·#351은 제외한다. 재검토/별도 승인 전에는 병합하지 않는다.
병합되면 #349에 전체 merge SHA·버전·사용 예제·최종 테스트 결과를 기록한 뒤
Pro에서 그 기준으로 자기 솔루션과 독립 테스트를 구성한다.
