# YS의 책장 개발 글 작성 양식

이 문서는 ActiveLogbook을 포함한 개발·배포 글의 Markdown 초안을 실제 WordPress 게시용 본문으로
변환할 때 사용하는 기준이다. 기존 게시 글에서는 레이아웃과 문체만 참조하고, 과거 사실은 새 글에
자동으로 복사하지 않는다.

## 작성 순서

1. 사실관계 중심의 Markdown 초안을 먼저 작성한다.
2. 버전, 게시 날짜, 배포 상태, 공개 링크를 확인한다.
3. `WORDPRESS_POST_TEMPLATE.ko.html`에 초안 내용을 배치한다.
4. 필요한 이미지를 WordPress에 올린 뒤 실제 미디어 URL과 ID를 넣는다.
5. 미리보기에서 목차, 제목 번호, 링크, 모바일 줄바꿈을 확인한다.

Markdown 초안은 내용의 기준이고 WordPress HTML은 게시 형식의 기준이다. 실제 게시본에서 내용을
수정했다면 중요한 사실 변경은 Markdown 초안에도 반영한다.

## 기본 구조

게시 본문은 다음 순서를 기본으로 한다.

1. 검은색 넓은 구분선
2. LuckyWP 목차 블록
3. 검은색 넓은 구분선
4. 이미 게시된 글의 정정이나 후속 업데이트가 있을 때만 인용문 블록
5. `1. 글의 핵심 제목` 형식의 `h2`
6. 제목 바로 아래 검은색 넓은 구분선
7. 인사와 글의 목적
8. `1.1.`, `1.2.` 형식의 `h3` 세부 절
9. 각 `h2`와 `h3` 바로 아래 구분선
10. `2. 마무리` 절
11. 저장소, 이슈, 후원 등 관련 링크
12. 하단 구분선, 대표 이미지, 구분선

글의 내용이 길면 `2.`, `3.`처럼 `h2` 절을 늘리고 마지막 번호를 마무리 절에 사용한다. 제목 번호와
본문 순서가 일치해야 하며 `1.2`와 `1.3.`처럼 마침표 표기를 섞지 않는다.

## 문체

- 첫 문장은 기본적으로 `안녕하세요 반갑습니다. YS의 책장의 YS입니다.`를 사용한다.
- 한 문단에는 하나의 사실이나 설명을 담고, 긴 문단은 둘로 나눈다.
- 무엇이 바뀌었는지뿐 아니라 왜 바뀌었는지와 사용자가 무엇을 해야 하는지를 설명한다.
- 확인하지 않은 효과, 일정, 호환성, 배포 상태를 단정하지 않는다.
- 공개 제품명은 `ActiveLogbook`을 사용한다. 내부 프로젝트명 `TimePilot`은 기술적 맥락에서만 쓴다.
- 일반 설치 안내에서는 Microsoft Store를 먼저 표시한다.
- GitHub EXE와 portable 파일은 보조·레거시 경로라는 현재 배포 정책을 따른다.
- Store와 EXE 전환을 다룰 때는 백업, 새 설치 확인, 이전 설치 제거 순서를 생략하지 않는다.

## WordPress 블록 규칙

- 제목은 `wp:heading`, 본문은 `wp:paragraph` 블록으로 감싼다.
- `h3`에는 `{"level":3}` 속성을 사용한다.
- 제목 다음에는 `is-style-wide`, `backgroundColor: black` 구분선을 둔다.
- 외부 링크는 가능하면 `target="_blank" rel="noreferrer noopener"`를 사용한다.
- URL을 링크 텍스트로 그대로 보여 줄 수 있지만, 다운로드 성격은 링크 앞 문장에서 설명한다.
- 수정 공지는 실제 게시 후 내용이 달라졌을 때만 `wp:quote`로 추가한다.
- 목차는 `<!-- wp:luckywp/tableofcontents /-->` 블록을 사용한다.
- 이미지 블록의 `id`, `src`, `wp-image-*` 값은 WordPress 업로드 후 받은 실제 값만 사용한다.
- 이미지가 준비되지 않은 초안에는 이전 글의 이미지 ID나 URL을 복사하지 않는다.

## 게시 전 확인

- [ ] 제목과 소제목 번호가 순서대로 이어진다.
- [ ] 버전과 게시 날짜가 Store 또는 GitHub의 실제 상태와 일치한다.
- [ ] Microsoft Store 링크가 현재 제품 ID `9NWXBR051GLM`을 가리킨다.
- [ ] 이전 글의 버전, 파일명, 업데이트 인용문이 남아 있지 않다.
- [ ] GitHub EXE를 Store보다 우선 권장하는 오래된 문구가 없다.
- [ ] 개인정보처리방침과 지원 페이지 링크가 유효하다.
- [ ] 이미지 URL과 미디어 ID가 현재 글의 이미지다.
- [ ] WordPress 미리보기에서 목차와 구분선이 정상 표시된다.
- [ ] 모바일 화면에서 문단과 긴 URL이 레이아웃을 깨지 않는다.

## 참조 파일

- 내용 초안 예시: `docs/BLOG_POST_STORE_RELEASE_DRAFT.ko.md`
- WordPress 복사용 골격: `docs/blog/WORDPRESS_POST_TEMPLATE.ko.html`
- 홈페이지 소개 본문: `docs/site/wordpress/blocks/ACTIVELOGBOOK_PAGE_BODY.ko.html`
- 지원 페이지 본문: `docs/site/wordpress/blocks/ACTIVELOGBOOK_SUPPORT_BODY.ko.html`
