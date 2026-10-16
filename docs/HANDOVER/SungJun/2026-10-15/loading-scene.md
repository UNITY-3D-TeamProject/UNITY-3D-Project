# 로딩씬 추가 + SceneLoader 변경 (2026-10-15)

관련 intent: [intent-020](../intent/intent-020-loading-scene.md)

## 1. 작업 요약 및 방법
- **목적:** 포탈로 다음 맵에 갈 때(특히 로비) 목적지 씬이 켜지는 순간의 멈춤을 로딩 화면 뒤로 숨긴다.
  - 기존에도 `LoadSceneAsync`였지만, 켜기 단계(이전 씬 정리 + 새 씬 Awake/Start)는 메인 스레드에서 한 번에 처리된다. 그래서 게임 화면이 그대로 멈췄다.
- **`Scripts/SceneTransition/SceneLoader.cs` 수정** (바뀐 곳 5군데)
  - 상수 `LOADING_SCENE_NAME = "Loading"`, 프로퍼티 `PendingScene`(로딩씬이 불러갈 목적지)을 추가했다.
  - `TryLoad`: 로딩씬이 빌드에 등록됐는지 확인한 뒤, 이벤트 발행 → `PendingScene = 목적지` → `SceneManager.LoadScene("Loading")` 순으로 진행한다. `_isLoading`은 여기서 풀지 않는다.
  - 신규 `LoadPendingSceneAsync()`: 목적지를 `allowSceneActivation = false`로 불러오기 시작한다. 로드가 끝나면(`completed`) `_isLoading`과 `PendingScene`을 해제한다.
  - `ResetState`에 `PendingScene = null`을 추가했다.
- **`Scripts/SceneTransition/LoadingScreen.cs` 신규**
  - `Start` → `CoLoad`에서 `progress / 0.9`로 Slider를 채운다.
  - 0.9에 도달하고 최소 표시 시간(`_minimumDuration`, 기본 0.5초, unscaled)이 지나면 `allowSceneActivation = true`로 바꾼다.
  - 목적지가 없으면 LogWarning만 한다.
- **`Scenes/Loading.unity` 신규** (MCP eval로 구성)
  - Main Camera(검정 Solid Color, AudioListener)
  - Canvas(Overlay, 1920×1080 Scale With Screen Size)
    - Background(검정 Image)
    - LoadingText(TMP "Loading...", 48, 하단 중앙 y 160)
    - ProgressBar(Slider: 1200×24, 하단 중앙 y 100, 손잡이 삭제, Interactable 끔, 바탕 회색 0.2, 채움 흰색)
  - LoadingScreen(`_progressBar` 연결)
- **Build Settings:** `Loading.unity`를 6번(맨 끝)에 추가했다. 0~5번과 `ESceneType`은 그대로다.

## 2. 결정 사항
- 로딩씬은 `ESceneType`에 넣지 않고 이름 상수로 참조한다. 포탈 목적지 드롭다운에 나오지 않게 하기 위해서다.
- 로딩씬은 **다음 씬 이동만** 거친다. `StageManager`의 첫 라운드 사망 재시작(`SceneManager.LoadScene`)은 그대로 둔다(사용자 결정).
- `GameManager`, `ScenePortal`, `MapTestHud`는 수정하지 않았다. 로딩씬에서도 `HandleSceneLoaded`가 불리지만 StageManager가 없어서 바로 return한다.
- 진행 바는 실제 progress를 그대로 쓴다. 튀면 `MoveTowards`로 바꾸는 것을 검토한다.

## 3. 현재 상태 및 이슈
- 컴파일 에러 0.
- Play 확인 (TestScene에서 Play → `LoadScene(0)`으로 튜토리얼 → eval로 `TryLoad(Lobby)` 호출):
  - 호출 직후 `IsLoading = true`, `PendingScene = Lobby`, 활성 씬은 Loading이었다. 로딩 중 `TryLoad`를 다시 부르면 false였다.
  - 도착 후 활성 씬은 Map_1_Lobby, `IsLoading = false`, `PendingScene = null`, `PreviousScene = Tutorial`이었다. 플레이어 스폰과 HUD 표시도 캡처로 확인했다.
  - 목적지 없이 Loading에 들어가면 `[LoadingScreen] 불러올 목적지 씬이 없습니다.` 경고만 나오고 에러는 없었다.
  - 로딩 화면 모양은 캡처로 확인했다(진행 바 60% 임시 값).
  - 새 코드에서 난 에러는 없었다. 콘솔의 MissingReferenceException은 씬이 바뀔 때 인스펙터가 사라진 TestScene 오브젝트를 다시 그리려다 난 기존 에디터 에러다.
