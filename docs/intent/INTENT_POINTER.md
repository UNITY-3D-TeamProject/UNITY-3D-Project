# Intent Pointer

이슈 트래커처럼 "어느 파트에 어떤 문제가 있는지"를 한눈에 보기 위한 목차이다.
새 intent 문서를 만들거나 상태가 바뀔 때마다 이 파일을 함께 갱신한다.

## 열려있는 인텐트 (Open)
<!-- 파트별로 묶어서 나열. 형식: - **[파트]** 문제 한 줄 요약 → [문서](./intent-NNN-slug.md) -->
- **[Movement System]** 이동 코어에 경로/회전/탑승 정책이 섞여 있음 → [문서](./intent-002-movement-core-scope.md)
- **[Movement System]** PlayMode 테스트로 발견된 이동 자체 버그 4건 → [문서](./intent-003-movement-playtest-bugfixes.md)
- **[Combat]** Combat 컴포넌트 재설계 — 의존성 0 + HP 옵서버 완료. `IHitReceiver`/`SHitInfo` 폐기로 입구가 `Health` setter 하나로 통합됨(HP 감소 = 피격). HP를 깎는 주체 등 이세훈과 협의할 열린 질문 3건 남음 → [문서](./intent-005-combat-component-decoupling.md)
- **[AI]** 적·NPC 조종부 부재 — BT 기반 AI 구조 28개 결정 확정, 1차는 이동만(순찰→감지→추격→수색→복귀). 스킬 매니저 대기 4건 + 문서-코드 불일치 4건 열림 → [문서](./intent-008-enemy-ai-system.md)

## 해결됨 (Resolved)
<!-- 형식: - **[파트]** 문제 한 줄 요약 → [문서](./clear/intent-NNN-slug.md) (resolved: YYYY-MM-DD) -->
- **[Movement System]** 이동 시스템 공용 코어(CharacterMotor/WaypointMover) 부재 → [문서](./clear/intent-001-movement-system-core.md) (resolved: 2026-09-11)
- **[Combat]** 전투 컴포넌트 통신 채널 부재 → [문서](./clear/intent-004-combat-component-channel.md) (resolved: 2026-09-20)
- **[Combat]** asmdef 부재로 자동 단위 테스트 불가(수동 테스트뿐) → [문서](./clear/intent-006-combat-unit-testing.md) (resolved: 2026-09-21)
