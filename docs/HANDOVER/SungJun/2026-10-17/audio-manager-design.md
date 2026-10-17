# AudioManager 설계 제안

## 작업 요약 및 방법
- 요청: 현재 프로젝트에 맞는 오디오매니저 설계, 싱글톤 적합성 검토.
- Singleton, GameManager, ObjectPoolManager, SceneLoader, LoadingScreen, SettingsPresenter/View, PlayerFacade와 공용 UI 프리팹을 정적으로 조사했다.
- 이번 작업은 설계 제안이다. 런타임 코드, 프리팹, 씬을 변경하지 않았다. 구현 작업용 intent는 구현 범위를 확정할 때 작성한다.

## 현재 상태
- 자체 Scripts에서 AudioSource/AudioClip/AudioMixer 기반 재생 구현을 찾지 못했다.
- Core.Singleton<T>는 씬에 미리 배치한 객체를 사용하며 DontDestroyOnLoad와 중복 제거를 제공한다. 빈 객체를 자동 생성하지 않는다.
- GameManager는 base.Awake() 이후 Instance != this 검사로 중복 객체의 파생 초기화를 막는다.
- ObjectPoolManager는 씬 단위이며 씬이 내려가면 함께 사라진다.
- SettingsPresenter/View는 열기·닫기만 처리한다. Canvas_MainMenu의 Row_Audio/Row_Music/Row_SFX/Row_Controls는 볼륨 로직에 연결되어 있지 않다.
- GameManager.PauseGame/ResumeGame은 timeScale과 커서만 바꾼다. 상태 변경 이벤트는 현재 없다.
- SceneLoader.TryStartNewGame은 OnBeforeSceneChange를 발행하지 않는다. Loading 씬은 Additive로 열리고 목적지 씬은 Single로 로드된다.

## 추천 구조 — 아직 사용자 확정 전
- Core.Audio.AudioManager : Singleton<AudioManager>. 전역 재생 창구, 소스 소유, 볼륨 적용 및 저장을 담당한다.
- SOAudioData: AudioClip, 기본 음량, 피치, 3D 최소/최대 거리 등 재사용할 재생 설정. SO 직접 참조로 시작한다. 런타임 재생 상태는 SO에 저장하지 않는다.
- EAudioChannel: Master, Music, Sfx. UI 소리는 일단 Sfx 볼륨을 공유한다.
- SceneBgm: 각 콘텐츠 씬에 배치하고 Start에서 자신의 BGM을 요청한다. 씬별 곡 선택은 이 컴포넌트가 담당한다.
- 코드 경로: Assets/_Project/Scripts/Core/Audio/. 프리팹과 믹서, SO 에셋도 Assets/_Project/ 아래 둔다.
- PlayerAudio 같은 연결 컴포넌트는 실제 소리를 붙일 때 추가한다. PlayerFacade.OnHit/OnDeath/OnSkillUsed 등을 구독하고 AudioManager에 재생을 요청한다. 매니저가 모든 게임 이벤트를 직접 구독하지 않는다.

## 재생 구성
- MusicSource 하나: 2D 루프. 첫 버전은 페이드아웃 → 곡 교체 → 페이드인. 동일 곡의 반복 요청은 재시작하지 않는다. 새 요청은 이전 페이드 작업을 취소하고 최종 요청만 적용한다. 실제 크로스페이드가 필요해지면 소스를 두 개로 늘린다.
- UiSource 하나: 2D 짧은 클릭/확인음, PlayOneShot. 빠른 반복 입력 제한은 UI 재생 요청 단에서 적용한다.
- WorldSources: 매니저 자식의 고정 크기 AudioSource 풀. 초기 제안은 16개이며 실제 전투 동시 재생량으로 조정한다. 포화 시 새 요청을 생략하는 단순 정책부터 시작한다.
- 3D 일회성 소리는 발생 위치에서 재생한다. 이동 대상을 따라가는 루프, 개별 중지 핸들, 우선순위 교체는 해당 요구가 생기면 확장한다.
- 기존 씬 ObjectPoolManager에 영구 오디오 소스를 넣지 않는다. 소스의 반환은 매니저가 관리하며 일시정지된 소스를 완료로 판단하지 않는다.

## 제안 API
- PlayBgm(SOAudioData data, float fadeDuration)
- StopBgm(float fadeDuration)
- PlayUi(SOAudioData data)
- PlaySfx(SOAudioData data, Vector3 position)
- StopSceneSfx()
- SetVolume(EAudioChannel channel, float value01)
- GetVolume(EAudioChannel channel)
- SetGameplayPaused(bool isPaused)
- SaveSettings()

