---
id: intent-093
title: 보안맵 전투 구역 몹을 MonsterSpawner 배치로 정하기
part: Stage / SecurityApp / Spawner
status: resolved   # open | resolved
created: 2026-10-30
resolved: 2026-10-30 # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
보안맵(`Map_4_Security`) 1·2라운드의 몹은 `CombatZone` 인스펙터의 `몬스터 데이터 + 마리 수` 목록과 `Spawn Points`로 정한다. 그래서
- 몹을 추가하려면 숫자를 바꾸고 위치를 따로 연결해야 해서, "몹이 나올 자리에 스포너를 놓는" 직관적인 배치가 안 된다.
- 위치보다 몹이 많으면 같은 위치를 다시 쓰므로 겹쳐 나온다 (`Room_2`: 6마리 / 위치 4개).
- 기존 `MonsterSpawner`의 랜덤 소환·추가 Effect·MoveSpeed 오차가 보안맵 몹에는 적용되지 않는다. (`MonsterSpawner`는 Start 때 한 번 소환만 하고 집계를 못 해서 intent-057에서 쓰지 않기로 했었다)

## 기대 결과 (Proposed outcome)
- `CombatZone` 자식으로 `MonsterSpawner` 프리팹을 두면 그 구역의 몹이 된다. 몹 추가 = 스포너 복제 후 위치 이동.
- 구역 차례가 되면(`Prepare`) 자식 스포너들이 한 마리씩 소환하고, `CombatZone`은 지금처럼 실제로 만든 적이 다 죽었는지로 클리어를 판정한다. 패널의 남은 적 수도 그대로 따라간다.
- 자식 스포너는 씬 시작(Start) 때 스스로 소환하지 않는다. 구역 밖에 둔 스포너는 지금처럼 Start 때 소환한다.
- 기존 `몬스터 데이터 + 마리 수 + Spawn Points` 방식은 없앤다. `Map_4_Security` 9개 구역의 현재 구성(종류·위치·방향)을 스포너 배치로 옮긴다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보안맵 레벨 배치 담당
- 어떤 시스템/모듈이 건드려지는가: `Core/MonsterSpawner.cs`, `Core/Stage/Security/CombatZone.cs`, `Scenes/Map_4_Security.unity`

## 제약 (Constraints)
- 지켜야 할 정책: 대기 묶음(Hold)·사망 리셋·클리어 드롭 등 `CombatZone`의 기존 동작은 바꾸지 않는다.
- 쓰지 말아야 할 것: 스포너를 인스펙터 배열로 따로 연결하는 방식 (사용자 결정: 자식 자동 집계)

## 열린 질문 (Open questions)
- 없음. (사용자 결정 2026-10-30: 자식으로 두면 자동 집계, 기존 마리 수 방식은 스포너로 교체)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: `CombatZone`은 자식 `MonsterSpawner.Spawn()`으로 소환하고 돌려받은 몹을 집계한다. 자식 스포너는 `CombatZone.Awake`에서 `ShouldSpawnOnStart = false`. 기존 `_enemies`·`_spawnPoints` 삭제, `Map_4_Security` 9개 구역을 같은 위치·방향 스포너 21개로 이전. Play에서 소환·클리어·다음 방 준비·사망 리셋·패널 값 확인. 보안맵 몹에도 MoveSpeed ±15% 오차가 적용된다. [작업 기록](../../HANDOVER/SungJun/2026-10-30/combat-zone-placed-spawners.md)
- 관련 커밋/PR: 커밋 전
