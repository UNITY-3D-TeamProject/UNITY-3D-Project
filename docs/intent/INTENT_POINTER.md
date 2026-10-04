# Intent Pointer

이슈 트래커처럼 "어느 파트에 어떤 문제가 있는지"를 한눈에 보기 위한 목차이다.
새 intent 문서를 만들거나 상태가 바뀔 때마다 이 파일을 함께 갱신한다.

## 열려있는 인텐트 (Open)
<!-- 파트별로 묶어서 나열. 형식: - **[파트]** 문제 한 줄 요약 → [문서](./intent-NNN-slug.md) -->
- **[Movement System]** 이동 코어에 경로/회전/탑승 정책이 섞여 있음 → [문서](./intent-002-movement-core-scope.md)
- **[Movement System]** PlayMode 테스트로 발견된 이동 자체 버그 4건 → [문서](./intent-003-movement-playtest-bugfixes.md)
- **[Combat]** Combat 컴포넌트 재설계 — 의존성 0 + HP 옵서버 완료. `IHitReceiver`/`SHitInfo` 폐기로 입구가 `Health` setter 하나로 통합됨(HP 감소 = 피격). HP를 깎는 주체 등 이세훈과 협의할 열린 질문 3건 남음 → [문서](./intent-005-combat-component-decoupling.md)
- **[Movement System]** Player_test 이동/점프 입력이 CharacterMotor까지 도달하지 않음(프리팹 참조 끊김 + 점프 명령/속도 미구현) → [문서](./intent-007-move-jump-not-reaching-motor.md)
- **[Character]** 몸통 회전 채널 부재로 AI 시야 부채꼴이 스폰 시점 forward에 고정됨. `feature/Rotation` 브랜치가 팀원 Mediator 리팩터 PR 머지 후 `git reset`으로 이전 구현을 잃어 재작성 → [문서](./intent-009-body-rotation.md)
- **[AI / Combat]** 적이 추격만 하고 공격하지 않음. 스킬(`EnemyMeleeAttack`) + 근접 판정 매개체 + BT 공격 가지로 구현, 코드 완료·에디터 배선(`[USER]`)과 Play 검증 남음 → [문서](./intent-010-enemy-melee-attack.md)
- **[AI]** 시야 감지·해제가 즉시 일어나 등 뒤로 돌면 엉뚱한 곳을 수색함. 발견 게이지 + 놓친 뒤 유예(실제 위치 추적)를 도입, 코드 완료·Play 검증 남음 → [문서](./intent-011-sensor-awareness.md)

## 해결됨 (Resolved)
<!-- 형식: - **[파트]** 문제 한 줄 요약 → [문서](./clear/intent-NNN-slug.md) (resolved: YYYY-MM-DD) -->
- **[Scene Test]** 박스 접촉으로 Home/Lobby 씬 전환 테스트 → [문서](../HANDOVER/SungJun/2026-09-26/intent-008-test-scene-transition-box.md) (resolved: 2026-09-26)
- **[UI]** 플레이어 Attribute를 표시할 HUD MVP 기반이 없음 → [문서](./clear/intent-006-player-hud-mvp-foundation.md) (resolved: 2026-09-25)
- **[Stage System]** 앱별 `StageBase` 구현체가 없음 → [문서](./clear/intent-005-stage-implementations.md) (resolved: 2026-09-24)
- **[Stage System]** 앱별 스테이지와 내부 Phase를 위한 공통 코어 타입 부재 → [문서](./clear/intent-004-stage-core-foundation.md) (resolved: 2026-09-24)
- **[Movement System]** 이동 시스템 공용 코어(CharacterMotor/WaypointMover) 부재 → [문서](./clear/intent-001-movement-system-core.md) (resolved: 2026-09-11)
- **[Combat]** 전투 컴포넌트 통신 채널 부재 → [문서](./clear/intent-004-combat-component-channel.md) (resolved: 2026-09-20)
- **[Combat]** asmdef 부재로 자동 단위 테스트 불가(수동 테스트뿐) → [문서](./clear/intent-006-combat-unit-testing.md) (resolved: 2026-09-21)
