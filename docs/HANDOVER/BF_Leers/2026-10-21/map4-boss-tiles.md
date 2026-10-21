# Map_4_Security 보스 타일 연결 (intent-053)

## 1. 작업 요약 및 방법
- 사용자 지시: `Map_4_Security`에 보스를 설정하되 우선 `BossFloor_Tile_R`로 시작하는 보스 타일 관련만 변경하고, 다른 설정과 예고는 건드리지 않는다.
- 대상: `Stage_BossRun/05_Boss/BossFloor_Tile_R01_C01 ~ R05_C05` 25개(프리팹 인스턴스, 칸 12 m, 회전 0).
- Unity MCP `eval`로 직접 설정했다. 실행자 예외 사유: 씬 하나에서 참조만 연결하는 작은 범위이고, 사용자와 질문을 주고받으며 바로 진행했다. `BossArenaSetupWindow.Configure`는 예고 오브젝트를 옮기고 머티리얼을 `M_BossTelegraph`로 바꾸므로 쓰지 않고, 같은 규칙으로 참조만 연결했다.
  - `Stage_BossRun/BossArena` 새로 만듦(부모 스케일 2를 상쇄하는 localScale 0.5로 월드 스케일 1, 원점 (322.64, 0, -20) = R01_C01 바깥 모서리·바닥 Collider 윗면). `_tileSize` 12.
  - 각 타일에 `BossTile` 추가: `_renderers` = 바닥·`Frame`·`SafeSignals`, `_colliders` = 바닥 BoxCollider, `_telegraph` = `BossFloor_SignalRail/WarningSignals`(맵 담당이 만든 것, 위치·머티리얼·꺼짐 상태 그대로), `_holeObstacle` = `BossArena/BossHole_XX`(12×4×12 carve, 기본 꺼짐).
  - `_tiles` 순서 = (R-1) × 5 + (C-1). R은 +Z, C는 +X 방향이며 25칸 모두 이름과 좌표가 일치함을 검사했다.
- 사용자 승인으로 씬을 저장했다. **저장 전부터 씬에 미저장 변경이 있었고 함께 저장되었다**(diff에 `SpaceStation_Light_A_LWRP` 이름·레이어 변경 등).

## 2. 결정 사항
- 실제 맵의 칸 예고는 새 쿼드를 만들지 않고 맵 담당의 `WarningSignals`를 사용한다(사용자 결정).
- `_circlePrefab`/`_linePrefab`은 비워 두었다(예고 미수정 지시).

## 3. 현재 상태 및 이슈
- eval 검사: 25칸 참조·칸 중심 좌표·예고 꺼짐·구멍 꺼짐 모두 정상. 콘솔 에러 1건은 `BossArena`를 붙이는 순간(칸 연결 전) `OnValidate`가 낸 것이다.
- **Play·육안 확인·게이트 루프 미실행, 커밋 전.**
- `WarningSignals` 기준점이 칸 모서리이고 판이 XZ 평면이라, `BossTile.ApplyFill`의 가운데 채우기가 한쪽 끝에서 X 방향으로만 늘어나는 모양일 것으로 예상된다(미확인).
- 원/직선 예고 프리팹이 비어 있어 B 도약 원·직선 예고는 이 씬에서 보이지 않는다.
- 보스 스포너·인카운터·체크포인트·낙사 구역·NavMesh 베이크는 설정하지 않았다(범위 밖).

## 4. 다음 할 일 (Next Steps)
- Play에서 칸 예고(`WarningSignals`) 채우기 모양 확인 후, 기준점/코드 중 무엇을 바꿀지 사용자와 결정.
- 원/직선 예고 프리팹 연결 여부 결정.
- 보스 스포너·인카운터 등 나머지 보스 설정을 `Map_4_Security`로 옮기기.
