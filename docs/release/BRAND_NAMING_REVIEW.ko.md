# 브랜드 이름 검토

이 문서는 Store 출시 준비 중 발견한 제품명 리스크와 이름 전환 결정을 기록한다.
법률 자문이 아니라 제품 기획 및 출시 준비용 기록이다.

## 현재 리스크

기존 프로젝트 이름인 `TimePilot`은 공개 Store 출시용 이름으로는 리스크가 있다.

조사 결과, 이미 `TimePilot` 이름을 사용하는 회사와 제품군이 시간 기록 및 근태 관리 영역에 존재한다.

- TimePilot Corporation은 직원용 타임클럭, 모바일 시간 기록, PC 타임클럭, 클라우드/온프레미스 근태 관리 소프트웨어를 `TimePilot` 이름으로 운영한다.
- `TIMEPILOT` 상표 기록도 computerized time and attendance 하드웨어 및 소프트웨어 영역에 존재한다.

현재 앱의 범위가 직원 근태나 급여 처리와 동일하지는 않다. 하지만 시간 추적, PC 소프트웨어, 생산성, 사용 기록이라는 접점이 가까우므로 Microsoft Store 제출 전 공개 제품명은 변경하는 것이 안전하다.

## 제외 후보: DeskTrace

`DeskTrace`는 한때 공개 제품명 후보로 선택했지만 사용하지 않는다.

추가 검토 중 `DeskTrack`이라는 시간 추적, 직원 모니터링, 생산성 분석 제품과 모바일 앱이 확인되었다. `DeskTrace`와 `DeskTrack`은 철자, 발음, 제품 영역이 가깝기 때문에 같은 출시 맥락에서는 리스크가 크다.

## 브랜드 결정

공개 제품명은 `ActiveLogbook`으로 확정한다.

Store, 홈페이지, 개인정보처리방침, 지원 페이지, 앱 표시 문구처럼 사용자에게 보이는 영역에는 `ActiveLogbook`을 사용한다. 한국어 문맥에서는 읽기 보조 표기로 `액티브 로그북`을 함께 쓸 수 있다.

다음 작업 전에는 공개 영역의 이름 전환을 끝낸다.

- Microsoft Store 앱 이름 예약
- Store 등록 문구 공개
- 공식 홈페이지 슬러그 확정
- 개인정보처리방침 및 지원 페이지 확정
- 유료 또는 Pro 에디션 공개

내부 저장소 이름, 네임스페이스, 저장 경로, 이벤트 키, 프로젝트 파일명까지 즉시 모두 바꿀 필요는 없다. 1차 전환은 사용자에게 보이는 브랜드를 바꾸는 방식으로 진행한다.

## 확정 이름: ActiveLogbook

`ActiveLogbook`을 공개 제품명으로 사용한다.

장점:

- 직원 감시나 근태 관리보다 개인 활동 기록부라는 느낌이 강하다.
- 사용자가 컴퓨터에서 무엇을 했고 시간이 어디로 흘렀고 집중이 어떻게 바뀌었는지 이해하도록 돕는다는 제품 방향과 잘 맞는다.
- 시간/근태 소프트웨어 영역의 `TimePilot` 직접 충돌을 피할 수 있다.
- 초기 웹 검색에서는 정확히 `ActiveLogbook` 이름을 사용하는 뚜렷한 PC 사용량 모니터링 제품이 잘 보이지 않았다.

주의사항:

- `Active`와 `Logbook`은 일반적인 설명 단어이므로 Store 표기에는 설명형 부제를 붙이는 것이 좋다.
- 검색 결과에는 운행 기록, 항공 로그북 같은 다른 분야의 일반 표현으로 "active logbook"이 등장한다.
- 이 검토는 정식 상표 검색을 대체하지 않는다.

## 이름 계획

