---
id: intent-038
title: AudioManager 1단계 — BGM·UI·3D 단발 재생, 볼륨 저장, 설정창 연결
part: Audio / Core / UI
status: open   # open | resolved
created: 2026-10-17
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
게임에 소리를 내는 공용 기반이 없다. 자체 Scripts에 AudioSource/AudioMixer 기반 재생 코드가 없고, 설정창(`Canvas_MainMenu`·`Canvas_InGame`)의 `Row_Audio`·`Row_Music`·`Row_SFX`는 볼륨 로직에 연결되어 있지 않다. 씬이 바뀌어도 BGM과 볼륨 설정이 이어지는 구조가 필요하다.

### 파일 미로 2·3라운드 곡 교체 (2026-10-30)
- 문제: 현재 2라운드는 `The Curious Maze`, 3라운드는 `The Labyrinth of Forgotten`이 재생된다.
- 기대 결과: 2라운드는 `The Labyrinth of Forgotten`, 3라운드는 새 `Maze Mayhem`을 재생한다.
- 영향 범위: 파일 미로 2·3라운드 BGM SO와 새 AudioClip 에셋.
- 제약: 1라운드 BGM 및 라운드 전환 코드는 유지한다. Unity 검증은 사용자 요청에 따라 하지 않는다.
- 열린 질문: 없음.
- 적용 상태: `The Labyrinth of Forgotten` 클립을 `SOAudioData_BgmFileRound2`에 연결하고, `Maze Mayhem.wav`와 메타를 추가해 `SOAudioData_BgmFileRound3`에 연결했다. 기존 1라운드 SO와 라운드 전환 코드는 유지했다. Unity 검증은 사용자 요청에 따라 생략했다.

### 파일 미로 1·2라운드 BGM 후속 (2026-10-30)
- 문제: 파일 맵의 기존 `Algorithm Arpeggio`는 라운드 구분 없이 씬 전체에서 반복된다.
- 기대 결과: 기존 곡을 `SOAudioData_BgmFileRound1`로 이름을 정리해 씬 시작 BGM으로 유지하고, 2라운드 진입 시 `The Curious Maze` 전용 `SOAudioData_BgmFileRound2`로 전환한다.
- 영향 범위: 파일 맵 BGM SO, `FileApp`, `Map_3_File`, 새 AudioClip 에셋.
- 제약: 기존 씬 참조 GUID와 다른 라운드 로직은 유지한다. 사용자 요청에 따라 Unity 검증은 하지 않는다.
- 열린 질문: 없음.
- 적용 상태: `SOAudioData_BgmFile`을 GUID를 유지한 채 `SOAudioData_BgmFileRound1`로 이름 변경했다. `The Curious Maze.wav`와 메타, `SOAudioData_BgmFileRound2`를 추가했다. 파일 씬의 AudioManager는 기존 1라운드 SO를 계속 참조하고, `FileApp.OnRoundChanged`에서 2라운드 진입 시 새 SO로 전환한다. 3라운드에는 별도 BGM 전환을 지정하지 않았다. Unity 검증은 사용자 요청에 따라 생략했다.

### 파일 미로 3라운드 BGM 후속 (2026-10-30)
- 문제: 미로 3라운드에 진입해도 2라운드 BGM이 계속 재생된다.
- 기대 결과: `The Labyrinth of Forgotten.wav`가 3라운드 진입 시 반복 재생된다.
- 영향 범위: 새 BGM SO와 AudioClip, `FileApp`, `Map_3_File`.
- 제약: 1·2라운드 BGM 연결과 다른 라운드 로직은 유지한다. 사용자 요청에 따라 Unity 검증은 하지 않는다.
- 열린 질문: 없음.
- 적용 상태: `The Labyrinth of Forgotten.wav`와 메타를 BGM 폴더에 두고 `SOAudioData_BgmFileRound3`를 만들었다. 파일 씬의 `FileApp._round3Bgm`에 연결하고 3라운드 진입 시 `AudioManager.ChangeBgm`을 호출한다. Unity 검증은 사용자 요청에 따라 생략했다.

