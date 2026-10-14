# 조준선 FirePoint 대상 조준 고정 (2026-10-13)

## 1. 작업 요약 및 방법
- 문제: 원거리 적의 조준선과 총알은 FirePosition 정면으로 나간다. 몸통은 수평으로만 돌고 FirePosition은 오른쪽 총구(local x -0.755)에 있어서, 선이 플레이어 옆을 평행하게 지나가고 높이 차도 반영되지 않았다.
- 수정: `Skill/Skills/EnemyAimLine.cs` 한 파일만 바꿨다.
  - `Awake`: `GetComponentInParent<AIController>()`로 대상 공급원을 찾아 둔다.
  - `Execute`: 발동 순간 `VisibleTarget`의 `Collider.bounds.center`(Collider가 없으면 `position`)를 향하는 방향을 `_aimDirection`에 한 번 저장한다. 대상이 없으면 현재 `forward`를 쓴다.
  - `LateUpdate`: 조준 중 매 프레임 `_firePoint.rotation = LookRotation(_aimDirection)`을 다시 넣는다. 몸통이 돌아도 월드 기준 방향은 고정된다.
  - `Stop`: 회전을 되돌리지 않는다. `AIRangedAttackAction`이 같은 프레임에 Windup Stop → Attack Execute를 호출하므로, 총알(`EnemyRangedAttack`, 같은 FirePosition)이 고정된 방향으로 나간다.
- intent-014의 영향 범위에 이 변경을 덧붙였다.

## 2. 결정 사항
- FirePoint를 Player Transform으로 교체하지 않는다. 선이 플레이어 몸에서 시작하게 되고, 프리팹에서는 씬 오브젝트를 참조할 수도 없다.
- 조준 방향은 예비 동작을 시작하는 순간에 고정한다. 조준 중에 플레이어를 따라가면 intent-014의 "선을 보고 움직이면 피할 수 있다"가 깨진다.
- 대상은 Sensor가 이미 찾은 것(`AIController.VisibleTarget`)을 재사용한다. 별도의 Player 검색은 없다.

## 3. 현재 상태 및 이슈
- 컴파일 에러 0. **Play 검증은 아직 안 했다.**
- 프리팹·BT 그래프는 수정하지 않았다(두 스킬이 이미 같은 FirePosition을 참조하고 있음을 확인했다).
- 사격 프레임에 몸통이 아직 덜 돌았다면, 마지막 LateUpdate 이후 부모가 회전한 만큼 총알 방향이 아주 조금 어긋날 수 있다(예비 동작 시간 동안 수렴하므로 실제로는 무시할 수준으로 예상된다).

## 4. 다음 할 일
- Ranged 적이 있는 씬에서 Play로 확인한다.
  - 선이 플레이어 몸통 중심을 지나는지, 높이 차가 있어도 맞는지
  - 선이 켜진 뒤 플레이어가 움직여도 선이 따라오지 않는지
  - 총알이 선을 따라가는지, 선이 켜진 순간 비켜 서면 피해지는지
