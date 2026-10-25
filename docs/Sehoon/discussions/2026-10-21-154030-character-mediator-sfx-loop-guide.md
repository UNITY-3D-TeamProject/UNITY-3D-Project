# CharacterMediator 변경 사항과 SFX 루프·정지 연결 요약

작성 기준: 2026-10-21 현재 저장된 코드와 `CharacterMediator.cs`의 Git diff. 이번 작업은 설명 문서 작성이며 코드·설정은 변경하지 않았다.

## 1. 핵심 구조

`CharacterMediator`가 캐릭터의 신호를 연결하고, `SoundMediator`가 신호와 이동 상태를 전달하며, `SoundController`가 소리 선택·재생 조건·루프 요청 상태를 관리한다. 실제 AudioSource의 재생·정지는 `AudioManager`가 담당한다. 플레이어와 AI 모두 이 연결을 사용한다.

```text
MoveMediator / SkillMediator / 피격·사망 처리
                    ↓ CharacterMediator의 연결
              SoundMediator
                    ↓
              SoundController
       ┌────────────┴────────────┐
       단발음                    반복음
       PlayOneShot               RequestLoop / ReleaseLoop
       ↓                         ↓
       AudioManager.PlaySfx      StartLoop / StopLoop ← 현재 TODO
                                 ↓
                                 AudioManager의 SFX 루프·정지 API ← 담당자 구현 예정
```

현재 단발음은 기존 `PlaySfx`에 연결되어 있다. 반복음은 **원하는 상태만 기록하며 실제로 재생하거나 정지하지 않는다.**

## 2. CharacterMediator에서 바뀐 내용

| 변경 위치 | 추가 동작 | 필요한 이유 |
| --- | --- | --- |
| `_soundMediator`, `Awake()` | Inspector 참조를 받고, 비어 있으면 자식에서 `SoundMediator` 탐색 | 캐릭터마다 사운드 경로 연결 |
| `BindCallbacks()` | 이동 의도·실제 수평 속도·접지·구르기 조회 함수를 전달 | 이동 모듈을 SoundController가 직접 참조하지 않도록 연결 |
| `UnbindCallbacks()` | 이동 상태 조회 함수 해제 | 비활성화 뒤 오래된 연결을 사용하지 않도록 정리 |
| `Subscribe()` / `Unsubscribe()` | 아래 사운드 이벤트를 구독·해제 | 실제 행동 시점의 신호 전달 및 중복 구독 방지 |
| `OnEnable()` | 연결과 구독 후 `SyncSoundState()` 호출 | 구독 전에 시작된 차지·보드 탑승 등 현재 상태 복구 |
| `OnDisable()` | `StopAllSounds()` 전달 | 이벤트가 끊길 때 반복음과 이동음 대체 상태 정리 |
| `OnHitCallback()` | `NotifyHit()` 전달 | 공통 피격음 요청 |
| `OnDeathCallback()` | 스킬 비활성화보다 먼저 `NotifyDeath()` 전달 | 반복음 해제 및 사망 처리 중 발생하는 새 사운드 요청 차단 |
| `ResetForReuse()` | 사운드의 사망 차단 해제 후 상태 동기화 | 풀에서 재사용되는 캐릭터의 소리 복구 |
| `SyncSoundState()` | 사망이면 사망 상태 전달, 생존이면 SkillMediator에 현재 상태 재발행 요청 | 비활성 중 놓친 이벤트를 실제 현재 상태로 보완 |
| `_isSyncingSound` | 동기화 중 재진입 차단 | 준비 알림을 통한 중첩 동기화 방지 |

이벤트 연결은 다음과 같다.

| 발신 | 수신 | 의미 |
| --- | --- | --- |
| `MoveMediator.OnJumpStarted` | `SoundMediator.NotifyJumpStarted` | 점프 단발음 |
| `SkillMediator.OnSkillSound` | `SoundMediator.NotifySkillSound` | 실제 스킬 단계의 단발음 |
| `SkillMediator.OnSkillSoundLoop` | `SoundMediator.NotifySkillSoundLoop` | 스킬 루프 시작·해제 |
| `SkillMediator.OnVehicleSoundChanged` | `SoundMediator.NotifyVehicleSound` | 실제 보드 부착·분리에 따른 이동음 교체 |
| `SoundMediator.OnSyncRequested` | `CharacterMediator.SyncSoundState` | 사운드 구성 재활성화 후 현재 상태 복구 |

`SoundController.OnReady → SoundMediator.OnSyncRequested → CharacterMediator.SyncSoundState → SkillMediator.SyncSoundState` 경로가 있어 Controller만 또는 SoundMediator만 다시 켜져도 현재 차지·탑승 상태를 재전달한다. 이 과정에서 스킬을 다시 실행하지 않는다.

근거: [CharacterMediator.cs](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/Mediator/CharacterMediator.cs:87), [이동 상태 연결](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/Mediator/CharacterMediator.cs:256), [상태 동기화](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/Mediator/CharacterMediator.cs:455), [이벤트 연결](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/Mediator/CharacterMediator.cs:667).