- **미확인 항목**
  - ~~실제 포탈을 밟아 이동하는 경우, 진행 바가 차오르는 모습~~ → 4장 추가 수정에서 확인
  - ~~첫 라운드 사망 재시작이 기존처럼 동작하는지.~~ → 4-5장에서 확인
- 작업 중 `Art/Scan/M_ScanPlatformHologram.mat`, `Art/Scan/NotUse/M_ScanRevealSciFi.mat`가 수정 상태로 보인다. 이번 작업에서 건드리지 않았으므로 커밋할 때 제외할지 확인이 필요하다.

## 4. 추가 수정: 진행 바 보정 (같은 날)
- **사용자 피드백:** 문에 닿으면 로딩 창이 너무 빨리 지나간다. 로드가 끝났어도 게이지가 천천히 차서 자연스럽게 보이게 해 달라.
- **`LoadingScreen` 변경**
  - `_minimumDuration`을 지우고 두 값을 새로 넣었다.
    - `_fillDuration`(기본 1초): 진행 바가 0에서 끝까지 차는 최소 시간
    - `_holdAfterFull`(기본 0.2초): 끝까지 찬 뒤 머무는 시간
  - 표시 진행률이 실제 진행률(0.9 이상이면 1)을 `Mathf.MoveTowards`로 초당 `1/_fillDuration` 이하 속도로 따라간다. 표시가 1이 되고 `_holdAfterFull`만큼 기다린 뒤 `allowSceneActivation = true`로 바꾼다.
  - 이전 씬 정리처럼 한 프레임이 길어도 진행 바가 튀지 않도록, 한 프레임에 반영할 시간을 `MAX_FILL_DELTA_TIME`(0.05초)로 제한했다.
  - `Loading.unity`를 다시 저장해 직렬화 값을 정리했다(`_fillDuration: 1`, `_holdAfterFull: 0.2`).
- **Play 확인:** 튜토리얼 포탈 안으로 플레이어를 옮겨 실제 트리거로 진입했고, 프레임마다 진행 바 값을 로그로 남겼다.
  - 포탈 → 로딩씬 활성: 0.29초
  - 진행 바 0 → 1: 약 1.25초. 0.1초마다 약 0.1씩 고르게 찼다. 첫 프레임 0.24초 멈춤도 0.05만 반영됐다.
  - 1에서 0.2초 머문 뒤 로비 켜기: 진행 바가 꽉 찬 상태로 약 0.3초 멈춤
  - 로딩 화면 전체 약 1.8초. 로비 도착 후 `IsLoading = false`, `PendingScene = null`, 플레이어 스폰을 확인했다.
- **측정 방법 메모:** 에디터가 포커스를 잃으면 Play 프레임이 멈춘다(Run In Background 꺼짐). 측정 중에만 `Application.runInBackground = true`로 두었다가 끝나고 false로 되돌렸다.
  - 에디터에서는 이 값을 바꾸면 `PlayerSettings`도 함께 바뀐다. 되돌린 뒤 `ProjectSettings.asset`에 변경이 없는 것을 확인했다.

## 4-2. 추가 수정: 로딩씬을 겹쳐 띄우고 이전 씬 정리 + 양쪽 페이드 (같은 날)
- **사용자 피드백:** 로딩씬이 켜지기 전에 잠깐 멈춘다. 원인은 `LoadScene("Loading")`(Single, 동기)이 튜토리얼 정리와 로딩씬 불러오기를 한 프레임(약 0.24초)에 처리하던 것이다.
- **흐름 변경**
  1. `SceneLoader.TryLoad`는 `LoadSceneAsync("Loading", Additive)`로 로딩씬을 튜토리얼 위에 겹쳐 연다.
  2. `LoadingScreen.CoLoad`는 다음 순서로 진행한다.
     - 페이드인(CanvasGroup 0→1, `_fadeDuration` 0.3초)
     - 로딩 카메라 켜기 → 로딩씬 외 씬 `UnloadSceneAsync`
     - 목적지 Single 로드 + 진행 바(기존)
     - `DontDestroyOnLoad(gameObject)` → 켜기
     - 페이드아웃(1→0) → `Destroy`
  3. 페이드와 진행 바 모두 한 프레임 반영 시간을 `MAX_STEP_DELTA_TIME`(0.05초)로 제한한다.
