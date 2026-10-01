# ActiveLogbook Microsoft Store 배포 안내 초안

> 게시 상태 확인: Microsoft Store에서 `v0.2.15` 공개를 확인했습니다(2026-10-01).
>
> 실제 WordPress 게시본으로 변환할 때는 `docs/blog/BLOG_POST_STYLE_GUIDE.ko.md`와
> `docs/blog/WORDPRESS_POST_TEMPLATE.ko.html`을 참조한다.

## 작성 메모 (게시 전 확인하고 제거)

- 연결할 과거 글: `[기존 TimePilot 또는 ActiveLogbook 배포 글 제목과 URL]`
- 다룰 과거 버전 또는 기간: `[GitHub EXE 배포 시작부터 Store 공개 시점까지]`
- 이어받을 사실: 기존 사용자는 TimePilot이라는 이름의 GitHub EXE 또는 portable 배포를 사용했음
- 달라진 내용: 공개 제품명은 ActiveLogbook이며 일반 사용자에게 Microsoft Store를 우선 권장함
- 이번 글에서 생략할 내용: 과거 핫픽스의 세부 구현과 현재 전환에 필요하지 않은 변경 내역
- 확인 근거: Microsoft Store에서 `v0.2.15` 게시 확인(2026-10-01)
- 사용할 이미지: Microsoft Store 제품 페이지 또는 설치 화면

## ActiveLogbook을 Microsoft Store에서 설치할 수 있습니다

Windows PC 사용 기록을 로컬에서 확인하는 ActiveLogbook을 Microsoft Store에서 설치할 수
있습니다. 현재 Store 버전은 초기 공개 테스트 단계이며, 최신 게시 버전은 `v0.2.15`입니다.

Microsoft Store 버전은 Windows의 일반적인 설치와 업데이트 흐름을 사용합니다. 처음 설치하는
사용자에게는 Store 버전을 권장합니다.

Microsoft Store:

https://apps.microsoft.com/detail/9NWXBR051GLM

[이미지 삽입 1
- 목적: Store에서 실제 제품을 찾고 설치할 수 있음을 보여 주기
- 화면: Microsoft Store의 ActiveLogbook 제품 페이지
- 촬영 상태: 최신 게시 버전과 설치 또는 열기 버튼이 보이는 상태
- 가릴 정보: Microsoft 계정 이름, 이메일, 개인화된 추천 정보
- 대체 텍스트: Microsoft Store의 ActiveLogbook 제품 페이지]

## 이전 이름은 TimePilot이었습니다

기존 공개 테스트 버전은 `TimePilot`이라는 이름으로 GitHub에서 배포했습니다. `ActiveLogbook`은
별개의 앱이 아니라 같은 프로젝트의 현재 공개 제품명입니다.

기존 사용자 데이터와의 호환성을 위해 프로젝트 파일, 일부 내부 진단 이름,
`%LocalAppData%\TimePilot` 데이터 폴더에는 이전 이름이 남아 있을 수 있습니다. 이 이름이 보이더라도
오류이거나 다른 앱의 데이터라는 뜻은 아닙니다. 데이터 폴더를 임의로 이름 변경하거나 삭제하지
말고 앱의 백업 및 전환 안내를 이용해 주세요.

## 기존 EXE 사용자가 전환하기 전에

기존 TimePilot/ActiveLogbook GitHub EXE 설치판과 Microsoft Store 버전은 별개의 설치입니다. 두 버전은 데이터 폴더와
Windows 자동 시작 등록이 다를 수 있으므로 바로 기존 앱을 제거하지 마세요.

1. 기존 EXE 버전에서 전체 백업 ZIP을 만듭니다.
2. Microsoft Store에서 ActiveLogbook을 설치합니다.
3. Store 버전에서 기존 기록과 설정이 보이는지 확인합니다.
4. 필요한 경우 백업 ZIP을 이용해 데이터를 복원하고 결과를 다시 확인합니다.
5. Store 자동 시작이 정상 동작하는지 확인합니다.
6. 모든 확인이 끝난 뒤 기존 EXE 설치판을 제거합니다.

[이미지 삽입 2
- 목적: 현재 설치 유형과 데이터 위치를 앱에서 확인하는 방법 보여 주기
- 화면: ActiveLogbook 환경설정의 `설치 및 데이터 정보`
- 촬영 상태: Store 설치 유형과 LocalState 데이터 폴더가 보이는 상태
- 가릴 정보: Windows 사용자 이름, 전체 로컬 경로, 개인 사용 기록
- 대체 텍스트: ActiveLogbook 설치 및 데이터 정보 화면]

[이미지 삽입 3
- 목적: 전환 전에 전체 백업을 만드는 위치 보여 주기
- 화면: ActiveLogbook 환경설정의 전체 백업 기능
- 촬영 상태: 백업 버튼과 안내 문구가 보이고 백업 작업은 실행하지 않은 상태
- 가릴 정보: 백업 대상 경로, 사용자 이름, 파일 이름에 포함된 개인 정보
- 대체 텍스트: ActiveLogbook 전체 백업 설정 화면]

전환 중에는 EXE와 Store 버전의 자동 시작을 동시에 켜지 않는 것이 좋습니다. 앱이 실행 중인
상태에서 데이터베이스 파일을 직접 복사하거나 덮어쓰지 마세요.

## GitHub 다운로드는 어떻게 되나요?

GitHub Releases의 EXE 설치 파일과 portable 압축 파일은 Microsoft Store를 사용할 수 없는 환경,
오프라인 설치 또는 테스트를 위한 보조·레거시 경로로 유지합니다.

https://github.com/YSbookcase/TimePilot/releases

새로 설치하는 일반 사용자라면 Microsoft Store 버전을 먼저 선택해 주세요. 이미 EXE 버전을
사용하고 있다면 데이터를 백업하고 Store 버전에서 확인을 끝낸 뒤 전환하는 것이 안전합니다.

[이미지 삽입 4 - 선택 사항
- 목적: Store와 GitHub 보조 배포의 역할 차이를 보여 주기
- 화면: GitHub Releases의 최신 공개 릴리스
- 촬영 상태: Store 링크와 보조 배포 안내가 반영된 릴리스 설명
- 가릴 정보: 로그인 계정, 비공개 알림, 초안 릴리스 정보
- 대체 텍스트: ActiveLogbook GitHub 보조 배포 파일]

## 데이터와 개인정보

ActiveLogbook은 앱 사용 기록을 기본적으로 사용자의 PC에 로컬로 저장합니다. 브라우저 방문
기록, URL, 문서명, 창 제목은 기본 동작에서 수집하지 않습니다. 자세한 내용은 개인정보처리방침을
확인해 주세요.

https://ys-bookcase.com/active-logbook/privacy-policy/

문제가 발생하거나 전환 과정에 도움이 필요하면 지원 페이지를 이용할 수 있습니다.

https://ys-bookcase.com/active-logbook/support/
