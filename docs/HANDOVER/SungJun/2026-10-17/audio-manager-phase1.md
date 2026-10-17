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
  - `Assets/_Project/ScriptableObjects/Audio/` — `SOAudioData_BgmMainMenu`(Shift Background), `SOAudioData_BgmTutorial`(Archanor loop_energy), `SOAudioData_UiClick`(Shift Click), `SOAudioData_SfxShootLaser`(Archanor shoot_laser), `SOBgmTable`(메인메뉴 + Tutorial).
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

## 다음 할 일 (Next Steps)
1. AudioManager 프리팹 씬 배치 — 사용자 결정 대기. 맵 씬은 `feature/Map/Maze` 병합 이후(intent-037과 같은 시점)를 권장한다.
2. 배치 후 에디터를 앞에 둔 상태에서 실제 청취 확인 (메인메뉴 곡, 튜토리얼 전환, 설정창 볼륨).
3. 2단계: `CharacterMediator.OnFireCallback`에 사격음, `PlayerFacade.OnHit/OnDeath`에 피격·사망음 연결.
4. 정식 음원이 생기면 `Assets/_Project/Audio/`로 옮기고 SO의 클립을 교체한다.
