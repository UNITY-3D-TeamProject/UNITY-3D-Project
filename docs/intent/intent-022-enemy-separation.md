---
id: intent-022
title: 적끼리 뭉치지 않도록 간격 벌리기 (BT 가지)
part: AI / Enemy
status: open   # open | resolved
created: 2026-10-13
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
근거리·원거리 적이 같은 목표(플레이어, 마지막 목격 위치)를 쫓으면 한 덩어리로 뭉쳐 다니고, 공격할 때는 플레이어 앞 같은 자리에 겹쳐 선다.
적끼리 서로를 피하는 처리가 전혀 없다(`AIController`는 NavMeshAgent의 `desiredVelocity`, 즉 회피가 반영되지 않은 경로 방향을 그대로 쓴다).

또한 적 프리팹(`Ranged`, `Melee`) 루트가 `Enemy`(8)가 아니라 `Obstacle`(7) 레이어에 있어, "Enemy 레이어끼리"를 찾는 것 자체가 불가능했다.

## 기대 결과 (Proposed outcome)
- 반경 안에 동료(Enemy 레이어)가 있으면 BT가 동료 반대쪽의 새 자리를 골라 이동한다. 다른 적을 직접 밀지 않는다.
- 대상이 보이는 중이면 **공격 사거리 안이고 대상까지 트인** 자리만 고르므로, 플레이어 주위로 퍼진 뒤 다시 공격한다.
- 최소 간격만 맞춘다(멀어지면 다시 모이는 처리는 하지 않는다).
- 적 프리팹 루트 레이어를 `Enemy`로 옮긴다.

## 1차 시도와 폐기 (AIController 조향)
- `AIController.Update`에서 경로 방향에 동료 반대 방향을 섞어 매 프레임 조향하도록 했다(이동 중에만 적용).
- Play 측정(근거리 4마리, 0.9m 간격 출발, 같은 목적지): 보정을 켜면 이동 중 최소 간격 1.2m 이상, 끄면 도착 후 0.13m.
- 하지만 **공격할 때는 멈춰 서므로 보정이 적용되지 않아** 공격 위치에서 완전히 뭉쳤다(사용자 확인). 정지 상태에서 "어디에 서 있을지"를 다시 정하는 것은 판단 문제라서 BT로 옮겼다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: AI 담당(BF_Leers), 플레이어 사격(총알 판정 레이어)
- 어떤 시스템/모듈이 건드려지는가:
  - `AIController` — 감지 API `TryGetAwayFromAllies(radius, out direction)` + `_separationLayers`. 조향 코드는 없다.
  - `SpreadPointFinder`(신규) — 동료 반대쪽 후보 지점 계산. 도달 가능 판정은 `RetreatPointFinder.IsReachable`(internal로 변경)을 재사용한다.
  - BT 노드(신규) — 조건 `AIHasNearbyAllyCondition`, 액션 `AISpreadOutAction`
  - `EnemyAI_Melee`, `EnemyAI_Ranged` 그래프 — 간격 벌리기 가지 추가([USER])
  - `Ranged.prefab`, `Melee.prefab` — 루트 레이어 Obstacle → Enemy, `_separationLayers` = Enemy
  - `Bullet.prefab`(플레이어 총알) — `_hitLayers` Obstacle → Obstacle + Enemy (안 바꾸면 적에게 안 맞음)
  - 부수 효과: `Sensor._obstacleLayers`·`AttackWindup._obstacleLayers`(Obstacle)에서 동료 적이 빠진다 → 동료가 시야·조준선을 가리지 않게 된다(적 총알은 원래 동료를 통과하므로 일치)
  - `GC.prefab`은 범위 밖(레이어 유지, `_separationLayers` 기본값 0이라 영향 없음)

## 제약 (Constraints)
- `Conditional Guard`의 Abort Lower Priority는 Guard가 실행 중이 아니고 조건이 참이면 **매 틱** 아래 가지를 끊는다(`com.unity.behavior` `BehaviorGraphModule.cs`의 `ObserverAbortTarget.LowerPriority`). 그래서 갈 곳이 없어 실패해도 조건이 계속 참이면 공격(원거리 예비 동작)이 매 틱 취소된다.
  → 액션이 끝날 때마다(성공·실패·중단) 블랙보드 `SpreadReadyTime`을 1초 뒤로 미루고, 조건이 그 값을 본다. 두 노드에 **같은 블랙보드 변수**를 연결해야 한다.
  단, 그 변수는 **Shared로 두지 않는다**. Shared면 모든 적이 값 하나를 공유해서 한 적이 퍼질 때마다 모든 적이 1초간 못 퍼진다.
- 프리팹은 MCP로 수정한다(YAML 직접 편집 금지).

