---
id: intent-065
title: DialogueDirector를 Core로 옮기고 Core가 UIManager를 직접 부르지 않게 정리
part: Core / UI / Cinematic / Dialogue
status: open   # open | resolved
created: 2026-10-22
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
intent-064(시간·입력·Brain 주인 정리)를 마친 뒤에도 구조 문제 두 가지가 남았다.

1. **카메라 연출 부품이 UI 폴더에 있다.** `UI/Dialogue/DialogueDirector.cs`는 말하는 사람 카메라 전환·고정 카메라·펫 세우기를 맡는 카메라 연출인데 UI 쪽에 있다. 카메라를 다루는 곳이 `UI.Dialogue.DialogueDirector`와 `Core.Cinematic.CinematicManager`로 나뉜다.
2. **Core가 UI를 직접 부른다.** 원래 방향은 UI → Core인데 아래는 반대다.

| 부르는 쪽 (Core) | 부르는 것 (UI) | 이유 |
|---|---|---|
| `CinematicManager.Play` | `UIManager.IsDialoguePlaying`, `IsPetGuideOpen` | 대화·펫 안내 중에는 연출을 시작하지 않음 |
| `CinematicManager.CoPlayDialogue` | `UIManager.PlayCinematicDialogue`, `IsDialoguePlaying` | 대화 단계 재생, 끝날 때까지 대기 |
| `CinematicManager.Stop` | `UIManager.StopDialogue` | 연출을 멈출 때 대화도 끔 |
| `StageManager.PlayIntroDialogue` | `UIManager.PlayDialogue` | 스테이지 시작 대화(지금은 모든 스테이지 번호 0이라 실제로 안 불림) |

동작 문제는 없다(asmdef가 없어 컴파일도 된다). 구조 정리다.

## 기대 결과 (Proposed outcome)
- `DialogueDirector`를 Core 폴더로 옮긴다. `.cs`·`.meta`를 함께 옮겨 GUID를 유지하므로 `Canvas_InGame` 프리팹의 연결(`UIManager._dialogueDirector`)은 그대로다. 네임스페이스는 폴더에 맞춘다.
  - `ShowLine(SDialogueLine)`은 UI 타입(`SDialogueLine`)을 받으므로 `ShowLine(string speakerName, int cameraIndex)`로 바꾼다. UIManager가 줄에서 두 값을 꺼내 넘긴다.
  - 호출 방향은 intent-049대로 UIManager → DialogueDirector 한 방향(UI → Core)이 유지된다.
- Core의 `UI` 참조(`using UI`)를 없앤다. Core에는 대화를 틀어 주는 쪽의 약속(인터페이스)만 두고, UIManager가 그것을 구현한다(열린 질문 2).
  - `CinematicManager`·`StageManager`는 인터페이스만 보고 대화 재생·중지·상태 확인을 한다.
- 동작은 지금과 같다: 연출 중 외부 대화 거절, 대화·펫 안내 중 연출 시작 안 함, 연출 대화 단계 재생·대기, `Stop` 때 대화 끄기.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 대화(UIManager), 카메라 연출(CinematicManager), 스테이지 시작 대화(StageManager)
- 어떤 시스템/모듈이 건드려지는가:
  - `UI/Dialogue/DialogueDirector.cs` → Core 폴더로 이동(GUID 유지), `ShowLine` 인자 변경
  - 새 Core 인터페이스(대화 재생 약속) 파일
  - `UI/UIManager.cs`: 인터페이스 구현·등록, `ShowLine` 호출 변경, `using`
  - `Core/Cinematic/CinematicManager.cs`, `Core/Stage/StageManager.cs`: `UIManager` 대신 인터페이스 사용, `using UI` 제거
  - 주석: `SODialogue.cs`·`DialoguePresenter.cs`·`CinematicSequence.cs`의 DialogueDirector·UIManager 언급
  - `Canvas_InGame` 프리팹: GUID 유지라 변경 없을 것으로 예상(확인 필요)

