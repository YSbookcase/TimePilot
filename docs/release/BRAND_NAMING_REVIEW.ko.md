# 브랜드 이름 검토

이 문서는 Store 출시 준비 중 발견한 제품명 리스크와 이름 전환 방향을 기록한다.
법률 자문이 아니라 제품 기획 및 출시 준비용 기록이다.

## 현재 리스크

현재 프로젝트 이름인 `TimePilot`은 공개 Store 출시용 이름으로는 리스크가 있다.

조사 결과, 이미 `TimePilot` 이름을 사용하는 회사와 제품군이 시간 기록 및 근태 관리 영역에 존재한다.

- TimePilot Corporation은 직원용 타임클럭, 모바일 시간 기록, PC 타임클럭, 클라우드/온프레미스 근태 관리 소프트웨어를 `TimePilot` 이름으로 운영한다.
- `TIMEPILOT` 상표 기록도 computerized time and attendance 하드웨어 및 소프트웨어 영역에 존재한다.

현재 앱의 범위는 직원 근태/급여 처리가 아니라 로컬 PC 사용량 모니터링이므로 완전히 같지는 않다. 하지만 시간 추적, PC 소프트웨어, 생산성, 사용 기록이라는 접점이 가까워서 Microsoft Store 제출 전 이름을 재검토하는 것이 안전하다.

## 권장 방향

다음 작업 전에 새 공개 제품명을 확정한다.

- Microsoft Store 앱 이름 예약
- Store 등록 문구 공개
- 공개 홈페이지 슬러그 확정
- 개인정보처리방침 및 지원 페이지 확정
- 유료 또는 Pro 에디션 공개

내부 저장소 이름, 네임스페이스, 프로젝트 파일까지 즉시 모두 바꿀 필요는 없다. 1차 전환은 사용자에게 보이는 브랜드부터 바꾸는 방식이 안전하다.

## 후보: DeskTrace

현재 우선 후보는 `DeskTrace`다.

장점:

- 데스크톱 또는 PC 활동 기록이라는 의미가 비교적 직접적이다.
- 일반 생산성 이름보다 컴퓨터 사용량 모니터링 기능과 잘 연결된다.
- 초기 웹 검색에서는 정확히 `DeskTrace` 이름을 사용하는 뚜렷한 활성 소프트웨어 제품이나 상표가 잘 보이지 않았다.
- 시간/근태 소프트웨어 영역의 `TimePilot` 직접 충돌을 피할 수 있다.

주의할 점:

- `Trace`라는 일반 단어를 쓰는 제품은 많으므로 Store 표기는 `DeskTrace - PC Usage Monitor`처럼 설명형 부제를 붙이는 것이 좋다.
- `desktrace.com`은 도메인/리다이렉트 색인에 흔적이 있으므로, 공개 페이지는 기존 보유 도메인 아래의 `https://ys-bookcase.com/desktrace/`를 쓰는 방향이 현실적이다.
- 이 검토는 정식 상표 검색을 대체하지 않는다.

## 임시 이름 계획

- 공개 앱 이름: `DeskTrace`
- Store 제목 후보: `DeskTrace - PC Usage Monitor`
- 제품 설명 첫 문장: `DeskTrace helps you understand how your Windows PC time is spent.`
- 공식 페이지 후보: `https://ys-bookcase.com/desktrace/`
- 개인정보처리방침 후보: `https://ys-bookcase.com/desktrace/privacy-policy/`
- 지원 페이지 후보: `https://ys-bookcase.com/desktrace/support/`
- 지원 이메일: `support@ys-bookcase.com`

## 게시자명과 사업자명 메모

현재 개인 Microsoft Store 개발자 등록에는 기존 개인 브랜드와 연결되지만, 등록된 회사처럼 보이지 않는 게시자 표시 이름을 사용한다.

추천 개인 게시자 표시 이름:

- `YS Bookcase`

나중에 사업자등록 또는 회사 개발자 계정을 만들 경우에는 개인 계정의 게시자명과 회사 계정의 게시자명을 구분한다. Microsoft가 게시자 표시 이름을 계정별 식별값처럼 다룰 수 있고, 개인 공개 테스트 출시와 이후 상업 계정을 분리해서 관리하기가 더 쉽기 때문이다.

향후 사업자/회사 브랜드 우선 후보:

- 영문: `YS Bookcase Works`
- 한국어: `와이에스북케이스 웍스`
- 자연어 설명: `YS Bookcase 제작소`

`Works`가 어울리는 이유:

- Windows 앱, 게임, 웹 콘텐츠, 리뷰, 영상 제작, 광고 관련 작업을 모두 담을 수 있다.
- `Software`보다 넓어서 게임과 미디어 프로젝트가 커져도 어색하지 않다.
- `Studio`보다 사진, 영상, 공연 작업실 느낌이 덜하고 소프트웨어 도구에도 더 중립적이다.
- `Labs`보다 실험실 느낌이 약해서 상업 게시자명으로 쓰기 쉽다.
- `Digital`보다 조금 더 개성이 있다.

검토한 다른 이름 느낌:

- `YS Bookcase Software`: 앱과 도구에는 강하지만 게임, 영상, 콘텐츠까지 담기에는 좁다.
- `YS Bookcase Studio`: 게임과 미디어에는 좋지만 사진/영상/공연 작업실 느낌이 있고 소프트웨어 도구 회사 느낌은 약하다.
- `YS Bookcase Labs`: 실험적 앱과 프로토타입에는 좋지만 상업 게시자명으로는 가볍게 느껴질 수 있다.
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

전환 기간에는 GitHub 저장소 이름을 `TimePilot`로 유지해도 된다. 다만 Store와 홈페이지처럼 사용자에게 직접 보이는 영역은 최종 제품명으로 맞추는 것이 좋다.

## 참고

- 기존 TimePilot 제품 사이트: https://www.timepilot.com/
- TimePilot 상표 정보: https://trademarks.justia.com/789/68/timepilot-78968293.html
- Microsoft Store 정책: https://learn.microsoft.com/en-us/windows/apps/publish/store-policies