### 로비·피드 라운드 BGM 후속 (2026-10-30)
- 문제: 로비 플레이리스트에 새 곡이 없고, 피드 맵은 1·2라운드에 같은 BGM이 재생된다.
- 기대 결과: `Whimsical City Adventure.wav`를 로비의 세 번째 곡으로 추가한다. 피드 1라운드는 기존 `Social Media Jump.wav`, 2라운드 시작부터 `Smartphone City Groove.wav`를 반복 재생한다.
- 영향 범위: 로비·피드 BGM SO, 피드 2라운드 시작점, AudioManager의 BGM 교체 호출, 새 AudioClip 에셋.
- 제약: 기존 곡과 다른 오디오 설정은 유지한다. 로비 곡은 추가만 한다.
- 열린 질문: 없음.
- 적용 상태: 두 WAV와 메타를 `Assets/ThirdParty/Audio/Bgm`에 추가했다. 로비 BGM 목록의 세 번째 곡으로 `Whimsical City Adventure`를 연결했다. 피드 씬 시작 BGM은 기존 `Social Media Jump`를 유지하고, `FeedRound2.Begin`이 성공하면 `Smartphone City Groove` 전용 SO로 전환하도록 연결했다. 사용자 요청에 따라 재생·컴파일 검증은 하지 않았다.
- 후속 수정: 에디터 포커스 복귀 시 `AudioSource.isPlaying == false`를 곡 종료로 오인해 다음 곡으로 넘어가는 조건을 제거했다. 남은 재생 시간이 페이드아웃 구간에 도달할 때만 곡을 넘긴다. 재생 검증은 요청에 따라 생략했다.
- 명명 정리: 기존 피드 시작 BGM SO 파일과 에셋 이름을 `SOAudioData_BgmFeedRound1`로 바꾸고 GUID를 유지했다. 2라운드 SO는 `SOAudioData_BgmFeedRound2`다.

### 메인 메뉴 음원 후속 (2026-10-29)
- 문제: 메인 씬 BGM이 검은 스플래시 화면에서도 즉시 시작하며, 지정한 `Whimsical Technology World.wav`가 프로젝트에 없다.
- 기대 결과: 음원을 `Assets/ThirdParty/Audio`에 넣고 메인 메뉴 BGM SO에 지정한다. 검은 화면의 AnyPress 입력으로 메뉴가 나타날 때부터 BGM을 재생한다.
- 영향 범위: `MainScene`, `MainMenuPresenter`, `AudioManager`, 메인 메뉴 BGM SO.
- 제약: 다른 씬의 BGM 자동 재생과 기존 스플래시 클릭 효과음은 유지한다.
- 열린 질문: 없음.
- 적용 상태: WAV가 `Assets/ThirdParty/Audio`로 이동했고 GUID는 유지됨. SO·씬·입력 연결 및 원본/현재 파일 SHA-256, 직렬화 참조 검사 통과. Unity 컴파일 최신 상태, MainScene Play에서 AnyPress 전 BGM 정지와 Space 입력 뒤 지정 클립 재생 확인. 귀로 듣는 음량 평가는 남음.

### 로비 BGM 음원 후속 (2026-10-29)
- 문제: 로비 씬에 AudioManager는 배치되어 있으나 BGM SO 참조가 비어 있다.
- 기대 결과: `Digital City Morning.wav`를 `Assets/ThirdParty/Audio`에 두고, 로비용 BGM SO의 단일 반복 곡으로 지정해 로비 입장부터 재생한다.
- 영향 범위: `Map_1_Lobby`, `SOAudioData_BgmLobby`, 서드파티 AudioClip.
- 제약: `Assets/ThirdParty`는 Git에서 제외되므로 음원·메타는 별도로 공유해야 한다.
- 열린 질문: 없음.
- 적용 상태: WAV와 메타 추가, 기존 로비 BGM SO를 단일 클립으로 교체하고 로비 씬 AudioManager에 연결. Unity 임포트 126.8초, Play에서 `playing=True`, 클립 `Digital City Morning`, `loop=True`, 음량 0.60 확인. 직접 청취 평가는 남음.

### 피드 맵 BGM 음원 후속 (2026-10-29)
- 문제: 피드 씬에 AudioManager는 배치돼 있으나 BGM SO 참조가 비어 있다.
- 기대 결과: `Social Media Jump.wav`를 `Assets/ThirdParty/Audio`에 넣고 피드 전용 BGM SO를 만들어 씬 입장부터 반복 재생한다.
- 영향 범위: `Map_2_Feed`, 새 `SOAudioData_BgmFeed`, 서드파티 AudioClip.
- 제약: `Assets/ThirdParty`는 Git에서 제외되므로 음원·메타는 별도로 공유해야 한다. 다른 씬 오디오 설정은 유지한다.
- 열린 질문: 없음.
- 적용 상태: WAV·메타와 `SOAudioData_BgmFeed` 생성, 피드 씬 AudioManager에 연결. Unity 임포트 72.2초, Play에서 `playing=True`, 클립 `Social Media Jump`, `loop=True`, 음량 0.60 확인. 직접 청취 평가는 남음.

