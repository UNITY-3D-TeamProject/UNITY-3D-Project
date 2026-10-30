# 보안맵 전투 구역 몹을 MonsterSpawner 배치로 교체 (intent-093)

## 1. 작업 요약 및 방법
- 문제: 보안맵 `CombatZone`은 인스펙터의 `몬스터 데이터 + 마리 수` + `Spawn Points`로 몹을 만들어서, 스포너를 놓는 식으로 배치할 수 없었고 `MonsterSpawner`의 랜덤·추가 Effect·MoveSpeed 오차도 적용되지 않았다.
- `MonsterSpawner`
  - `SpawnMonster()` → `public GameObject Spawn()` (소환한 몬스터를 돌려주고, 실패하면 null).
  - `ShouldSpawnOnStart` 프로퍼티(기본 true). false면 Start에서 소환하지 않는다.
- `CombatZone`
  - `_enemies`·`_spawnPoints`·`SZoneEnemy`·`ApplyMonsterData`·`GetSpawnPoint` 삭제.
  - `Awake`에서 자식(손자 포함) `MonsterSpawner`의 `ShouldSpawnOnStart`를 끈다.
  - `SpawnEnemies`는 소환할 때마다 자식 스포너를 찾아 `Spawn()`을 부르고, 돌려받은 몹의 `CharacterMediator.OnDeath`를 구독한다. 대기 묶음·클리어·리셋·클리어 드롭은 그대로.
- `Map_4_Security`: 9개 구역의 기존 구성을 같은 위치·방향의 `MonsterSpawner` 프리팹 인스턴스(Fixed, 해당 `SOMonsterData.MonsterPrefab`)로 옮기고 `SpawnPoint_*`는 지웠다. 총 21개.
  - Room_1: Melee 3 + Ranged 1 / Room_2: Melee 4 + Ranged 2 / Room_3·Room_4: Melee 3 / Platform_1~5: Special 1
  - 이름은 `MonsterSpawner_{종류}_{번호}`.

## 2. 결정 사항
- 구역의 몹 = 그 `CombatZone` 오브젝트 아래의 `MonsterSpawner` 수. 인스펙터 배열 연결은 쓰지 않는다 (사용자 결정).
- 기존 `마리 수 + Spawn Points` 방식은 없앴다 (사용자 결정).
- 구역 밖의 `MonsterSpawner`는 예전처럼 Start 때 혼자 소환한다 (집계 안 됨).
- 스포너는 `ObjectPoolManager`가 꼭 있어야 한다 (예전 `CombatZone`의 Instantiate 대체 경로는 없어짐). 보안맵에는 있다.

## 3. 현재 상태 및 이슈
- 컴파일 0에러. `Map_4_Security` Play(에디터 비포커스라 시간이 거의 흐르지 않은 상태)에서 확인:
  - 시작 시 Room_1 4마리 + Platform_1 1마리만 생김 (다른 구역 스포너가 Start에서 소환하지 않음), 패널 값 `1/4 방, 남은 4`.
  - 몹 값 적용(MaxHp 10), MoveSpeed 개체별 오차(±15%) 적용.
  - Room_1 4마리 `OnDeath` → Room_1 클리어 → Room_2 6마리 준비, 패널 `2/4, 남은 6`.
  - 사망 리셋(`HandlePlayerRespawnRequested`) → Room_2 6마리 다시 생성. Console 에러 0.
- **동작 변화**: 이제 보안맵 몹에도 MoveSpeed ±15% 오차가 들어간다 (스포너 프리팹 기본값 0.15).
- Room_2의 원거리 2마리는 예전처럼 근접 1·2번과 같은 위치에 있다(예전에도 위치 4개를 돌려 써서 겹쳤음). 필요하면 스포너를 옮기면 된다.
- 직접 조작 플레이, 2라운드 발판 진행·클리어 드롭은 이번에 다시 확인하지 않았다. 커밋 안 함.

## 4. 다음 할 일 (Next Steps)
- 직접 플레이로 방·발판 진행 확인, Room_2 원거리 스포너 위치 조정 여부 결정.
- 몹을 늘릴 때: 해당 구역 아래 `MonsterSpawner_*`를 복제(Ctrl+D)해 위치만 옮긴다. 다른 종류는 `Monster Prefab`을 바꾸거나 Spawn Mode를 Random으로.