## 3. 어떤 소리가 반복되는가

공통 소리는 `_footstep`, `_jump`, `_land`, `_hit`에 SO를 직렬화한다. 특수 소리는 `SSkillSound[]`를 `(실제 SkillName, ESoundPhase) → SOAudioData` Dictionary로 변환한다. 스킬 이름 비교는 대소문자를 무시한다.

| 소리 | 시작 조건 | 정지·교체 조건 | 선택 방식 |
| --- | --- | --- | --- |
| 발소리 루프 | 이동 의도 있음 + 실제 속도 기준 초과 + 접지 + 비구르기 + 재생 가능 상태 | 조건 하나라도 해제되거나 보드 이동음으로 전환 | 공통 `_footstep` |
| 보드 이동음 루프 | 실제 보드가 부착되어 있고 위 이동 조건 충족 | 정지·공중·구르기·하차·사망·비활성화 | 실제 보드 스킬 이름 + `Move` |
| 차지 루프 | `Fire`의 `ChargeStarted` | `Fired` 또는 `Canceled`, 사망·비활성화 | 실제 사격 스킬 이름 + `Charge` |

- 보드에 올라탄 사실만으로 이동음을 계속 틀지 않는다. 부착 상태는 발소리를 대체할 키를 선택하고, 실제 루프 여부는 이동 조건으로 판정한다.
- 보드에서 내리면 공통 발소리 선택으로 돌아가며, 이동 조건을 만족할 때 재생한다. 보드에 탑승했으나 `Move` 매핑이 없으면 무음이다.
- 이동음 전환은 이전 키를 해제한 다음 새 키를 요청한다. 차지 루프는 이동 루프와 별개로 관리한다.
- `_minMoveSpeed` 기본값은 `0.05 m/s`이며 Inspector에서 조절한다. `SoundMediator.LateUpdate()`가 전달한 실제 수평 속도가 이 값보다 커야 한다.
- 점프·피격·착지·발사·탑승·스캔 파동 등은 단발음이다. 착지는 접지 상태 전환으로 판단하며 기본 체공 기준은 `0.1초`다. 첫 표본은 기준 설정에만 사용한다.

근거: [SoundController 설정·상태](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/CharacterSound/SoundController.cs:17), [이동 판정](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/CharacterSound/SoundController.cs:146), [차지·발사·취소 신호](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/Mediator/SubMediators/SkillMediator.cs:580).

## 4. 루프·정지 기능은 어디에 넣는가

### AudioManager 담당자: 실제 재생 기능

구현 대상은 [AudioManager.cs](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/Core/Audio/AudioManager.cs:175)다. 기존 `PlaySfx`는 위치를 한 번 지정해 단발음을 재생하며 반환값이 없다. 캐릭터 루프를 제어하려면 다음 기능이 필요하다.

1. **루프 시작:** SO의 클립·음량·피치·거리 설정과 SFX 믹서 라우팅을 적용하고, 캐릭터 Transform을 따라가는 반복음을 시작한다.
2. **개별 정지:** 특정 캐릭터가 시작한 특정 루프만 정지하고 재생 자원을 반환한다. 같은 SO를 쓰는 다른 캐릭터의 소리는 유지한다.
3. **실제 재생 식별:** 시작 성공 여부와 이후 정지 대상을 식별할 수 있어야 한다. 핸들 또는 소유자+키 등 정확한 공개 API와 타입은 담당자와 확정한다.
4. **수명 정리:** 소유자 파괴·씬 종료 등에 남는 루프가 없도록 정리하고, 이미 끝난 대상에 대한 중복 정지에도 안전하게 처리한다.

기존 SFX 소스 풀을 공유한다면 특히 다음을 고려해야 한다.

- 현재 `FindFreeSfxSource()`는 `_sfxEndTimes`만 보고 빈 소스를 찾는다. 루프를 클립 한 번의 종료 시각으로 반환하면 재생 중인 루프가 단발음에 덮어써질 수 있으므로, 정지 전까지 점유 상태를 유지해야 한다.
- 루프 소스를 반환하거나 단발음으로 재사용할 때 `loop`, 위치 추적 참조, 재생 식별 정보 등 루프 상태를 초기화해야 한다.
- 같은 클립 요청을 거르는 기존 `TryPickClip()`이 서로 다른 캐릭터의 루프 시작에 어떤 영향을 줄지 확인해야 한다. 시작 거절은 호출 측에서 알 수 있어야 한다.

소스 풀 공유 여부, 핸들 타입, 재시도 간격, 페이드 정책·수치는 이 문서에서 확정하지 않는다. `SOAudioData.FadeInDuration/FadeOutDuration`은 현재 BGM 전용이므로 SFX 루프에도 적용된다고 가정하지 않는다.

### 사운드 연결 담당자: SoundController의 TODO 연결

AudioManager API가 준비되면 [StartLoop](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/CharacterSound/SoundController.cs:291)와 [StopLoop](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/CharacterSound/SoundController.cs:299)에 연결한다. `CharacterMediator`에는 AudioSource 조작이나 AudioManager 직접 호출을 추가할 필요가 없다.

