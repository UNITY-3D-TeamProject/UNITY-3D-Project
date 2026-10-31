# 라이브 엔딩 E 대화와 크레딧 음악

## 작업 요약 및 방법
- TV 밖으로 나온 뒤 자동 독백으로 표시하던 6줄을 `SOCutscene_Live_AfterShow`에 옮겨 34번 E 진행 컷씬으로 연결했다.
- `LiveEndingScreen`이 TV 영상 종료 시 34번 컷씬을 중지하지 않도록 컷씬 제어를 넘긴 상태를 기록한다.
- 대화 종료 후 `EndingCredits.Play()`를 호출하고 TV 영상 소리를 끈 뒤 `다시,_무대.wav`의 67.5초부터 재생한다.

## 변경된 파일
- `Assets/_Project/Scenes/Map_5_Live.unity`
- `Assets/_Project/ScriptableObjects/UIandCinematic/Cutscene/SOCutscene_Live_AfterShow.asset` 및 `.meta`
- `Assets/_Project/ScriptableObjects/UIandCinematic/CutsceneTable/SOCutsceneTable.asset`
- `Assets/_Project/Scripts/Map/Live/LiveEndingScreen.cs`
- `Assets/_Project/Scripts/UIandCinematic/CameraShot/ScreenVideoZoom.cs`
- `Assets/_Project/Scripts/UI/Credits/EndingCredits.cs`
- `docs/intent/intent-096-ending-cutscene.md`

## 결정 사항
- 크레딧 음원과 시작 위치는 사용자가 지정한 `다시,_무대.wav`, 1분 7.5초다.
- 크레딧은 영상 시각과 무관하게 34번 대화가 끝나면 시작한다.

## 현재 상태 및 이슈
- 사용자 요청에 따라 최종 플레이 검증은 진행하지 않았다. 변경 중 컴파일과 씬 참조는 확인했지만, 이후 시작 시각을 67.5초로 바꾼 뒤 추가 검증은 하지 않았다.
- 기존 `SOMonologue_Live_AfterShow` 에셋은 참조만 제거하고 파일은 남겨 두었다.
- 다른 작업에서 수정된 폰트 SDF, 로딩 RenderTexture, 솔루션 파일은 이 작업에 포함하지 않는다.

## 다음 할 일
- 사용자가 원하면 라이브씬에서 E 대화, TV 영상 종료, 크레딧 음악의 실제 이어짐을 확인한다.
