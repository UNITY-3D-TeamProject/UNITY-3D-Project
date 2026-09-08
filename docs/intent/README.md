# Intent 관리 시스템

작업을 시작하기 전에 "왜 하는지"와 "무엇이 끝인지"를 먼저 문서로 고정해 두는 시스템이다.
사람과 AI 에이전트 모두 새 기능/변경을 시작하기 전에 이 폴더에 intent 문서를 만든다.

## 폴더 구조
- `docs/intent/INTENT_POINTER.md` : 이슈 트래커처럼 "어느 파트에 어떤 문제가 있는지"를 파트별로 모아 보여주는 목차. 각 항목은 문제 한 줄 요약 + 문서 링크로 구성된다.
- `docs/intent/` : 아직 해결되지 않은(open) intent 문서들
- `docs/intent/clear/` : 해결이 끝난(resolved) intent 문서들
- `docs/intent/_template.md` : 새 intent 작성 시 복사해서 쓰는 템플릿

## 파일 이름 규칙
`intent-NNN-짧은-슬러그.md` (예: `intent-001-inventory-ui-lag.md`)
NNN은 3자리 순번으로, 기존 파일(open + clear 전체)에서 가장 큰 번호 다음 값을 사용한다.

## 작성 항목
1. **파트 (Part)** — 어느 부분/모듈 문제인지 (frontmatter `part` 필드, 예: Inventory, Player Controller, Build/CI)
2. **문제 (Problem)** — 지금 무엇이 불편하거나 되지 않는지
3. **기대 결과 (Proposed outcome)** — 해결되면 무엇이 어떻게 달라지는지
4. **영향 범위 (Affected users and systems)** — 누가 쓰고 어떤 시스템이 건드려지는지
5. **제약 (Constraints)** — 예산, 기한, 지켜야 할 정책, 쓰지 말아야 할 것
6. **열린 질문 (Open questions)** — 아직 정하지 못한 것

## 운영 규칙
- 새 작업을 시작하기 전, 해당 작업을 설명하는 intent 문서가 없으면 `_template.md`를 복사해 `docs/intent/`에 새로 만든다.
- 이미 해당 작업을 다루는 intent 문서가 있으면 새로 만들지 않고 기존 문서를 갱신한다.
- **열린 질문**이 있는 상태에서 임의로 답을 추측해 코딩하지 않는다. 확인이 필요하면 작업을 멈추고 사용자에게 질문한다. (CLAUDE.md의 "코딩 전 생각하기" 원칙과 연결됨)
- 새 intent 문서를 만들면 `INTENT_POINTER.md`의 "열려있는 인텐트" 목록에 파트별로 항목을 추가한다: `- **[파트]** 문제 한 줄 요약 → [문서](./intent-NNN-slug.md)`
- 작업이 끝나 문제가 해결되면:
  1. 문서 상단 frontmatter의 `status`를 `resolved`로, `resolved` 날짜를 채운다.
  2. `## 해결 기록 (Resolution)` 섹션에 최종 결정과 관련 커밋/PR을 적는다.
  3. 파일을 `docs/intent/`에서 `docs/intent/clear/`로 이동한다.
  4. `INTENT_POINTER.md`에서 해당 항목을 "열려있는 인텐트"에서 지우고 "해결됨" 목록으로 옮긴다 (링크 경로를 `./clear/...`로 수정하고 resolved 날짜를 표기).
- 하나의 intent 문서는 하나의 문제만 다룬다. 범위가 커지면 쪼갠다.

## AI 에이전트 사용 지침
- 새로운 세션에서 기능 요청을 받으면, 먼저 `INTENT_POINTER.md`를 훑어 관련 파트에 이미 열려있는 intent가 있는지 확인한다.
- 관련 문서가 없고 작업 규모가 사소한 수정 이상이라면, 코딩을 시작하기 전에 intent 문서 초안을 사용자와 함께 작성하고 `INTENT_POINTER.md`에 등록한다.
- intent 문서의 "열린 질문"이 남아 있는 항목은 AI가 임의로 결정하지 않는다.
- intent 문서를 만들거나, 상태를 바꾸거나, 해결 처리할 때는 반드시 그 자리에서 `INTENT_POINTER.md`도 함께 갱신한다. 포인터가 실제 파일 상태와 어긋나지 않도록 유지하는 것이 핵심이다.
