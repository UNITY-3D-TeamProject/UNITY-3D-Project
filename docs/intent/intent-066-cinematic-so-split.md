---
id: intent-066
title: 카메라 연출·컷씬·독백을 종류별 SO로 나누기
part: Core / Cinematic / UI / Dialogue
status: open   # open | resolved
created: 2026-10-22
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
연출 데이터가 종류별로 나뉘어 있지 않아 헷갈린다.

- 카메라 연출은 씬 컴포넌트(`CinematicSequence`)에만 정의할 수 있다. 그래서 데이터를 에셋으로 모아 볼 수 없다.
- 컷씬 대사(`SODialogue`)에서 카메라를 고르는 방법은 `CameraIndex` 번호 하나뿐이다. 이 번호는 `DialogueDirector._fixedCameras` 배열 순서를 가리키므로, 번호만 봐서는 어떤 카메라인지 알 수 없다.
- 대화 사이에 카메라 연출을 넣으려면 대화를 둘로 나누고, 씬의 `CinematicSequence`에 [대화] → [샷] → [대화] 단계를 따로 구성해야 한다.
- 독백 표(`SOMonologueTable`)도 컷씬과 같은 `SODialogue`를 쓴다. 그래서 컷씬용 칸(초상화·`CameraIndex`)이 독백에도 보이고, 두 데이터가 섞여 헷갈린다.

## 기대 결과 (Proposed outcome)
연출 데이터를 종류별 SO로 나누고, 번호 표로 재생한다.

| 종류 | SO | 내용 | 표 |
|---|---|---|---|
| 1. 카메라 연출 | `SOCameraShot` | 샷 목록: 카메라 키, 보여 줄 시간, 들어오는 블렌드, 게임 시간 멈춤 | `SOCameraShotTable` (번호 → SO) |
| 2. 컷씬 | `SOCutscene` | 단계 목록: [대사 + 카메라 키] 또는 [카메라 연출(`SOCameraShot`)] | `SOCutsceneTable` (번호 → SO) |
| 3. 독백 | `SOMonologue` | 줄 목록: 말하는 사람·초상화·대사, 한 줄 시간 | `SOMonologueTable` (번호 → SO) |
| 4. 펫 안내 | `SOPetGuideTable` | 지금 그대로 | 지금 그대로 |

- **카메라 키**: 씬 카메라에 키를 적는 작은 컴포넌트(`CinematicCamera`, `Key` 문자열)를 붙인다. SO에는 키만 적고, 재생할 때 씬에서 같은 키의 카메라를 찾는다. SO는 씬 오브젝트를 직접 가리킬 수 없기 때문이다.
- **컷씬 단계**
  - 대사 단계: 말하는 사람·초상화·대사 + 카메라 키. 키를 비우면 지금처럼 말하는 사람(PLAYER/PET) 카메라를 쓴다.
  - 카메라 단계: `SOCameraShot`을 가리킨다. 그 SO의 샷을 순서대로 재생한 뒤 다음 단계로 넘어간다.
- **재생**: 표는 `CinematicManager`가 들고 있고, `PlayCutscene(번호)`·`PlayCameraShot(번호)`로 재생한다. 단계 진행·시간·카메라·입력은 `CinematicManager`가 맡고, 대사 표시는 `IDialogueController`(UIManager)에 맡긴다.
- **독백**: 표는 번호 → `SOMonologue`만 들고 있다. 한 줄 시간은 독백 SO가 가진다(0이면 `MonologuePresenter` 기본 시간).

예시: 튜토리얼에서 대화 도중 다리를 보여 주는 경우

```
SOCutscene_Tutorial
  0: Line   PET "저기 봐"          Camera=(비움 → 펫 카메라)
  1: Camera → SOCameraShot_Bridge  (Key="Tutorial_Bridge", 2초, 블렌드 1초, 시간 멈춤)
  2: Line   PLAYER "..."           Camera="Tutorial_Gate"
```

