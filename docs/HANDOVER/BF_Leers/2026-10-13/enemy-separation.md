# 적 간격 벌리기 (BT 가지) + 적 레이어 Enemy 이동

관련 intent: [intent-022](../../../intent/intent-022-enemy-separation.md) (`open`)

## 1. 작업 요약 및 방법
- **1차 시도(폐기)**: `AIController.Update`에서 경로 방향에 동료 반대 방향을 섞어 매 프레임 조향했다. Play 측정(근거리 4마리, 0.9m 간격 출발, 같은 목적지, BT 끔)에서 이동 중 최소 간격은 1.2m 이상이었고, 보정을 끄면 0.13m였다. 하지만 공격할 때는 멈춰 서므로 보정이 적용되지 않아 공격 위치에서 완전히 뭉쳤다(사용자 확인). 그래서 조향 코드는 전부 지웠다.
- **최종(BT)**:
  - `AIController.TryGetAwayFromAllies(radius, out direction)` — 반경 안 동료(`_separationLayers`)에게서 멀어지는 수평 방향을 구한다. 가까울수록 크게 반영하고, 자기 자신(`_body` 및 자식)은 제외한다.
  - `SpreadPointFinder.TryFind(...)`(신규) — 동료 반대 방향 기준 0, ±30, ±60, ±90, ±120° 후보 중 처음 통과한 지점을 쓴다. 대상이 보이면 대상과 `Range` 이내이고 `NavMesh.Raycast`로 대상까지 트인 지점만 쓴다. 도달 가능 판정은 `RetreatPointFinder.IsReachable`을 `internal`로 바꿔 재사용했다. `_path` 버퍼를 그 안에서도 만들도록 한 줄 추가했다.
  - BT 조건 `AIHasNearbyAllyCondition`("주변에 동료가 있다"), 액션 `AISpreadOutAction`("간격 벌리기") 신규.
- **레이어**: `Ranged.prefab`/`Melee.prefab` 루트를 Obstacle(7) → Enemy(8)로 옮기고, AIController `_separationLayers` = Enemy로 지정했다. 플레이어 `Bullet.prefab`의 `_hitLayers`를 128 → 384(Obstacle + Enemy)로 바꿨다. **이것을 안 바꾸면 플레이어 총알이 적에게 안 맞는다.**
- 프리팹은 `PrefabUtility.LoadPrefabContents`/`SaveAsPrefabAsset`(MCP eval)로 수정했다.

## 2. 결정 사항
- 간격 유지는 BT 가지로 처리한다. "공격 중 어디에 서 있을지"는 판단 문제라서 매 프레임 조향으로는 해결되지 않는다.
- **Abort Lower Priority 루프 방지**: Guard는 조건이 참이고 자기가 실행 중이 아니면 매 틱 아래 가지를 끊는다(`BehaviorGraphModule.cs`). 그래서 액션이 끝날 때마다(성공·실패·중단) 블랙보드 `SpreadReadyTime`을 1초 뒤로 미루고, 조건은 그 시각 전까지 거짓을 반환한다. **두 노드에 같은 블랙보드 변수를 연결해야 한다.**
- BT 노드는 AIController 공개 API에만 묻는다는 기존 규칙을 따랐다(감지는 AIController, 지점 계산은 정적 Finder).
- `Sensor._obstacleLayers`·`AttackWindup._obstacleLayers`(Obstacle)는 그대로 둔다. 레이어 이동으로 동료 적이 시야·조준선을 가리지 않게 된 것은 의도한 개선이다(적 총알은 원래 동료를 통과함).
- `GC.prefab`은 범위 밖이다(Obstacle 유지, `_separationLayers` 0).

## 3. 현재 상태 및 이슈
- 컴파일 0에러. **그래프 배선([USER])과 BT 방식 Play 검증은 아직 안 했다.**
- **[USER] 그래프 배선** — 블랙보드에 float `SpreadRadius`(1.5), `SpreadDistance`(2), `SpreadReadyTime`(0)을 추가한다:
  ```
  EnemyAI_Melee  Try In Order
   0. Conditional Guard (Abort Lower Priority)
        조건: 동료가 [SpreadRadius] 안에 있다   (ReadyTime = SpreadReadyTime)
        → 간격 벌리기 (Radius = SpreadRadius, MoveDistance = SpreadDistance,
                        Range = AttackRange, ReadyTime = SpreadReadyTime)
   1~. (기존) 공격 / 추격 / 수색 / 순찰

  EnemyAI_Ranged  Try In Order
   1. (기존) 후퇴
   2. 위와 같은 간격 벌리기 Guard   ← 후퇴 다음, 공격 앞
   3~. (기존) 공격 / 추격 / 수색 / 순찰
  ```
  - `SpreadReadyTime`은 **Shared를 체크하지 않는다.** Shared 변수는 모든 적이 값 하나를 함께 써서, 한 적이 퍼질 때마다 모든 적이 1초 동안 못 퍼진다(`com.unity.behavior` `Blackboard.cs`: Shared가 아닌 변수만 적마다 복제).
- 프리팹 재저장 때 Unity가 함께 정리한 내용이 있다. `Melee.prefab`에서 코드에서 이미 지운 Sensor 옛 필드(`_detectTimeNear`/`_detectTimeFar`/`_awarenessDecayTime`)가 빠졌다. 기본값 필드(`_patrolRoute`, `_destinationTimeout`, `_animationMediator`)는 새로 기록됐다. 동작에는 영향이 없다.
- 1차 시도 때 프리팹에 기록된 `_separationRadius`/`_separationWeight` 값은 코드에서 사라졌으므로 다음 저장 때 지워진다(무해).
- `Ranged.prefab`의 `_sightRange` 30 / `_sightAngle` 150은 이번 작업 전부터 있던 미커밋 변경이다.
- **팀원 공유 필요**: `Bullet.prefab`(플레이어 총알) `_hitLayers` 변경, 적 레이어가 Enemy로 바뀐 것.
- 테스트 중 알게 된 점: 적 프리팹을 직접 Instantiate하면 `MoveSpeed`가 0이라 움직이지 않는다. 스포너처럼 `CharacterMediator.SetAttribute("MoveSpeed", v)`를 넣어야 한다.

## 4. 다음 할 일 (Next Steps)
1. [USER] 위 그래프 배선.
2. Play 검증: 근거리 적 3마리를 플레이어 옆에 붙여 생성하고 `MoveSpeed`를 넣는다. 5~10초 뒤 적끼리 최소 거리가 약 1.2m 이상이고 모두 사거리 2m 안에서 공격 중인지 확인한다.
3. 원거리 적 2마리를 붙여 두고, 예비 동작(조준선)이 매 틱 끊기지 않고 사격이 이어지는지 확인한다(ReadyTime 루프 방지).
4. 플레이어 총알이 Enemy 레이어 적에게 맞는지 회귀 확인.
5. 반경·이동 거리 조정 후 intent-022 resolved 처리.