- **GameManager:** `HandleSceneLoaded`에서 `LoadSceneMode.Additive`이면 return한다. 로딩씬이 열릴 때 튜토리얼 StageManager를 다시 잡아 `TakeCurrentStage`를 반복하는 것을 막기 위해서다.
- **Loading.unity 구조**
  - 루트 `LoadingScreen` = Canvas(sortingOrder 100) + CanvasGroup(alpha 0) + LoadingScreen
  - 자식: Background, LoadingText, ProgressBar
  - `Main Camera`는 비활성으로 저장한다. 튜토리얼 카메라·AudioListener와 겹치지 않게 이전 씬 정리 직전에 켠다.
- **Play 확인** (실제 포탈 트리거, 측정 도구 없이 프레임 기록)
  - 페이드인 0.3초 동안 튜토리얼 위에 로딩 화면이 서서히 덮였다.
  - 튜토리얼 정리 멈춤 0.25초와 로비 켜기 멈춤 0.38초는 모두 alpha 1(완전히 덮임)에서 일어났다.
  - 로비 도착 후 페이드아웃 0.3초 뒤 로딩 화면이 삭제됐다. 씬 1개, AudioListener 1개, 플레이어 스폰, `IsLoading = false`를 확인했다.
  - 목적지 없이 로딩씬을 열면 alpha 1, 카메라 켜짐으로 화면이 보인다.
- **남은 멈춤:** 로딩씬 자체가 켜지는 프레임이 0.13~0.15초(첫 로드)였다. 이때는 아직 alpha 0이라 튜토리얼이 멈춘 채 보인다.
  - 같은 Play에서 두 번째로 열면 0.075초 두 번이다. 처음 읽는 에셋 비용과 에디터 오버헤드로 보인다.
  - 이전(0.24초)보다 줄었지만 0은 아니다.
- **컷씬 관련 결정:** 1번(로딩 화면 위에서 재생하며 뒤에서 불러오기)을 추천했지만, 사용자가 **2번(로딩이 다 끝난 뒤 컷씬)**으로 결정했다.
  - 순서: 진행 바 완료(목적지 0.9 대기) → 진행 바 숨기고 로딩 화면 위에서 컷씬 → 컷씬 끝(또는 스킵) → 목적지 켜기 → 페이드아웃.
  - 상세와 남은 질문은 intent-020에 적었다.

## 4-3. 컷씬 SO 분리 + 재생 구현 (같은 날)
- **사용자 요청:** 출발 씬 → 목적지 씬마다 어떤 영상을 틀지 SO에서 관리하고, 로딩 화면이 그걸 가져다 쓰게 해 달라.
- **신규 `Scripts/SceneTransition/SOLoadingCutsceneData.cs`**
  - `SLoadingCutscene { ESceneType From, ESceneType To, VideoClip Clip }` 배열을 가진다.
  - `TryGetClip(from, to, out clip)`으로 찾는다. 로비가 여러 번 나오므로 목적지만이 아니라 출발 + 목적지 쌍으로 고른다.
  - 에셋: `ScriptableObjects/SceneTransition/SOLoadingCutsceneData.asset`(현재 빈 목록). 메뉴는 `SceneTransition/Loading Cutscene Data`.
- **`LoadingScreen` 변경**
  - 필드: `_progressGroup`, `_videoPlayer`, `_cutsceneScreen`(RawImage), `_cutsceneData`(비우면 컷씬 없음)
  - `CoLoad` 시작 때 `PreviousScene`과 `PendingScene`을 받아 둔다. 진행 바 완료와 `_holdAfterFull` 뒤에 이동에 맞는 영상이 있으면 `CoPlayCutscene`을 실행한다.
  - `CoPlayCutscene`:
    - ProgressGroup 숨김 → Prepare → RawImage에 `videoPlayer.texture` 연결 → Play → `loopPointReached`까지 대기
    - `errorReceived`가 오면 경고를 남기고 넘어간다.
    - 끝나면 RawImage를 꺼서 목적지 켜는 동안은 검은 배경이 보인다.