### 로비 BGM 두 번째 곡 추가 (2026-10-29)
- 문제: 로비 BGM 목록에 `Digital City Morning` 한 곡만 있다.
- 기대 결과: 제공된 `Colorful City Morning.wav`를 로비 음악으로도 재생한다.
- 영향 범위: `SOAudioData_BgmLobby`, 새 AudioClip 에셋.
- 제약: 기존 로비 곡을 유지하고, 사용자 요청에 따라 검증은 실행하지 않는다.
- 열린 질문: 없음.
- 적용 상태: WAV와 AudioImporter 메타를 `Assets/ThirdParty/Audio/Bgm`에 두고 로비 BGM 목록의 두 번째 곡으로 연결했다. 재생 순서는 기존 곡 다음이다. 검증은 사용자에게 남겼다.

### 파일 앱 미로맵 BGM 추가 (2026-10-29)
- 문제: `Map_3_File` 씬의 AudioManager에 BGM이 지정되지 않았다.
- 기대 결과: 제공된 `Algorithm Arpeggio.wav`가 미로맵에서 반복 재생된다.
- 영향 범위: `Map_3_File`, 새 `SOAudioData_BgmFile`, 새 AudioClip 에셋.
- 제약: 기존 씬 오디오 설정을 유지하고, 사용자 요청에 따라 검증은 실행하지 않는다.
- 열린 질문: 없음.
- 적용 상태: WAV와 메타를 `Assets/ThirdParty/Audio/Bgm`에 두고 단일 클립 BGM SO를 씬 AudioManager에 연결했다. 검증은 사용자에게 남겼다.

### BGM 파일 폴더 통일 (2026-10-29)
- 문제: BGM WAV 5개가 `Assets/ThirdParty/Audio`와 `Assets/_Project/Audio`에 나뉘어 있다.
- 기대 결과: 모든 BGM WAV와 메타를 `Assets/ThirdParty/Audio/Bgm`에 둔다.
- 영향 범위: BGM 5개와 각 메타 파일, 새 Bgm 폴더 메타.
- 제약: 기존 GUID와 SO·씬 연결을 유지한다. 사용자 요청에 따라 검증은 실행하지 않는다.
- 열린 질문: 없음.
- 적용 상태: `Digital City Morning`, `Social Media Jump`, `Whimsical Technology World`, `Algorithm Arpeggio`, `Colorful City Morning`의 WAV와 메타를 새 폴더로 이동했다.

설계 배경과 검토 과정은 [audio-manager-design.md](../HANDOVER/SungJun/2026-10-17/audio-manager-design.md)에 있다. 이 문서는 그 뒤 대화에서 단순화·확정한 내용을 기준으로 한다.

## ★ 구조 변경: 씬 단위 AudioManager (2026-10-17 사용자 확정)
아래 "기대 결과"의 싱글톤·`SOBgmTable` 설계는 이 결정으로 대체된다. 바뀌지 않은 부분(BGM 플레이리스트·곡 넘기기·SO별 페이드, UI 소스, 3D 소스 16개 재사용, 같은 클립 중복 차단, 믹서 볼륨·저장, 설정창 연결, `AudioListener.pause` 일시정지)은 그대로 유지한다.
- **이유**: 씬마다 필요한 소리만 메모리에 올리고, 씬을 나가면 그 씬 클립이 같이 내려가게 한다. 팀이 익숙한 `ObjectPoolManager`와 같은 방식.
- **매니저**: `Singleton<T>`·`DontDestroyOnLoad`를 쓰지 않는다. 씬마다 하나 배치하고, `static Instance`는 Awake에서 채우고 OnDestroy에서 비운다. 매니저가 없는 씬에서는 `Instance`가 null이다.
- **효과음 지정 (SO 직접 참조, 2026-10-17 사용자 확정)**: 한때 문자열 키 + 소리 묶음 SO(`SOAudioPack`)로 씬마다 등록하는 방식을 구현했으나, 부르는 쪽이 키를 다 알아야 해서 되돌렸다.
  - 소리를 내는 컴포넌트가 `[SerializeField] SOAudioData` 필드로 SO를 들고 있다가 넘긴다. 매니저에 등록하지 않는다.
  - "씬마다 필요한 소리만"은 Unity가 씬 오브젝트가 참조하는 에셋만 로드하는 것으로 자동으로 지켜진다. 예) 플레이어 프리팹이 소리 4개를 들고 있으면, 플레이어가 있는 씬에서만 4개가 올라온다.