## 결정 사항 (2026-10-22 사용자 답변)
1. 카메라 참조: **키(이름)로 찾는다.** 씬 카메라에 키 컴포넌트를 붙이고, SO에는 키 문자열을 적는다.
2. 컷씬 단계: **[대사 + 카메라 키] 또는 [대사 없이 카메라만]** 두 종류. 대사의 카메라 키를 비우면 지금처럼 말하는 사람 카메라를 쓴다.
3. 컷씬의 카메라 단계: **카메라 연출 SO(`SOCameraShot`)를 가리킨다.** 같은 카메라 연출을 단독으로도, 컷씬 안에서도 쓸 수 있다.
4. 기존 구조: **당분간 같이 유지.** `CinematicSequence`, `SODialogue`·`SODialogueTable`·`UIManager.PlayDialogue`, `CameraIndex`·`Fixed Cameras`는 그대로 둔다.
5. 재생: **번호 표로 재생.** 카메라 표·컷씬 표는 따로 둔다.
6. 독백 SO: **말하는 사람·초상화·대사 + 한 줄 시간(독백 SO 하나에 하나).** 카메라 칸은 없다.
7. 표를 들고 재생하는 쪽: **`CinematicManager`.**

## 영향 범위 (Affected users and systems)
- 누가 사용하는가:
  - 기획: 컷씬·카메라 연출·독백 SO 작성
  - 맵 담당: 씬 카메라에 키 컴포넌트를 붙이고, 씬의 `CinematicManager`에 표를 연결
- 어떤 시스템/모듈이 건드려지는가 (예상):
  - 새 파일 `Scripts/Core/Cinematic/` (`Core.Cinematic`)
    - `CinematicCamera`: 씬 카메라 키
    - `SOCameraShot`·`SOCameraShotTable`
    - `SOCutscene`·`SOCutsceneTable`
  - 수정 `Core/Cinematic/CinematicManager.cs`: 표 필드, `PlayCutscene`·`PlayCameraShot`, 컷씬 단계 진행
  - 수정 `Core/Cinematic/IDialogueController.cs`·`DialogueDirector.cs`: 컷씬 대사 한 줄 표시와 카메라 키로 고른 카메라 켜기(방식은 구현 때 정함)
  - 수정 `UI/UIManager.cs`·`UI/Dialogue/DialoguePresenter.cs`: `SODialogue` 없이 컷씬 대사 한 줄을 표시
  - 새 파일 `UI/Dialogue/SOMonologue.cs`, 수정 `SOMonologueTable.cs`·`MonologuePresenter.cs`·`UIManager.PlayMonologue`
  - 에셋 폴더 (예상)
    - `ScriptableObjects/UIandCinematic/` 한 폴더 아래 종류별 폴더 9개: `CameraShot`·`CameraShotTable`·`Cutscene`·`CutsceneTable`·`Dialogue`·`DialogueTable`·`Monologue`·`MonologueTable`·`PetGuideTable`
  - 그대로: `CinematicSequence`, `SODialogue`·`SODialogueTable`, `StageManager` 시작 대화 번호, 펫 안내
- 관련 intent
  - [intent-063 카메라 연출](./intent-063-camera-cinematic-sequence.md)
  - [intent-065 DialogueDirector Core 이동](./intent-065-dialogue-director-core.md)
  - [intent-046 컷씬 테이블](./intent-046-cutscene-table.md)
  - [intent-062 독백 자동 숨김](./clear/intent-062-monologue-auto-hide.md)

## 제약 (Constraints)
- 예산/기한: 없음.
- 지켜야 할 정책:
  - 연출 흐름(단계 진행·시간·카메라·입력)은 `CinematicManager`가 맡고, UIManager는 대사 표시만 한다(intent-063).
  - Core는 UIManager를 직접 참조하지 않고 `IDialogueController`만 안다(intent-065).
  - 기존 대화·연출 흐름의 지금 동작은 바꾸지 않는다(결정 4).
  - 작업·확인은 개인 TestScene에서만 한다. 맵 씬 배치는 맵 담당 몫이다.
- 쓰지 말아야 할 것: Timeline(`PlayableDirector`)은 쓰지 않는다(intent-063과 같음).

