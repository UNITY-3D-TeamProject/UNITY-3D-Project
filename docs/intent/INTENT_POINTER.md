# Intent Pointer

이슈 트래커처럼 "어느 파트에 어떤 문제가 있는지"를 한눈에 보기 위한 목차이다.
새 intent 문서를 만들거나 상태가 바뀔 때마다 이 파일을 함께 갱신한다.

## 열려있는 인텐트 (Open)
- **[Stage / Player State]** StageManager 체크포인트 컴포넌트 연결 및 도달 처리 → [문서](../HANDOVER/SungJun/intent/intent-016-stage-checkpoint-events.md)
- **[Stage / Player State]** PlayerFacade 기반 낙하 복귀(이동)·사망 복구(파괴 후 재스폰) 구현, 사망 시 Destroy 전제 확인 필요 → [문서](../HANDOVER/SungJun/intent/intent-017-fall-death-recovery-playerfacade.md)
<!-- 파트별로 묶어서 나열. 형식: - **[파트]** 문제 한 줄 요약 → [문서](./intent-NNN-slug.md) -->
- **[Movement System]** 이동 코어에 경로/회전/탑승 정책이 섞여 있음 → [문서](./intent-002-movement-core-scope.md)
- **[Movement System]** PlayMode 테스트로 발견된 이동 자체 버그 4건 → [문서](./intent-003-movement-playtest-bugfixes.md)
- **[Combat]** Combat 컴포넌트 재설계 — 의존성 0 + HP 옵서버 완료. `IHitReceiver`/`SHitInfo` 폐기로 입구가 `Health` setter 하나로 통합됨(HP 감소 = 피격). HP를 깎는 주체 등 이세훈과 협의할 열린 질문 3건 남음 → [문서](./intent-005-combat-component-decoupling.md)
- **[Movement System]** Player_test 이동/점프 입력이 CharacterMotor까지 도달하지 않음(프리팹 참조 끊김 + 점프 명령/속도 미구현) → [문서](./intent-007-move-jump-not-reaching-motor.md)
- **[Character]** 몸통 회전 채널 부재로 AI 시야 부채꼴이 스폰 시점 forward에 고정됨. `feature/Rotation` 브랜치가 팀원 Mediator 리팩터 PR 머지 후 `git reset`으로 이전 구현을 잃어 재작성 → [문서](./intent-009-body-rotation.md)
- **[AI / Combat]** 적이 추격만 하고 공격하지 않음. 스킬(`EnemyMeleeAttack`) + 근접 판정 매개체 + BT 공격 가지로 구현, 코드 완료·에디터 배선(`[USER]`)과 Play 검증 남음 → [문서](./intent-010-enemy-melee-attack.md)
- **[AI]** 시야 감지·해제가 즉시 일어나 등 뒤로 돌면 엉뚱한 곳을 수색함. 발견 게이지 + 놓친 뒤 유예(실제 위치 추적)를 도입, 코드 완료·Play 검증 남음 → [문서](./intent-011-sensor-awareness.md)
- **[AI / Combat]** 적이 근거리 한 종류뿐. 프로토타입용으로 `EnemyRangedAttack`(스킬이 총알을 직접 생성) 구현. 풀링 도입 시 Factory 경로로 옮기는 것이 기술 부채. 코드 완료, 에디터 배선(`[USER]`)과 Play 검증 남음 → [문서](./intent-012-enemy-ranged-attack.md)
- **[Movement System]** 점프력이 모터에 고정되고 공중 점프 불가, 시작/착지 이벤트 없음. `Jump(float jumpPower)` 무조건 실행 + `OnJumpStarted`/`OnJumpEnded` 추가, 접지 정책은 MoveMediator로. 코드 완료·컴파일 0에러, Play 검증 남음 → [문서](./intent-015-jump-power-param-and-events.md)
- **[Attribute / Mediator / Combat]** 데미지 경로에 출처(누가·어디서)가 없어 피격 반응 불가. `SEffectContext{Cursor, Origin}`를 Apply→어트리뷰트 콜백까지 관통(C안). 1차 전달 경로 진행, 2차(원점 기록·CombatMediator 연결·AI 반응) 남음 → [문서](./intent-016-hit-context.md)

## 해결됨 (Resolved)
- **[Stage / Player State]** Core 기준 생존 낙하 복귀와 라운드 사망 복구 설계 완료, 구현 전 → [문서](../HANDOVER/SungJun/intent/clear/intent-015-stage-round-recovery-design.md) (resolved: 2026-10-08)
- **[UI / Game State]** 초기 Playing, GameManager 커서 처리, 설정창 열기·닫기 액션 구독 구현 → [문서](../HANDOVER/SungJun/intent/intent-013-settings-input-flow.md) (resolved: 2026-10-05)
<!-- 형식: - **[파트]** 문제 한 줄 요약 → [문서](./clear/intent-NNN-slug.md) (resolved: YYYY-MM-DD) -->
- **[Monster Spawner]** 몬스터 프리팹 생성과 시작 능력치 설정 기반 부재 → [문서](../HANDOVER/SungJun/intent/intent-012-monster-spawner-foundation.md) (resolved: 2026-10-04)
- **[Scene Test]** 박스 접촉으로 Home/Lobby 씬 전환 테스트 → [문서](../HANDOVER/SungJun/2026-09-26/intent-008-test-scene-transition-box.md) (resolved: 2026-09-26)
- **[UI]** 플레이어 Attribute를 표시할 HUD MVP 기반이 없음 → [문서](./clear/intent-006-player-hud-mvp-foundation.md) (resolved: 2026-09-25)
- **[Stage System]** 앱별 `StageBase` 구현체가 없음 → [문서](./clear/intent-005-stage-implementations.md) (resolved: 2026-09-24)
- **[Stage System]** 앱별 스테이지와 내부 Phase를 위한 공통 코어 타입 부재 → [문서](./clear/intent-004-stage-core-foundation.md) (resolved: 2026-09-24)
- **[Movement System]** 이동 시스템 공용 코어(CharacterMotor/WaypointMover) 부재 → [문서](./clear/intent-001-movement-system-core.md) (resolved: 2026-09-11)
- **[Combat]** 전투 컴포넌트 통신 채널 부재 → [문서](./clear/intent-004-combat-component-channel.md) (resolved: 2026-09-20)
- **[Combat]** asmdef 부재로 자동 단위 테스트 불가(수동 테스트뿐) → [문서](./clear/intent-006-combat-unit-testing.md) (resolved: 2026-09-21)
