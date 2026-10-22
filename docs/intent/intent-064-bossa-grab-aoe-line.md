---
id: intent-064
title: 보스 A 그랩·어깨포, 보스 B 돌진·도약 예고를 DTT AOE(GrapWarnning·CircleWarnning)로 교체
part: AI / Boss / Arena
status: open   # open | resolved
created: 2026-10-22
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
- 그랩(A4) 조준 예고는 `BossArena.ShowLine` 이 `_linePrefab`(납작한 판)을 늘려서 그린다. 실제 맵 `Map_4_Security` 는 `_linePrefab`·`_circlePrefab` 이 비어 있어 그랩 직선·어깨포 원 예고가 아예 보이지 않는다.
- 사용자는 그랩 예고를 테스트 툴로 만든 DTT `LineRegion` 기반 AOE Line 3(화살촉 + 조준 시간 동안 채워짐)으로 바꾸고 싶다.
- `LineRegion` 은 기준점이 선 시작점이고 `Length`·`Width` 로 몸통·화살촉을 따로 계산하므로, 루트를 늘리는 지금 방식으로는 화살촉이 찌그러지고 위치가 밀린다.

## 기대 결과 (Proposed outcome)
- `_linePrefab` 에 `LineRegion` 이 있으면 `ShowLine` 이 늘리지 않고 시작점에 놓은 뒤 `Length`·`Width` 를 넣고, `AoeVfxPlayer` 로 조준 시간 동안 채운다. 없으면 기존처럼 늘린다.
- 그랩이 조준 시간(`_aimSeconds × TelegraphScale`)을 채움 시간으로 넘긴다.
- `Map_4_Security`·보스A 테스트 씬의 `BossArena._linePrefab` = `GrapWarnning.prefab`.
- (추가, 사용자 요청) `ShowCircle` 도 `CircleRegion` 이면 루트를 늘리지 않고 `Radius` 를 넣고, 어깨포 예고 시간 동안 채운다. `Map_4_Security._circlePrefab` = `CircleWarnning.prefab`(사용자 연결).
- (추가, 사용자 요청) 보스 B 돌진(`BossBCharge._linePrefab` = `GrapWarnning`)·도약(`BossBLeap` Leap `_circlePrefab` = `CircleWarnning`). `BossBTelegraph` 가 DTT 예고면 직선은 시작점 배치·`Length`/`Width`, 원은 `Radius`, 진행도는 알파 대신 `FillProgress`.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보스전 플레이어(그랩 조준 예고)
- 어떤 시스템/모듈이 건드려지는가: `BossArena.cs`, `IBossArena.cs`, `BossAGrab.cs`, `BossShoulderCannon.cs`, `BossBTelegraph.cs`, `BossB.prefab`, `Map_4_Security.unity`, 보스A 테스트 씬. 내려치기(칸 예고)는 영향 없음.

## 제약 (Constraints)
- 지켜야 할 정책: 씬·프리팹 YAML 직접 수정 금지(에디터 MCP 사용).
- `AoeVfxPlayer` 는 `Assets/_Project/Scripts/Boss/VFX/`(`Boss.VFX`), 셰이더·머티리얼은 `Assets/ThirdParty/DTT/Boss/` 에 둔다(사용자 결정). ThirdParty 는 Git 제외라 팀원에게 `ThirdParty/DTT/Boss/` 를 따로 전달해야 한다.
- 보스 코드는 `AI.Boss.*` 안이므로 `Boss.VFX` 는 파일 맨 위 `using` 으로만 참조한다(안에서 `Boss.` 로 한정하면 `AI.Boss` 로 해석됨).

## 열린 질문 (Open questions)
- ~~`Map_4_Security` 의 `_circlePrefab` 연결 여부~~ → 사용자가 `CircleWarnning` 으로 연결함.
- B 돌진 직선을 그랩과 같은 `GrapWarnning` 으로 둘지(현재 임시로 공유), 따로 만들지.
- 색·두께·채움 속도는 실제 패턴 중 육안 확인 필요.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
