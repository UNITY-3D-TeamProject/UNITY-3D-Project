# Map_3_File 3라운드 테스트 설정 (도둑 1명, 제한 시간 180초)

## 작업 요약 및 방법
- 사용자 요청: 3라운드를 도둑 1명만 잡으면 되도록 하고, 제한 시간을 3분으로 늘린다. `Map_3_File` 씬에서만 바꾼다.
- `Map_3_File`(저장함)
  - `Thief_Spawner`: `_count` 20 → 1. 씬 오버라이드로만 바꿨고, 프리팹 `Prefabs/System/Thief_Spawner.prefab`은 그대로다.
  - `FileApp` → `_thiefChaseRound._timeLimitSeconds`: 60 → 180.
- 코드 변경은 없다. 씬 diff는 이 두 값뿐이다.
- 씬을 열 때 저절로 바뀐 `Pretendard-Bold SDF.asset`은 되돌렸다. 에디터는 다시 TestScene으로 열어 두었다.

## 결정 사항
- 테스트용 값은 프리팹이 아니라 씬 오버라이드로 둔다. 다른 씬의 `Thief_Spawner`에는 영향이 없다.

## 현재 상태 및 이슈
- Play로 3라운드에 들어가 도둑 1명 생성·클리어·180초 타이머는 확인하지 않았다. 값 저장만 diff로 확인했다.
- 커밋하지 않았다.
- 2라운드 가비지 컬렉터 속도는 `GC.prefab` `SpeedOverTime._speedCurve`(0초 2.5 → 60초 7)에서 조절한다. 사용자에게 안내만 했고 바꾸지 않았다.

## 다음 할 일 (Next Steps)
- 테스트가 끝나면 도둑 수 오버라이드를 되돌릴지(프리팹 값 20) 정한다.
- 가비지 컬렉터 속도를 줄이기로 하면 값을 정해 `GC.prefab` 커브를 수정한다(AI 파트 프리팹이라 GC를 쓰는 모든 곳이 바뀐다).
