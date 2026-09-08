# Intent 시스템을 이슈 트래커 방식으로 전환 (INTENT_POINTER.md)

## 작업 요약
- Intent 시스템에 `docs/intent/INTENT_POINTER.md`를 추가해, 이슈 트래커처럼 "어느 파트에 어떤 문제가 있는지"를 한눈에 볼 수 있게 했다.

## 방법/접근
- intent 문서 frontmatter에 `part` 필드를 추가해 어느 모듈/영역 문제인지 명시하도록 했다.
- `INTENT_POINTER.md`에 "열려있는 인텐트"와 "해결됨" 두 섹션을 두고, 각 항목을 `- **[파트]** 문제 한 줄 요약 → [문서 링크]` 형식으로 기록하게 했다.
- `docs/intent/README.md`와 `.claude/skills/intent/SKILL.md`에 "intent 문서를 만들거나/갱신하거나/해결 처리할 때는 반드시 그 자리에서 INTENT_POINTER.md도 함께 갱신한다"는 규칙을 명시했다.

## 변경된 파일
- `docs/intent/_template.md` (frontmatter에 `part` 필드 추가)
- `docs/intent/INTENT_POINTER.md` (신규)
- `docs/intent/README.md` (폴더 구조, 작성 항목, 운영 규칙, AI 사용 지침 갱신)
- `.claude/skills/intent/SKILL.md` (0/1/2/3단계에 포인터 갱신 절차 추가)

## 결정 사항
- `INTENT_POINTER.md`는 파트별 그룹핑보다는 각 줄에 `[파트]` 라벨을 붙이는 단순 목록 형식으로 시작한다. 파트 수가 많아지면 나중에 섹션을 파트별로 쪼갤 수 있다.
- 해결된 intent도 포인터에서 완전히 지우지 않고 "해결됨" 섹션으로 옮겨 이력을 남긴다.

## 현재 상태 및 이슈
- 아직 실제 intent 문서가 하나도 등록되지 않아 `INTENT_POINTER.md`는 두 섹션 모두 플레이스홀더 상태다.

## 다음 할 일
- 첫 실제 intent(`intent-001-...`)를 만들 때 이 포인터 갱신 절차가 실무에서 매끄러운지 확인한다.
