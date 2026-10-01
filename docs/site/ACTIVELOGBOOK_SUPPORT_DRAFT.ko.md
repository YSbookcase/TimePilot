# ActiveLogbook 지원 페이지 초안

게시 예정 URL:

```text
https://ys-bookcase.com/active-logbook/support
```

이 문서는 WordPress에 게시할 ActiveLogbook 지원 페이지 초안이다.

## ActiveLogbook 지원

ActiveLogbook 사용 중 문제가 발생했거나 개선 의견이 있다면 아래 경로로 문의할 수 있습니다.

## 문의 이메일

일반 문의, 개인정보처리방침 관련 문의, Microsoft Store 관련 문의는 다음 이메일로 보내 주세요.

```text
support@ys-bookcase.com
```

문의할 때 가능하면 다음 정보를 함께 적어 주세요.

- 사용 중인 ActiveLogbook 버전
- Windows 버전
- Microsoft Store, EXE 설치판, portable 중 어떤 배포판인지
- 문제가 발생한 화면 또는 기능
- 재현 방법
- 오류 메시지 또는 스크린샷

민감한 정보가 포함된 화면, 문서명, URL, 개인 파일 경로는 가리고 보내는 것을 권장합니다.

## 버그 제보와 기능 제안

개발 관련 버그 제보와 기능 제안은 GitHub Issues를 사용할 수 있습니다.

```text
https://github.com/YSbookcase/TimePilot/issues
```

GitHub Issues는 공개 공간입니다. 개인 정보, 민감한 사용 기록, 화면 캡처, 로그 파일을 올릴 때는 공개해도 되는 정보인지 먼저 확인해 주세요.

## 다운로드와 업데이트

일반 사용자는 Microsoft Store 버전을 권장합니다.

```text
https://apps.microsoft.com/detail/9NWXBR051GLM
```

GitHub Releases의 EXE 설치 파일과 portable 압축 파일은 Microsoft Store를 사용할 수 없는 환경,
오프라인 설치 또는 테스트를 위한 보조·레거시 배포입니다.

```text
https://github.com/YSbookcase/TimePilot/releases
```

Store와 EXE 또는 portable 버전을 동시에 사용하면 데이터 위치와 자동 시작 등록이 나뉠 수
있습니다. 배포판을 전환하기 전에 전체 백업을 만들고 새 설치에서 기록과 설정을 확인하세요.
확인이 끝난 뒤 이전 설치를 제거하고, 두 버전의 자동 시작을 동시에 활성화하지 마세요.

ActiveLogbook은 현재 초기 공개 테스트 단계이므로, 사용 중인 버전의 알려진 제한사항과 릴리스
노트를 함께 확인하는 것을 권장합니다.

## 데이터 저장과 삭제

일반 실행 파일의 주요 로컬 데이터는 기본적으로 다음 위치에 저장됩니다.

```text
%LocalAppData%\TimePilot
```

Microsoft Store/MSIX 설치본은 다음 패키지별 LocalState 경로를 사용합니다.

```text
%LocalAppData%\Packages\YSBookcase.ActiveLogbook_qx0xt5p8pr0jp\LocalState\TimePilot
```

이전 EXE 또는 초기 MSIX 버전의 데이터가 다른 위치에 남아 있을 수 있습니다. 앱의
`환경설정 > 설치 및 데이터 정보`에서 현재 실행 채널과 실제 데이터 폴더를 확인하고,
`환경설정 > 데이터 관리 > 폴더 열기`로 현재 사용 중인 폴더를 열 수 있습니다.

앱 제거 후에도 로컬 데이터와 설정이 남아 있을 수 있습니다. 완전히 삭제하려면 앱의 데이터 삭제 기능을 사용하거나 위 폴더를 확인해 주세요.

자세한 개인정보 처리 내용은 ActiveLogbook 개인정보처리방침을 확인해 주세요.

```text
https://ys-bookcase.com/active-logbook/privacy-policy
```

## 알려진 제한사항

- 현재 Windows 전용 앱입니다.
- 초기 공개 테스트 단계이므로 기능과 UI가 변경될 수 있습니다.
- 코드 서명이 적용되지 않은 배포 파일은 Windows SmartScreen 경고가 표시될 수 있습니다.
- 선택형 상세 추적 기능은 아직 기본 기능으로 제공되지 않습니다.
- 브라우저 방문 기록, URL, 문서명, 창 제목 수집은 기본 동작에 포함되지 않습니다.