## 2차 수정 (Play 피드백: 퍼지지 않고 자리만 바뀜, 원거리가 안 쏨)
- **자기 제외 버그**: `Melee.prefab`의 `_body`는 자식 `Body`인데, Enemy 레이어 콜라이더(`CharacterController`)는 루트에 있다. `IsChildOf(_body)`로는 자기 루트가 빠지지 않아 혼자서도 "동료 있음"이 됐고, 멀어질 방향에 루트→Body 오프셋이 섞였다. → 맞은 콜라이더에서 `GetComponentInParent<AIController>()`를 찾아 `this`와 비교한다.
- **동시 판단으로 인한 교차**: 붙은 둘이 같은 틱에 서로를 피해 대칭으로 움직여 자리만 맞바꿨다. → `TryGetAwayFromAllies`는 나보다 InstanceID가 작은 동료만 피한다. 뭉친 무리에서 ID가 가장 작은 적은 제자리에 있고 나머지가 비킨다.
- **도착 지점 검사 없음**: 후보 지점이 다른 동료 옆이어도 통과했다. → `AIController.HasAllyNear(point, radius)`로 후보 근처에 동료(ID 무관)가 있으면 버린다(`SpreadPointFinder.TryFind`의 `isOccupied`).
- **원거리 사격 끊김**: 원거리 공격은 대기 1.5~3초 + 조준 0.6초라서 Guard가 1초마다 끊으면 한 발도 못 쏜다. → `AIRangedAttackAction`은 한 발 쏘면 Success로 끝나고, `EnemyAI_Ranged`의 간격 벌리기 Guard는 Abort **None**으로 둔다. 이러면 사격과 사격 사이(그래프가 다시 판단할 때)에만 간격을 벌린다. 근거리는 즉시 발동이라 Lower Priority를 유지한다.
- 두 그래프의 `SpreadRadius`/`SpreadDistance`/`SpreadReadyTime`이 Shared로 체크되어 있었다(Ranged 런타임 값은 공유 에셋을 참조). → Shared를 모두 해제한다([USER]).

## 3차: 올라타기 방지 (CharacterController끼리 올라탐)
- **원인**: CharacterController는 다른 적의 캡슐을 바닥으로 취급한다. 밀면 캡슐 윗부분 반구를 타고 올라가고, 올라간 뒤에는 `isGrounded`가 true라 `CharacterMotor`가 중력을 고정해 그대로 붙어 있는다. `Melee` CC(높이 1, 반지름 0.4)는 거의 구라서 특히 잘 올라간다.
- **A. 적끼리 충돌 끄기**: `DynamicsManager` 레이어 충돌표에서 Enemy(8) ↔ Enemy(8)를 해제했다. 물리 질의(Overlap/Raycast)는 충돌표와 무관해서 Sensor, 간격 벌리기, 총알 판정에는 영향이 없다.
- **B. 약하게 밀어내기**: 충돌이 없으면 서로 통과해 겹치므로 `AllySeparationPusher`(신규)가 `TryGetAwayFromAllies` 방향으로 `CharacterMotor.AddExternalDisplacement`를 매 프레임 준다. `AIController`(중재자를 통해서만 이동)와 `CharacterMotor`(플레이어와 같이 씀)는 건드리지 않으려고 별도 컴포넌트로 뒀다. 트리거 콜라이더 방식 대신 Overlap 질의를 재사용했다.
  - 프리팹 값: Melee `_pushRadius` 0.9, Ranged 3.0(CC 반지름 1.44), `_pushSpeed` 1.5.
- **가중치 버그 수정**: `TryGetAwayFromAllies`의 `1 - distance/radius`는 동료 중심이 반경 밖이고 콜라이더 표면만 걸릴 때 음수가 돼 방향이 동료 쪽으로 뒤집혔다 → `Mathf.Clamp01`로 0 이상으로 제한한다.
- **Play 측정**(Melee 4마리, 0.3m 간격으로 겹쳐 생성): 3초 후 최소 간격 1.04m. 같은 목적지로 몰았을 때도 최소 간격 1.00m, 4마리 모두 y가 바닥 높이(0.08)와 같아서 올라탄 개체가 없었다.
  - 수정 전과 비교하는 실험(Play 중 충돌을 다시 켬)은 이미 생성된 적끼리 충돌이 다시 걸리지 않아 재현에 실패했다.