아래는 역할을 설명하는 의사 코드이며, 새 API의 이름·시그니처를 확정한 코드가 아니다.

```text
StartLoop(key, sfx)
    이 캐릭터의 key에 이미 유효한 재생/시작 요청이 있으면 중복 시작 방지
    AudioManager에 sfx와 추적 대상 Transform을 전달해 루프 시작 요청
    시작이 성공한 경우 실제 재생 식별 정보와 해당 매니저를 보관
    실패한 경우 재생 중으로 기록하지 않음

StopLoop(key)
    이 캐릭터의 key에 남은 시작 요청을 취소
    보관한 식별 정보로 해당 루프만 정지/반환
    정리 책임을 넘기거나 정리를 마친 뒤 식별 정보 제거
```

`SSoundKey`만 전역 정지 키로 사용하면 여러 캐릭터의 동일 스킬이 충돌한다. 소유자를 함께 구분하거나, 각 시작에 발급된 고유 식별 정보로 정지해야 한다. 정지는 SO 매핑을 다시 찾는 데 의존하지 않도록 한다.

### 연결 시 함께 보완해야 할 요청·실제 재생 상태

현재 `_desiredLoops`는 **재생을 원하는 키 집합**이다. 실제 재생 핸들 목록이 아니다. `RequestLoop()`는 키를 처음 추가할 때만 `StartLoop()`를 호출한다.

따라서 AudioManager 부재·소스 부족·클립 요청 제한으로 시작에 실패했는데 원하는 상태만 남으면, 계속 이동하거나 차지해도 자동 재시작되지 않을 수 있다. API 연결 시 원하는 상태와 실제 재생 상태를 구분하고, 실패·자원 회수 뒤 둘을 다시 맞추는 처리가 필요하다. 이동은 `_activeMoveKey`도 요청 상태로 보관하고 있으므로 TODO 본문만 채우면 실패 복구까지 완성된다고 보아서는 안 된다.

비동기 시작을 사용한다면 정지 요청 이후 늦게 완료된 시작도 취소·정리해야 한다. 매니저가 교체됐을 때 이전 식별 정보로 새 매니저의 다른 소리를 정지하지 않도록, 시작한 매니저와 재생 식별 정보의 유효성을 함께 관리해야 한다.

근거: [RequestLoop / ReleaseLoop](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/CharacterSound/SoundController.cs:268), [AudioManager의 빈 소스·클립 판정](C:/Users/inha/Desktop/Project/Unity/3DProject/Assets/_Project/Scripts/Core/Audio/AudioManager.cs:255).

## 5. 정지는 어떤 경로로 들어오는가

| 상황 | 현재 호출 경로 |
| --- | --- |
| 차지 발사·취소 | `OnSkillSoundLoop(..., false) → SetSkillLoop → ReleaseLoop → StopLoop` |
| 이동 조건 해제·이동음 교체 | `UpdateLocomotion → ApplyMoveLoop → ReleaseLoop → StopLoop` |
| 캐릭터 사망 | `CharacterMediator.OnDeathCallback → SoundMediator.NotifyDeath → SoundController.NotifyDeath → StopAllSounds` |
| CharacterMediator 비활성화 | `OnDisable → SoundMediator.StopAllSounds → SoundController.StopAllSounds` |
| SoundMediator / SoundController 단독 비활성화 | 각 `OnDisable`에서 루프 상태 정리 |

`StopAllSounds()`는 현재 **이 캐릭터의 반복음 요청과 이동음 대체 상태를 정리**한다. 이미 `PlaySfx`로 요청한 점프·피격 같은 단발음을 개별 정지하거나 씬 전체 효과음을 정지하는 기능은 아니다. 실제 소리 정지는 `StopLoop()` 연결이 끝나야 동작한다.

## 6. 담당자에게 전달할 범위와 확인 항목

- AudioManager 담당자: 소유자별 루프 시작·정지, 위치 추적, 성공/실패와 재생 식별, 소스 반환·수명 처리 제공.
- 연결 담당자: SoundController의 TODO 연결, 실제 재생 정보 보관, 시작 실패·자원 회수·취소 후 상태 정합성 처리.
- 캐릭터 설정: 플레이어·AI에 SoundMediator와 SoundController를 연결하고, 공통 SO 및 실제 스킬명+단계 매핑 입력.
- 연결 후 확인: 동일 SO를 사용하는 두 캐릭터 중 하나만 정지, 정지·점프·구르기 시 이동 루프 해제, 보드 교체와 하차, 차지 발사·취소, 사망·비활성화·재사용, 재활성화 복구, 시작 실패 후 복구, 씬 종료 시 잔여 루프 없음.

이번 문서는 현재 소스와 diff를 정적으로 확인해 작성했다. 새 컴파일·플레이·청취 테스트는 수행하지 않았으며, 실제 SFX 루프 API의 구현·연결은 포함하지 않는다.