- **호출**: `PlaySfx(SOAudioData, 위치, volumeScale, pitchScale)`, `PlayUi(SOAudioData, volumeScale)`, `SkipBgmTrack()`.
- **BGM**: 매니저 인스펙터의 BGM 칸(`SOAudioData`, 플레이리스트 가능)을 Start에서 페이드인으로 튼다. `SOBgmTable`·씬 판별(`MAIN_SCENE_NAME`, 빌드 인덱스)은 제거한다.
- **씬 전환**: 로딩씬이 Additive로 열리면 지금 BGM을 `_sceneExitFadeDuration`(기본 0.3초, `LoadingScreen._fadeDuration`과 맞춤) 동안 줄인다 → 로딩 중 무음 → 새 씬 매니저가 페이드인. 씬을 넘어 곡이 이어지지는 않는다 (튜토리얼에서 메인메뉴 곡 이어 듣기 불가, 씬 재시작 시 곡 처음부터). `TryStartNewGame`은 `OnBeforeSceneChange`를 보내지 않으므로 로딩씬 Additive 로드로 판단한다.
- **볼륨**: 매니저가 생길 때마다 PlayerPrefs에서 읽고 Start에서 믹서에 적용한다.
- **구현 결과 (2026-10-17)**: `AudioManager`를 `MonoBehaviour` + `static Instance`로 변경, `SOBgmTable` 스크립트·에셋 삭제. (키 방식용 `SOAudioPack`과 예시 묶음은 만들었다가 SO 직접 참조로 되돌리며 삭제.) 프리팹 인스펙터에는 믹서 참조·BGM·씬 나갈 때 페이드 시간·3D 소스 수·중복 차단 간격만 남았다. MainScene 인스턴스에서 BGM만 메인메뉴 곡으로 오버라이드 (씬 저장 시 Unity가 Canvas_MainMenu 배치값을 같이 써서, 씬 파일은 되돌리고 `_bgm` 오버라이드 한 줄만 넣음). 컴파일 에러 0.
  - Play(프레임 스텝) 확인: MainScene 매니저가 씬 소속(DontDestroyOnLoad 아님), 메인메뉴 곡 재생, SO를 넘긴 `PlaySfx`(위치·음량 배율·거리 적용)·`PlayUi` 동작, null SO는 경고만. START → 로딩씬 Additive 로드 다음 프레임에 MainScene 매니저 파괴, 튜토리얼에서는 매니저가 없어 `Instance == null` (에러 없음).
  - 씬 나갈 때 0.3초 페이드아웃은 에디터 일시정지 중 `isPlaying`이 false라 스텝으로 확인할 수 없어 청취 확인이 필요하다.
  - 맵 씬에는 아직 매니저가 없다 → 튜토리얼·로비 등은 현재 소리가 나지 않는다. 로비용 `Bgm/SOAudioData_BgmLobby`는 로비에 매니저를 둘 때 BGM 칸에 넣으면 된다.

## 기대 결과 (Proposed outcome)

### 구조
- `AudioManager : Singleton<AudioManager>` (씬이 바뀌어도 유지). 재생과 볼륨만 담당하고, 언제 어떤 소리를 낼지는 각 기능이 정한다.
- AudioManager가 가진 스피커(AudioSource)는 세 종류다.
  - **BGM 1개**: 곡 변경 시 페이드아웃 → 교체 → 페이드인. 같은 곡을 다시 요청하면 이어서 재생한다. 페이드는 `unscaledDeltaTime`을 쓰고, 페이드 시간은 매니저 필드로 고정한다.
  - **UI 1개**: `PlayOneShot`으로 버튼·확인음을 낸다.
  - **3D 16개 고정 배열**: 발생 위치에서 단발음을 낸다. 반납 단계 없이, 재생할 때 `끝나는 시각 = Time.time + clip.length / pitch`를 배열에 적어 두고 새 요청 때 끝난 소스를 고른다. Update 루프와 `ObjectPool<T>`를 쓰지 않는다. 16개가 모두 재생 중이면 새 요청을 생략한다.
- 같은 클립이 짧은 간격(약 0.05초) 안에 다시 요청되면 건너뛴다 (클립별 마지막 재생 시각 Dictionary).

