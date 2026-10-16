# 적 피격 경직 (intent-026)

## 1. 작업 요약 및 방법
적이 맞아도 Hit 애니메이션만 나오고 추격·공격이 계속되던 문제를 고쳤다. `AIController.OnHitFromCallback`이 대상이 보이면 바로 return 해서 전투 중 피격에는 아무 반응이 없었다.

- `AIController`
  - `_hitStunDuration`(0.4초), `_hitStunImmuneDuration`(1.5초) 직렬화 필드를 추가했다.
  - `IsStunned`, `IsSuperArmor { get; set; }` 프로퍼티를 추가했다.
  - `OnHitFromCallback`에서 대상이 보이는지와 상관없이 경직을 건다. 단, `IsHolding`/`IsSuperArmor` 중이거나 면역 시간 안이면 걸지 않는다. 면역 타이머는 경직 시작 시 `경직 + 면역`으로 잡는다.
  - `Update`의 hit-look 블록 뒤에서, 경직 중이면 `RequestMove(zero)` 후 return 한다. 목적지는 유지되므로 풀리면 추격을 이어간다.
  - `ExecuteSkill`은 경직 중이면 무시한다. `Attack()`도 이 함수를 거치므로 함께 막힌다. `StopSkill`은 막지 않는다.
  - `OnDisable`에서 경직·면역 타이머와 `IsSuperArmor`를 초기화한다.
- `AIRangedAttackAction`
  - `UpdateWait`: 경직 중에는 예비 동작을 시작하지 않는다. 이렇게 하지 않으면 조준선 스킬만 무시되고 `_isWindingUp`이 켜져서, 조준선 없이 발사된다.
  - 예비 동작을 시작할 때 `IsSuperArmor = true`, 발사할 때와 `OnEnd`에서 `false`로 돌린다.

## 2. 결정 사항
- 디메리트는 경직이다. 이동과 스킬 요청을 모두 멈춘다(감속안은 채택하지 않음).
- 연사로 계속 묶이지 않도록, 경직이 끝난 뒤 면역 시간을 둔다. 면역 중에도 애니메이션은 재생된다.
- 스킬 사용 중에는 슈퍼아머로 경직하지 않는다. Special 바람은 `IsHolding`으로, 원거리 예비 동작은 `IsSuperArmor`로 판정한다.
- 대상이 안 보일 때의 피격 돌아보기는 기존대로 유지한다. 두 동작이 겹치면 hit-look 블록이 먼저 멈춤과 회전을 처리한다.

## 3. 현재 상태 및 이슈
- 컴파일 에러 0.
- Play에서 Special 개체에 리플렉션으로 `OnHitFromCallback`을 호출해 다음을 확인했다.
  - 피격하면 경직된다.
  - 면역 시간 안에 다시 맞으면 경직되지 않는다.
  - 슈퍼아머 중이나 Hold 중에는 경직되지 않는다.
  - 경직 중 `ExecuteSkill` 호출 0회, 풀린 뒤 1회.
- **미확인**: MCP Play 프레임이 멈춰서 경직 중 이동이 정지했다가 재개되는 것은 관찰하지 못했다. 실제 총알로 Melee·Ranged를 맞히는 Play 검증도 하지 않았다.
- `OnHitFrom`은 `hitSource`가 있을 때만 발생한다. 맞은 지점이나 공격자 정보가 없는 피해는 경직을 일으키지 않는다.
- 0.4초 / 1.5초는 초기값이다. Hit 클립 길이에 맞춘 조정이 필요하다.

## 4. 다음 할 일 (Next Steps)
- 실제 Play에서 추격 중인 Melee를 쏴서, 멈췄다가 다시 추격하는지와 연사 시 면역이 체감되는지 확인한다.
- 원거리 적의 조준선이 뜬 뒤에 맞혀도 발사되는지 확인한다.
- 수치 튜닝이 끝나면 intent-026을 `resolved`로 바꾸고 `docs/intent/clear/`로 옮긴다.
