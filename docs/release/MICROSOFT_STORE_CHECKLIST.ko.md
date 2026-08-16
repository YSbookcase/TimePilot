# Microsoft Store 출시 체크리스트

이 문서는 TimePilot을 Microsoft Store에 제출하기 전에 필요한 비개발 준비 항목을 추적한다.

TimePilot은 제품, 개인정보처리방침, 패키징, 지원 흐름이 안정될 때까지 Store 배포를 초기 공개 테스트 성격으로 다룬다.

## 출시 위치

- [ ] 다음 Store 목표 버전을 `v0.2.x`로 할지 `v0.3.0`으로 할지 결정한다.
- [ ] 유료 Pro 운영, 세금, 정산, 고객지원 흐름이 준비되기 전까지 첫 Store 등록은 무료로 유지한다.
- [ ] 회사 계정을 별도로 준비하기 전까지는 개인 개발자 계정을 기준으로 검토한다.
- [ ] `v1.0` 이전에는 초기 공개 테스트 단계임을 명확히 표시한다.
- [ ] Microsoft Store 앱 이름을 예약하기 전에 공개 제품명을 확정한다.
- [ ] Store 브랜드를 확정하기 전에 `docs/release/BRAND_NAMING_REVIEW.ko.md`를 검토한다.

## Partner Center

- [x] Microsoft Partner Center 개발자 계정을 만들거나 확인한다.
- [ ] 앱 이름을 예약한다.
- [ ] 제출 전에 게시자 표시 이름을 확인한다.
- [ ] 카테고리와 연령 등급을 선택한다.
- [x] 지원 연락처를 준비한다.
- [x] 공개 개인정보처리방침 URL을 준비한다.

## 준비된 공개 링크

- 현재 임시 공식 페이지: https://ys-bookcase.com/timepilot/
- 현재 임시 지원 페이지: https://ys-bookcase.com/timepilot/support/
- 현재 임시 개인정보처리방침: https://ys-bookcase.com/timepilot/privacy-policy/
- `DeskTrace` 후보를 채택할 경우 변경 대상: `https://ys-bookcase.com/desktrace/`
- 지원 이메일: support@ys-bookcase.com

## 패키지 선택

Microsoft Store는 Win32 앱에 대해 MSIX와 MSI/EXE 제출 경로를 모두 지원한다.

### MSIX 경로

- [ ] TimePilot MSIX 패키지를 만든다.
- [ ] 설치, 실행, 제거, 업데이트 동작을 확인한다.
- [ ] 제거 시 로컬 데이터가 의도한 정책대로 보존되거나 삭제되는지 확인한다.
- [ ] 패키지 배포 상태에서 Windows 시작 프로그램 등록이 정상 동작하는지 확인한다.
- [ ] 패키지 배포 상태에서 단일 실행 동작이 정상 동작하는지 확인한다.

### MSI/EXE 경로

- [ ] 기존 Inno Setup 설치 파일을 Store installer로 제출할 수 있는지 확인한다.
- [ ] 버전이 고정된 HTTPS 설치 파일 URL을 준비한다.
- [ ] 설치 파일과 관련 PE 파일을 Microsoft Trusted Root Program에 연결되는 CA 코드 서명 인증서로 서명한다.
- [ ] 이 경로는 Store 관리 업데이트가 아니므로 업데이트 책임을 문서화한다.
- [ ] 설치, 실행, 제거, 로컬 데이터 보존 동작을 확인한다.

## Store 등록 자료

- [ ] 짧은 앱 설명
- [ ] 자세한 앱 설명
- [ ] 로컬 사용 시간 추적 중심의 기능 목록
- [ ] 요약, 타임라인, 상세, 설정, 트레이 동작 스크린샷
- [ ] 앱 아이콘과 Store 이미지
- [ ] 릴리스 노트
- [ ] 알려진 제한사항
- [ ] GitHub 저장소 링크
- [x] 지원 페이지 또는 GitHub Issues 링크

## 개인정보와 신뢰

- [x] TimePilot 전용 개인정보처리방침 페이지를 공개한다.
- [ ] Partner Center에 개인정보처리방침 URL을 입력한다.
- [x] README 또는 앱 지원 문서에서 개인정보처리방침을 연결한다.
- [ ] 사용 기록 데이터가 기본적으로 로컬에 저장된다는 점을 설명한다.
- [ ] TimePilot은 기본적으로 사용 기록을 개발자 서버로 전송하지 않는다는 점을 설명한다.
- [ ] TimePilot은 기본적으로 창 제목, URL, 웹 페이지 제목, 문서명, 명령줄, 키 입력, 화면 캡처를 수집하지 않는다는 점을 설명한다.
- [ ] 내보내기와 백업 파일 관리 책임을 설명한다.
- [ ] 사용자가 로컬 데이터를 삭제하는 방법을 설명한다.
- [ ] 추적 동작이 바뀌면 개인정보처리방침도 함께 업데이트한다.

## 기능 검증

- [ ] 빌드가 성공한다.
- [ ] 테스트가 통과한다.
- [ ] 새 설치 후 TimePilot이 실행된다.
- [ ] foreground 앱 사용 시간이 기록된다.
- [ ] idle 시간이 분리된다.
- [ ] background process runtime tracking이 의도대로 동작한다.
- [ ] 트레이 상주 모드가 동작한다.
- [ ] Windows 시작 프로그램 설정이 동작한다.
- [ ] 단일 실행 동작이 유지된다.
- [ ] CSV 내보내기가 동작한다.
- [ ] 원시 데이터 내보내기가 동작한다.
- [ ] 백업 생성이 동작한다.
- [ ] 복원 흐름이 현재 복원 정책대로 동작한다.
- [ ] 로컬 데이터 삭제 흐름이 동작한다.

## 참고 자료

- Microsoft Store 정책: https://learn.microsoft.com/en-us/windows/apps/publish/store-policies
- 첫 Windows 앱 게시: https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/publish-first-app
- Microsoft Store 시작하기: https://learn.microsoft.com/en-us/windows/apps/publish/get-started
- Windows 앱 배포 경로 선택: https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/choose-distribution-path