### 데이터 (SO)
- `SOAudioData`: 클립 배열(랜덤 선택), 기본 음량, 피치 범위. 런타임 상태는 저장하지 않는다. (2026-10-17 변경: 3D 거리 min/max를 SO마다 두도록 바꿈 — 폭발·발소리처럼 들리는 거리가 다른 소리 대응)
- `SOBgmTable`: `ESceneType` → `SOAudioData` 목록 + 메인메뉴 곡 전용 필드 `_mainMenuBgm`. 곡을 비워 둔 항목은 무음(`StopBgm`)으로 처리한다.
- `EAudioChannel`: Master, Bgm, Sfx. (2026-10-17 Music → Bgm으로 이름 통일)

### 씬별 BGM 선택
`AudioManager`가 `SceneManager.sceneLoaded`를 구독해 아래 순서로 판단한다. 씬마다 컴포넌트를 두지 않으므로 씬 파일을 수정하지 않는다.
1. `LoadSceneMode.Additive`(로딩씬)면 무시한다 → 이전 곡 유지.
2. MainScene이면(`scene.name == MAIN_SCENE_NAME`) `_mainMenuBgm`을 재생한다.
3. `scene.buildIndex`가 `ESceneType`에 정의된 값이면 표에서 곡을 찾아 재생한다 (`SceneLoader`의 `(int)ESceneType` = 빌드 인덱스 규칙을 그대로 따름).
4. 그 밖의 씬(테스트 씬 등)은 현재 곡을 유지한다.
- 씬 재시작(`StageManager`의 `LoadScene(buildIndex)`)도 같은 곡이 나오므로 이어서 재생된다.
- ~~씬 안에서 곡을 바꾸는 경우(보스전 등)는 해당 코드가 `PlayBgm`을 직접 부른다.~~ → (2026-10-17 사용자 확정) BGM은 현재 씬 플레이리스트 안에서만 바꾼다. `PlayBgm`·`StopBgm`은 private이고, 밖에서는 `SkipBgmTrack()`(다음 곡으로, 페이드아웃 → 페이드인)만 부른다. 예: 로비의 곡 넘기기 버튼.
- 표는 씬 진입 곡만 정한다. 실행 중에 표(SO)를 바꾸지 않는다 (에디터에서는 에셋에 남고, 표는 씬 로드 때만 읽는다).

### BGM 플레이리스트 (2026-10-17 추가)
- BGM용 `SOAudioData`에 클립이 2개 이상이면 플레이리스트로 재생한다. 한 곡이 끝나면 다음 곡을 바로 이어서 튼다 (곡 사이 페이드 없음). 클립이 1개면 지금처럼 반복 재생한다.
- 순서는 SO의 `_isShuffle`로 정한다. 끄면 위에서부터 차례로(끝나면 처음으로), 켜면 무작위(같은 곡 연속 없음). 곡마다 피치 범위에서 피치를 다시 고른다.
- 지금 몇 번째 곡인지는 SO가 아니라 AudioManager가 기억한다. 같은 SO를 다시 요청하면 플레이리스트를 처음부터 다시 틀지 않고 이어간다.
- 곡이 끝났는지는 BGM 코루틴이 `isPlaying`으로 판단한다. BGM 소스는 `ignoreListenerPause`라 게임 일시정지 중에도 계속 흐르고, 에디터 일시정지·앱 비활성 중에는 코루틴도 멈춘다.
- 효과음·UI는 지금처럼 매번 무작위로 클립을 고른다 (`_isShuffle`은 BGM에만 쓰인다).
- (2026-10-17 사용자 확정) 반복 설정은 추가하지 않는다: 플레이리스트는 끝에서 처음으로 돌아가 계속 재생, 클립 1개는 무한 반복.
- (2026-10-17 사용자 확정) 페이드는 SO마다 `_fadeInDuration`·`_fadeOutDuration`(기본 1초)으로 정한다. AudioManager의 공통 `_bgmFadeDuration`은 제거했다.
  - 다른 BGM으로 바뀌거나 꺼질 때: 지금 곡을 그 SO의 페이드아웃 시간 동안 줄이고, 새 곡을 새 SO의 페이드인 시간 동안 키운다.
  - 플레이리스트 곡 사이: 곡이 끝나기 페이드아웃 시간 전부터 줄여 끄고, 다음 곡을 0에서 페이드인한다 (겹치는 크로스페이드는 하지 않는다, 스피커 1개 유지).

