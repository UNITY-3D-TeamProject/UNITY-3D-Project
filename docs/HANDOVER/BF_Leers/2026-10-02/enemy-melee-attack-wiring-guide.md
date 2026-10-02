# 적 근거리 공격 에디터 배선 가이드 작성 (2026-10-02)

## 1. 작업 요약 및 방법
- 적 근거리 공격(intent-010)의 남은 `[USER]` 작업(에디터 배선·BT 그래프)을 위한 단계별 가이드를 `docs/features/enemy-melee-attack-wiring.md`에 작성했다.
- 코드·프리팹·에셋은 수정하지 않았다. 현재 `Melee.prefab`, `SkillPrefab.prefab`, `EnemyAI_Melee` 그래프의 직렬화 상태를 읽어 가이드의 값과 연결 대상을 정했다.

## 2. 결정 사항
- 배선은 사용자가 Unity 에디터에서 직접 하고, AI는 가이드만 제공한다 (intent-010 제약 유지).
- 공격 가지는 `Try In Order`의 맨 왼쪽에 두고, Guard는 **Abort Lower Priority**로 설정한다. `AIChaseTargetAction`이 대상이 보이는 한 Running이라 이 설정 없이는 공격 가지로 넘어가지 못한다.
- `HitPoint`는 `Melee` 루트의 자식으로 둔다. RotatorPrefab의 `_body`가 루트 Transform이라 루트가 회전한다.
- 스킬의 **Skill Name**(요청 키)은 `Attack`으로 통일한다. 클래스·프리팹 이름 `EnemyMeleeAttack`은 그대로다. Skill Name은 `SkillController`의 조회 키일 뿐 어트리뷰트 맵핑(`AttackDamage` → `CurrentHp`)과 무관하다. 가이드와 intent-010에 반영.
- 권장 초기값: `HitPoint` 로컬 (0, 0.5, 1.0), 판정 반지름 1.0, `AttackRange` 1.5, `CooldownCost._cooldown` 1.0.

## 3. 현재 상태 및 이슈
- 배선과 Play 검증은 아직 하지 않았다. intent-010은 `open`이다.
- 커밋된 프리팹/씬에는 `Player` 레이어(6) 오브젝트가 없다. `Player.prefab` 루트는 `Default`다. 테스트 씬의 플레이어 레이어를 확인해야 판정에 잡힌다.
- `Melee.prefab`의 `CharacterMediator`에는 `_skillMediator`가 비어 있다 (가이드 4번에서 연결).
- `origin/develop`(InitAttribute, PR #18)은 이미 현재 브랜치에 들어와 있다.
- `ProjectSettings/EditorBuildSettings.asset`의 미커밋 변경(`com.unity.dt.app-ui`)은 이번 작업과 무관해 그대로 두었다.

## 4. 다음 할 일 (Next Steps)
- `[USER]` 가이드 0~5번 배선, 6번 Play 검증.
- 검증 통과 시 intent-010을 `resolved`로 갱신하고 `docs/intent/clear/`로 이동.
- 플레이어 쪽 `Fire` → `Attack` 이름 통일(`PlayerInputComponent`의 `"Fire"` 요청, `Fire.prefab`의 Skill Name)은 이세훈 담당 작업과 맞춰야 한다. 이번에는 건드리지 않았다.
- `origin/feature/Skill`이 develop에 들어오면 `ISkillRequestController` 시그니처 변경에 맞춰 `AIController` 수정 (2026-10-01 기록 참고).
