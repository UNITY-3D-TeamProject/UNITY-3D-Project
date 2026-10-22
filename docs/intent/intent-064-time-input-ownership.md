---
id: intent-064
title: 게임 시간·입력 맵·카메라 Brain 설정의 주인을 하나로 정리
part: Core / UI / Cinematic
status: open   # open | resolved
created: 2026-10-22
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
카메라 연출(intent-063)을 붙이면서, 하나뿐인 공유 상태 세 가지를 맡는 곳이 없다는 것이 드러났다. 지금은 여러 클래스가 각자 바꿨다가 되돌린다.

| 공유 상태 | 지금 바꾸는 곳 | 결과 |
|---|---|---|
| 게임 시간(`Time.timeScale`) | UIManager(대화·펫 안내·설정창), CinematicManager(샷), GameManager(설정창 PauseGame/ResumeGame) | 나중에 쓴 쪽이 이김. 서로 덮어쓰는 것을 막는 땜질이 양쪽에 있음(`UIManager.CloseSettings`의 다시 멈추기, `CinematicManager.CoPlayShot`의 매 프레임 확인) |
| 입력 맵(Player / UI / Dialogue) | UIManager 안의 `UIInputHandler`만 | 연출이 조작을 막으려면 UIManager에 부탁해야 함 → `BeginCinematic`/`EndCinematic`, `_isCinematicActive` 분기 다섯 곳 |
| CinemachineBrain `IgnoreTimeScale`·`DefaultBlend` | DialogueDirector, CinematicManager가 각자 저장·복구 | 켜고 끄는 순서가 어긋나면 서로 덮어씀 |

이 구조에서 나오는 버그(코드 흐름 기준, [검토 기록](../HANDOVER/SungJun/2026-10-22/camera-cinematic-review.md)):
1. 연출 종료 블렌드 중 새 대화가 시작되면, 늦게 도는 Brain 복구가 대화 중 `IgnoreTimeScale`을 false로 덮고, 대화가 끝나면 true가 영구히 남는다.
2. 시간이 흐르는 샷 도중 밖(발판·StageManager)에서 `PlayDialogue`를 부르면 그대로 시작된다. 그 상태로 마지막 샷이 끝나면 시간·Player 맵이 돌아와 대화창이 남은 채 E로 넘길 수 없다(ESC로 설정창을 열고 닫아야 풀림). 중간 샷이면 연출의 대화 단계가 말없이 바뀐다.
3. 꺼진 CinematicManager에 `Play`가 들어가면 조작을 막은 채 `Update`가 돌지 않아 멈춘다.
4. 연출 중 UIManager만 껐다 켜면 입력 연결이 끊긴 사이 연출이 끝나 Player 맵으로 못 돌아가고, 다시 켜도 Dialogue 맵이 남는다.
5. 시간을 멈춘 샷 중 설정창을 닫으면 `ResumeGame`이 시간을 1로 돌리고, 코루틴이 다시 멈추기 전 한 프레임 동안 게임이 돈다.

## 기대 결과 (Proposed outcome)
- 각 기능은 "나 때문에 멈춰 줘 / 풀어 줘"를 요청만 하고, 결정은 주인 한 곳이 한다.
- **게임 시간 → GameManager.** 설정창이 열려 있거나(`CurrentState == Pause`) 정지 요청이 하나라도 있으면 0, 아니면 1.
  - `PauseGameTime`/`ResumeGameTime`을 요청자별 요청·해제로 바꾼다.
  - 씬이 로드되면 이전 씬과 함께 파괴된 쪽의 요청을 지운다(이전 씬 요청이 남아 새 씬에서 멈추는 것 방지). 새 씬 오브젝트가 OnEnable에서 낸 요청은 지우지 않도록, 전부 비우지 않고 파괴된 요청자만 지운다.
  - `timeScale`은 이미 전역이고 설정창 일시정지도 GameManager가 맡으므로, 같은 곳에 둬야 설정창 닫기 땜질이 사라진다.
- **입력 맵 → 새 씬 컴포넌트(Core).** 가칭 `InputMapController`, 씬 안 `Instance`로 접근, `Canvas_InGame` 프리팹에 둔다.
  - 요청 우선순위: 설정창(UI 맵) > 조작 막기(Dialogue 맵) > 요청 없음(Player 맵).
  - `GameManager.OnPlayerSpawned`를 구독해 새 플레이어에 지금 상태를 다시 적용한다.
  - 꺼지면 요청을 비우고 Player 맵으로 돌린다.
  - 싱글톤에 두지 않는 이유: 대상인 플레이어와 요청하는 쪽(UIManager·CinematicManager)이 모두 씬 오브젝트라, 씬과 같이 사라져야 남은 요청이 다음 씬으로 넘어가지 않는다.
  - `Canvas_InGame`에 두는 이유: 맵 씬 5개에 이미 있어 맵 씬을 고치지 않아도 된다.
  - `UIInputHandler`는 ESC·E 입력 이벤트를 받아 UIManager에 알리는 일만 남긴다.