### 공개 API (6개, 2026-10-17 갱신)
`SkipBgmTrack()`, `PlayUi(SOAudioData, float volumeScale = 1)`, `PlaySfx(SOAudioData, Vector3, float volumeScale = 1, float pitchScale = 1)`, `SetVolume(EAudioChannel, float)`, `GetVolume(EAudioChannel)`, `SaveVolume()`
- 효과음 조절: 소리 자체의 성질(클립 후보, 기본 음량, 피치 범위, 들리는 거리)은 SO에서, 상황에 따라 달라지는 값(차지샷은 크게, 차지 단계에 따라 피치 올리기 등)은 부르는 쪽이 `volumeScale`·`pitchScale`로 정한다. 최종 음량은 0~1로 자르고, 피치는 0.1 이상으로 막는다.

### 볼륨
- AudioMixer: Master 아래 Bgm, Sfx. UI와 3D 효과음은 Sfx 그룹으로 출력한다. 볼륨 파라미터 3개를 Expose한다.
- 0~1 값을 `20 * log10(max(value, 0.0001))` dB로 바꿔 믹서에 적용한다. 믹서 초기 적용은 `Start`에서 한다 (Awake/OnEnable에서는 적용되지 않음).
- PlayerPrefs에 저장한다. 슬라이더를 움직일 때마다 저장하지 않고, 설정창을 닫을 때(`SettingsPresenter.Close`) `SaveVolume()`을 호출한다.

### 일시정지
- `GameManager.PauseGame/ResumeGame`에 `AudioListener.pause = true/false`를 한 줄씩 추가한다.
- BGM·UI 소스는 `ignoreListenerPause = true` → 설정창에서도 계속 들린다. 3D 효과음만 멈춘다.
- 별도 일시정지 상태나 API는 두지 않는다. 일시정지 중에는 `Time.time`이 멈추므로 멈춘 3D 소스가 끝난 것으로 잘못 처리되지 않는다.

### 성공 기준
- 컴파일 에러 0.
- MainScene → 로딩 → 튜토리얼 이동 시 곡이 표대로 바뀌고, 로딩 중에는 이전 곡이 유지된다.
- 맵 씬 단독 Play, 매니저 중복(씬마다 배치) 상황에서 에러가 없다.
- 볼륨 0·1 설정 후 재실행해도 값이 복원된다.
- 설정창을 연 동안 BGM·UI 소리는 들리고 3D 효과음은 멈췄다가 닫으면 이어진다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 소리를 붙일 팀 전체 (`PlaySfx`·`PlayUi` 호출), 설정창을 쓰는 플레이어.
- 새로 만드는 것 (`Assets/_Project/Scripts/Core/Audio/`): `AudioManager.cs`, `SOAudioData.cs`, `SOBgmTable.cs`, `EAudioChannel.cs`
- 기존 코드 수정:
  - `Core/GameManager.cs` — `PauseGame/ResumeGame`에 `AudioListener.pause` 각 한 줄
  - `UI/SettingsView.cs` — 슬라이더 3개 필드, `VolumeChanged` 이벤트, 이벤트 없이 값을 표시하는 메서드
  - `UI/SettingsPresenter.cs` — `Open`에서 현재 볼륨 표시, 변경 시 `SetVolume`, `Close`에서 `SaveVolume`
- 에셋/씬: AudioMixer 1개, `SOBgmTable` 에셋 1개, AudioManager 프리팹 (MainScene·Loading·맵 씬 6개에 배치), `Canvas_MainMenu`·`Canvas_InGame` 설정창 슬라이더 연결
- 건드리지 않는 것: `Singleton.cs`, `SceneLoader.cs`, `ObjectPoolManager.cs`

## 제약 (Constraints)
- 지켜야 할 정책: `docs/coding-convention.md` (SO 이름은 `SO` + PascalCase). `base.Awake()` 다음 `Instance != this`면 반환하고, `OnEnable` 구독도 같은 조건으로 보호한다 (GameManager와 같은 방식).
- 매니저가 없는 씬에서는 `Singleton.Instance`가 호출마다 씬 전체를 탐색하고 에러를 낸다 → 소리를 낼 수 있는 모든 씬에 프리팹을 배치한다.
- 맵 씬은 팀원도 작업하는 파일이다. AudioManager 프리팹 배치 외에는 건드리지 않는다.
- 클립 임포트 설정: BGM `Streaming`, 짧은 효과음 `Decompress On Load`, 중간 길이 `Compressed In Memory`, 3D 효과음 Force To Mono.
- 쓰지 말아야 할 것 (1단계): 씬 전환 시 효과음 정리(`StopSceneSfx`, `sceneUnloaded` 구독), 루프·추적음, 크로스페이드, UI 연타 제한, 소스 우선순위 교체, IAudioService 같은 인터페이스 분리, 이벤트 버스. 필요해지면 그때 추가한다.
- 2단계(범위 밖): 실제 사격음(`CharacterMediator.OnFireCallback`)·피격·사망음(`PlayerFacade.OnHit/OnDeath`) 연결.

