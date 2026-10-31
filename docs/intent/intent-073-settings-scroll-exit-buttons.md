---
id: intent-073
title: 인게임 설정창 슬라이더가 실제로 동작하게 + 스크롤로 메인 화면·게임 종료 버튼 추가
part: UI / Settings / Core
status: open   # open | resolved
created: 2026-10-23
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
- 인게임에서 ESC를 누르면 설정창(`Canvas_InGame/SettingsView`)이 열리지만, 게임을 끝내거나 메인 화면으로 돌아갈 방법이 없다.
- 설정창 슬라이더(마스터·음악·효과음·마우스 감도)를 마우스로 움직여도 실제 값이 바뀌지 않는다. 원인 두 가지:
  1. 맵 씬 6개와 TestScene 어디에도 EventSystem이 없어 UI가 마우스 입력을 받지 못한다(MainScene에만 있음).
  2. `SettingsView`(일반 Transform)의 local z가 -154.93이라 Screen Space Overlay 캔버스에서 클릭 위치를 슬라이더 좌표로 바꾸는 계산(`ScreenPointToLocalPointInRectangle`, 레이 시작 z = -100)이 실패한다. 선택만 되고 값은 그대로다. `Canvas_MainMenu`도 같았다.
- `MOUSE SENSITIVITY` 슬라이더는 코드에 연결되어 있지 않았다. 카메라 감도는 `CameraControl/PlayerBaseCamera._sensitivity`(private, 다른 파트)에만 있다.
- 슬라이더 `Handle Slide Area`에 여백이 없어 값 0·1에서 손잡이 절반이 트랙 밖으로 나간다.
- 메인 씬 `MainScene`은 Build Settings에 없다(`SceneLoader`가 빌드 번호를 `ESceneType`으로 쓰기 때문, intent-036).

## 기대 결과 (Proposed outcome)
- 설정창 4개 슬라이더를 마우스로 움직이면 믹서 볼륨·마우스 감도가 바로 바뀌고, 닫을 때(또는 버튼으로 나갈 때) 저장된다.
- 설정창을 열자마자 `MAIN MENU`·`QUIT GAME` 버튼이 하단에 항상 보이고, 4개 슬라이더는 보이는 두께보다 넓은 영역에서 마우스로 조절할 수 있다.
- 마우스 감도까지 4개 설정 행과 하단의 `RETRY`·`MAIN MENU`·`QUIT GAME` 버튼을 스크롤 없이 한 화면에서 볼 수 있다.
- `MAIN MENU`는 멈춘 시간·커서를 풀고 `MainScene`을 바로 불러온다. `QUIT GAME`은 빌드에서 종료, 에디터에서 Play 중지.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 플레이어(모든 맵 씬 — 모두 `Canvas_InGame` 사용), 메인 메뉴 설정창
- 어떤 시스템/모듈이 건드려지는가:
  - 코드: `Core/InputMap/MouseSensitivity`(새 파일), `Core/InputMap/InputMapController`, `Core/GameManager`, `UI/Settings/SettingsView`·`SettingsPresenter`, `UI/UIManager`
  - 에셋: `Prefabs/UI/Canvas_InGame.prefab`, `Prefabs/UI/Canvas_MainMenu.prefab`, `ProjectSettings/EditorBuildSettings.asset`

## 제약 (Constraints)
- 코드는 `Scripts/Core`·`Scripts/UI`만 수정. `CameraControl`·`Facade`·`SceneTransition`은 수정하지 않음.
- Build Settings 기존 0~6번 순서 유지.
- 확인은 TestScene에서만(맵 씬은 열지 않음).

## 열린 질문 (Open questions)
- (결정) 슬라이더 수정 범위 → 모양(손잡이 여백) + 4개 슬라이더 실제 동작 모두.
- (결정) 마우스 감도 → 카메라 코드는 그대로 두고, Look 마우스 바인딩에 실행 중 `scaleVector2` processor 오버라이드로 배율 적용. 슬라이더 0 / 0.5 / 1 = 0.25배 / 1배 / 4배.
- (결정) 확인 팝업 → 없음.
- (결정) 로딩씬 → 거치지 않고 `MainScene`을 바로 불러옴.
- (결정) `SettingsPanel`의 빈 `SettingsView` 중복 컴포넌트 → 그대로 둠.
- 맵 씬에는 AudioManager가 없어(MainScene·TestScene에만 있음) 맵에서는 볼륨 슬라이더가 반영·저장되지 않는다. AudioManager 맵 배치는 intent-038의 남은 일.

