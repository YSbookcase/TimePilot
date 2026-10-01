# ActiveLogbook Microsoft Store 배포 안내 초안

> 게시 전 확인: Store에 표시되는 최신 버전과 게시 날짜를 확인하고 아래 대괄호 항목을 교체한다.

## ActiveLogbook을 Microsoft Store에서 설치할 수 있습니다

Windows PC 사용 기록을 로컬에서 확인하는 ActiveLogbook을 Microsoft Store에서 설치할 수
있습니다. 현재 Store 버전은 초기 공개 테스트 단계이며, 최신 게시 버전은 `[버전]`입니다.

Microsoft Store 버전은 Windows의 일반적인 설치와 업데이트 흐름을 사용합니다. 처음 설치하는
사용자에게는 Store 버전을 권장합니다.

Microsoft Store:

https://apps.microsoft.com/detail/9NWXBR051GLM

## 기존 EXE 사용자가 전환하기 전에

기존 GitHub EXE 설치판과 Microsoft Store 버전은 별개의 설치입니다. 두 버전은 데이터 폴더와
Windows 자동 시작 등록이 다를 수 있으므로 바로 기존 앱을 제거하지 마세요.

1. 기존 EXE 버전에서 전체 백업 ZIP을 만듭니다.
2. Microsoft Store에서 ActiveLogbook을 설치합니다.
3. Store 버전에서 기존 기록과 설정이 보이는지 확인합니다.
4. 필요한 경우 백업 ZIP을 이용해 데이터를 복원하고 결과를 다시 확인합니다.
5. Store 자동 시작이 정상 동작하는지 확인합니다.
6. 모든 확인이 끝난 뒤 기존 EXE 설치판을 제거합니다.

전환 중에는 EXE와 Store 버전의 자동 시작을 동시에 켜지 않는 것이 좋습니다. 앱이 실행 중인
상태에서 데이터베이스 파일을 직접 복사하거나 덮어쓰지 마세요.

## GitHub 다운로드는 어떻게 되나요?

GitHub Releases의 EXE 설치 파일과 portable 압축 파일은 Microsoft Store를 사용할 수 없는 환경,
오프라인 설치 또는 테스트를 위한 보조·레거시 경로로 유지합니다.

https://github.com/YSbookcase/TimePilot/releases

새로 설치하는 일반 사용자라면 Microsoft Store 버전을 먼저 선택해 주세요. 이미 EXE 버전을
사용하고 있다면 데이터를 백업하고 Store 버전에서 확인을 끝낸 뒤 전환하는 것이 안전합니다.

## 데이터와 개인정보

ActiveLogbook은 앱 사용 기록을 기본적으로 사용자의 PC에 로컬로 저장합니다. 브라우저 방문
기록, URL, 문서명, 창 제목은 기본 동작에서 수집하지 않습니다. 자세한 내용은 개인정보처리방침을
확인해 주세요.

https://ys-bookcase.com/active-logbook/privacy-policy/

문제가 발생하거나 전환 과정에 도움이 필요하면 지원 페이지를 이용할 수 있습니다.

https://ys-bookcase.com/active-logbook/support/
