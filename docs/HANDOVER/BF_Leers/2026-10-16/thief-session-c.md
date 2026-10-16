# 도둑(Thief) 세션 C — `ThiefSpawner` 구현

## 작업 요약 및 방법
- 스펙 UNITY-4대로 `AI/Thief/ThiefSpawner.cs`(+ `.meta`)를 만들었다. `RoundSpawnerBase`를 상속한다.
- 도둑마다 `ThiefEntry`(루트, `CharacterMediator`, `OnDeath`/`OnDeathEnded` 핸들러, 잡힘 여부)를 만들어 목록으로 보관한다. 핸들러를 들고 있어서 `Despawn`과 `OnDeathEnded`에서 구독을 해제할 수 있다.
- 스폰 후보는 `StageManager.GetSpawnPoint()`에서 수평 거리 `_spawnExclusionRadius`(25m) 안인 웨이포인트를 뺀 인덱스 목록이다. 한 웨이포인트에 `_maxPerWaypoint`(3)마리가 차면 후보에서 뺀다.
- 초기값은 `MonsterSpawner.ApplyMonsterData`와 같은 방식(`IsValidTarget` 검사 후 `SetValue`)으로 `_monsterData`에서 넣는다.
- 컴파일 에러 0 (unity-editor-mcp `recompile` 확인). 처음에 `CS0165`(`out` 변수가 `&&` 뒤에서 미할당)가 났고, `AttributeSet` 조회를 앞으로 빼고 `||` 조건으로 바꿔 해결했다.

## 결정 사항
- 도둑을 하나도 못 만든 경우(후보 0개)도 실패로 본다. `LogSpawnFailureOnce` 후 `_hasSpawned`를 false로 두므로 다음 프레임에 다시 시도한다. 스펙은 이 경우를 따로 적지 않았다.
- 프리팹 구성 오류(필수 컴포넌트 없음)가 나면 이미 만든 도둑까지 전부 정리하고 반환한다(스펙대로).
- `OnDeath`가 두 번 와도 한 번만 세도록 `IsCaught` 플래그를 둔다.

## 현재 상태 및 이슈
- 컴파일만 확인했다. 동작은 Play에서 본 적이 없다(세션 D에서 확인).
- 스펙의 Phase 4 리뷰는 아직 안 했다. 세션 A·B·C 리뷰를 한꺼번에 해도 된다.
- 이 세션의 변경은 커밋하지 않았다.

## 다음 할 일 (Next Steps)
- USER-1(`SOMonsterData_Thief`, `SOAttributeData_Thief`) → USER-2(`NPC_Thief` 그래프, 파일은 이미 있음) → USER-3(`Thief.prefab`) → USER-4(테스트 씬)를 사용자가 진행한다.
- 테스트 씬의 `ThiefSpawner`: `_activeRoundIndex`를 **2**로, `_prefab`·`_monsterData`·`_waypointTransforms`를 채운다.
- 세션 D: 체크리스트 통과 + GC 스포너 회귀 확인.
