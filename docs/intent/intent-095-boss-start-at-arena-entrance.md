---
id: intent-095
title: 보스전 시작을 3라운드 체크포인트 도착 → 아레나 진입으로 변경
part: AI / Boss / Fight
status: open
created: 2026-10-30
resolved: null
---

## 문제 (Problem)
- AI_TODO2 보스-29: 보안 맵 3라운드 체크포인트에 도착하면 `BossFightSpawner` 가 곧바로 B 등장 연출·`BossEncounter.Begin` 을 시작한다. 플레이어가 체크포인트 자리에서 총을 쏘면 큰 보스 단독 페이즈까지 넘어간다.
- 원인: `RoundSpawnerBase.Update` 가 `CurrentRoundIndex == _activeRoundIndex(2)` 이면 바로 `Spawn()` 을 부른다. 아레나 진입 판정(`BossRound._arenaEntrance`)은 아레나 벽·발판용으로만 쓰이고 보스전과 연결돼 있지 않았다.

## 기대 결과 (Proposed outcome)
- 체크포인트에서는 보스전이 시작되지 않는다(A 는 기존대로 미리 세워 둠).
- 보스 바닥(아레나 진입 트리거)에 올라서면 그때 B 등장 연출 → 보스전이 처음 단계부터 시작된다.
- 보스 라운드 중 사망·리스폰하면 보스를 지우고, 다시 아레나에 들어갈 때까지 시작하지 않는다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보안 맵 스테이지 3 플레이어
- 건드려지는 것: `BossFightSpawner`(`AllowStart` 허가 플래그, `Spawn` 이 허가 없으면 대기), `BossRound.HandleArenaEntered`(허가 호출). 씬·프리팹 변경 없음(기존 연결 재사용, `BossFightSpawner` 는 `Map_4_Security` 에서만 사용).

## 제약 (Constraints)
- 아레나 트리거는 `_isOneShot: 0` 이므로 리스폰 후 재진입 때도 다시 발동한다.
- 허가는 `Despawn`·`OnDisable` 에서 거둔다.

## 열린 질문 (Open questions)
- 아레나 진입 트리거 박스가 체크포인트·리스폰 자리와 겹치지 않는지 플레이로 확인 필요.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