## 제약 (Constraints)
- 수정하는 스크립트는 `Scripts/Core`, `Scripts/UI` 안만.
- 맵 씬은 열지 않는다. 확인은 TestScene만.
- `UIManager.PlayDialogue(int)` 시그니처 유지(맵 씬 UnityEvent 연결).
- 새 기능 추가 없음. 지금 동작 그대로 구조만 바꾼다.

## 열린 질문 (Open questions)
- 없음. 아래 두 가지는 2026-10-22에 결정했다.

### 결정 (2026-10-22)
1. **DialogueDirector 위치: `Core/Cinematic/` (`Core.Cinematic`).** 카메라 연출이 한 폴더에 모인다. (고르지 않은 안: 새 `Core/Dialogue/`)
2. **Core → UI 끊는 방식: 인터페이스 등록.** `Core/Cinematic/`에 `IDialogueController`(`IsDialoguePlaying`, `IsPetGuideOpen`, `PlayDialogue`, `PlayCinematicDialogue`, `StopDialogue`)와 등록 자리 `DialogueController.Current`(static, `Register`/`Unregister`)를 둔다. UIManager가 구현하고 OnEnable에서 등록·OnDisable에서 해제한다. (고르지 않은 안: 이벤트 방식, 값을 돌려받는 부분 때문에 복잡)

3. **이름: `IDialogueController` / `DialogueController.Current`** (2026-10-22 사용자 결정). 처음에는 `IDialoguePlayer`/`DialoguePlayer`였으나 게임 플레이어와 헷갈려 바꿨다. (고르지 않은 안: Runner, Service, Handler, Display, Host, Bridge)

## 진행 기록
### 1. 구현 (2026-10-22, 커밋 안 함)
- `DialogueDirector`: `AssetDatabase.MoveAsset`으로 `UI/Dialogue/` → `Core/Cinematic/` 이동(GUID `36f4da30…` 유지), 네임스페이스 `Core.Cinematic`. `ShowLine(SDialogueLine)` → `ShowLine(string speakerName, int cameraIndex)`.
- 새 `Core/Cinematic/IDialogueController.cs`(인터페이스), `Core/Cinematic/DialogueController.cs`(static `Current`·`Register`·`Unregister`, 등록한 쪽일 때만 해제).
- `UIManager`: `IDialogueController` 구현, OnEnable 맨 앞 등록·OnDisable 맨 끝 해제. 줄 바뀜 때 `ShowLine(line.SpeakerName, line.CameraIndex)`.
- `CinematicManager`: `using UI` 삭제. `Play`·`Stop`·`CoPlayDialogue`가 `DialogueController.Current`만 사용. 대화 단계 대기는 등록이 풀리면 끝냄.
- `StageManager`: `using UI` → `using Core.Cinematic`, 시작 대화는 `DialogueController.Current.PlayDialogue`.
- `CinematicSequence` 대화 번호 툴팁 문구 수정.
- 결과: `Scripts/Core`에서 `UIManager`·`using UI` 참조 없음(주석만).
- 확인: 컴파일 0에러. `Canvas_InGame` 프리팹 변경 없음, `UIManager._dialogueDirector` 연결 유지·누락 스크립트 0. TestScene Play(eval 직접 호출)
  - `DialogueController.Current == UIManager.Instance`.
  - 대화 1번: 줄마다 SpeakerCam_Pet → Player → Pet, 끝나면 카메라 꺼짐·timeScale 1.
  - 연출 중 밖에서 `PlayDialogue`·펫 안내 거절(경고), 연출 대화 단계 재생, 대화 끝나도 Dialogue 맵·0, 연출 끝 Player·1.
  - UIManager 끄면 `Current` null·`Play` false(경고), 다시 켜면 등록·`Play` true, `Stop` 후 1.
  - 미확인: 맵 씬.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
