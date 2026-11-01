# 로딩 컷씬(폰 주인 영상) 중 ESC 설정창 (intent-115)

## 1. 작업 요약 및 방법
- 확인: ESC 설정창은 Cutscene 입력 맵을 쓰는 인게임 대화·카메라 연출·라이브 엔딩 영상(`ScreenVideoZoom`)에서만 됐다. 로딩 화면 영상(오프닝·피드→로비·파일→로비, `SOLoadingCutsceneData`)은 ESC 처리가 없었다.
- `LoadingScreen`:
  - 컷씬 재생 루프에서 `Keyboard.current.escapeKey`(로딩씬엔 PlayerInput 없음)로 설정창 열기/닫기. 열린 동안 영상 `Pause`, E 스킵 게이지 0으로 초기화(사용자 요청, 닫은 뒤엔 E를 한 번 뗐다 눌러야 다시 참), 커서 풀기(닫을 때 이전 상태로), EventSystem 켜기.
  - 설정창 "메인 화면으로"/"게임 종료": 코루틴·영상 정지 → `SceneLoader.CancelPendingLoad()` → `GameManager.ReturnToMainMenu()`/`QuitGame()` (둘 다 로딩 중이면 거절하므로 먼저 이동을 취소).
  - 후속(사용자 요청): 오프닝에서도 메인 화면 버튼을 보임. 원래 오프닝은 영상 뒤에서 튜토리얼을 미리 불러왔는데, 미리 불러온 씬은 취소가 안 돼 메인 화면으로 갈 때 튜토리얼이 잠깐 켜졌다(StageManager가 커서 잠금·플레이어 스폰). 그래서 오프닝도 영상이 끝난 뒤 튜토리얼을 불러오게 바꿈(진행 바 없이 검은 화면 대기, 에디터 기준 약 4초).
  - 후속(사용자 요청): 오프닝(메인 메뉴 → 영상 → 튜토리얼)에서는 파란 로딩 배경(`T_LoadingBackground`)을 검게 칠함 (`_background` 필드, `LoadingScreen/Background` 연결).
- `SceneLoader.CancelPendingLoad()`: `_isLoading`·`PendingScene`만 비움. 진행 순번은 이미 목적지로 넘어가 있어 이어하기는 목적지부터.
- `Loading.unity` (사용자 허락):
  - `LoadingScreen` 아래에 `Canvas_InGame`의 `SettingsPresenter`·`SettingsView` 복사(프리팹 연결 없음, `RetryButton` 끔). Presenter의 `_view`를 복사본에 다시 연결.
  - 루트에 `EventSystem`(Canvas_InGame 복사, 꺼 둔 채 저장), `AudioManager` 프리팹(BGM 없음).
  - `Cutscene` VideoPlayer 소리: Direct → AudioSource(새로 추가, Bgm 믹서 그룹, ignoreListenerPause).
  - `LoadingScreen` 새 필드 연결: `_settingsPresenter`, `_eventSystem`, `_background` (처음 넣었던 `_mainMenuButton`은 오프닝 메인 버튼을 보이게 하면서 제거).

- `ScreenVideoZoom`(라이브 엔딩 영상)도 설정창이 열리면(Pause) E 스킵 게이지·쌓은 시간을 0으로 되돌림 (이전에는 그대로 유지).

## 2. 결정 사항
- 로딩 컷씬 소리는 Bgm 채널을 따른다 (마스터·음악 슬라이더 적용).
- 로딩씬 설정창은 인게임 설정창의 복사본이다. 인게임 설정창 디자인을 바꾸면 로딩씬 것도 같이 바꿔야 한다 (메인 메뉴 설정창도 원래 따로 있음).

## 3. 현재 상태 및 이슈
- 리컴파일 통과. TestScene Play → `TryStartNewGame`으로 오프닝을 띄워 확인(PlayerPrefs 진행 저장은 백업 후 복원): 영상 출력 AudioSource, 로딩씬 AudioManager가 Instance, 설정창 열기 시 영상 멈춤·패널 표시·EventSystem 켜짐·커서 풀림, 닫기 시 원래대로. 캡처로 설정창 표시 확인.
- 후속 확인(Play): 오프닝 배경 검정, 영상 중 튜토리얼 미리 불러오지 않음, 설정창에 MAIN MENU·QUIT GAME, 메인 화면 → MainScene·커서 풀림·튜토리얼 안 켜짐(로그로 확인), 영상 끝 → 튜토리얼 정상 진입·로딩 화면 정리.
- 에디터가 백그라운드라 시뮬레이트한 ESC 키는 입력 시스템이 받지 않아, ESC 감지 줄은 직접 확인 못 함(열기·닫기 메서드를 직접 호출해 확인).
- 확인 못 함: 피드→로비·파일→로비 영상, 메인 화면·게임 종료 버튼 실제 클릭, 볼륨 슬라이더로 영상 소리 크기 변화, Direct → 믹서로 바꾼 뒤 영상 소리 크기가 이전과 같은지.
- 라이브 엔딩 영상(`Map_5_Live`, Direct 출력)은 여전히 볼륨 슬라이더를 받지 않음 (이번 범위 아님).
- 설정창이 열려 있어도 "E 꾹 눌러서 건너뛰기" 안내는 그대로 보임.

## 4. 다음 할 일 (Next Steps)
- 실제 게임에서 각 로딩 컷씬 ESC → 볼륨 조절 → 닫기 → 이어 재생, 메인 화면·게임 종료 확인.
- 영상 소리 크기가 너무 다르면 AudioSource 볼륨 조정.
