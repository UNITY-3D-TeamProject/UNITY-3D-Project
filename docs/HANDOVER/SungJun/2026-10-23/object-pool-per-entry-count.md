# 오브젝트 풀 개수를 항목(키)별로 설정 (intent-069)

## 작업 요약 및 방법
- 문제: `PrefabPool`의 `Default Capacity`·`Prewarm Count`·`Max Size`가 카테고리마다 하나뿐이라, Crowd의 Car(20개)와 Pedestrian(50개)처럼 같은 카테고리 안에서 개수를 따로 정할 수 없었다.
- `PrefabPool.SPoolEntry`에 `_initialCount`(인스펙터 이름 Initial Count, 이전 Prewarm Count)와 `_maxSize`를 추가하고, 카테고리 필드 3개와 기본값 상수 3개를 삭제했다.
- `Initialize`가 항목 값으로 풀을 만든다. `defaultCapacity`에는 Initial Count(Max Size까지)를 넘기고, `Prewarm(pool, count)`는 개수를 인자로 받는다.
- `ObjectPoolManager.cs`는 주석 2곳의 "Prewarm Count"만 "Initial Count"로 바꿨다. Get/Release 사용법은 그대로다.
- `ObjectPoolManager.prefab`은 YAML에서 카테고리 값을 각 항목으로 옮겼다(전부 10/50, Crowd만 10/100).

## 결정 사항
- 세 값의 뜻: Default Capacity = 내부 목록 칸 수(객체 생성 없음), Initial Count = 씬 시작 때 미리 만드는 개수, Max Size = 반납 시 보관 최대 개수(동시 사용 수 제한 아님, 넘치면 Destroy).
- Default Capacity는 사람이 정할 이유가 없어 인스펙터에서 없애고 코드에서 Initial Count를 넘긴다.
- 카테고리는 인스펙터 묶음 용도만 남는다. 개수 때문에 카테고리를 새로 만들 필요가 없다.
- 빈 목록에 처음 추가한 항목은 0으로 들어올 수 있어 Max Size < 1이면 경고 후 1로 보정한다.

## 현재 상태 및 이슈
- 컴파일 0에러. 프리팹을 다시 불러와 항목별 값(10/50, Crowd 10/100)이 읽히고 옛 필드가 없는 것을 확인했다.
- 미리보기 씬(저장 안 함)에서 Crowd 복사본으로 Initialize: Car Initial 20/Max 30 → 20개 생성, Pedestrian Max 0 → 경고 후 1개로 보정 확인. 실제 씬 Play는 하지 않았다.
- `Map_1_Lobby`에 옛 오버라이드(`_crowd._prewarmCount` 40, `_crowd._defaultCapacity` 40, `_playerBullets._defaultCapacity` 15)가 남아 있다. 이제 적용되지 않으므로 로비는 Car·Pedestrian이 프리팹 값(10)으로 시작한다. 맵 씬은 열지 않았다.
- `Map_2_Feed`·`Map_3_File`·`Map_4_Security`는 `_crowd._entries` 크기 0 오버라이드만 있어 영향 없다.
- `ProjectSettings/ShaderGraphSettings.asset` 변경은 이번 작업에서 직접 고친 것이 아니다(에디터가 바꾼 것으로 보임). 커밋 안 함.

## 다음 할 일 (Next Steps)
- 로비 담당: `Map_1_Lobby`의 ObjectPoolManager에서 Crowd의 Car·Pedestrian Initial Count·Max Size를 다시 입력 (예: Car 20, Pedestrian 50).
- 필요하면 `Player Charged Bullets` 카테고리를 `Player Bullets`에 합칠지 결정 (원래 개수를 따로 정하려고 나눈 것).
