# Intent / Handover 문서 시스템 구축

## 작업 요약
- 작업을 시작하기 전 "왜, 무엇을 위해" 하는지를 기록하는 Intent 문서 시스템(`docs/intent/`)을 도입했다.
- 루트의 단일 `MEMORY.md` 방식을 폐기하고, 참여자별 인수인계 기록 시스템(`docs/HANDOVER/{이름}/`)으로 교체했다.

## 방법/접근
- Intent 문서는 문제/기대 결과/영향 범위/제약/열린 질문 5개 항목으로 구성하고, 해결되면 `docs/intent/clear/`로 이동시키는 방식을 택했다.
- Intent 작성·해결 절차를 `.claude/skills/intent/SKILL.md` 스킬로 만들어 AI가 알아서 작성/해결을 제안하도록 했다.
- 세션 종료(Stop) 시 `docs/intent/`에 열려있는(`intent-*.md`) 문서가 있으면 자동으로 리마인드하는 Stop 훅을 `.claude/settings.json`에 추가했다. (열린 문서 없으면 아무 출력도 하지 않도록 pipe-test로 검증)
- Handover는 사람 이름 기준 폴더(`docs/HANDOVER/{이름}/`)를 두고, 그 안에 `HANDOFF_POINTER.md`(변경 이력 목차, 최신순)와 `{YYYY-MM-DD}/{제목}.md`(실제 작업 기록) 구조로 나눴다. 여러 AI/도구를 쓰더라도 사람 단위로 하나의 폴더만 두기로 했다.

## 변경된 파일
- `docs/intent/README.md`, `docs/intent/_template.md`, `docs/intent/clear/.gitkeep`
- `.claude/skills/intent/SKILL.md`
- `.claude/settings.json` (Stop 훅)
- `docs/HANDOVER/README.md`, `docs/HANDOVER/_template/HANDOFF_POINTER.md`
- `CLAUDE.md` (5장 Intent 관리 시스템 추가, 4장 Memory Protocol을 Handover 시스템으로 교체)
- 루트 `MEMORY.md` 삭제
- `.claude/skills/basic_setup/SKILL.md`의 MEMORY.md 참조를 handover 시스템 참조로 수정

## 결정 사항
- Intent 문서 파일명 규칙: `intent-NNN-짧은-슬러그.md` (3자리 순번)
- Handover 폴더는 "사람" 기준이며, 이름은 실제 참여자 이름/닉네임을 사용한다 (예: `Minsung`).
- MEMORY.md는 완전히 폐기하고 handover 시스템으로 대체한다.

## 현재 상태 및 이슈
- 아직 실제 기능 작업에서 두 시스템을 실전 사용해본 적은 없다 (틀만 구축된 상태).
- Stop 훅은 세션이 실제로 끝나야 발동하므로, 이번 세션 안에서 직접 트리거해 확인하지는 못했다. `.claude/` 폴더가 방금 새로 생겨 설정 워처가 못 잡았을 수 있으니, 반응이 없으면 `/hooks`를 한 번 열어보는 것을 권장한다.

## 다음 할 일
- 다음 실제 기능 작업부터 `docs/intent/intent-001-...`과 `docs/HANDOVER/{이름}/{날짜}/...` 형식을 실제로 사용해보고, 절차가 실무에 맞는지 점검한다.
- 팀에 다른 참여자가 합류하면 `docs/HANDOVER/_template/`을 복사해 그 사람 이름의 폴더를 만들어준다.