- **Loading.unity**
  - 루트 자식 순서: Background → Cutscene → ProgressGroup(LoadingText, ProgressBar)
  - Cutscene 오브젝트: RawImage(꺼 둠)와 VideoPlayer(Play On Awake 끔, Render Mode API Only, Audio Direct). 오브젝트는 켜 둔다(VideoPlayer가 동작해야 함).
- **Play 확인** (SO 에셋은 그대로 두고 Play 중 임시 SO를 끼워 넣음, 테스트 영상 `Assets/Test/LoadingCutsceneTest/TestCutscene.avi` 2초)
  - Tutorial → Lobby(실제 포탈), Lobby → Feed(실제 포탈): 진행 바 완료 → 진행 바 숨김 → 영상 2.0초 → 그 뒤 목적지 켜기 → 페이드아웃 순서를 확인했다.
  - Lobby → File: 영상 프레임이 1→29까지 진행됐다. 15번째 프레임 스크린샷에 영상이 화면 전체로 나왔다.
  - File → Lobby(컷씬 없음): 영상 없이 진행 바만 보이고 넘어갔다.
- **주의**
  - 직접 만든 무압축 AVI는 Windows 디코더가 풀지 못해 `frame = -1`로 시간만 흘렀다. 임포트 설정에서 VP8로 변환한 뒤 정상 재생됐다. 실제 컷씬도 재생이 안 되면 VideoClip 임포트 설정(Transcode, VP8 또는 H264)을 먼저 확인한다.
  - 테스트 중 콘솔에 `Got null handle from IDXGIResource::GetSharedHandle` 에러가 여러 번 찍혔다. 스택이 없고 컷씬이 없는 이동 중에도 나와서 원인은 확인하지 못했다(에디터 그래픽 쪽으로 추정).

## 4-4. 실제 영상(mp4)이 안 나온 원인 (같은 날)
사용자가 `Art/TestVideo/Roblox 2026-09-30 14-24-56.mp4`(H.264, 1824×1080, 13.9초, 소리 있음)를 SO에 넣었지만 컷씬이 나오지 않았다. 원인은 세 가지였다.
1. **SO 칸 설정:** `From = Tutorial`, `To = Tutorial`로 되어 있어 어떤 이동에도 선택되지 않았다. To의 기본값(첫 항목)이 그대로 남은 것이다. 사용자가 `To = Lobby`로 고쳤다.
2. **AI 시험 훅 간섭:** AI가 Play 중 걸어 둔 `sceneLoaded` 훅(로딩씬의 `_cutsceneData`를 임시 SO로 바꿔 끼움)이, 사용자가 같은 시간에 Play한 세션에 등록되어 사용자 테스트에서 SO를 바꿔 버렸다. 훅은 모두 해제했다.
   - 이후 시험은 데이터를 바꿔 끼우지 않고 실제 SO 그대로 했다.
   - 교훈: 에디터에 시험용 정적 훅을 걸면 반드시 같은 단계에서 해제한다. 사용자가 동시에 Play할 수 있다.
3. **영상 디코딩:** 원본 mp4는 에디터에서 디코딩이 극도로 느렸다.
   - Prepare까지 오래 걸렸고, 프레임 0에 `isPaused`로 멈췄다. 오디오를 꺼도 8초에 0.33초만 진행됐다.
   - 콘솔의 `IDXGIResource::GetSharedHandle` 에러도 같이 났다(Windows 하드웨어 디코딩 쪽으로 추정).
   - 임포트 설정을 **Transcode 켜기, Codec VP8**로 바꾸니(변환 38초) 해결됐다. `.meta`의 `enableTranscoding: 1`, `codec: 2`가 바뀐다.
- **확인:** 실제 SO(Tutorial → Lobby), 실제 포탈 트리거로 시험했다.
  - 진행 바 완료 → 영상 13.9초 실시간 재생(1초에 약 30프레임) → 끝난 직후 로비 켜기 → 플레이어 스폰을 확인했다.
- **앞으로 넣는 컷씬 영상:** 임포트 설정에서 Transcode를 켜고 VP8로 맞춘다. 재생이 안 되면 SO 칸의 From/To부터 확인한다.
- `Art/TestVideo/`는 아직 Git에 추가되지 않은 사용자 테스트 폴더다(18MB). 커밋에 넣을지는 사용자가 정한다.
- **3초로 자르기 (사용자 요청):** 원본 mp4는 그대로 두고 임포트 설정에서 Frame Range를 Custom, 0~90프레임(30.23fps 기준 3.01초)으로 지정했다.
  - `.meta`의 `frameRange: 1`, `startFrame: 0`, `endFrame: 90`이 바뀐다. 자르기는 Transcode(VP8)가 켜져 있을 때만 적용된다.
  - 실제 SO와 포탈로 확인: 진행 바 완료 → 영상 3초 재생 → 로비 켜기. 로딩 화면 전체 약 5.3초.
  - 구간을 바꾸려면 영상 Inspector에서 Start/End Frame을 고친다.