## 결정 사항 (2026-10-17, 열린 질문 확인 결과)
1. **MainScene 판별**: `AudioManager`에 `MAIN_SCENE_NAME = "MainScene"` 상수를 두고 `scene.name`으로 비교한다 (`SceneLoader.LOADING_SCENE_NAME`과 같은 방식).
2. **맵 씬 EventSystem**: 이번 작업에서 다루지 않고 intent-037(Canvas_InGame 교체)에서 처리한다. 그 전까지 볼륨 슬라이더 검증은 MainScene 설정창으로 한다.
3. **설정창 행 구성** (프리팹 확인 결과): `Canvas_MainMenu`·`Canvas_InGame` 모두 `Row_Audio`(라벨 "MASTER VOLUME")·`Row_Music`("MUSIC")·`Row_SFX`("SFX") 아래에 UGUI `Slider`(0~1, 정수 아님)가 있다. `Row_Audio` = Master, `Row_Music` = Bgm, `Row_SFX` = Sfx로 연결한다. `Row_Controls`는 제외한다.
4. **검증용 음원**: Unity 엔진 자체에는 기본 제공 오디오 클립이 없다. 이미 프로젝트에 있는 ThirdParty 소리로 검증한다.
   - BGM: `Shift - Complete Sci-Fi UI/Audio/Background.ogg` (MainScene), 곡 전환 확인용 두 번째 곡은 `Archanor/Sci-Fi Arsenal/.../Sound/Loops/loop_energy.wav` (튜토리얼 → 2026-10-17 사용자 요청으로 표에서 Tutorial 항목 삭제, 튜토리얼은 메인메뉴 곡을 이어서 재생. `SOAudioData_BgmTutorial`은 예시로 남김)
   - UI: `Shift - Complete Sci-Fi UI/Audio/Click.ogg`
   - 3D: `Archanor/Sci-Fi Arsenal/.../Sound/Impact/shoot_laser.wav`
   - ThirdParty 폴더는 Git에 없으므로 이 클립을 참조하는 `SOAudioData`는 팀원도 같은 에셋을 가져와야 소리가 난다 (`Canvas_InGame`의 Shift 의존과 같은 조건). 정식 음원이 생기면 `Assets/_Project/Audio/`로 교체한다.
5. **수치 기본값**: BGM 페이드 1초, 같은 클립 재요청 간격 0.05초, 3D 거리 min 1 / max 30으로 시작한다. 모두 인스펙터 필드로 두고 Play하면서 조정한다.

