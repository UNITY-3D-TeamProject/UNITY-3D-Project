# 원거리 적 사격 예고 (랜덤 간격 + 고정 예비 동작 + 조준 고정)

- 날짜: 2026-10-04
- 브랜치: `feature/AI/RangedEnemy`
- 관련 intent: [intent-014](../../../intent/intent-014-ranged-enemy-telegraph.md) (`open`)

## 1. 작업 요약 및 방법
원거리 적은 사격 순간까지 몸이 플레이어를 따라 돌아서 항상 맞혔다. 원칙을 "언제 쏠지는 예측할 수 없지만, 신호를 보고 움직이면 반드시 피할 수 있다"로 정하고 다음을 구현했다.

- `AI/AIController.cs`: 범용 API 3개를 추가했다.
  - `FaceDirection(Vector3)`, `ExecuteSkill(string)`, `StopSkill(string)`
  - 기존 `Attack()`은 시그니처와 동작을 유지하고, 내부만 이 3개를 조합하도록 바꿨다(근접 노드 무변경).
- `AI/BT/AIRangedAttackAction.cs` (신규, "원거리 공격" 노드): 대기 → 예비 동작 → 발사를 반복한다.
  - 대기: `MinInterval`~`MaxInterval` 사이 랜덤, 대상을 바라본다.
  - 예비 동작: `WindupDuration` 고정. 시작 순간의 수평 대상 방향으로 조준을 고정하고, 그 방향만 바라본다. `AttackWindup` 스킬을 켠다.
  - 발사: 조준선을 끄고 `Attack` 스킬을 발동한다.
  - `OnEnd`에서 예비 동작 중이었으면 조준선만 끄고 쏘지 않는다. 사거리 이탈이나 Guard 중단(사선 차단) 시 이 경로로 취소된다.
- `Skill/Skills/EnemyAimLine.cs` (신규 스킬): Execute로 켜고 Stop으로 끄는 LineRenderer 조준선.
  - 발사 지점 정면으로 그리고, `_obstacleLayers`에 닿으면 거기서 끊는다.

## 2. 결정 사항
- 타이밍은 BT 노드가 갖는다(intent-013 "행동 파라미터는 BT 노드" 규칙).
- 조준선은 스킬(`AttackWindup`)로 만들어 기존 스킬 요청 경로를 쓴다. 새 중재자 경로는 없다.
- 조준 고정은 "몸을 고정 방향으로 붙잡기"로 구현했다. 조준선과 총알이 모두 `_firePoint.forward`를 쓰므로 선이 곧 탄도다. `EnemyRangedAttack`과 `BulletController`는 무변경.
- 플레이어 이동 예측 조준(리드샷)은 금지한다.

## 3. 현재 상태 및 이슈
- Unity 컴파일 0에러(새 파일 경고 없음). **Play 검증은 아직 안 했다.**
- [USER] 에디터 작업이 남아 있다(아래 Next Steps).
- `RangedAttack`의 `CooldownCost`(1초)는 `MinInterval + WindupDuration`(기본 2.1초)보다 짧아야 한다. 지금 값은 문제없다.
- 기존 `Assets/Test/AI/Ranged_T.prefab`에 Missing Nested Prefab 에러가 콘솔에 있다(이번 작업과 무관, Git 미추적 폴더).

## 4. 다음 할 일 (Next Steps)
1. [USER] `Ranged.prefab`에 자식 `AttackWindup`을 추가한다.
   - `EnemyAimLine`: Skill Name `AttackWindup`, Fire Point = `FirePosition`, Obstacle Layers = 벽/지형
   - `LineRenderer`: 얇은 폭, 붉은 Unlit 머티리얼, Use World Space
2. [USER] `EnemyAI_Ranged` 그래프에서 공격 노드를 "원거리 공격"으로 바꾸고, 기존 Guard(사거리 AND 사선) 아래에 둔다. `Range`는 기존 블랙보드 변수에 연결한다.
3. Play 검증 항목:
   - 사격 간격이 매번 다른지
   - 조준선을 따라 총알이 나가는지
   - 예비 동작 중 적이 회전하지 않는지
   - 옆으로 피하면 회피되는지
   - 엄폐하면 취소되는지
   - 근접 적의 동작이 그대로인지
4. intent-014의 열린 질문(피격 시 취소, 동시 예비 동작 제한, 회피 수치)을 정리한다.
