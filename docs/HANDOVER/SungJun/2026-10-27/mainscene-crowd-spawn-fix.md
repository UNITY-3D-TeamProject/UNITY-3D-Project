# 2026-10-27 — 메인씬 배경 차량·NPC 미생성 수정

## 1. 작업 요약 및 방법
- 증상: `MainScene` 시작 시 배경(`LobbyMenuBackdrop.prefab`)에서 차와 NPC가 하나도 생성되지 않음.
- 원인: 이정빈 님 커밋 `b38b6e23`에서 `CrowdLane`의 생성 프리팹 필드가 `_prefab`(단일) → `_prefabs`(목록)로 바뀜. `Map_1_Lobby`는 옮겨졌지만, 그 전 로비를 복사해 만든 `LobbyMenuBackdrop.prefab`(커밋 `e6dfb1b5`)의 레인 9개는 예전 `_prefab` 값만 갖고 있어 `_prefabs`가 비었음 → `IsPrefabListValid()`에서 `Start`가 바로 끝남.
- 수정 (`Assets/_Project/Prefabs/UI/MainMenu/LobbyMenuBackdrop.prefab` YAML 직접 편집, 줄바꿈 CRLF 유지):
  - 레인 9개 `_prefab` → `_prefabs`. `Car_Lane_1·2`는 Car·Car_2·Car_3, `Pedstrian_Lane_1~7`은 Pedstrian (Map_1_Lobby와 동일).
  - 프리팹 안 `ObjectPoolManager`의 Crowd 카테고리에 `Car2`(Car_2)·`Car_3` 추가 (Initial 10 / Max 100, Map_1_Lobby 순서·값과 동일).
- `MainScene`에는 배경 프리팹의 레인·풀 관련 오버라이드가 없어 씬은 손대지 않음.

## 2. 결정 사항
- 레인 `_count` 등 다른 값은 그대로 둠. 로비는 `b38b6e23`에서 합계 132 → 79로 줄였지만 배경 프리팹은 예전 값 그대로라, 필요하면 로비 담당과 맞출 것.

## 3. 현재 상태 및 이슈
- 에디터 확인: 다시 불러온 뒤 레인 9개 `_prefabs` 크기(차 3 / 보행자 1)와 풀 키 `Car Car2 Car_3 Pedestrian` 확인.
- Play 확인: MainScene Play 직후 군중 46개 생성(Pedstrian 41, Car 1, Car_2 1, Car_3 3), 콘솔 에러 0.
- 에디터가 백그라운드라 프레임이 2에서 멈춰 `Update`의 이어지는 생성은 자동 확인 못 함 (코드는 로비와 같아 동작할 것으로 봄).
- NPC가 나오면서 `[AudioManager] 반복할 클립 또는 대상이 없습니다: 'SOAudioData_Footstem'` 경고가 대량으로 뜸. 이번 수정과 별개의 기존 문제(발소리 SO 클립 없음, ThirdParty 클립 누락 가능성).
- 커밋 안 함.

## 4. 다음 할 일 (Next Steps)
- 메인씬을 직접 Play해서 차·NPC가 계속 순환하는지 눈으로 확인.
- 배경 프리팹 레인 개수를 로비 최적화 값에 맞출지 결정.
- `SOAudioData_Footstem` 경고 원인 확인.
