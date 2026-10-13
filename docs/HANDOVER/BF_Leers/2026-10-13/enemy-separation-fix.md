# 적 간격 벌리기 2차 수정 (자리만 바뀜 / 원거리 안 쏨)

관련 intent: [intent-022](../../../intent/intent-022-enemy-separation.md) (`open`), 이전 기록: [enemy-separation.md](./enemy-separation.md)

## 1. 작업 요약 및 방법
사용자 Play 피드백 두 가지: (1) 뭉친 적이 퍼지지 않고 자리만 바꾼다, (2) 원거리 적이 쏘지 않는다(근거리는 공격함).

- **원인 1-a (근거리)**: `Melee.prefab`의 `_body`는 자식 `Body`이고, Enemy 레이어 `CharacterController`는 루트에 있다. 그래서 `other.IsChildOf(_body)`로는 자기 루트가 빠지지 않았다. 혼자서도 "동료 있음"이 됐고, 멀어질 방향에 루트→Body 오프셋(약 0.22m, 가중치 약 0.85)이 섞였다.
- **원인 1-b (공통)**: 붙은 둘이 같은 틱에 대칭으로 피해 서로를 지나쳤다. 후보 지점이 다른 동료 옆인지도 검사하지 않았다.
- **원인 2 (원거리)**: 공격은 대기 1.5~3초 + 조준 0.6초인데 Guard(Abort Lower Priority)가 1초마다 끊어서 매번 대기부터 다시 시작했다.
- **수정**
  - `AIController.TryGetAwayFromAllies`: 맞은 콜라이더에서 `GetComponentInParent<AIController>()`를 찾아 자기(`this`)를 뺀다. **나보다 InstanceID가 작은 동료만** 피한다(ID가 가장 작은 적은 제자리, 나머지가 비킴).
  - `AIController.HasAllyNear(point, radius)` 신규: 지점 근처에 자기를 뺀 동료가 있는지 묻는다.
  - `SpreadPointFinder.TryFind`에 `Func<Vector3, bool> isOccupied` 인자를 추가했다. NavMesh로 당긴 후보에 동료가 있으면 버린다. `AISpreadOutAction`이 `HasAllyNear`를 넘긴다.
  - `AIRangedAttackAction`: 한 발 쏘면 `Success`로 끝난다(`UpdateWindup`이 `bool` 반환). 다음 대기는 재시작 시 `OnStart → BeginWait`가 정하므로 간격은 같다.
- **검증**: 컴파일 0에러. 에디트 모드에서 Melee 프리팹 2개를 임시 생성해 확인했다(확인 후 삭제). 혼자일 때 `TryGetAwayFromAllies` = false. 0.5m로 붙였을 때 ID가 큰 쪽만 true이고 방향은 상대 반대쪽. `HasAllyNear`는 근처 true, 3m 밖 false.

## 2. 결정 사항
- 붙은 무리에서는 InstanceID가 가장 작은 적이 고정점이다. 동시 판단으로 교차하는 것을 막는 가장 단순한 방법이다.
- 원거리는 "사격과 사격 사이"에만 간격을 벌린다(공격 노드가 한 발마다 끝나고, Guard는 Abort None). 근거리는 즉시 발동이라 Lower Priority를 유지한다.
- 동료 판정은 AIController가 붙은 오브젝트만 센다(Enemy 레이어라도 AIController가 없으면 무시한다).

## 3. 현재 상태 및 이슈
- **[USER] 그래프 수정 필요 (아직 안 함)**:
  - `EnemyAI_Ranged`: 간격 벌리기 Conditional Guard의 Abort를 **Lower Priority → None**으로 바꾼다.
  - `EnemyAI_Melee`, `EnemyAI_Ranged`: 블랙보드 `SpreadRadius`/`SpreadDistance`/`SpreadReadyTime`의 **Shared를 모두 해제**한다. 지금은 셋 다 Shared이고, Ranged 런타임 값은 공유 에셋을 참조한다(직렬화 값 0).
- Play 검증은 아직 안 했다.

## 4. 다음 할 일 (Next Steps)
1. [USER] 위 그래프 수정 후 저장.
2. Play: 근거리 3마리를 0.5m 간격으로 플레이어 옆에 생성하고 `MoveSpeed`를 넣는다. 10초 동안 서로 지나치지 않고 쌍별 거리가 1.5m 쪽으로 커지는지, 사거리 안에서 계속 공격하는지 확인한다.
3. Play: 원거리 2마리를 1m 간격으로 생성해 둘 다 조준선 → 사격이 이어지는지, 사격 사이에 벌어지는지 확인한다.
4. 원거리 1마리의 발사 간격(1.5~3초 + 0.6초)과 후퇴 끊기가 이전과 같은지 회귀 확인한다.
5. 반경·거리 조정 후 intent-022 resolved 처리.
