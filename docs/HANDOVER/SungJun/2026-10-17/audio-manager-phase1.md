# AudioManager 1단계 구현 (intent-038)

## 작업 요약 및 방법
- [audio-manager-design.md](./audio-manager-design.md) 설계를 검토하고 단순화한 뒤 [intent-038](../../../intent/intent-038-audio-manager-phase1.md)으로 확정하고 구현했다.
- 새 스크립트 (`Assets/_Project/Scripts/Core/Audio/`, 네임스페이스 `Core.Audio`)
  - `AudioManager` — `Singleton<AudioManager>`. Awake에서 자식 AudioSource를 만든다(BGM 1, UI 1, 3D 16). `sceneLoaded`로 씬별 BGM을 고르고, 볼륨을 dB로 바꿔 믹서에 적용한다.
  - `SOAudioData` — 클립 배열, 음량, 피치 범위.
  - `SOBgmTable` — `SBgmEntry(ESceneType → SOAudioData)` 배열 + 메인메뉴 곡 필드.
  - `EAudioChannel` — Master, Music, Sfx.
- 기존 코드 수정
  - `GameManager.PauseGame/ResumeGame` — `AudioListener.pause` 한 줄씩.
  - `SettingsView` — 볼륨 슬라이더 3개, `VolumeChanged` 이벤트, `SetVolume`(이벤트 없이 표시).
  - `SettingsPresenter` — Open에서 현재 볼륨 표시, 변경 시 `AudioManager.SetVolume`, Close에서 `SaveVolume`.
- 에셋
  - `Assets/_Project/Audio/MainAudioMixer.mixer` — Master > Music, Sfx. 노출 파라미터 `MasterVolume`·`MusicVolume`·`SfxVolume`. (공개 API가 없어 에디터 내부 `AudioMixerController`를 리플렉션으로 호출해 만들었다.)
  - `Assets/_Project/ScriptableObjects/Audio/` — 종류별 하위 폴더로 나눈다. 새 소리 SO도 같은 규칙으로 넣는다.
    - `Bgm/` — `SOAudioData_BgmMainMenu`(Shift Background), `SOAudioData_BgmTutorial`(Archanor loop_energy, 표에서는 뺌), `SOAudioData_BgmPlaylistExample`(클립 2개 플레이리스트 예시, 미연결)
    - `Ui/` — `SOAudioData_UiClick`(Shift Click)
    - `Sfx/` — `SOAudioData_SfxShootLaser`(Archanor shoot_laser)
    - `Bgm/SOAudioData_BgmLobby` — 로비 테스트 플레이리스트 (Background·loop_energy·loop_lightning)
    - ~~`SOBgmTable`(메인메뉴 곡 + Lobby 플레이리스트)은 `Audio/` 바로 아래에 둔다.~~ → 씬 단위 구조로 바꾸며 삭제. BGM은 씬의 AudioManager 인스펙터에서 정한다.
  - `Assets/_Project/Prefabs/System/Manager/AudioManager.prefab` — 믹서·그룹·표 연결.
  - `Canvas_MainMenu`·`Canvas_InGame` — Presenter가 참조하는 `SettingsView`에 `Row_Audio/Row_Music/Row_SFX` 슬라이더 연결.

## 결정 사항
- 3D 소스는 반납 단계 없이 "끝나는 시각" 배열로 재사용한다. Update·ObjectPool 없음. 모두 재생 중이면 생략한다.
- 일시정지는 `AudioListener.pause` + BGM·UI `ignoreListenerPause`. 별도 상태·API 없음.
- MainScene은 `MAIN_SCENE_NAME` 상수로 판별하고, 나머지는 빌드 인덱스 = `(int)ESceneType` 규칙을 따른다. Additive(로딩씬)는 무시한다.
- 볼륨은 설정창을 닫을 때만 PlayerPrefs에 저장한다.
- **사용자 지시로 AudioManager 프리팹을 아직 어떤 씬에도 배치하지 않았다.** (MainScene 배치를 한 번 했다가 되돌렸다. 파일 내용은 원래와 같고 줄바꿈만 다르다.)

## 현재 상태 및 이슈
- 컴파일 에러 0.
- Play 검증은 MainScene에 저장하지 않은 임시 인스턴스를 두고, 에디터 일시정지 + 프레임 스텝으로 했다. 곡 전환, 로딩 중 유지, 같은 곡 유지, 중복 제거, 슬라이더 → dB → 저장, 3D 소스 배정, 클립 중복 차단, 일시정지 상태를 확인했다. 테스트로 바꾼 PlayerPrefs는 지웠다.
- 에디터가 백그라운드라 실제 소리는 듣지 못했다. 사용자 청취 확인이 필요하다.
- 테스트 중 Unity가 Play 모드에서 재컴파일해 런타임 상태가 초기화된 적이 있다. Play를 다시 시작해 재검증했다.
- 긴 프레임(씬 활성화) 직후에는 `unscaledDeltaTime`이 커서 BGM 페이드아웃이 한 프레임에 끝날 수 있다.
- `SettingsPanel`에 Presenter가 쓰지 않는 두 번째 `SettingsView`가 있다 (기존 상태, 손대지 않음).
- 테스트 음원이 ThirdParty(Git 미포함)라 팀원도 Shift·Archanor 에셋이 있어야 소리가 난다.