## 4-5. 남은 확인 마무리 (같은 날, 이어받은 세션)
- 코드는 바꾸지 않았다. 컴파일 에러 0, Play 전후 `git status` 변화 없음.
- **첫 라운드 사망 재시작 확인** (`Map_2_Feed`, `_restartSceneOnFirstRoundDeath = 1`)
  - Tutorial에서 Play → eval로 `TryLoad(Lobby)` → `TryLoad(Feed)`로 피드 도착. `IsRunning = true`, `_currentRoundIndex = 0`.
  - `StageManager.HandlePlayerDeath`를 리플렉션으로 직접 호출했다(실제 사망 애니메이션 이벤트는 거치지 않음).
  - 결과: 로딩씬 없이 `Map_2_Feed`만 다시 불러와졌다. StageManager는 새 인스턴스로 `IsRunning = true`였다.
  - `IsLoading = false`, `PreviousScene = Lobby`, `CanEnter(Lobby) = true`(순서 단계 유지), LoadingScreen 없음, AudioListener 1개, PlayerFacade 1개 스폰.
- **에디터 주의: 포커스를 잃으면 컷씬이 멈춘다**
  - 에디터가 백그라운드일 때 Tutorial → Lobby 컷씬이 Prepare까지는 되고 `isPaused = true`, `frame = 0`에서 멈췄다. 그래서 로딩 화면이 끝나지 않았다.
  - `Application.runInBackground = true`로 바꾸거나 `VideoPlayer.Play()`를 다시 불러도 풀리지 않았다. 에디터에 포커스를 주자 바로 재생되고 로비에 도착했다.
  - 코드 문제가 아니라 VideoPlayer가 앱 포커스를 따르는 동작으로 본다. 빌드는 포커스를 잃으면 게임 전체가 멈췄다가 돌아오면 이어서 재생되므로 영향이 없을 것으로 본다(빌드 미확인).
  - MCP로 시험할 때는 Play 전에 에디터에 포커스를 준다(`editor_focus`).
- Run In Background는 시험 중에만 켜고 false로 되돌렸다(`PlayerSettings.runInBackground = False` 확인).
- **커밋 전 확인할 것**
  - `ScriptableObjects/SceneTransition/SOLoadingCutsceneData.asset`이 `Art/TestVideo/` 영상(Tutorial → Lobby)을 참조한다. 영상 폴더를 빼고 커밋하면 다른 사람 환경에서 Clip이 Missing이 된다(재생은 건너뜀).
  - `Art/Scan/M_ScanPlatformHologram.mat`은 줄바꿈 차이만 있고, `Art/Scan/NotUse/M_ScanRevealSciFi.mat`은 셰이더·부모 머티리얼이 바뀌어 있다. 둘 다 이번 작업과 관계없다.

## 5. 다음 할 일 (Next Steps)
- 실제 컷씬 영상이 생기면 `SOLoadingCutsceneData.asset`에 (From, To, Clip) 칸을 추가한다. 스킵 허용 여부를 정한다.
- 빌드에서 튜토리얼 → 로비를 재서 로딩씬이 켜지는 프레임 멈춤이 남는지 확인한다.
  - 남으면 로딩 화면을 게임 시작 때 한 번 만들어 DontDestroyOnLoad로 숨겨 두는 방식을 검토한다. 이렇게 하면 전환 때 씬을 불러오지 않는다. 컷씬 VideoPlayer를 미리 Prepare하기에도 유리하다.
- 실제 플레이 감각으로 `_fadeDuration`, `_fillDuration`, `_holdAfterFull` 값을 조절한다(Loading 씬의 LoadingScreen Inspector).
- 빌드에서 로비 이동 체감을 확인한다(에디터와 progress 동작이 다르다).
- 로비 켜기 단계의 멈춤 자체를 줄이려면 Profiler로 로비 `Awake`/`Start` 비용을 먼저 측정한다(이번 범위 밖).
