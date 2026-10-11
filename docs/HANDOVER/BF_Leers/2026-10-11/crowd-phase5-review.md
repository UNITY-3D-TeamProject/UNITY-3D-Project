# 군중 Phase 5 심층 리뷰 + 소규모 수정

## 작업 요약 및 방법
- `/start-feature` 파이프라인 진행 상황을 정리했다. `spec/crowd-system.md`(Git 제외) 세션 C를 `done (성능 보류)`로 바꾸고, 지금까지 스펙과 달라진 점(열린 레인·풀·속도 배율·`Crowd` 레이어·`CrowdAppearance`)을 메모에 적었다. 보류한 성능 대응은 세션 D로 분리했다.
- `code-review`(high)로 `Assets/_Project/Scripts/AI/Crowd/` 전체를 리뷰해 10건(R1~R10)을 받았다. 메인 세션이 소스와 씬 파일로 주요 항목을 확인했다. 표는 스펙의 `## 심층 리뷰 (Phase 5)` 절에 있다.
- 사용자 결정에 따라 R1·R3·R4·R5를 수정했다. 컴파일 0에러(MCP recompile)이고, Play 검증은 하지 않았다.
  - R1: `CrowdLane._spawnBlockLayers`가 씬의 레인 8개 모두에서 0이라 시작점 검사가 항상 "비어 있음"이었다 → 직렬화 필드를 삭제하고, 프리팹 `CrowdController.DetectLayers`(신규 읽기 전용 속성)를 쓰도록 바꿨다.
  - R3: 풀에서 꺼낸 개체를 위치로 옮긴 뒤 활성화한다(`actionOnGet` 제거).
  - R4: `CrowdLane`이 레인 길이를 캐시한다(`SpawnMember(float distance)`).
  - R5: `CrowdAppearance`에서 빈 머티리얼 칸이 뽑히면 기존 머티리얼을 유지한다.

## 결정 사항
- 시작점 막힘 레이어는 개체의 감지 레이어와 같은 값을 쓴다(따로 설정하지 않는다).
- `CrowdController.SetLane(SplineContainer, float)` 시그니처는 유지한다(길이 중복 계산은 재생성 때만 생겨 비용이 무시할 수준).
- R6(합류·역방향 교착), R7(풀 밖 Destroy), R8(짧은 레인), R9(`InitValue` 우회), R10(`CrowdLane` 책임 과다)은 기록만 한다.

## 현재 상태 및 이슈
- **Play 미검증.** R1 때문에 열린 레인 재생성이 이제 시작점 근처의 플레이어·개체에 막힌다. 왕복 인도 레인(1.5m 간격)에서는 반대편 개체가 시작점 반경(1.25m + 캡슐 0.3m) 안을 지나갈 때 재생성이 잠깐 늦어질 수 있다.
- R2(미해결, 사용자 담당): `Pedstrian.prefab`, `Car.prefab`이 Git 제외 폴더 `Assets/Test/NPC/`에만 있고, 레이어 30/31(`VehicleBody`/`Crowd`)은 커밋 안 된 `TagManager` 변경에만 있다.
- 기존 이슈: 플레이어 생성 지점과 Parking 레인 끝이 겹침, LilRobot Legacy 경고 스파이크, 성능 관문 불합격(세션 D 보류).

## 다음 할 일
- `Map_1_Lobby_T` Play: 열린 레인 재생성이 계속 일어나는지, 시작점에서 개체가 겹쳐 생기지 않는지 확인한다.
- 사용자: R2(프리팹을 `_Project`로 이동, ProjectSettings 커밋), 그다음 USER-6(실제 `Map_1_Lobby` 배치).
- 커밋: 군중 코드·문서 변경.
- Phase 6: `spec/crowd-system.md`는 이번 세션에서 갱신을 마쳤다.