## 해결 기록 (Resolution) — 해결 후 작성
- 진행(2026-10-23):
  - `MouseSensitivity`(Core.InputMap, static): 값 0~1을 PlayerPrefs `MouseSensitivity`에 저장, `Apply(PlayerInput)`이 `Player/Look`의 `<Pointer>` 바인딩에 `scaleVector2(x=k,y=k)` 덮어쓰기(에셋 파일 불변). `InputMapController`가 스폰 때와 `OnChanged` 때 현재 플레이어에 적용.
  - `SettingsView`: `_sensitivitySlider`·`_mainMenuButton`·`_quitButton`(버튼은 비워도 됨). `SettingsPresenter`: 감도 표시·변경, 닫을 때와 나가기 버튼 직전에 볼륨·감도 저장, `MainMenuRequested`·`QuitRequested`. `UIManager`: 설정창 닫고 `GameManager.ReturnToMainMenu()`/`QuitGame()`.
  - `GameManager.ReturnToMainMenu`: 시간 정지 요청 비움, Playing, timeScale 1, 커서 풀기, 스폰 사유 NewGame, `SpawnStage` null, `LoadScene("MainScene")`. 맵 씬(StageManager 있는 씬)을 불러오면 커서를 다시 잠금(메인에서 다시 들어올 때 대비).
  - `Canvas_InGame`: `Window/Content` → `ScrollView`(ScrollRect, `Viewport` RectMask2D, 오른쪽 세로 Scrollbar) 안으로, 행마다 LayoutElement 높이 72, `Row_Exit`(MAIN MENU·QUIT GAME), 프리팹 루트 아래 `EventSystem`(InputSystemUIInputModule, 기본 UI 액션 — MainScene과 같음).
  - 두 프리팹: `SettingsView` z -154.93 → 0, 슬라이더 `Handle Slide Area` 좌우 4px, 감도 슬라이더 연결.
  - Build Settings 7번에 `MainScene` 추가.
- 확인(TestScene Play, 시뮬레이션 클릭): 마스터 0.5(-6.0dB)·음악 0.19(-14.5dB)·효과음 0.81(-1.9dB), 감도 0.81 → 배율 2.35. 스크롤 후 MAIN MENU → MainScene, timeScale 1·커서 풀림·값 저장, 메인 메뉴 설정창에 저장값 표시·클릭 동작. 다시 Play 시 저장 감도가 스폰 때 적용. QUIT GAME → Play 중지. 테스트 후 PlayerPrefs 볼륨·감도 키는 지움.
- 남은 것: 실제 마우스로 시점 회전 감도 체감, 맵 씬에서의 확인, 메인 → 새 게임으로 튜토리얼 재진입 확인(맵 씬이라 미실행).
- 후속 변경(2026-10-30): 사용자 요청에 따라 인게임 `Row_Exit`를 ScrollView 밖의 `Window` 하단에 고정하고 ScrollView 높이를 줄였다. 4개 슬라이더의 RectTransform 높이는 18→40, 배경·채움·손잡이 영역은 18 유지, 투명 Image로 넓어진 영역의 클릭을 받는다. 프리팹 구조·참조 정적 점검 완료, 실제 Editor Play 확인은 남음.
- 후속 변경(2026-10-30): 설정창 높이를 480→600으로 늘려 4개 행(총 330px)이 표시 영역(380px)에 모두 들어가게 했다. 더 이상 스크롤이 필요하지 않아 세로 스크롤과 스크롤바를 껐다. 프리팹을 1920×1080으로 렌더해 4개 행(마우스 감도 포함)과 하단 버튼 3개가 스크롤 없이 모두 보이는 것을 확인(내용 330px / 표시 영역 380px, 맵 씬 오버라이드 없음). 실제 Play 중 ESC로 연 화면은 미확인(에디터 Play 루프가 멈춰 캡처 불가).
- 관련 커밋/PR: `252e3706`(MouseSensitivity), `4a091bab`(GameManager), `d213b1ea`(설정창 UI·프리팹), `a4ed336d`(Build Settings 보완)