## 진행 상황 (2026-10-17)
- **완료**: 스크립트 4개(`Core/Audio/`), GameManager·SettingsView·SettingsPresenter 수정, 컴파일 에러 0.
- **완료**: `Assets/_Project/Audio/MainAudioMixer.mixer`(Master > Music, Sfx, 노출 파라미터 `MasterVolume`·`MusicVolume`·`SfxVolume`), `ScriptableObjects/Audio/`(SO 5개, 종류별 하위 폴더 `Bgm/`·`Ui/`·`Sfx/`, `SOBgmTable`은 `Audio/` 바로 아래), `Prefabs/System/Manager/AudioManager.prefab`, `Canvas_MainMenu`·`Canvas_InGame`의 `SettingsView` 슬라이더 3개 연결.
- **씬 배치**: MainScene 루트에 AudioManager 프리팹 인스턴스 배치 완료 (2026-10-17, 오버라이드 없음). 맵 씬 6개는 아직 배치하지 않았다 (사용자 결정 대기). Loading 씬은 단독 실행되지 않으므로 배치 대상에서 뺀다.
- **Play 검증** (MainScene에 저장하지 않은 임시 인스턴스, 에디터 일시정지 + 프레임 스텝): 메인메뉴 곡 → 로딩 중 유지 → 튜토리얼 곡 전환, 같은 곡 재요청 시 유지, 중복 매니저 제거, 슬라이더 → 믹서 dB(0.5 → -6.02, 0 → -80) → 닫을 때 PlayerPrefs 저장, 3D 소스 위치·끝나는 시각 기록, 같은 프레임 같은 클립 차단, 일시정지 시 `AudioListener.pause`·`Time.time` 정지 확인. 실제 소리는 에디터가 백그라운드라 듣지 못했다 → 사용자 청취 확인 필요.
- **BGM 플레이리스트**: `SOAudioData._isShuffle`·`ClipCount`·`GetClip`, AudioManager `_bgmTrackIndex`·`PlayBgmTrack`·`PickNextTrackIndex` 추가, 컴파일 에러 0. 프레임 스텝 검증: 차례로(0→1→2→0 반복), 무작위(페이드인 이후 300곡 연속 중복 0), 클립 1개 SO는 `loop = true`로 반복. 실제 곡 길이만큼 기다렸다 넘어가는지는 에디터가 백그라운드라 실시간으로 확인하지 못했다. 예시 `Bgm/SOAudioData_BgmPlaylistExample`(Background → loop_energy, 차례로)은 어디에도 연결하지 않았다.
- **BGM 페이드 (SO마다)**: `SOAudioData._fadeInDuration`·`_fadeOutDuration`, AudioManager `_musicFadeOutDuration`·`GetRemainingMusicTime` 추가, `CoFadeMusic`에 시간 인자 추가, `_bgmFadeDuration` 제거. 컴파일 에러 0. 프레임 스텝으로 곡마다 0에서 시작해 SO의 페이드인 시간(0.5초 설정)만큼 걸려 음량에 도달하는 것을 확인했다. 곡 끝 페이드아웃은 에디터 일시정지 중 `isPlaying`이 false라 스텝으로 확인할 수 없어 사용자 청취 확인이 필요하다.
- **BGM 곡 넘기기·효과음 매개변수**: `SkipBgmTrack()` 추가, `PlayBgm`·`StopBgm` private로 변경, 플레이리스트 재생을 `CoPlayTracks`·`CoFadeOutMusic`로 정리(씬 진입과 곡 넘기기가 같은 흐름 사용). `SOAudioData`에 3D `_minDistance`·`_maxDistance` 추가, AudioManager의 공통 3D 거리 필드 제거. `PlaySfx`에 `volumeScale`·`pitchScale`, `PlayUi`에 `volumeScale` 추가. 로비 플레이리스트 `Bgm/SOAudioData_BgmLobby`(테스트 클립 3개: Background·loop_energy·loop_lightning)를 만들고 `SOBgmTable`에 Lobby로 등록. 프레임 스텝 검증: 곡 넘기기 시 다음 곡 번호로 바뀌고 0에서 다시 페이드인, 끝에서 처음으로 돌아감, 곡 1개 SO에서는 무시. 효과음 배율·거리 적용, 음량 1 초과는 1로, 피치 0은 0.1로 막힘. 곡 넘기기 때의 페이드아웃은 청취 확인 필요. 버튼(입력) 연결은 하지 않았다.
- **이름 통일 (Music → Bgm)**: Music 채널로 나가는 소리가 BGM뿐이라 이름을 Bgm으로 맞췄다. `EAudioChannel.Bgm`, 믹서 그룹 `Bgm`·노출 파라미터 `BgmVolume`, PlayerPrefs 키 `Volume_Bgm`(기존 저장 값은 기본값 1로 돌아감), AudioManager `_bgmGroup`·`_bgmSource`·`GetRemainingBgmTime`·`CoFadeBgm`·`CoFadeOutBgm`, SettingsView `_bgmSlider`·`HandleBgmChanged`. 직렬화 필드 `_bgmGroup`·`_bgmSlider`는 `[FormerlySerializedAs]`로 기존 프리팹 연결을 유지한다(확인: AudioManager 프리팹 → Bgm 그룹, 두 Canvas의 SettingsView → `Row_Music` 슬라이더). 설정창의 `Row_Music` 오브젝트 이름과 "MUSIC" 라벨은 화면 표시라 바꾸지 않았다. 컴파일 에러 0, 실제 Play 확인은 하지 않았다.
- **수정**: `SettingsPresenter.Close`가 볼륨을 바꾸지 않았을 때도 `AudioManager.Instance`를 불러, 매니저 없는 씬·씬 정리 중에 "AudioManager이 없습니다" 에러가 났다 → 바꿨을 때만 저장하도록 수정 (Play로 확인).
- **알려진 점**: 씬 활성화처럼 긴 프레임이 끼면 `unscaledDeltaTime`이 커서 BGM 페이드아웃이 한 프레임에 끝날 수 있다. `Canvas_MainMenu`·`Canvas_InGame`의 `SettingsPanel`에 Presenter가 참조하지 않는 두 번째 `SettingsView`가 붙어 있다(기존 상태, 손대지 않음). 클립 임포트 설정은 ThirdParty 에셋이라 바꾸지 않았다.

## 열린 질문 (Open questions)
- 없음.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
