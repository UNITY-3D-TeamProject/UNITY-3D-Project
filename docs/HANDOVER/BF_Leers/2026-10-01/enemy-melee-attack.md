# 적 근거리 공격 구현 (2026-10-01)

## 1. 작업 요약 및 방법
- 적이 사거리 안에서 근접 공격을 하도록 코드를 작성했다. 에디터 배선은 하지 않았다.
- 신규
  - `Scripts/Hit/MeleeHitController.cs`: 근접 판정 매개체. `Initialize(hitEffect, cursor)` 호출 즉시 `OverlapSphere`로 범위 안 대상을 찾아 `SOAttributeEffect.Apply`를 호출하고, 수명 뒤 소멸한다. 공격자 자신은 제외하고, 한 캐릭터에는 한 번만 적용한다.
  - `Scripts/Skill/Skills/EnemyMeleeAttack.cs`: `SkillBase` 상속. `Execute`에서 `_hitPoint`에 판정 프리팹을 만들고 Effect와 cursor를 넘긴다.
  - `Scripts/AI/BT/AIIsTargetWithinRangeCondition.cs`: 블랙보드 `Range` 안에 대상이 있는지 묻는 범용 거리 조건.
  - `Scripts/AI/BT/AIAttackTargetAction.cs`: 블랙보드 `Range` 안에서 멈춰 매 틱 `Attack()`을 요청한다. 대상이 벗어나면 Success로 끝난다.
- 수정
  - `Scripts/AI/AIController.cs`: `ISkillRequestController` 구현, `_attackSkillName` 필드, `DistanceToTarget` 프로퍼티, `Attack()` 메서드 추가.
- 검증: Unity 배치 모드 컴파일 에러 0건. Play 검증은 하지 않았다.

## 2. 결정 사항
- 공격은 수신 컴포넌트나 중재자 창구 없이 스킬이 직접 수행한다. 스킬은 공격 종류별로 나눈다(`Fire`가 전부 알지 않는다).
- 매개체가 Effect를 들고 대상의 `IEffectTarget`을 `GetComponentInParent`로 얻어 `Apply`를 호출한다. intent-005 열린 질문 ①이 이것으로 확정됐다.
- 근거리 매개체는 날아가지 않는 제자리 판정이다. 총알(`BulletController`) 재사용 안은 기각했다.
- 공격 사거리는 BT 블랙보드 변수로 둔다. `SOAttributeData_Enemy`의 `AttackRange` 어트리뷰트는 읽지 않는다.
- 공격 간격은 AI가 아니라 스킬의 `CooldownCost`가 정한다. `AIController.Attack()`은 실행 요청 직후 중지 요청을 보내 한 번만 발동시킨다.

## 3. 현재 상태 및 이슈
- 현재 브랜치는 `CurrentHp` 초기값이 0이라 전원이 시작부터 사망 상태다. develop의 InitAttribute(PR #18)에서 해결됐으므로 HP 검증 전에 `origin/develop`을 받아야 한다.
- `origin/feature/Skill`이 develop에 들어오면 `ISkillRequestController`가 `Action<string>`으로 바뀐다. `AIController`의 `SetRequestExecuteSkill` / `SetRequestStopSkill` 시그니처와 이벤트 타입을 맞춰야 한다(머지 시 컴파일 에러로 드러난다).
- `EnemyMeleeAttack`은 `CooldownCost`를 강제하지 않는다. 붙이지 않으면 `AIAttackTargetAction`이 매 틱 공격을 요청해 사거리 안에서 매 틱 발동하므로 스킬 프리팹에 직접 추가해야 한다. 공격 간격은 `CooldownCost._cooldown` 값(기본 1초)이며, `SOAttributeData_Enemy`의 `AttackCooldown` / `AtackIntervel` 어트리뷰트는 읽지 않는다.

## 4. 다음 할 일 (Next Steps)
- `[USER]` `SOAttributeEffect` 에셋: Modifier = Subtract, Target = `CurrentHp`, ValueSource = Attribute, Cursor = `AttackDamage`.
- `[USER]` 근접 판정 프리팹: `MeleeHitController`(`_hitLayers` = Player, 반지름, 수명).
- `[USER]` 적 근접 스킬 프리팹: `EnemyMeleeAttack`(SkillName 지정, `CooldownCost` 컴포넌트를 직접 추가하고 `_cooldown`에 공격 간격 입력), 위 프리팹과 에셋 연결.
- `[USER]` `Melee.prefab`: SkillPrefab(SkillMediator + SkillController) 아래 스킬 배치, `_hitPoint` 지정, `AIController._attackSkillName`을 같은 이름으로, `CharacterMediator._skillMediator` 연결.
- `[USER]` `EnemyAI_Melee` 그래프: 블랙보드에 `AttackRange`(float) 추가, 추격 가지 앞에 `대상이 AttackRange 안 → 공격` 가지 추가. 값은 판정 반지름과 `_hitPoint` 거리에 맞춘다.
- Play 검증: 사거리 안에서 정지·대상 바라보기, 쿨타임 간격으로 플레이어 `CurrentHp` 감소, 적 자신의 HP는 그대로, 사거리 이탈 시 추격 복귀.
- 돌진 액션과 적 원거리 스킬은 intent-010의 열린 질문으로 남겼다.
