---
id: intent-010
title: 적 근거리 공격 — 스킬 + 근접 판정 매개체 + AI 발동
part: AI / Combat
status: open
created: 2026-10-01
resolved: null
---

## 문제 (Problem)
적 AI는 추격까지만 하고 공격하지 않는다. 코드 어디에도 다른 캐릭터의 HP를 깎는 경로가 없었다(intent-005 열린 질문 ①).

## 기대 결과 (Proposed outcome)
근거리 적이 대상이 사거리 안에 들어오면 멈춰서 대상을 바라보고, 쿨타임 간격으로 근접 공격을 해 대상의 HP를 깎는다.

확정된 구조:

- 공격은 별도 수신 컴포넌트나 중재자 창구 없이 **스킬(`SkillBase`)이 직접 수행**한다 (Roll과 같은 구조).
- 스킬은 공격 종류별로 나눈다. 플레이어 사격은 `Fire`(추후 `Attack`, 이세훈 담당), 적 근거리는 `EnemyMeleeAttack`, 적 원거리는 추후 별도 스킬. `Fire` 하나가 모든 공격을 알지 않는다.
- 스킬 **클래스**는 종류별로 나누되, 요청 키인 **Skill Name**(인스펙터 값)은 모든 캐릭터가 `Attack`으로 통일한다. 어떤 공격이 나가는지는 그 캐릭터의 `SkillController`에 등록된 스킬이 정한다. 한 `SkillController`에는 `Attack` 이름의 스킬을 하나만 둔다(같은 이름은 등록이 거부된다).
- 스킬이 `SOAttributeEffect`를 참조하고, **매개체가 Effect를 들고 대상에 닿았을 때 `Apply(target, cursor)`를 호출**한다. target/cursor는 `GetComponentInParent<IEffectTarget>()`로 직접 얻는다 (`BulletController`와 같은 방식).
- 근거리 매개체(`MeleeHitController`)는 날아가지 않는다. 공격 지점에 생겨 범위 안 대상을 한 번씩 때리고 사라진다.
- **공격 간격은 `CooldownCost`가 정한다.** `EnemyMeleeAttack`이 강제하지는 않으며, 스킬 프리팹에 `CooldownCost`를 직접 붙이고 인스펙터의 `_cooldown` 값으로 간격을 정한다.
- AI가 공격 여부를 판단하는 **사거리는 BT 블랙보드 변수**다. 거리 조건 노드를 범용으로 만들어 "너무 멀면 돌진" 같은 가지에도 다른 블랙보드 값으로 재사용한다.

흐름:

```
BT 공격 액션 → AIController.Attack() → (ISkillRequestController) → SkillMediator → SkillController
  → EnemyMeleeAttack.Execute() → MeleeHitController 생성 + Initialize(effect, cursor)
  → 범위 안 대상의 IEffectTarget 에 effect.Apply(target, cursor)
  → AttributeSet HP 감소 → CombatMediator → CharacterCombat.OnHit / OnDeath
```

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 근거리 적(`Melee.prefab`), 이후 원거리 적·돌진 패턴
- 어떤 시스템/모듈이 건드려지는가:
  - 신규 `Scripts/Hit/MeleeHitController.cs`, `Scripts/Skill/Skills/EnemyMeleeAttack.cs`
  - 신규 `Scripts/AI/BT/AIIsTargetWithinRangeCondition.cs`, `AIAttackTargetAction.cs`
  - 수정 `Scripts/AI/AIController.cs` (`ISkillRequestController` 구현, `DistanceToTarget`, `Attack()`)

## 제약 (Constraints)
- `Fire.cs`, `BulletController.cs`, `SkillMediator.cs`, `CharacterMediator.cs`, `Combat/`는 수정하지 않는다 (이세훈 담당 영역 / Combat 의존성 0 원칙).
- `.prefab` / `.asset` / BT 그래프는 직접 편집하지 않는다 → `[USER]` 작업. 순서와 값은 `docs/features/enemy-melee-attack-wiring.md`를 따른다.

## 열린 질문 (Open questions)
- 돌진 액션(대상이 너무 멀 때)의 구체적 동작 — 구상 단계, 이번 범위 밖.
- 적 원거리 공격 스킬 — `feature/Skill`의 `BulletController`가 develop에 들어온 뒤 진행.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
