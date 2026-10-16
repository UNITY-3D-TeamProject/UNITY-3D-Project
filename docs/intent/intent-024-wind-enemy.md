---
id: intent-024
title: 특수 몹(바람) — 점점 세지는 바람으로 플레이어를 밀어내는 적
part: AI / Enemy / Skill
status: open
created: 2026-10-15
resolved: null
---

## 문제 (Problem)
적은 근접(Melee)·원거리(Ranged) 두 종류뿐이고, 둘 다 HP를 깎는 방식으로만 위협한다. 플레이어의 위치를 흔드는 적이 없어서, 붙어서 싸우는 플레이어를 떼어 내는 압박 수단이 없다.

## 기대 결과 (Proposed outcome)
Melee와 같은 골격(순찰·감지·추격·근접 공격)에 **바람 스킬**을 더한 특수 몹 `Special`을 만든다.

- 대상이 `WindRange`(5 m) 안이고 바람 쿨타임이 끝났으면 바람을 쓴다. 근접 사거리(2 m) 안이면 먼저 한 번 후퇴하고(실패해도 진행) 쓴다.
- 바람 시작 순간 몹 → 플레이어 수평 방향을 고정하고 제자리에 멈춘다.
- 2초 동안 미는 속도가 1 → 5 m/s로 선형 증가한다. 정면 부채꼴(수평 ±30°, 수직 ±30°, 3D 6 m) 안이고 차폐가 없을 때만 수평 방향으로 민다.
- 램프가 끝나는 순간 조건을 만족하면 4 m / 0.3초 EaseOut 넉백 후 끝, 아니면 넉백 없이 끝.
- 쿨타임(8초)은 바람이 끝난 시점부터 센다. 피해·연출은 없다.

확정된 구조:

- 바람은 스킬 `EnemyWindBlast`(요청 키 `Wind`)가 수행한다. 미는 통로는 `CharacterMotor.AddExternalDisplacement`.
- 바람 시간·세기·판정 수치는 **스킬 프리팹 한 곳**에만 둔다. 스킬이 `AIController`의 범용 Hold API로 몹을 붙잡고 끝나면 놓는다.
- 쿨타임은 스킬(`CooldownCost`)이 아니라 **BT 블랙보드 준비 시각**(`WindReadyTime`)으로 관리한다. 기본 `Cooldown` 노드는 Guard 조건에서 보이지 않아, 추격을 매 틱 끊는 abort 루프가 생기기 때문이다.
- BT 노드는 범용으로 만든다: `AIUseSkillAndWaitAction`(스킬 요청 후 Hold가 풀릴 때까지 대기), `AIIsTimeReachedCondition`, `AISetReadyTimeAction`.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 신규 `Special.prefab`(Melee Prefab Variant), 이후 다른 특수 몹(범용 Hold·노드 재사용)
- 어떤 시스템/모듈이 건드려지는가:
  - 수정 `Scripts/AI/AIController.cs` (Hold API)
  - 신규 `Scripts/Skill/Skills/EnemyWindBlast.cs`
  - 신규 `Scripts/AI/BT/AIUseSkillAndWaitAction.cs`, `AIIsTimeReachedCondition.cs`, `AISetReadyTimeAction.cs`
  - 신규 에셋([USER]): `EnemyWindBlast.prefab`, `Special.prefab`, `EnemyAI_Special.asset`
  - 신규 애니메이션 에셋(2026-10-16): `Animations/Enemy/Special.controller`, `Clips/Special_WindCharge.anim`, `Special_WindMask.mask` — `Special.prefab`의 Animator·`EnemyAnimator`에 연결 (코드 변경 없음)

## 제약 (Constraints)
- `SkillMediator`, `ISkillRequestController`, `CharacterMediator`, `Map/Gimmicks/` 는 수정하지 않는다 (담당 영역 밖).
- Melee 프리팹·그래프와 기존 BT 노드는 수정하지 않는다 (기존 몹 영향 0).
- asmdef·EditMode 테스트는 도입하지 않는다. 수동(Play) 검증으로 대체.
- `.prefab` / `.asset` / BT 그래프는 직접 편집하지 않는다 → `[USER]` 작업.

## 열린 질문 (Open questions)
- 사망 후 풀 재사용 시 BT 블랙보드(`WindReadyTime`)가 초기화되는지 — 구현 전 검증에서 확인.
- 설계 상세와 티켓은 `spec/wind-enemy.md`, 결정 과정은 개인 기획 파일(`docs/planning/wind-enemy.md`, Git 제외)에 있다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
