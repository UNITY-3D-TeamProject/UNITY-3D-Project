# 스테이지2 특수몹 HP·에너지 순차 드롭 (2026-10-29)

## 1. 작업 요약 및 방법
- 요청: AI_TODO2 [스테이지2-3] — 스테이지2 특수몹 아이템 드롭을 HP → 에너지 순서로 (intent-085).
- 사용자 결정: 발판 순서대로 HP·에너지 **번갈아**, **항상 드롭**, **발판 번호 기준**(못 깬 발판이 리셋돼도 같은 발판은 같은 아이템).
- 구현:
  - `CombatZone` 에 선택 항목 `_clearDropItem`(+ `_clearDropHeight` 0.5, `_clearDropLifetime` 15) 추가. 구역의 마지막 적이 죽으면(`HandleEnemyDeath` → `DropClearItem`) 그 자리 0.5 m 위에 `Instantiate`, 수명 뒤 `Destroy`. 비우면 지금과 같음(1라운드 몹 방 4곳은 비어 있음).
  - `Map_4_Security` 특수몹 발판 구역 5곳(`SecurityApp._specialMobRound._steps` 순서)에 `Item_HealHp` / `Item_ChargeBattery` / `Item_HealHp` / `Item_ChargeBattery` / `Item_HealHp` 지정 (MCP `SerializedObject`).
  - `Special.prefab` 에서 `EnemyItemDropper` 제거 (25% 무작위 드롭과 겹치지 않게). Melee·Ranged 는 그대로.
- 실행자: Claude 직접(Codex CLI 미설치).

## 2. 결정 사항
- 순서는 런타임 카운터가 아니라 **구역(발판)마다 아이템을 지정**하는 데이터로 표현한다. 리셋·재시작 처리가 따로 필요 없다.
- "구역을 깬 보상"은 `CombatZone` 이 맡는다. 잡몹 개별 무작위 드롭은 `EnemyItemDropper` 가 맡는다.

## 3. 현재 상태 및 이슈
- 리컴파일 에러 0·경고 0 (`gate.cjs check recompile`).
- Play(`Map_4_Security`)에서 특수몹 `CharacterCombat.Health = 0` 으로 발판 1~5 를 차례로 처치: HP → 에너지 → HP → 에너지 → HP, 마리당 1개, 발 위치 +0.5 m. 2번 발판은 `ResetUnclearedSteps` 를 한 번 거친 뒤 처치했고 리셋 때 추가 드롭 없음. 새 에러 없음(기존 `Ranged(Clone)` 풀 생성 Assert 90건은 무관).
- 씬 저장 시 `Canvas_InGame` 프리팹 인스턴스의 RectTransform 오버라이드 값(앵커·크기·위치, 0 → 실제 값)이 에디터에서 다시 계산돼 함께 바뀜. 이번 작업과 무관하며 YAML 직접 수정 금지라 그대로 둠 — 커밋 때 포함 여부 결정 필요.
- 특수몹이 낙사 등으로 발판 밖에서 죽으면 아이템도 그 자리(허공)에 생김 — 확인하지 않음.
- **직접 플레이(실제 처치·획득) 육안 확인, 게이트 루프 미실행, 커밋 전.**

## 4. 다음 할 일
- 사용자 직접 플레이: 2라운드 특수몹을 잡았을 때 HP·에너지가 번갈아 떨어지고 획득되는지.
- 확인 후 intent-085 resolved 처리, AI_TODO2 [스테이지2-3] 체크.
- 다음 권장 순서: [연출-1] 0·1 개별 낙하 흐름 조정.
