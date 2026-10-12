# 가비지 컬렉터(GC) 세션 B 종결 + 세션 C — `GarbageCollectorSpawner` 구현

## 1. 작업 요약 및 방법
- 세션 B(USER-4, 1단계 GC 동작 확인)를 사용자 확인으로 `done` 처리했다. 세션 B에서 나온 수정은 2건이다.
  - `MeleeHitController`: 충돌체 루트에 `AttributeSet`이 없고 자식에 있으면 `GetComponentInChildren<IEffectTarget>`로 찾는다.
  - `NPC_GC` 그래프: 공격 가지 앞에 사거리 Guard(`AttackRange` 2.0)를 추가했다(USER 작업).
- 세션 C(UNITY-4)로 `AI/GarbageCollector/GarbageCollectorSpawner.cs`(+ `.meta`)를 만들었다.
  - `Update`가 `IsRunning && CurrentRoundIndex == _activeRoundIndex`이면 생성, 아니면 제거한다.
  - `OnPlayerRespawnRequested`가 오면 인스턴스가 있을 때만 제거 후 재생성한다(시계 0부터).
  - 스폰 위치는 `GetSpawnPoint()`에서 직선거리로 가장 먼 웨이포인트, 기준점이 없으면 0번이다.
  - 프리팹·웨이포인트·`WaypointPatrolRoute` 누락은 `LogError`를 한 번만 찍는다.

## 2. 결정 사항
- 스펙과 달라진 점 없음. `SpawnerBase`는 상속하지 않았다.

## 3. 현재 상태 및 이슈
- Unity 재컴파일 에러 0. 동작은 미검증(USER-5).
- 알려진 이슈 없음.

## 4. 다음 할 일 (Next Steps)
- 사용자: USER-5(`Map_3_File_T` 복사, 웨이포인트 6~8개·NavMeshSurface·스포너 배치) 후 세션 D 체크리스트.
