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
    - `SOBgmTable`(메인메뉴 곡만, Entries 비어 있음)은 소리 하나가 아니라 표라서 `Audio/` 바로 아래에 둔다.
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
- `SettingsPresenter.Close`가 볼륨을 바꾸지 않았으면 AudioManager를 찾지 않도록 수정 (매니저 없는 씬에서 설정창을 닫거나 씬 정리 중에 에러가 나던 문제).

## 다음 할 일 (Next Steps)
1. 맵 씬 AudioManager 배치 — 사용자 결정 대기. 메인메뉴에서 시작하면 매니저가 씬을 넘어 유지되므로, 맵 씬 배치는 맵을 단독 Play할 때만 필요하다.
2. 배치 후 에디터를 앞에 둔 상태에서 실제 청취 확인 (메인메뉴 곡, 튜토리얼 전환, 설정창 볼륨).
3. 2단계: `CharacterMediator.OnFireCallback`에 사격음, `PlayerFacade.OnHit/OnDeath`에 피격·사망음 연결.
4. 정식 음원이 생기면 `Assets/_Project/Audio/`로 옮기고 SO의 클립을 교체한다.