- 공개 앱 이름: `ActiveLogbook`
- 한국어 읽기 보조 표기: `액티브 로그북`
- Store 제목: `ActiveLogbook - PC Usage Insights`
- 한국어 Store 제목: `ActiveLogbook - PC 사용 기록 분석`
- 제품 설명 첫 문장: `ActiveLogbook helps you understand what you did on your Windows PC and where your time went.`
- 한국어 제품 설명 첫 문장: `ActiveLogbook(액티브 로그북)은 Windows PC에서 무엇을 했고 시간이 어디에 쓰였는지 이해할 수 있게 도와주는 로컬 사용 기록 앱입니다.`
- 공식 페이지: `https://ys-bookcase.com/active-logbook/`
- 개인정보처리방침: `https://ys-bookcase.com/active-logbook/privacy-policy/`
- 지원 페이지: `https://ys-bookcase.com/active-logbook/support/`
- 지원 이메일: `support@ys-bookcase.com`

## 게시자명과 사업자명 메모

현재 개인 Microsoft Store 개발자 등록에서는 기존 개인 브랜드와 연결되지만 등록된 회사처럼 보이지 않는 게시자 표시 이름을 사용한다.

추천 개인 게시자 표시 이름:

- `YS Bookcase`

나중에 사업자등록 또는 회사 개발자 계정을 만들 경우에는 개인 계정의 게시자명과 회사 계정의 게시자명을 구분한다. Microsoft가 게시자 표시 이름을 계정별 식별값처럼 다룰 수 있고, 개인 공개 테스트 출시와 향후 상업 계정을 분리해서 관리하기가 더 좋기 때문이다.

향후 사업자 또는 회사 브랜드 우선 후보:

- English: `YS Bookcase Works`
- Korean: `와이에스 북케이스 웍스`
- 자연스러운 설명: `YS Bookcase 제작소`

`Works`가 어울리는 이유:

- Windows 앱, 게임, 웹 콘텐츠, 리뷰, 영상 제작, 광고 관련 작업을 모두 담을 수 있다.
- `Software`보다 넓어서 게임과 미디어 프로젝트가 커져도 어색하지 않다.
- `Studio`보다 사진, 영상, 공연 작업 느낌이 약하고 소프트웨어 도구에도 중립적이다.
- `Labs`보다 실험적 느낌이 약해서 상업 게시자명으로 쓰기 쉽다.
- `Digital`보다 조금 더 개성이 있다.

검토한 다른 이름 느낌:

- `YS Bookcase Software`: 앱과 도구에는 강하지만 게임, 영상, 콘텐츠까지 담기에는 좁다.
- `YS Bookcase Studio`: 게임과 미디어에는 좋지만 사진/영상/공연 작업 느낌이 있고 소프트웨어 도구 회사 느낌은 약하다.
- `YS Bookcase Labs`: 실험성 앱과 프로토타입에는 좋지만 상업 게시자명으로는 가볍게 느껴질 수 있다.
- `YS Bookcase Digital`: 웹, 앱, 콘텐츠, 광고까지 넓게 담지만 개성이 약하다.

현재 방향은 개인 개발자 계정에는 `YS Bookcase`를 쓰고, 향후 상업 출시가 사업자등록을 정당화할 만큼 진지해지면 `YS Bookcase Works`를 사업자 또는 게시자 브랜드 후보로 남겨두는 것이다.

## Store 출시 영향

Store 제출 전 다음 항목을 업데이트한다.

- 앱 창 제목과 About 창
- README 제품명과 링크
- 개인정보처리방침 및 지원 페이지 제목
- WordPress 페이지 슬러그
- 설치 프로그램 표시명
- Store 등록 스크린샷과 설명
- 릴리스 노트

전환 기간에는 GitHub 저장소 이름을 `TimePilot`로 유지해도 된다. 다만 Store와 홈페이지처럼 사용자에게 직접 보이는 영역은 `ActiveLogbook`으로 맞추는 것이 좋다.

## 참고

- 기존 TimePilot 제품 사이트: https://www.timepilot.com/
- TimePilot 상표 정보: https://trademarks.justia.com/789/68/timepilot-78968293.html
- DeskTrack 제품 사이트: https://desktrack.timentask.com/
- Microsoft Store 정책: https://learn.microsoft.com/en-us/windows/apps/publish/store-policies
