# 인게임 설정창 슬라이더 실제 동작 + 메인 화면·게임 종료 버튼 (intent-073)

## 작업 요약 및 방법
- 설계([settings-exit-buttons-design.md](./settings-exit-buttons-design.md)) 뒤 사용자가 "볼륨 3개·감도까지 실제 값이 바뀌게, 메인 화면·게임 종료 버튼까지 다 만들어 달라, 질문은 받지 않는다"고 해서 열린 질문은 기본안으로 정하고 구현했다.
- 슬라이더가 안 움직이던 원인을 TestScene Play에서 찾음:
  1. 맵 씬·TestScene에 EventSystem이 없음 → `Canvas_InGame` 프리팹 루트 아래 `EventSystem`(InputSystemUIInputModule, DefaultInputActions) 추가. 모든 맵 씬이 이 프리팹을 쓴다.
  2. `SettingsView`(일반 Transform) local z = -154.93 → Overlay 캔버스의 클릭 레이(z -100 시작)가 슬라이더 평면에 닿지 않아 선택만 되고 값이 안 바뀜 → 두 프리팹 모두 z 0.
- 마우스 감도: 새 `Core/InputMap/MouseSensitivity.cs`(static). PlayerPrefs 저장, `Player/Look`의 `<Pointer>` 바인딩에 실행 중 `scaleVector2` processor 오버라이드. `InputMapController`가 스폰·변경 때 적용. `PlayerBaseCamera`(다른 파트)는 그대로.
- 버튼: `SettingsView`(`_sensitivitySlider`·`_mainMenuButton`·`_quitButton`) → `SettingsPresenter`(저장 후 `MainMenuRequested`·`QuitRequested`) → `UIManager`(설정창 닫기) → `GameManager.ReturnToMainMenu()`·`QuitGame()`.
- 프리팹 `Canvas_InGame`: `Window/Content`를 `ScrollView/Viewport` 안으로, 오른쪽 세로 Scrollbar, 행 높이 72 고정, `Row_Exit`(MAIN MENU·QUIT GAME). 두 프리팹 슬라이더 `Handle Slide Area` 좌우 4px.
- Build Settings 7번(`Loading` 뒤)에 `MainScene`.
- 프리팹 편집은 scratchpad의 일회용 에디터 스크립트로 `PrefabUtility.LoadPrefabContents`/`SaveAsPrefabAsset` 실행.

## 결정 사항
- 감도 배율: 슬라이더 0 / 0.5 / 1 = 0.25배 / 1배 / 4배 (지수). 기본 0.5 = 기존 카메라 감도 그대로.
- 확인 팝업 없음, 로딩씬 거치지 않고 `SceneManager.LoadScene("MainScene")`.
- `ReturnToMainMenu`는 시간 정지 요청을 비우고 Playing·timeScale 1·커서 풀기, 스폰 사유 NewGame·`SpawnStage` null. 맵 씬(StageManager 있는 씬) 로드 때 `GameManager.HandleSceneLoaded`가 커서를 다시 잠금.
- 버튼은 선택 연결이라 메인 메뉴 설정창에는 없다.
- `SettingsPanel`의 빈 `SettingsView` 중복 컴포넌트는 그대로 둠.

## 현재 상태 및 이슈
- TestScene Play(시뮬레이션 클릭) 확인: 볼륨 3개가 믹서 dB까지 바뀜, 감도 0.81 → 배율 2.35, 스크롤 후 MAIN MENU → MainScene(timeScale 1, 커서 풀림, 값 저장), 메인 메뉴 설정창 저장값 표시·클릭 동작, 재Play 때 저장 감도 적용, QUIT GAME → Play 중지. 테스트로 바뀐 PlayerPrefs 키는 지움.
- 맵 씬에는 AudioManager가 없어 맵에서는 볼륨 슬라이더가 반영되지 않는다(intent-038 남은 일).
- 실제 손 마우스로 시점 회전 체감, 맵 씬 확인, 메인 → 새 게임 → 튜토리얼 재진입은 미확인.
- 빌드 시작 씬은 여전히 0번 Tutorial(메인 메뉴로 시작하려면 `ESceneType`·`SceneLoader` 쪽 조정 필요, 범위 밖).
- 커밋 안 함.

## 다음 할 일 (Next Steps)
- 사용자가 직접 마우스로 슬라이더·감도 체감 확인, 맵 씬에서 ESC 설정창 확인.
- 메인 화면 → START로 튜토리얼 재진입 시 커서 잠금·스폰 정상 여부 확인.
- 맵 씬 AudioManager 배치(intent-038).