## 열린 질문 (Open questions)
구현 전에 기본값으로 정해 둔 것들이다. 다르게 원하면 바꾼다.
- 대사 단계의 카메라: 키만 적는다. 다음 줄까지 유지되고, 블렌드는 Brain 기본값을 쓴다.
- 컷씬·카메라 연출 SO 전체 설정: `CinematicSequence`처럼 Id, 한 번만 재생, 끝 블렌드를 둔다.
- 키에 맞는 카메라가 씬에 없을 때: 경고 로그를 남긴다. 대사 단계는 말하는 사람 카메라로, 카메라 단계는 그 샷을 건너뛴다.
- 컷씬 중 펫: 대사 단계가 이어지는 동안 지금처럼 앞에 세운다. 카메라 단계(특히 시간이 흐르는 샷)에서 펫을 어떻게 할지는 구현하면서 확인한다.
- 기존 독백 데이터: `_Project`의 `SOMonologueTable`은 비어 있어 옮길 데이터가 없다. 테스트 폴더의 `SODialogue_TestMonologue`는 새 `SOMonologue`로 다시 만든다.

## 진행 기록 (2026-10-22 1차 구현)
- 새 파일 `Scripts/Core/Cinematic/`
  - `CinematicCamera`: 씬 카메라 키. 켜져 있는 동안 static 목록에 올라가고 `TryFind(key)`로 찾는다. Awake에서 CinemachineCamera를 꺼 둔다.
  - `SOCameraShot`(`SCameraShot`: 키·시간·블렌드·시간 멈춤)·`SOCameraShotTable`
  - `SOCutscene`(`ECutsceneStepType` Line·Camera, `SCutsceneStep`)·`SOCutsceneTable`
- `CinematicManager`
  - 표 두 개, `PlayCutscene(id)`·`PlayCameraShot(id)`(void, UnityEvent 연결용). 시작 검사는 `TryStart`로 모아 `Play(sequence)`와 같이 쓴다.
  - 컷씬 대사 단계: 키 카메라를 켜고(없으면 말하는 사람 카메라), 시간 정지, `ShowCutsceneLine` 뒤 E를 기다린다. 마지막 단계면 "E ▶ 닫기".
  - 컷씬 카메라 단계: 대사창·말하는 사람 카메라를 끄고 `SOCameraShot`을 끝까지 재생한다.
  - `Finish`에서 `EndCutscene`으로 펫·카메라를 돌려놓는다. 본 기록은 Id 문자열로 바꿔 세 종류가 같이 쓴다.
  - `Stop`은 `StopAllCoroutines`로 단계 코루틴까지 모두 멈춘다.
