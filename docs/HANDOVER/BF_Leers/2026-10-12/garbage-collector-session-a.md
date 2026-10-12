# 가비지 컬렉터(GC) 세션 A — 순찰 경로·공격 1회 노드·속도 곡선 구현

## 1. 작업 요약 및 방법
- 스펙(`spec/garbage-collector.md`) 세션 A(UNITY-1~3)를 구현했다.
- `AI/WaypointPatrolRoute.cs` 신규: `SetWaypoints(IReadOnlyList<Vector3>)`, `TryGetNextWaypoint(out Vector3)`. 직전 지점을 뺀 무작위. 임시 입력 `Transform[]`은 `Awake`에서 위치 목록으로 복사.
- `AIController.cs` 변경: `_patrolRoute` 필드(비면 `GetComponent`), `MoveToRandomPatrolPoint()`가 경로가 있으면 거기서 받고 없으면 기존 반경 무작위, 상수 `DESTINATION_TIMEOUT`을 `_destinationTimeout`(기본 10) Inspector 값으로 변경.
- `AI/BT/AIAttackOnceAction.cs` 신규: 사거리 안이면 `StopMove` → `Attack` → Success, 밖이면 Failure.
- `AI/GarbageCollector/SpeedOverTime.cs` 신규: 경과 시간 곡선 값을 `CharacterMediator.SetAttribute`로 전달(값이 바뀔 때만). 키가 없으면 경고 후 컴포넌트를 끈다.
- `.meta` 파일 4개(스크립트 3 + 폴더 1)를 함께 만들었다.

## 2. 결정 사항
- 스펙과 달라진 점 없음.

## 3. 현재 상태 및 이슈
- Unity 컴파일 에러 0 확인. 동작은 미검증(USER-4에서 확인).
- 콘솔에 `Assets/Test/AI/Map_4_Security_T.unity`의 Missing Prefab(RespawnZone) 오류가 있으나 이번 작업과 무관한 기존 문제다.

## 4. 다음 할 일 (Next Steps)
- 사용자: USER-1(어트리뷰트 SO) → USER-3(그래프 `GarbageCollectorAI`) → USER-2(프리팹). USER-6(맵 담당 요청 전달)은 언제든.
- 이후 세션 B: USER-4 체크리스트.