## 수명주기와 씬 전환
- 첫 실행 씬과 단독 실행할 테스트 씬에 같은 설정의 AudioManager 프리팹을 루트로 배치한다. GameManager 자식에 넣지 않는다.
- Awake에서 base.Awake() 후 Instance != this이면 즉시 반환한다. OnEnable의 이벤트 구독도 같은 소유자 조건으로 보호한다. OnDisable에서 해제한다.
- Awake는 참조 검증, 소스 준비, 저장된 값 읽기까지 담당한다. 믹서 초기 볼륨 적용은 Start에서 한다. 외부 재생 요청도 Start 이후로 제한한다.
- 씬별 SceneBgm.Start가 BGM을 요청한다. BGM 없는 씬은 Stop을 명시하고, 로딩 중에는 기존 BGM을 유지하는 것을 기본 제안으로 삼는다. SceneBgm.OnDestroy에서 StopBgm을 호출하지 않는다.
- AudioManager는 sceneUnloaded를 구독해 이전 씬 효과음을 정리한다. 현재의 단일 콘텐츠 씬 구조를 전제로 하며, 콘텐츠 씬을 여러 개 Additive로 쌓으면 소스별 소유 씬 추적이 필요하다.
- 이 경로는 새 게임, 일반 이동, 씬 재시작을 포함한다. OnBeforeSceneChange만으로 정리를 처리하지 않는다.
- AudioListener는 씬 카메라에 두며 매니저에 추가하지 않는다. 로딩 카메라와 플레이어 카메라 교체 중 리스너 중복 여부를 실제 Play에서 확인한다.

## 볼륨과 설정창
- AudioMixer: Master 아래 Music과 Sfx. UI와 월드 효과음은 Sfx 그룹에 출력한다.
- Row_Audio를 Master로 사용하는 안을 제안한다. 실제 UI 라벨과 슬라이더 컴포넌트 종류는 구현 시 확인한다. Row_Controls는 오디오 범위에서 제외한다.
- SettingsView는 슬라이더 입력 이벤트와 값 표시를 담당한다. SettingsPresenter가 AudioManager.GetVolume/SetVolume을 호출한다.
- Open 시 현재 저장 상태를 가져와 이벤트를 재발행하지 않고 화면에 표시한다. OnEnable/OnDisable 구독을 대칭으로 관리한다.
- 0~1 입력을 제한하고 믹서 dB로 변환한다. 0은 하한값으로 처리해 log(0)을 피한다. 초기 제안은 20 * log10(max(value, 0.0001))이다.
- 사용자 볼륨은 믹서, 곡별 기본 음량과 페이드는 소스에서 곱해 적용해 서로 덮어쓰지 않게 한다.
- PlayerPrefs에 Master/Music/Sfx를 저장하고 설정창 닫기 등 변경 확정 시 Save한다. 슬라이더 이동마다 디스크 저장하지 않는다.

## 일시정지 제안
- 설정창에서는 BGM과 UI 소리를 계속 재생하고 월드 효과음만 Pause/UnPause한다.
- GameManager.PauseGame/ResumeGame에서 SetGameplayPaused를 명시 호출하는 최소 연결을 제안한다. 현재 없는 OnGameStateChanged 이벤트를 이미 있다고 가정하지 않는다.
- Pause 상태에서는 새 월드 재생 요청을 생략하고, 일시정지된 소스를 풀로 반환하지 않는다. Resume은 실제 일시정지시킨 소스만 재개한다.
- 페이드에는 unscaledDeltaTime을 사용한다. AudioListener.pause를 선택한다면 BGM/UI의 ignoreListenerPause와 전역 상태 복구를 함께 관리해야 한다.

## 다음 할 일 / 확정할 항목
- 이번 문서는 추천안이며 BGM 전환 연출, 일시정지 연출, 3D 루프·추적음 필요 여부는 구현 전 사용자와 확정한다.
- 컷씬 소리가 필요하면 VideoPlayer 출력이 AudioMixer를 통과하도록 별도 AudioSource에 연결하고 BGM 유지/감쇠 정책을 정한다.
- 1단계: 싱글톤 프리팹, Mixer, BGM/UI/3D 단발, 볼륨 저장과 Settings 연결.
- 2단계: 실제 씬 BGM 및 플레이어/적 재생 이벤트 연결.
- 검증: 씬 직접 실행, 중복 매니저, 메뉴→Loading→게임, 씬 재시작, 같은 BGM 재요청, 볼륨 0·1·재실행 복원, Pause 중 UI 재생/월드 소스 보존, 풀 포화, 리스너 중복, 반복 Play 진입.
- 이번에는 설계 검토만 수행했으며 컴파일·Play 검증을 실행하지 않았다.

## Unity 공식 문서 확인
- [AudioMixer.SetFloat](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Audio.AudioMixer.SetFloat.html): Awake/OnEnable 대신 Start 이후에 초기 적용한다.
- [AudioSource.isPlaying](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioSource-isPlaying.html): Pause 호출 후 false이므로 이것만으로 풀 반환을 결정하지 않는다.
- [AudioListener.pause](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioListener-pause.html): 전역 정지와 ignoreListenerPause 예외 동작.

## 변경된 파일
- 이 설계 기록과 SungJun/HANDOFF_POINTER.md에 이력 한 줄 추가. 기존 사용자 변경을 보존했다.