- `IDialogueController`/`UIManager`: `IsCutsceneLineWaiting`·`ShowCutsceneLine`·`HideCutsceneLine`·`EndCutscene`. 첫 대사 때 `BeginStaging`, 컷씬이 끝날 때 `EndStaging`. E는 컷씬 대사면 기다림만 푼다.
- `DialoguePresenter`: `ShowSingleLine`·`HideSingleLine`(줄 그리기는 `DrawLine`으로 공유). `DialogueDirector.HideCameras()` 추가.
- 독백: `SOMonologue`(줄 + `LineDuration`), `SOMonologueTable`은 번호 → `SOMonologue`, `MonologuePresenter.Play(SOMonologue)`.
- 에셋
  - `_Project/ScriptableObjects/Cinematic/` 아래 `CutsceneTable/SOCutsceneTable`, `CameraShotTable/SOCameraShotTable`(빈 표), `Cutscene`·`CameraShot` 폴더
  - `UI/Monologue` 폴더
  - 맵 씬 `CinematicManager`에는 연결하지 않았다(맵 담당 몫).
  - 사용자 요청으로 `ScriptableObjects/Cinematic`과 `ScriptableObjects/UI`의 하위 폴더를 `ScriptableObjects/UIandCinematic` 하나로 합쳤다. 테스트 폴더도 `UnityAssets/UIandCinematic`으로 합쳤다.
    - Unity에서 옮겨 GUID를 유지했다. `Canvas_InGame`의 대화·독백·펫 안내 표 연결도 그대로다.
  - 스크립트도 사용자 요청으로 `Scripts/UIandCinematic/` 하나로 모았다.
    - 이전 `Core/Cinematic`·`UI/Dialogue`·`UI/PetGuide`의 19개 파일을 Unity에서 옮겨 GUID를 유지했다.
    - 종류별 폴더: `CameraShot`(SOCameraShot·Table·CinematicCamera), `Cutscene`(SOCutscene·Table), `Dialogue`(SODialogue·Table·DialoguePresenter·DialogueDirector), `Monologue`(SOMonologue·Table·MonologuePresenter), `PetGuide`(SOPetGuideTable·PetGuidePresenter·PetGuideView), `Common`(CinematicManager·CinematicSequence·DialogueController·IDialogueController)
    - 네임스페이스는 하위 폴더와 상관없이 `UIandCinematic` 하나다(사용자 결정, 컨벤션의 "폴더 = 네임스페이스" 규칙의 예외).
    - `UIManager`·`StageManager`의 using을 바꿨다. TestScene 발판 이벤트의 저장된 타입 이름(`Core.Cinematic.CinematicManager`)을 다시 연결해 갱신했다.
    - 컴파일 0에러. `Canvas_InGame`·TestScene에 빠진 스크립트가 없고, TestScene Play로 발판 4개와 펫 안내 동작을 확인했다.
  - TestScene `CutsceneTest` 발판 4개를 SO 종류별 예시로 바꿨다.
    - 1. 컷씬(대화만) `SOCutscene_TestTalk` → `PlayCutscene(2)`
    - 2. 컷씬(대화+카메라) → `PlayCutscene(1)`
    - 3. 카메라 샷 → `PlayCameraShot(1)`
    - 4. 독백 → `PlayMonologue(1)`
    - 팻말에 기반 SO 이름을 적었다.
  - 끝 블렌드 중 조작 수정(사용자 제보): 연출이 끝나면 플레이어 카메라로 돌아오는 블렌드를 시작하자마자 시간·조작을 풀어, 카메라가 돌아오는 동안 움직이거나 탈것(블루투스)이 앞으로 나가 떨어졌다.
    - `Finish`는 시간 정지를 유지하고, 끝 블렌드가 끝난 뒤 `CoRestoreBrain`에서 `ReleaseControl`(시간 재개·입력 맵 해제)을 부른다.
    - 매니저가 꺼질 때는 바로 푼다.
    - TestScene 확인: 3번 카메라 샷은 블렌드 동안 Dialogue 맵·시간 0, 끝난 뒤 Player 맵·시간 1. 2번 컷씬은 끝까지 진행 후 복구.
  - 생성 메뉴도 `UI/…`·`Cinematic/…`(Unity 기본 메뉴의 Cinematic과 헷갈림)을 `UIandCinematic/…` 하나로 모았다. 9개: Camera Shot, Camera Shot Table, Cutscene, Cutscene Table, Dialogue, Dialogue Table, Monologue, Monologue Table, Pet Guide Table.
- TestScene
  - 테스트 카메라 3대에 키(`Test_IntroOverview`·`Test_PuzzleOverview`·`Test_MonsterFloor`)를 붙였다.
  - `CinematicManager`에 테스트 표를 연결했다.
  - 테스트 컷씬 1번: 펫 대사 → 키 카메라 대사 → 카메라 연출(2샷) → 펫 대사.
  - 테스트 독백을 `SOMonologue_Test`로 옮겼다.
- 확인(TestScene Play, eval)
  - 컷씬 전체 흐름: 펫 카메라 → 키 카메라 → 대사창 숨김·샷 2개(시간 멈춤/흐름) → 펫 카메라 "닫기" → Player 맵·시간 1·UI 보임·펫 자리 복구
  - 카메라 연출 단독 재생, 대사·카메라 단계 중 Stop 복구, 없는 번호 경고, 독백
  - 기존 `CinematicSequence`·`PlayDialogue` 회귀
- 확인 중 발견: `EditorApplication.Step`으로 프레임을 강제로 넘기면, 연출을 Stop하거나 끝난 직후 새 연출을 시작했을 때 첫 샷 코루틴이 다시 돌지 않는 경우가 있었다.
  - 같은 순서를 실제 시간(`Application.runInBackground = true`)으로 돌리면 재현되지 않았다. 테스트 방식 문제로 보고 코드는 바꾸지 않았다.
  - 실제 조작으로 한 번 더 확인할 것.
