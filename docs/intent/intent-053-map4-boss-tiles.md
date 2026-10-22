---
id: intent-053
title: Map_4_Security 보스 바닥 25칸을 보스 아레나 칸으로 연결
part: AI / Boss / Arena
status: open   # open | resolved
created: 2026-10-21
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
보스전은 테스트 씬(`Assets/Test/Boss/Map_4_Security_Boss_T`)에만 설정되어 있다. 실제 맵 `Map_4_Security`에는 맵 담당이 `Stage_BossRun/05_Boss`에 바닥 `BossFloor_Tile_R01_C01 ~ R05_C05`(12 m, 5×5)를 배치해 두었지만, `BossArena`·`BossTile`이 없어 칸 붕괴·예고가 동작하지 않는다.

## 기대 결과 (Proposed outcome)
`Map_4_Security`에 `BossArena`가 생기고, 25칸이 `BossTile`로 연결된다(칸 번호 = (R-1) × 5 + (C-1), R은 +Z, C는 +X 방향).
칸 예고는 맵 담당이 만든 각 칸의 `BossFloor_SignalRail/WarningSignals`를 그대로 사용한다.

2차(같은 날): 보스 스포너와 NavMesh 추가.
- 아레나 입구 직전 마지막 발판(`Platform_5`)에 `Checkpoint_Round3_Start`를 추가하고 `StageManager` 라운드를 [0,1,2]로 늘려, 3라운드(index 2)에 보스 A·B 생성(사용자 결정).
- `05_Boss/BossFight`에 `BossFightSpawner`·`BossEncounter`와 생성점(A: 아레나 +X 끝에서 26 m 뒤, B: +X 끝 칸 위)을 테스트 씬과 같은 상대 위치로 배치.
- BossB 프리팹이 쓰는 에이전트 타입(ID -1372625422)이 ProjectSettings에서 빠져 있어, 테스트 씬 NavMesh 데이터에 남은 값(반지름 1.2·높이 3.4·경사 45·등반 0.3)으로 `Boss` 타입 복원(사용자 결정). `05_Boss`에 NavMeshSurface(PhysicsColliders) 추가·베이크.
- NavMesh를 아레나 크기와 같게: 반지름 1.2는 유지하고, 아레나 바깥에 베이크 전용 테두리 `05_Boss/BossNavMeshBorder`(기본 꺼짐)를 켜고 구워 가장자리 깎임을 보정(사용자 결정). 다시 구울 때도 테두리를 켜고 굽는다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보스전 플레이어, 맵 담당
- 어떤 시스템/모듈이 건드려지는가: `BossTile.cs`(예고 채움 기준점 보정, 3차), `Map_4_Security.unity`(`BossArena` 오브젝트 추가, 타일 25개에 `BossTile` 추가, 붕괴용 `BossHole_00~24` 추가, `BossFight`·`Checkpoint_Round3_Start`·`05_Boss` NavMeshSurface 추가, `StageManager` 라운드 3개), `ProjectSettings/NavMeshAreas.asset`(에이전트 타입 `Boss` 복원), `Map_4_Security/NavMesh-05_Boss.asset`(신규).

## 제약 (Constraints)
- 지켜야 할 정책: 보스 타일 관련만 변경하고, 스포너·체크포인트·NavMesh 등 다른 설정은 건드리지 않는다(사용자 지시).
- 쓰지 말아야 할 것: 예고 코드·머티리얼·프리팹 수정 금지(사용자 지시). `BossArenaSetupWindow` 일괄 설정은 예고 오브젝트를 옮기고 `M_BossTelegraph`로 바꾸므로 쓰지 않는다.

## 열린 질문 (Open questions)
- ~~`WarningSignals` 기준점·평면 문제~~ → 해결(2026-10-21, 사용자 결정 "가운데서 시작해 가장자리까지"): `BossTile`이 예고 메시 bounds 중심을 채움 기준점으로, 가장 얇은 축을 뺀 두 축을 채움 축으로 써서 위치를 보정한다. 메시·씬 수정 없음.
- `BossArena`의 `_circlePrefab`/`_linePrefab`(B 도약 원·A4/B 돌진 직선 예고)은 사용자 지시에 따라 비워 두었다. 연결 여부를 정해야 한다. (2026-10-21 재확인: 씬에 아직 비어 있음. `ShowCircle` 을 쓰는 어깨포 예고 원, `ShowLine` 을 쓰는 A4 그랩 직선 예고가 이 맵에서는 안 보인다.)
- `SafeSignals`는 붕괴 때 꺼지는 렌더러로만 연결했다. 예고 중 안전 표시를 끌지는 정하지 않았다.
- ~~`Platform_5`(x≈306)와 아레나(x≈322.6) 사이 16 m에 바닥이 없다.~~ → 사용자 육안 확인에서 문제없음(2026-10-21).
- ~~낙사 구역(`BossArenaFallZone`)은 아직 배치하지 않았다.~~ → 배치 완료(`Stage_BossRun/BossArenaFallZone`), 사용자 육안 확인 통과(2026-10-21).

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