- **연출 중 외부 대화는 거절한다(2026-10-22 결정).** 밖에서 부르는 `PlayDialogue`는 연출 중이면 무시하고, 연출이 대화 단계로 여는 대화만 받는다.
- **CinematicManager.Play는 꺼져 있으면(`!isActiveAndEnabled`) false.**
- UIManager에서 `_isCinematicActive`, `BeginCinematic`/`EndCinematic`, 연출 분기를 지운다. 펫 안내·독백·외부 대화를 막을 때는 `CinematicManager.IsPlaying`을 읽는다(UI → Core 방향).
- 땜질 두 곳(설정창 뒤 다시 멈추기, 샷 중 매 프레임 확인)을 지운다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 대화·펫 안내·설정창(UI), 카메라 연출(Cinematic), 이후 조작을 잠깐 막아야 하는 기능
- 어떤 시스템/모듈이 건드려지는가:
  - `Core/GameManager.cs`: 시간 정지 요청·해제, 씬 로드 때 요청 비우기
  - 새 `Core` 입력 맵 컴포넌트 + `Canvas_InGame` 프리팹에 배치
  - `UI/UIInputHandler.cs`: 입력 맵 전환을 빼고 입력 이벤트만
  - `UI/UIManager.cs`: 시간·입력을 요청 방식으로, 연출 전용 코드 제거, 외부 대화 거절
  - `UI/Dialogue/DialogueDirector.cs`, `Core/Cinematic/CinematicManager.cs`: Brain 설정 정리(열린 질문 1), 시간·입력 요청 방식
  - TestScene: 다섯 버그 상황 재현용(개인 씬)

## 제약 (Constraints)
- 수정하는 스크립트는 `Scripts/Core`, `Scripts/UI` 안만. GameManager는 수정 가능(본인 작성).
- `PlayerFacade`(입력 맵 전환 `SwitchToPlayerInput`·`SwitchToUIInput`)는 호출만 하고 고치지 않는다. Dialogue 맵은 지금처럼 `PlayerInput`에서 직접 바꾼다.
- 맵 씬은 열거나 고치지 않는다. 확인은 TestScene에서만.
- 대화 흐름 자체(DialoguePresenter, 대사 진행)는 바꾸지 않는다. `DialogueDirector`를 Cinematic 쪽으로 옮기는 일은 이번 범위 밖(2단계, 필요하면 따로 intent).
- `UIManager.PlayDialogue(int)` 시그니처는 유지한다(`Map_0_Tutorial` 등 UnityEvent 연결).

## 열린 질문 (Open questions)
- 없음. 아래 두 가지는 2026-10-22에 결정했다.

### 결정 (2026-10-22)
1. **CinemachineBrain `IgnoreTimeScale`: 대화·연출을 시작할 때 true로만 두고 되돌리지 않는다.**
   - DialogueDirector·CinematicManager의 `IgnoreTimeScale` 저장·복구 코드를 지운다. 서로 덮어쓸 일이 없어진다.
   - 다른 곳에서 timeScale을 바꾸지 않아 평소 동작은 같다. 첫 대화·연출 뒤에는 설정창 일시정지 중에도 진행 중이던 카메라 블렌드·흔들림이 끝까지 돈다(받아들임).
   - 고르지 않은 안: 요청 방식(코드가 늘어남), `Main Camera` 프리팹에서 항상 true(카메라 파트 프리팹 수정).
   - `DefaultBlend`는 CinematicManager만 바꾸므로 그대로 둔다. 종료 블렌드 동안 새 대화가 시작되면 첫 카메라 전환이 연출의 끝 블렌드 시간으로 넘어가는 정도의 차이만 남는다.
2. **인게임 UI 숨기기는 이벤트 방식으로 바꾼다.** CinematicManager가 시작·끝 이벤트를 내고 UIManager가 구독해 숨기고 보인다. 대화와 연출이 둘 다 숨길 수 있으므로 UIManager 안에서 숨기는 쪽을 세어 마지막이 풀 때 보인다. 연출 대화 단계의 `UIManager.PlayDialogue` 호출은 남는다(StageManager와 같은 방식).

