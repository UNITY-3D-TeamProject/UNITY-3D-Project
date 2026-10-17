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

설계 배경과 검토 과정은 [audio-manager-design.md](../HANDOVER/SungJun/2026-10-17/audio-manager-design.md)에 있다. 이 문서는 그 뒤 대화에서 단순화·확정한 내용을 기준으로 한다.

## 기대 결과 (Proposed outcome)

### 구조
- `AudioManager : Singleton<AudioManager>` (씬이 바뀌어도 유지). 재생과 볼륨만 담당하고, 언제 어떤 소리를 낼지는 각 기능이 정한다.
- AudioManager가 가진 스피커(AudioSource)는 세 종류다.
  - **BGM 1개**: 곡 변경 시 페이드아웃 → 교체 → 페이드인. 같은 곡을 다시 요청하면 이어서 재생한다. 페이드는 `unscaledDeltaTime`을 쓰고, 페이드 시간은 매니저 필드로 고정한다.
  - **UI 1개**: `PlayOneShot`으로 버튼·확인음을 낸다.
  - **3D 16개 고정 배열**: 발생 위치에서 단발음을 낸다. 반납 단계 없이, 재생할 때 `끝나는 시각 = Time.time + clip.length / pitch`를 배열에 적어 두고 새 요청 때 끝난 소스를 고른다. Update 루프와 `ObjectPool<T>`를 쓰지 않는다. 16개가 모두 재생 중이면 새 요청을 생략한다.
- 같은 클립이 짧은 간격(약 0.05초) 안에 다시 요청되면 건너뛴다 (클립별 마지막 재생 시각 Dictionary).

### 데이터 (SO)
- `SOAudioData`: 클립 배열(랜덤 선택), 기본 음량, 피치 범위. 3D 거리는 넣지 않고 3D 소스 공통값으로 둔다. 런타임 상태는 저장하지 않는다.
- `SOBgmTable`: `ESceneType` → `SOAudioData` 목록 + 메인메뉴 곡 전용 필드 `_mainMenuBgm`. 곡을 비워 둔 항목은 무음(`StopBgm`)으로 처리한다.
- `EAudioChannel`: Master, Music, Sfx.

### 씬별 BGM 선택
`AudioManager`가 `SceneManager.sceneLoaded`를 구독해 아래 순서로 판단한다. 씬마다 컴포넌트를 두지 않으므로 씬 파일을 수정하지 않는다.
1. `LoadSceneMode.Additive`(로딩씬)면 무시한다 → 이전 곡 유지.
2. MainScene이면(`scene.name == MAIN_SCENE_NAME`) `_mainMenuBgm`을 재생한다.
3. `scene.buildIndex`가 `ESceneType`에 정의된 값이면 표에서 곡을 찾아 재생한다 (`SceneLoader`의 `(int)ESceneType` = 빌드 인덱스 규칙을 그대로 따름).
4. 그 밖의 씬(테스트 씬 등)은 현재 곡을 유지한다.
- 씬 재시작(`StageManager`의 `LoadScene(buildIndex)`)도 같은 곡이 나오므로 이어서 재생된다.
- 씬 안에서 곡을 바꾸는 경우(보스전 등)는 해당 코드가 `PlayBgm`을 직접 부른다 (1단계 범위 밖).

### 공개 API (7개)
`PlayBgm(SOAudioData)`, `StopBgm()`, `PlayUi(SOAudioData)`, `PlaySfx(SOAudioData, Vector3)`, `SetVolume(EAudioChannel, float)`, `GetVolume(EAudioChannel)`, `SaveVolume()`

### 볼륨
- AudioMixer: Master 아래 Music, Sfx. UI와 3D 효과음은 Sfx 그룹으로 출력한다. 볼륨 파라미터 3개를 Expose한다.
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
3. **설정창 행 구성** (프리팹 확인 결과): `Canvas_MainMenu`·`Canvas_InGame` 모두 `Row_Audio`(라벨 "MASTER VOLUME")·`Row_Music`("MUSIC")·`Row_SFX`("SFX") 아래에 UGUI `Slider`(0~1, 정수 아님)가 있다. `Row_Audio` = Master, `Row_Music` = Music, `Row_SFX` = Sfx로 연결한다. `Row_Controls`는 제외한다.
4. **검증용 음원**: Unity 엔진 자체에는 기본 제공 오디오 클립이 없다. 이미 프로젝트에 있는 ThirdParty 소리로 검증한다.
   - BGM: `Shift - Complete Sci-Fi UI/Audio/Background.ogg` (MainScene), 곡 전환 확인용 두 번째 곡은 `Archanor/Sci-Fi Arsenal/.../Sound/Loops/loop_energy.wav` (튜토리얼)
   - UI: `Shift - Complete Sci-Fi UI/Audio/Click.ogg`
   - 3D: `Archanor/Sci-Fi Arsenal/.../Sound/Impact/shoot_laser.wav`
   - ThirdParty 폴더는 Git에 없으므로 이 클립을 참조하는 `SOAudioData`는 팀원도 같은 에셋을 가져와야 소리가 난다 (`Canvas_InGame`의 Shift 의존과 같은 조건). 정식 음원이 생기면 `Assets/_Project/Audio/`로 교체한다.
5. **수치 기본값**: BGM 페이드 1초, 같은 클립 재요청 간격 0.05초, 3D 거리 min 1 / max 30으로 시작한다. 모두 인스펙터 필드로 두고 Play하면서 조정한다.

## 진행 상황 (2026-10-17)
- **완료**: 스크립트 4개(`Core/Audio/`), GameManager·SettingsView·SettingsPresenter 수정, 컴파일 에러 0.
- **완료**: `Assets/_Project/Audio/MainAudioMixer.mixer`(Master > Music, Sfx, 노출 파라미터 `MasterVolume`·`MusicVolume`·`SfxVolume`), `ScriptableObjects/Audio/`(SO 5개), `Prefabs/System/Manager/AudioManager.prefab`, `Canvas_MainMenu`·`Canvas_InGame`의 `SettingsView` 슬라이더 3개 연결.
- **보류 (사용자 지시)**: AudioManager 프리팹의 씬 배치. MainScene·맵 씬 모두 아직 배치하지 않는다. Loading 씬은 단독 실행되지 않으므로 배치 대상에서 뺀다.
- **Play 검증** (MainScene에 저장하지 않은 임시 인스턴스, 에디터 일시정지 + 프레임 스텝): 메인메뉴 곡 → 로딩 중 유지 → 튜토리얼 곡 전환, 같은 곡 재요청 시 유지, 중복 매니저 제거, 슬라이더 → 믹서 dB(0.5 → -6.02, 0 → -80) → 닫을 때 PlayerPrefs 저장, 3D 소스 위치·끝나는 시각 기록, 같은 프레임 같은 클립 차단, 일시정지 시 `AudioListener.pause`·`Time.time` 정지 확인. 실제 소리는 에디터가 백그라운드라 듣지 못했다 → 사용자 청취 확인 필요.
- **알려진 점**: 씬 활성화처럼 긴 프레임이 끼면 `unscaledDeltaTime`이 커서 BGM 페이드아웃이 한 프레임에 끝날 수 있다. `Canvas_MainMenu`·`Canvas_InGame`의 `SettingsPanel`에 Presenter가 참조하지 않는 두 번째 `SettingsView`가 붙어 있다(기존 상태, 손대지 않음). 클립 임포트 설정은 ThirdParty 에셋이라 바꾸지 않았다.

## 열린 질문 (Open questions)
- 없음.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
