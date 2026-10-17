# 메인 메뉴 로직 연결 — MVP (2026-10-16)

## 작업 요약 및 방법
- 사용자 요청: 기존 UI처럼 MVP로 메인 메뉴 로직 작성. 스플래시는 Space 대신 아무 키로 넘어가게, 씬은 바꾸지 않음. intent: `docs/intent/clear/intent-032-main-menu-logic.md`
- 새 스크립트(`Assets/_Project/Scripts/UI/`, namespace `UI`)
  - `MainMenuView`: 스플래시 루트·메뉴 루트·START/SETTINGS/EXIT 버튼 참조. `ShowSplash()`/`ShowMenu()`, 버튼 클릭을 `StartClicked`/`SettingsClicked`/`ExitClicked` 이벤트로 알림.
  - `MainMenuPresenter`: 시작 시 스플래시 표시 → `InputSystem.onAnyButtonPress.CallOnce`로 아무 버튼(키보드·마우스·게임패드) 1회 받으면 메뉴 표시. SETTINGS는 기존 `SettingsPresenter`를 `UIManager`와 같은 순서로 열고 닫음. ESC는 `FID_InputSystem` `UI/CloseSettings`를 `InputActionReference`로 받아 직접 Enable/Disable. START는 `SceneLoader.TryStartNewGame()`, EXIT는 `Application.Quit()`(에디터는 플레이 종료).
- 추가 요청(START → 튜토리얼, 저장 시스템 없음 가정): `SceneTransition/SceneLoader.cs`에 `TryStartNewGame()` 추가. 로딩 중이면 무시 → `Sequence[0]`(튜토리얼)과 Loading 씬이 Build Settings에 있는지 확인 → `_step = 0`, `_hasStep = true`로 순서를 처음으로 맞춤 → `PreviousScene = null`(컷씬 없음), `PendingScene = 튜토리얼` → Loading 씬 Additive로 엶. `OnBeforeSceneChange`는 이전 씬·플레이어가 없어 발행하지 않음. `TryLoad`의 Build Settings 확인 두 가지는 private `IsLoadable`로 옮겨 같이 씀(동작·로그 동일).
- Test 저장소 변경
  - `Canvas_Splash.prefab`: 키 배지 `SPACE` → `ANY KEY` (ContentSizeFitter로 폭 자동).
  - `Canvas_MainMenu.prefab`: `ContinueButton` interactable 끔, SettingsButton 지속 리스너 제거.
  - `Canvas_Settings.prefab`: CloseButton 지속 리스너(`SettingsPresenter.Close`) 제거.
  - `MainScene.unity`: 루트에 `MainMenu`(View + Presenter) 추가·참조 연결. 스플래시는 켜져 있던 `Canvas_Splash (1)`(FID 텍스트 버전)을 연결, 꺼진 원본 `Canvas_Splash`(이미지 로고)는 그대로 둠. `Canvas_Settings` 켬, 그 안 `SettingsPresenter`·`SettingsPanel`은 꺼 둔 상태로 시작. 씬 저장 시 기존 미저장 변경도 함께 저장됨.

## 결정 사항
- 메인 메뉴는 액션 맵을 새로 만들지 않는다. 아무 키는 `onAnyButtonPress`(맵과 무관), ESC만 기존 `UI/CloseSettings` 재사용.
- 메뉴 씬엔 플레이어·`PlayerInput`이 없으므로 `UIManager`/`PlayerFacade.SwitchToUIInput` 경로를 쓰지 않고 `MainMenuPresenter`가 씬 단위 조정 역할을 한다.
- 버튼 연결은 Inspector 지속 리스너 대신 View 이벤트 → Presenter로 통일.
- Model 클래스는 두지 않음(상태가 설정창 열림 여부 하나). CONTINUE 비활성은 프리팹 설정으로만 처리.

## 현재 상태 및 이슈
- 컴파일 에러 0. Play 모드에서 A 키 → 스플래시 꺼짐·메뉴 켜짐, SETTINGS 열기, 닫기 버튼·ESC 닫기, CONTINUE 비활성, EXIT 플레이 종료 확인.
- START: Play 모드(이번엔 프레임 정상 진행)에서 아무 키 → START → Loading 경유 `Map_0_Tutorial` 활성, 메뉴 씬 정리됨, GameManager `SpawnReason = NewGame`·플레이어 스폰·HUD 표시, `CanEnter(Lobby) == true` 확인. 새 에러 0. 튜토리얼 로드 중 missing script 경고 2건은 이번 변경과 무관해 보임(출처 미확인).
- 에디터가 비포커스 상태라 플레이어 루프가 멈춰(frameCount 1) 화면 캡처는 갱신되지 않았다. 확인은 오브젝트 상태로 했고, 버튼은 `onClick.Invoke()`(실제 클릭과 같은 경로)로 눌렀다. 실제 마우스 클릭·화면 전환 모습은 사람이 Play로 한 번 봐야 한다.
- `Canvas_Settings` 안 `SettingsPanel`에 참조가 비어 있는 두 번째 `SettingsView`가 붙어 있다(기존 것, 동작 영향 없음, 지우지 않음).
- 메뉴의 `PRESS ENTER TO SELECT` 안내 문구는 그대로다(첫 선택 버튼 지정 없음).
- 커밋 안 함(공유 저장소·Test 저장소 모두).

## 다음 할 일
- 실제 빌드에서 메뉴를 첫 씬으로: 빌드는 Build Settings 0번으로 시작하는데 `ESceneType` 값 = Build 번호(`Tutorial = 0`)라, 메뉴 씬을 `_Project/Scenes`로 옮겨 0번에 넣을 때 enum·씬 번호에 걸린 데이터(컷씬 SO 등)를 함께 바꿔야 한다.
- 게임 중 메인 메뉴로 돌아가기: 살아 있는 GameManager(DontDestroyOnLoad)·플레이어 상태 초기화가 필요해 별도 작업.
- 메뉴 씬의 `GameState.Title` 사용 여부는 GameManager 담당과 결정(지금은 메뉴 씬에 GameManager 없음, 튜토리얼에서 생성).