## 추가 작업 (같은 날)
- 커밋 `fa27e60`(구현)·`96ac171`(문서) 후 develop(PR #87까지)을 `feature/System/SoundManager`에 머지하고 push했다. `INTENT_POINTER.md` 충돌은 양쪽 줄을 모두 남겼다.
- 사용자 요청으로 MainScene 루트에 `AudioManager.prefab` 인스턴스를 배치했다 (씬 diff는 PrefabInstance 블록과 SceneRoots 한 줄뿐).
- Audio SO를 `Bgm/`·`Ui/`·`Sfx/` 하위 폴더로 옮겼다 (`AssetDatabase.MoveAsset`, GUID 유지 확인, `SOBgmTable` 참조 정상).
- `SOBgmTable`에서 Tutorial 항목을 지웠다 → 튜토리얼은 메인메뉴 곡을 이어서 튼다. 씬 안에서 곡을 바꾸는 건 표를 고치지 않고 해당 코드가 `PlayBgm(다른 SO)`를 부르는 방식으로 정했다.
- BGM 플레이리스트 추가: `SOAudioData`에 클립이 2개 이상이면 곡이 끝날 때마다 다음 곡을 이어서 튼다. `_isShuffle` 끄면 차례로, 켜면 무작위(같은 곡 연속 없음). 곡 번호는 AudioManager가 기억한다. 프레임 스텝으로 순서 로직만 확인했고, 실제 곡 길이 기준 전환은 사용자 청취 확인이 필요하다.
- BGM 페이드를 SO마다 정하도록 변경: `SOAudioData`에 Fade In / Fade Out 시간(기본 1초) 추가, AudioManager의 공통 `_bgmFadeDuration` 제거. 플레이리스트는 곡이 끝나기 페이드아웃 시간 전부터 줄이고 다음 곡을 0에서 키운다 (크로스페이드 아님). 반복 on/off 설정은 사용자 결정으로 넣지 않았다 (항상 계속 재생). 페이드인은 스텝으로 확인, 곡 끝 페이드아웃은 청취 확인 필요.
- 사용자 요구 반영: BGM은 현재 씬 플레이리스트 안에서만 바꾼다. `SkipBgmTrack()`(다음 곡, 페이드아웃 → 페이드인)을 추가하고 `PlayBgm`·`StopBgm`은 private로 바꿨다. 로비 테스트 플레이리스트 `SOAudioData_BgmLobby`를 표에 Lobby로 등록했다. 버튼 연결은 아직 없다.
- 효과음 조절: SO에 3D 들리는 거리(min/max) 추가, `PlaySfx(data, pos, volumeScale, pitchScale)`·`PlayUi(data, volumeScale)`로 부르는 쪽이 상황별 배율을 줄 수 있게 했다.
- **구조 변경 (사용자 결정): 싱글톤 → 씬 단위 매니저.** `ObjectPoolManager`처럼 씬마다 배치하고 `static Instance`로 접근한다 (DontDestroyOnLoad 없음).
  - 씬마다 인스펙터에서 BGM(플레이리스트 가능)만 설정한다.
  - 효과음은 SO 직접 참조: 소리를 내는 컴포넌트가 `[SerializeField] SOAudioData`를 들고 `PlaySfx(_shootSound, pos)`, `PlayUi(_clickSound)`로 부른다. 한때 문자열 키 + 묶음 SO(`SOAudioPack`)로 씬마다 등록하는 방식을 만들었지만, 키를 다 알아야 해서 사용자 결정으로 되돌렸다. 씬에 있는 오브젝트가 참조하는 소리만 그 씬과 함께 메모리에 올라오므로 따로 등록하지 않아도 씬별로 관리된다.
  - `SOBgmTable`과 씬 판별 로직은 삭제했다. 로딩씬이 Additive로 열리면 0.3초(`_sceneExitFadeDuration`) 동안 BGM을 줄이고, 새 씬 매니저가 자기 곡을 페이드인한다. 씬을 넘어 곡이 이어지지는 않는다.
  - 프리팹은 BGM 비움, MainScene은 BGM만 메인메뉴 곡으로 오버라이드.
- 볼륨 채널 이름 통일 (Music → Bgm): `EAudioChannel.Bgm`, 믹서 그룹 `Bgm`·노출 파라미터 `BgmVolume`, PlayerPrefs 키 `Volume_Bgm`, AudioManager·SettingsView 필드와 메서드 이름. 프리팹에 저장된 `_musicGroup`·`_musicSlider`는 `[FormerlySerializedAs]`로 이어 받는다 (연결 유지 확인). 저장돼 있던 음악 볼륨은 기본값 1로 돌아간다. 위의 `Music`·`MusicVolume` 표기는 이 변경 전 기록이다.
- `SettingsPresenter.Close`가 볼륨을 바꾸지 않았으면 AudioManager를 찾지 않도록 수정 (매니저 없는 씬에서 설정창을 닫거나 씬 정리 중에 에러가 나던 문제).

## 다음 할 일 (Next Steps)
1. 맵 씬 AudioManager 배치 — 사용자 결정 대기. 메인메뉴에서 시작하면 매니저가 씬을 넘어 유지되므로, 맵 씬 배치는 맵을 단독 Play할 때만 필요하다.
2. 배치 후 에디터를 앞에 둔 상태에서 실제 청취 확인 (메인메뉴 곡, 튜토리얼 전환, 설정창 볼륨).
3. 2단계: `CharacterMediator.OnFireCallback`에 사격음, `PlayerFacade.OnHit/OnDeath`에 피격·사망음 연결.
4. 정식 음원이 생기면 `Assets/_Project/Audio/`로 옮기고 SO의 클립을 교체한다.
