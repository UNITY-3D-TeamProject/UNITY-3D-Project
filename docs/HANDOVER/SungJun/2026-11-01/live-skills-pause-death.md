# 라이브씬 스킬 잠금 + 엔딩 ESC 일시정지(영상 포함) + 사망 팝업 중 게임 멈춤 (intent-112)

## 1. 작업 요약 및 방법
- **라이브씬 스킬 잠금**: `Core/Stage/LiveApp.cs`가 `StartStage`에서 `Roll`·`SpawnVehicle`·`Scan`을 `SetSkillEnabled(false)`, `OnPlayerSpawned`로 재스폰 플레이어에도 다시 끈다 (튜토리얼과 같은 방식). 공격·점프·상호작용은 그대로.
- **엔딩 ESC로 흐름이 끊기던 원인**: `Map_5_Live` 스크린 VideoPlayer가 `UnscaledGameTime`·Direct 오디오라 설정창(Pause)에도 영상이 계속 돌고, `ScreenVideoZoom`의 대기·페이드도 실제 시간으로 돌았다. 설정창이 열린 채 TV 밖 대화(35번)로 넘어갈 때가 되면 `CinematicManager.TryStart`가 Pause 상태라 거절 → 앞의 스크린 컷씬은 이미 `Stop`해서 조작만 풀리고 대화·크레딧이 안 나왔다.
- **수정 (`UIandCinematic/CameraShot/ScreenVideoZoom.cs`)**:
  - `Update`: 시작 뒤 `IsGamePaused()`면 재생 중인 영상을 `Pause`(TV·전체화면이 같은 VideoPlayer라 둘 다 멈춤, 소리 포함), 닫히면 우리가 멈춘 경우에만 `Play`로 이어서 재생.
  - `CoWait`·`CoFade`·`IsSkipHoldComplete`: 설정창이 열린 동안 시간을 세지 않음 (E 스킵 게이지도 그대로 유지).
  - 결과: 설정창 동안 TV 밖 대화로 넘어가는 시점이 오지 않으므로, 닫은 뒤 이어서 대화 → 크레딧으로 간다.
- **사망 팝업 중 게임 멈춤 (`UI/UIManager.cs`)**: `OpenDeathPopup`에서 `PauseGameTime(_deathPopupPresenter)`, `CloseDeathPopup`에서 `ResumeGameTime`. BGM·UI 소리는 계속(ignoreListenerPause), 3D 효과음은 멈춤. `GameManager.SetCursorLocked` 주석도 맞춤.

## 2. 결정 사항
- 막는 스킬 3개 = HUD 스킬 칸 3개(구르기·블루투스·스캔).
- 사망 시 멈추는 시점은 사망 애니메이션이 끝나 팝업이 뜰 때 (죽는 순간 멈추면 사망 애니메이션도 멈춤). 죽은 직후 애니메이션 동안은 기존처럼 진행.
- `Map/Live/LiveEndingScreen.cs`는 건드리지 않음 (코드 범위). 같은 VideoPlayer를 `ScreenVideoZoom`이 멈춘다.

## 3. 현재 상태 및 이슈
- 리컴파일 에러 0. **Play 확인 안 함** (`Map_5_Live`는 열지 않는 규칙, 실제 엔딩·사망 팝업 미확인). 커밋 안 함.
- 이미 시작된 카메라 블렌드(TV 안으로 1.5초, 밖으로 3초)는 Brain이 게임 시간과 상관없이 돌아 설정창 중에도 끝까지 간다. 이 블렌드 중 설정창을 열면 카메라는 움직이고 페이드만 멈춘다.
- 사망 팝업 중 설정창은 원래 열리지 않음(UI 맵).

## 4. 다음 할 일 (Next Steps)
- 실제 라이브 엔딩에서 TV·전체화면·대화·크레딧 각 구간에 ESC → 멈춤 → 닫기 → 이어짐 확인.
- 라이브씬에서 구르기·블루투스·스캔이 안 나가고 HUD 칸이 잠김 표시되는지 확인.
- 전투 맵에서 사망 팝업 중 몬스터가 멈추고 BGM은 나오는지, 다시하기·메인 메뉴 후 정상 진행되는지 확인.

## 후속 (같은 날)
### 엔딩 크레딧 중 ESC로 연 설정창이 안 보이던 문제
- 원인: 설정창 `Canvas_InGame/SettingsPanel`의 자체 Canvas(Override Sorting)가 Sort Order 10, `Map_5_Live`의 `Canvas_EndingCredits`가 100이라 크레딧이 설정창을 덮었다. (설정창은 열려 있고 게임도 멈춰 있었음)
- 수정: `Prefabs/UI/Canvas_InGame.prefab`의 SettingsPanel Canvas Sort Order 10 → 200. 씬에서 이 값을 덮어쓴 곳 없음. 다른 씬의 높은 Sort Order는 `Loading.unity` 100뿐(설정창과 겹치지 않음). 씬 수정 없음.

### 춤출 때 캐릭터·화면이 흔들려 보이던 문제
- 원인: intent-111에서 만든 변환 클립 `ThirdParty/Mixamo/EndingCast_Player_Thriller.anim`이 키를 줄이면서 선형 접선을 쓰고 쿼터니언 x·y·z·w를 따로따로 줄여, 몸 전체(맨 위 노드 `SK_ZMike`) 움직임이 덜컹거렸다. 무대 프리팹에 같은 몸으로 120Hz 샘플링해 비교: 머리 가속도 평균 17.9·최대 342 (원본 FBX 8.8·29.7).
- 수정: 원본 FBX(`Thriller Part 3.fbx`) 곡선 171개를 접선 그대로 경로만 `SK_ZMike/`로 바꿔 같은 .anim에 다시 넣고(GUID 유지, 컨트롤러 연결 그대로), 눈 뜨기 고정 곡선 250개는 그대로 남김. 다시 재 보니 원본과 위치 차이 0, 가속도 평균 8.8·최대 29.7로 같다. 파일 약 25MB.
- 이전 클립 백업: 이 세션 스크래치패드 `EndingCast_Player_Thriller.anim.bak` (임시 폴더라 사라질 수 있음).
- ThirdParty 폴더라 커밋되지 않는다. 다른 사람에게는 이 클립을 따로 공유해야 한다.

### 상태
- 실제 엔딩에서 설정창 표시·춤 모습은 Play로 확인 안 함 (TestScene 규칙). 커밋 안 함.
