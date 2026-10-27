# CrowdLane 프리팹 목록 랜덤 생성 (2026-10-22)

## 작업 요약 및 방법
- `Scripts/AI/Crowd/CrowdLane.cs`: `_prefab`(GameObject) → `_prefabs`(List<GameObject>). `SpawnMember` 가 꺼낼 때마다 목록에서 균등 랜덤으로 하나를 고른다.
- `IsPrefabValid` → `IsPrefabListValid`: 목록이 비었거나, 빈 칸이 있거나, 루트에 `CrowdController`/`CharacterMediator` 가 없으면 에러 후 생성 안 함.
- 시작점 막힘 레이어(`_spawnBlockLayers`)는 목록 프리팹 `DetectLayers` 의 합집합, 옆 오프셋 범위는 꺼낸 프리팹 값을 사용(`_lateralOffsetRange` 필드 제거).
- `Map_1_Lobby` 레인 9개(Car_Lane_1·2 → Car, Pedstrian_Lane_1~7 → Pedstrian)의 기존 연결을 에디터(eval + SerializedObject)로 목록 0번 칸에 옮기고 저장. 씬 diff 는 해당 필드 9곳뿐.
- 실행자: Claude 직접 (변경이 작아 Codex 위임 비용이 더 큼).

## 결정 사항
- 선택 확률은 균등. 가중치는 넣지 않음.
- `_minSpacing` 은 레인 단위 하나 — 섞인 차 중 가장 긴 차 기준으로 설정.

## 현재 상태 및 이슈
- 리컴파일 통과(에러·경고 0). Play·게이트 루프·육안 확인 미실행, 커밋 전.
- `Car_2.prefab`(사용자 작업 중, 미추적)은 아직 `ObjectPoolManager` Crowd 카테고리와 차 레인 목록에 등록되지 않음. 등록 안 된 프리팹이 뽑히면 풀이 에러를 내고 그 회차는 생성되지 않는다.

## 다음 할 일
- `Car_2` 를 `ObjectPoolManager` Crowd 카테고리에 등록하고 `Car_Lane_1·2` 의 `_prefabs` 에 추가 → Play 로 섞여 나오는지 확인.
