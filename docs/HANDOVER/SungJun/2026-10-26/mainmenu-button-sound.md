# 2026-10-26 — 메인 메뉴 버튼·스플래시 효과음 (intent-076)

## 1. 작업 요약 및 방법
- 테스트 폴더 씬 `Assets/Test/UnityAssets/Scene/MainScene.unity` → `test_mainscene.unity`로 이름 변경 (에디터 rename, GUID 유지).
- 새 컴포넌트 `Assets/_Project/Scripts/UI/UIButtonSound.cs` (`namespace UI`)
  - 마우스 올림: `IPointerEnterHandler`, 버튼이 `IsInteractable()`일 때만 `_hoverSound`
  - 클릭: `OnEnable`/`OnDisable`에서 `Button.onClick` 구독 → `_clickSound`
  - SO가 비었거나 `AudioManager.Instance`가 없으면 조용히 건너뜀
- `MainMenuPresenter`에 `[Header("Sounds")] _splashPressSound` 추가, `HandleAnyKeyPressed`에서 `PlayUi`.
- SO: `SOAudioData_UiHover` 생성(Click_16), 기존 미사용 `SOAudioData_UiClick` 클립을 Click_18로 교체. 클립 위치 `Assets/ThirdParty/UI Casual Soundpack/Click Samples/`.
- `MainScene`의 `Canvas_MainMenu` 버튼 5개(Start·Continue·Settings·Exit·설정창 CloseButton)에 `UIButtonSound` 추가 + SO 연결, Presenter `_splashPressSound`에 UiClick 연결.

- 후속: `Canvas_InGame.prefab` 설정창 버튼 3개(CloseButton·MainMenuButton·QuitButton)에 `UIButtonSound` 추가 + SO 연결. 프리팹 자체에 넣어 Map_0~5·Map Total test 모두 적용.- 후속: `UIButtonSound` 내부 함수 `PlayUi` → `PlayUiSound`로 이름 변경 (`AudioManager.PlayUi`와 헷갈리지 않게).
- 후속: `MainScene`에 `GameManager` 프리팹 배치. `GameManager.Awake`의 `SetCursorLocked(true)` 제거 → 메인 메뉴에서 커서가 잠기지 않음. 맵 씬 진입 시 잠금은 기존 `HandleSceneLoaded`(StageManager 있는 씬)가 담당.

## 2. 결정 사항
- 커서 잠금은 `GameManager.Awake`가 아니라 `HandleSceneLoaded`(맵 씬 진입)·`ResumeGame`에서만 건다. 맵 씬을 에디터에서 바로 Play해도 `sceneLoaded`가 첫 씬에도 불려 잠긴다.
- 맵 씬 6개에는 `AudioManager`가 없어 인게임 버튼은 아직 무음. 사용자 결정으로 배치는 하지 않고 맵 담당이 따로 넣음.
- 버튼 소리는 버튼마다 `UIButtonSound`를 붙이는 방식. 다른 씬·인게임 버튼에도 같은 컴포넌트 재사용.
- `Canvas_MainMenu`는 프리팹 인스턴스지만 이번 컴포넌트는 **씬 오버라이드로만** 추가 (프리팹 Apply 안 함).
- 스플래시는 클릭음(Click_18)을 같이 씀.

## 3. 현재 상태 및 이슈
- Play 검증: 스플래시 Space → 메뉴 표시 + `UiSource` 재생, Settings 마우스 올림/클릭 재생, CloseButton 클릭 재생 + 설정창 닫힘, 저장 없을 때 비활성 Continue 마우스 올림은 무음.
- 실제 귀로 듣는 확인(음량·어울림)은 안 함.
- `UI Casual Soundpack`이 ThirdParty(Git 제외)라 팀원은 SVN 등으로 받아야 SO 클립 참조가 살아남.
- Play 확인: MainScene 시작 시 GameManager 존재 + 커서 `None`/보임. MainScene → Start → 맵 진입 후 잠금은 에디터가 백그라운드 저속(약 0.1fps)이라 로딩이 끝나지 않아 **자동 확인 못 함**. 맵 씬 직접 Play 때 `HandleSceneLoaded` 잠금 로그는 이전 실행에서 확인됨.
- MainScene 재저장 때 레이아웃 그룹이 다시 계산한 메뉴 버튼 RectTransform 값 20줄이 섞여 들어가 되돌림 (추가 내용만 남김).
- 커밋 안 함.

## 4. 다음 할 일 (Next Steps)
- MainScene → Start → 맵 진입 시 커서 잠금, ESC 설정창 열고 닫기, MAIN MENU로 복귀 시 커서 풀림 직접 확인.
- 사운드팩 SVN 업로드.
- 직접 들어보고 `SOAudioData_UiHover`/`UiClick`의 `_volume` 조정.
- 맵 씬에 `AudioManager` 프리팹이 들어오면 인게임 설정창 버튼 소리 확인 (현재 Play 미확인).