## 진행 기록
### 1. 게임 시간 요청 방식 (2026-10-22, 커밋 안 함)
- `GameManager`
  - `PauseGameTime(UnityEngine.Object requester)`/`ResumeGameTime(requester)`: 요청자별 요청·해제(`HashSet`). 같은 요청자가 여러 번 불러도 하나다.
  - `ApplyGameTime()`: `CurrentState == Pause`이거나 요청이 하나라도 있으면 `timeScale` 0·`AudioListener.pause` true, 아니면 1·false. `PauseGame`/`ResumeGame`도 이것으로 정한다. 설정창을 닫아도 요청이 남아 있으면 멈춘 채로 있다.
  - `HandleSceneLoaded`(Single): 파괴된 요청자를 지우고 다시 적용한다.
- `UIManager`: 대화·펫 안내는 `this`로 요청·해제(둘은 동시에 열리지 않는다). 대화가 끝나면 연출 중이어도 자기 요청만 푼다. `CloseSettings`의 "대화·펫 안내 중이면 다시 멈추기" 땜질을 지웠다.
- `CinematicManager`: 샷은 `this`로 요청·해제, 끝나면(`Finish`) 해제. 대화 단계 동안에도 요청해 둬서 대화가 끝난 뒤 다음 단계 전까지 시간이 흐르지 않는다(전과 같은 동작). 샷 중 매 프레임 "시간이 돌았으면 다시 멈추기" 땜질을 지웠다(설정창 동안 샷 시간을 세지 않는 것은 그대로).
- 확인: 컴파일 0에러. TestScene Play(eval 직접 호출)
  - 연출 시간 정지 샷 중 설정창 열고 닫기 → 닫은 직후 `timeScale` 0 (한 프레임 흐르던 문제 해결), `Stop` → 1
  - 연출 대화 단계 중 설정창 열고 닫기 → 0, 대화 끝 직후 0(연출 요청 유지), 시간 흐르는 샷 1, 연출 끝 1
  - 단독 대화·펫 안내: 시작 0 → 설정창 열고 닫기 0 → 끝 1, `AudioListener.pause` false 복구
  - 미확인: 씬 전환 때 파괴된 요청자 정리(TestScene만 사용), 실제 키 입력
- 커밋: `29605df6`(코드), `93c3c87f`(문서)

### 2. 입력 맵 컴포넌트 (2026-10-22, 커밋 안 함)
- 새 `Core/InputMap/InputMapController.cs`(`Core.InputMap`)
  - `EInputMapType { Player, Dialogue, UI }`, 값이 큰 쪽이 우선.
  - `RequestMap(requester, map)`/`ReleaseMap(requester)`: 요청자별 맵(`Dictionary`). 남은 요청 중 가장 큰 맵을 현재 플레이어에 적용, 없으면 Player.
  - Player·UI 맵은 `PlayerFacade.SwitchToPlayerInput/SwitchToUIInput`, Dialogue 맵은 `PlayerInput.SwitchCurrentActionMap("Dialogue")`.
  - `GameManager.OnPlayerSpawned` 구독으로 새 플레이어에 다시 적용. 꺼지면 요청을 비우고 Player 맵.
  - 씬 안 `Instance`(UIManager·CinematicManager와 같은 방식).
- `Canvas_InGame` 프리팹 `UIManager` 오브젝트에 컴포넌트 추가(프리팹 13줄). 맵 씬 5개는 프리팹으로 자동 반영, 맵 씬 직접 수정 없음.
- `UIInputHandler`: `SwitchToPlayerInput/UIInput/DialogueInput`과 대화 맵 이름 상수 삭제. ESC·E 입력 이벤트만 남음.
- `UIManager`: 요청자는 기능별 Presenter(`_dialoguePresenter`·`_petGuidePresenter`·`_settingsPresenter`). 대화·펫 안내는 Dialogue, 설정창은 UI를 요청·해제. `CloseSettings`·`HandlePlayerSpawned`·`EndCinematic`의 맵 고르기 분기 삭제. `BeginCinematic`은 입력을 바꾸지 않음(UI 숨기기만).
- `CinematicManager`: `Play`에서 `this`로 Dialogue 요청, `Finish`에서 해제.
- 확인: 컴파일 0에러. TestScene Play(eval 직접 호출)
  - 시작 Player → 대화 Dialogue → 설정창 UI → 닫기 Dialogue → 대화 끝 Player. 펫 안내도 같음.
  - 연출: 시작 Dialogue, 대화 단계 끝나도 Dialogue, 연출 끝 Player.
  - **버그 4 해결**: 연출 중 UIManager 컴포넌트를 끄고 연출이 끝나면 Player, 다시 켜도 Player.
  - 미확인: 사망 재스폰 때 맵 다시 적용, 실제 키 입력.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
