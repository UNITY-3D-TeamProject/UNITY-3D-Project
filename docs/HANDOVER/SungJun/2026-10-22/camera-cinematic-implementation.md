# 카메라 연출 1차 구현 (intent-063)

## 1. 작업 요약 및 방법
- 새 스크립트 `Assets/_Project/Scripts/Core/Cinematic/` (네임스페이스 `Core.Cinematic`)
  - `CinematicSequence.cs`
    - `ECinematicStepType { Shot, Dialogue }`
    - `SCinematicStep`: Camera·Duration·BlendInSeconds·ShouldPauseTime·DialogueId
    - 연출 설정 `_id`·`_shouldPlayOnce`·`_endBlendSeconds`(기본 1)
    - Awake에서 샷 카메라를 끈다. `Play()`는 UnityEvent용이다.
  - `CinematicManager.cs`
    - 씬 오브젝트, `Instance`, `IsPlaying`, `Play(sequence)`(bool)·`Stop()`. 처음 넣었던 `PlayIntro()`·`_introSequence`는 후속으로 없앴다(아래).
    - 연출 동안 CinemachineBrain `IgnoreTimeScale` true. 샷마다 `DefaultBlend`를 바꾸고, 끝 블렌드가 끝나면 원래대로 돌린다.
    - 샷은 `ShouldPauseTime`에 따라 `PauseGameTime`/`ResumeGameTime`을 부른다. 설정창이 열린 동안에는 시간을 세지 않고, 닫은 뒤 멈추는 샷이면 다시 멈춘다.
    - 대화 단계는 `UIManager.PlayDialogue(id)`를 부르고 `IsDialoguePlaying`이 false가 될 때까지 기다린다.
    - 한 번만 재생: 시작할 때 PlayerPrefs `Cinematic.Seen.{Id}` = 1.
    - `Play`는 상태만 바꾸고 코루틴은 다음 `Update`에서 시작한다(아래 이슈).
- `UI/UIManager.cs`
  - `_isCinematicActive`, `BeginCinematic()`(bool)·`EndCinematic()` 추가. 연출 동안 상호작용 안내를 막고, 인게임 UI alpha를 0으로, Dialogue 맵으로 바꾼다.
  - `HandleDialogueEnded`: 연출 중이면 시간·입력·UI를 돌려주지 않는다(DialogueDirector 정리는 한다).
  - 연출 중에는 `ShowPetGuide`·`PlayMonologue`를 무시한다. `CloseSettings`·`HandlePlayerSpawned`는 연출 중이면 Dialogue 맵을 유지한다.
- `Core/Stage/StageManager.cs` `PlayIntroDialogue`: 시작 연출이 재생되면 시작 대화를 부르지 않는다.
  - 후속: `CinematicManager.PlayIntro()`·`_introSequence`를 없애고, 시작 연출을 `StageManager._introCinematic`(Header Intro)에 지정해 `CinematicManager.Instance.Play(_introCinematic)`로 직접 부르게 바꿨다.
  - TestScene은 StageManager에 `Cinematic_Intro`를 연결했다. 컴파일 0에러, Play에서 시작 연출 1.5초 → 플레이어 카메라 복귀를 확인했다.
- TestScene(개인 저장소, 커밋 안 함)
  - 루트 `CinematicManager`(시작 연출 = `Cinematic_Intro`)
  - `Cinematic_Intro`: Id `TestScene.Intro`, 한 번만 재생, 끝 블렌드 2초. 단계는 [전경 샷 `CinematicCam_IntroOverview` 1.5초, 컷, 시간 멈춤].
  - `CutsceneTest/Cinematic_PlateTest`: Id `TestScene.PlateCinematic`, 매번 재생, 끝 블렌드 1.5초. 단계는 [퍼즐 전경 2.5초, 블렌드 1.5, 시간 멈춤] → [대화 1번] → [몬스터 바닥 3초, 블렌드 1, 시간 흐름].
  - `CutsceneTest/Plate_Cinematic` (61, 0.55, -28.03): TriggerPlate → `CinematicSequence.Play()`. `Sign_Cinematic` 문구 `카메라 연출`.
  - 샷 카메라는 CinemachineCamera Priority 20, 꺼진 상태.

## 2. 결정 사항
- 연출 실행 흐름은 `CinematicManager`가 맡는다. UIManager는 입력 맵·UI 숨김만 공개 함수로 제공한다(연출 → UIManager 한 방향).
- 대화 끝 알림 이벤트는 만들지 않고 `IsDialoguePlaying` 확인으로 대신했다.
- 무적은 없다. 시간이 흐르는 샷은 적이 없는 곳에서만 쓴다.

## 3. 현재 상태 및 이슈
- 컴파일 0에러. TestScene Play(EditorApplication.Step 프레임 진행)
  - 시작 연출: 처음 Play에서 1.5초 전경(timeScale 0, Dialogue 맵, UI 숨김) → Player 맵·UI 복구, 2초 뒤 Brain 블렌드 0.2·IgnoreTimeScale false 복구. 다음 Play에서는 본 기록 때문에 생략된다.
  - 발판 연출: 순서대로 진행된다. 대화와 샷 사이에 Dialogue 맵·UI 숨김이 유지됐고, 시간이 흐르는 샷에서 적이 움직였으며, 끝나면 모두 복구됐다. 게임 화면 캡처로 UI 숨김·퍼즐 전경도 확인했다.
  - 샷 중 설정창 열고 닫기, 대화 중 `Stop()` 복구 확인.
- 처음 구현에서는 발판 트리거에서 시작하면 첫 샷이 끝나지 않았다. eval(Update 쪽)에서 부르면 정상이었다. 물리 콜백에서 시작한 중첩 코루틴이 시간 정지 후 진행되지 않는 것으로 보고, 시작을 다음 Update로 미뤄 해결했다. 정확한 Unity 내부 원인은 확인하지 않았다.
- 확인 뒤 `Cinematic.Seen.TestScene.Intro` 키를 지워 두었다. 다음 Play에서 시작 연출을 한 번 볼 수 있다.
- 실제 키보드 조작·맵 씬 배치는 미확인. 커밋 안 함.

## 4. 다음 할 일 (Next Steps)
- 실제 조작으로 TestScene 시작 연출과 `Plate_Cinematic` 확인.
- 맵 담당과 로비 첫 입장 전경·미로 시작 연출 배치(씬에 `CinematicManager` + `CinematicSequence` + 샷 카메라).
- 필요해지면 건너뛰기·무적·Timeline 단계 검토.