## 4차: BT 간격 벌리기 폐기 (2026-10-13)
- 사용자 판단으로 BT 간격 벌리기 가지를 전부 걷어냈다. 겹침 방지는 3차의 **충돌 해제 + `AllySeparationPusher`만** 남긴다.
- 삭제: `AIHasNearbyAllyCondition`, `AISpreadOutAction`, `SpreadPointFinder`, `AIController.HasAllyNear`. `EnemyAI_Ranged` 블랙보드의 `SpreadRadius`/`SpreadDistance`/`SpreadReadyTime`과 `Ranged.prefab`의 `SpreadRadius` 오버라이드도 지웠다.
- 되돌림: `RetreatPointFinder.IsReachable` → private, `AIRangedAttackAction` → 대기·조준·발사 반복(한 발마다 Success로 끝내던 것은 간격 벌리기용이었다).
- 유지: `AIController.TryGetAwayFromAllies`·`_separationLayers`(Pusher가 씀), Enemy 레이어, Enemy ↔ Enemy 충돌 해제, `Bullet._hitLayers`.
- 위 "기대 결과"의 BT 항목, "제약"의 Guard/`SpreadReadyTime` 내용, "2차 수정"의 도착 지점 검사·원거리 사격 끊김 항목은 더 이상 적용되지 않는다(기록으로 남겨 둔다).

## 5차: 도둑 — 에이전트 회피로 인한 웨이포인트 정체 (2026-10-17)
- **증상(사용자 보고)**: 도둑 한 마리가 길에 서 있고, 뒤에 온 도둑이 못 지나가다가 겹쳐 붙고, 다른 도둑들도 끼어 한 덩어리가 된다.
- **전제 정정**: "문제" 절의 "`desiredVelocity`는 회피가 반영되지 않은 경로 방향"은 틀렸다. NavMeshAgent 회피(Obstacle Avoidance)가 켜져 있으면 `desiredVelocity`에 다른 에이전트 회피가 섞인다. Play에서 정체된 도둑의 `desiredVelocity` 방향이 경로 방향(`steeringTarget`)과 크게 달랐다(예: (0.47,-1.18) vs (-0.68,-0.74)).
- **원인(Play 관찰)**: 도둑 20마리가 웨이포인트 몇 곳을 나눠 쓴다. 같은 웨이포인트를 목적지로 잡은 도둑들이 서로를 회피하느라 도착 판정 거리(0.25m) 안으로 못 들어가고, 남은 거리 0.26~0.36m에서 맴돌았다. 몸끼리는 충돌이 꺼져 있어 겹치고, 맴도는 동안 조금씩은 움직여서 정체 판정(2초에 0.1m 미만)에도 잘 걸리지 않는다. 그 사이 같은 웨이포인트로 오는 도둑이 계속 합류해 **7마리가 0.4m 안에 겹친 덩어리**가 생겼다(스크린샷과 같은 모습). CC끼리 부딪혀 끼는 것이 아니다.
- **밀어내기 반경 부족**: `Thief.prefab` 루트 스케일이 2라서 CC 실제 반지름은 0.6, 두 몸이 닿는 거리는 1.2m다. 그런데 `_pushRadius`는 Melee 값 0.9 그대로라서 0.9~1.2m에서는 겹쳐 보여도 밀지 않았다.
- **수정(프리팹 값만, 코드 변경 없음)**: `Thief.prefab`
  - `NavMeshAgent` Obstacle Avoidance Type: High Quality → **None**. Agent는 경로 계산 전용이고, 겹침 처리는 `AllySeparationPusher`가 맡는다.
  - `AllySeparationPusher._pushRadius`: 0.9 → **1.4**(CC 실제 반지름 합 1.2보다 조금 크게).
- **Play 측정**(`Map_3_File 1`, 도둑 20마리, 순찰만, 새 Play마다 약 250초, 0.5초 간격 샘플):

  | | 수정 전 | 수정 후 |
  |---|---|---|
  | 1.2m 안에 동료가 있는 비율 | 13.1% | 5.0% |
  | 0.6m 안에 겹친 비율 | 5.9% | 2.6% |
  | 최대 덩어리 | 4마리(첫 실행에서는 7마리) | 3마리 |

  첫 실행에서 회피만 런타임으로 끄자 7마리 덩어리가 약 30초 안에 풀렸다. 수정 후 이동 중 0.3m/0.5초 미만으로 느려진 최대 연속 시간은 1.0초였다.
- **범위 밖**: `GC`·`Melee`·`Special`·`Ranged`도 회피가 High Quality이고, Melee·Special은 스케일 2 + `_pushRadius` 0.9로 같은 조건이다. 이번에는 바꾸지 않았다.

## 열린 질문 (Open questions)
- 밀어내기 반경·속도(Melee 0.9m / Ranged 3.0m, 1.5m/s)가 적당한지 → 실제 전투 Play 후 조정
- `Ranged` CC가 높이 2.88, 반지름 1.44인 구 형태라 모델보다 넓어 보인다. 의도한 값인지 확인 필요
- BT 없이 Pusher만으로 공격 위치에서 뭉침이 충분히 풀리는지 → Play 확인
- 다른 적 프리팹(Melee·Special·Ranged·GC)도 회피를 끌지(5차 참고) → 사용자 결정
- 도둑 도망 중(플레이어가 쫓을 때) 겹침이 남는지 → Play 확인 필요(5차 측정은 순찰만)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
