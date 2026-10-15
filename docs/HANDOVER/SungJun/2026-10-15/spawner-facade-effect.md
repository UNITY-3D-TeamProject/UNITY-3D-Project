# 스포너 Effect 적용 경로 변경 + 몬스터 고정/랜덤 소환

관련 intent: [intent-024](../intent/clear/intent-024-spawner-facade-effect-random-monster.md)

## 1. 작업 요약 및 방법
- **PlayerSpawner** (`Scripts/Core/PlayerSpawner.cs`)
  - 시작 Effect 적용을 `SpawnerBase.ApplySpawnEffects(AttributeSet, ...)` → 새 private 메서드 `ApplyPlayerEffects(PlayerFacade)`로 교체.
  - `ApplyPlayerEffects`는 `_playerEffects`를 돌며 `playerFacade.ApplyEffect(effect)` 호출, false면 `LogWarning`(effect 이름 포함).
  - 더 이상 안 쓰는 out 변수 `_spawnedPlayerattributeSet`은 `out AttributeSet _`로 변경. `TrySpawnWithAttributes`는 생성 + AttributeSet 존재 검사용으로 계속 사용.
  - 이 파일은 **CP949 인코딩**이라 Python으로 cp949 읽기/쓰기 해서 인코딩 유지(`git diff | iconv -f cp949`로 바뀐 줄만 바뀐 것 확인).
- **MonsterSpawner** (`Scripts/Core/MonsterSpawner.cs`)
  - `ESpawnMode { Fixed, Random }`(클래스 내부), `_spawnMode`(기본 Fixed), `_randomMonsterPrefabs` 추가. 각 칸에 Tooltip.
  - `SelectMonsterPrefab()`: Fixed면 `_monsterPrefab`, Random이면 목록에서 `UnityEngine.Random.Range`로 하나. 목록이 비면 null → `SpawnMonster`에서 에러 로그 후 소환 안 함.
  - Effect 적용은 몬스터에 Facade가 없어 기존 `ApplySpawnEffects` 그대로, 어떤 몬스터든 `_monsterEffects` 공통 적용.
- `SpawnerBase`, `PlayerFacade`, `CharacterMediator`는 변경 없음.

## 2. 결정 사항
- 플레이어는 PlayerFacade를 단일 창구로 Effect 적용. 반복문은 스포너 안(이세훈 담당 PlayerFacade 미수정).
- 몬스터는 Facade가 없으므로 AttributeSet 직접 적용 유지.
- 기존 `_monsterPrefab` 칸 유지 + 모드 기본값 Fixed → MonsterSpawner 프리팹·Map_4_Security 스포너 13개 값 그대로.

## 3. 현재 상태 및 이슈
- 컴파일 에러 0.
- Play 검증(Map_3_File, 새 게임): 플레이어 스폰 직후 CurrentHp 50 / JumpPower 8 / CurrentBattery 80 / MoveSpeed 6 — `PlayerSpawnEffect` SO 값대로 적용, 새 경고 없음.
- Play 검증(런타임 임시 생성, 씬 저장 안 함): Random 스포너 10개 → Melee 5·Ranged 5, Fixed 기본값 → Melee, 빈 랜덤 목록 → 에러 로그 후 소환 안 함.
- 사망 후 라운드 재스폰(RoundRecovery)·StageRestart 경로는 같은 코드 경로지만 따로 Play 확인은 안 함.
- 에디터 포커스가 없으면 Play 프레임이 멈춤(`Time.frameCount` 고정) — MCP로 시험할 때 `editor_focus` 필요.
- 코드 커밋: 4a9a9e0. intent-024 resolved 처리.

## 4. 다음 할 일 (Next Steps)
- 실제 맵에서 랜덤이 필요한 스포너를 Random으로 바꾸고 목록 채우기(목록의 프리팹은 모두 ObjectPoolManager에 등록 필요).
- Map_4_Security에 ObjectPoolManager 배치(기존 이슈, 없으면 몬스터 안 나옴).
