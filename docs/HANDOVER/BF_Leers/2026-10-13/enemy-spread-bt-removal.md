# BT 간격 벌리기 제거 (올라타기 방지만 유지)

관련 intent: [intent-022](../../../intent/intent-022-enemy-separation.md) (`open`, "4차" 섹션), 이전 기록: [enemy-no-stacking.md](./enemy-no-stacking.md)

## 1. 작업 요약 및 방법
- 사용자 요청으로 BT에서 동료를 찾는 간격 벌리기 코드를 전부 지웠다. 그래프에서는 노드가 이미 빠진 상태였다.
- **삭제한 파일**(미추적이라 파일 삭제로 끝): `AI/BT/AIHasNearbyAllyCondition.cs`, `AI/BT/AISpreadOutAction.cs`, `AI/SpreadPointFinder.cs` (+ `.meta`)
- **`AIController`**: `HasAllyNear`만 삭제. `TryGetAwayFromAllies`, `_separationLayers`, `_separationHits`, `MAX_SEPARATION_NEIGHBORS`는 `AllySeparationPusher`가 쓰므로 남겼다.
- **`git checkout`으로 원복**: `RetreatPointFinder.cs`(`IsReachable` private 복귀), `AIRangedAttackAction.cs`(대기 → 조준 → 발사 반복 방식 복귀). 두 파일 모두 이 작업 외 다른 변경은 없었다.
- **에셋 정리 (MCP eval)**:
  - `EnemyAI_Ranged.asset`: 작성용 블랙보드와 런타임 그래프의 블랙보드 복사본 양쪽에서 `SpreadRadius`/`SpreadDistance`/`SpreadReadyTime` 제거. 작성용 블랙보드만 지우고 `RebuildAndSave`해도 런타임 그래프 쪽 복사본은 남아 있어서 따로 지웠다.
  - `Ranged.prefab`: `BehaviorGraphAgent`의 `SpreadRadius` 오버라이드 제거. 리스트(`m_BlackboardVariableOverridesList`)만 지우면 딕셔너리(`m_BlackboardOverrides`)에서 다시 직렬화되므로 둘 다 지웠다.
- **검증**: 재컴파일 에러 0(새 경고 없음). `Assets/_Project`, `ProjectSettings`에서 `Spread`/`HasAllyNear`/`AIHasNearbyAlly` 검색 결과 0건. 두 프리팹에 `AllySeparationPusher`가 그대로 붙어 있다.

## 2. 결정 사항
- 적끼리 겹치지 않게 하는 처리는 **Enemy ↔ Enemy 충돌 해제 + `AllySeparationPusher`** 하나만 둔다. BT로 설 자리를 고르는 처리는 하지 않는다.
- 원거리 공격 노드는 다시 노드 안에서 사격을 반복한다.

## 3. 현재 상태 및 이슈
- Play 검증은 하지 않았다. 특히 원거리 공격 반복 방식이 복귀된 뒤 정상적으로 쏘는지 확인이 필요하다.
- 이전 기록에 있던 [USER] 작업(Ranged Guard Abort None, Spread 변수 Shared 해제)은 해당 가지가 사라져 더 이상 필요 없다.
- 이전 기록의 남은 항목(충돌표 변경 팀 공지, `Ranged` CC 크기 확인)은 그대로 남아 있다.

## 4. 다음 할 일 (Next Steps)
1. Play: 원거리 적이 사거리 안에서 계속 쏘는지, 근거리 여러 마리가 Pusher만으로 올라타거나 겹치지 않는지 확인한다.
2. 팀원에게 Enemy ↔ Enemy 충돌 해제 공지, `Ranged` CC 크기 확인.
3. 확인 후 intent-022 resolved 처리.
