---
id: intent-067
title: 컷씬 대사 부품의 Dialogue 이름을 Cutscene으로 변경
part: UIandCinematic / UI / Core.InputMap
status: resolved
created: 2026-10-22
resolved: 2026-10-22
---

## 문제 (Problem)
- 앞으로 대사 연출은 `SOCutscene`으로 만들고 옛 대화 SO(`SODialogue`·`SODialogueTable`)는 쓰지 않는다.
- 그런데 컷씬 대사를 띄우는 공용 부품 이름이 여전히 Dialogue라서, 다른 파트가 보면 옛 대화 시스템과 헷갈린다.
  - `IDialogueController`·`DialogueController`(대사 맡는 쪽 등록), `DialoguePresenter`(대사창), `DialogueDirector`(펫 세우기·말하는 사람 카메라)
  - 입력 맵 `Dialogue`(조작 막고 E로 넘기기·ESC 설정창)

## 기대 결과 (Proposed outcome)
- 공용 부품 이름이 Cutscene으로 바뀐다.
  - `IDialogueController` → `ICutsceneController`, `DialogueController` → `CutsceneController`
  - `DialoguePresenter` → `CutscenePresenter`, `DialogueDirector` → `CutsceneDirector`
  - 두 부품 스크립트는 `Scripts/UIandCinematic/Dialogue/` → `Scripts/UIandCinematic/Cutscene/`으로 옮긴다.
  - `UIManager` 필드 `_dialoguePresenter`·`_dialogueDirector` → `_cutscenePresenter`·`_cutsceneDirector`
- 입력 맵 `Dialogue` → `Cutscene` (`FID_InputSystem.inputactions`, `EInputMapType`, `InputMapController`, `UIInputHandler`)
- 동작은 그대로다. `Canvas_InGame` 연결이 끊기지 않는다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 컷씬·카메라 샷·독백·펫 안내를 부르는 모든 파트
- 어떤 시스템/모듈이 건드려지는가:
  - 이름 변경: `UIandCinematic`(Common·Cutscene·Dialogue 폴더, CinematicManager, SOCutscene 툴팁), `UI/UIManager`·`UI/UIInputHandler`, `Core/InputMap/InputMapController`, `Core/Stage/StageManager`(등록 클래스 이름만)
  - 에셋: `Assets/FID_InputSystem.inputactions`의 맵 이름, `Prefabs/UI/Canvas_InGame.prefab`의 UIManager 필드 이름·`DialogueDirector` 오브젝트 이름
  - 씬: 두 필드를 씬에서 덮어쓴 곳이 없어 씬 수정 없음

## 제약 (Constraints)
- 스크립트는 `.cs`와 `.meta`를 같이 옮겨 GUID를 유지한다.
- `Map_0_Tutorial`과 다른 맵 씬은 열거나 고치지 않는다.
- **옛 대화 시스템은 이번에 지우지 않는다.** `ScriptableObjects/UIandCinematic/Dialogue`·`DialogueTable` 폴더(공용·Test)를 아직 남겨 두므로, 이 에셋이 쓰는 코드도 이름·기능 그대로 둔다.
  - 그대로: `SODialogue`·`SDialogueLine`·`SODialogueTable`, `UIManager.PlayDialogue`·`PlayCinematicDialogue`·`StopDialogue`·`IsDialoguePlaying`, `CutscenePresenter` 안의 여러 줄 대화 진행(`StartDialogue` 등), `CutsceneDirector`의 `CameraIndex`·고정 카메라, `CinematicSequence` 대화 단계, `StageManager.PlayIntroDialogue`
  - 튜토리얼 담당이 대사를 `SOCutscene`으로 옮기고 발판을 `PlayCutscene`으로 바꾼 뒤, 폴더와 함께 지운다(후속 작업).
- 펫 안내(PetGuide)는 컷씬과 다른 기능이라 그대로 둔다.

## 열린 질문 (Open questions)
- 없음 (2026-10-22 사용자 확인: 새 이름 4개 그대로, 입력 맵도 Cutscene으로, Dialogue·DialogueTable 폴더는 지금 지우지 않음)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: 기대 결과대로 이름만 바꿨다. 스크립트 4개는 `git mv`로 `.cs`·`.meta`를 같이 옮겨 GUID 유지, `Canvas_InGame`은 UIManager 필드 이름(`_cutscenePresenter`·`_cutsceneDirector`)·오브젝트 이름(`CutsceneDirector`)을 프리팹 텍스트에서 바꿔 연결 유지. 입력 맵은 `FID_InputSystem`의 맵 이름만 `Cutscene`으로(액션 `NextLine`·`OpenSettings`와 id는 그대로). 씬 수정 없음.
- 확인: 컴파일 0에러, `Canvas_InGame`·TestScene 빠진 스크립트 0·필드 연결 유지, 입력 에셋에 `Cutscene/NextLine`·`Cutscene/OpenSettings` 있음. TestScene Play에서 컷씬(`PlayCutscene(2)`) 중 입력 맵 `Cutscene`·시간 0, E 4번으로 끝까지 진행 후 끝 블렌드 뒤 `Player`·시간 1, 펫 안내 중 `Cutscene` 맵·E로 닫힘, 카메라 샷 중 ESC로 설정창 열고 닫기(맵 UI → Cutscene), `Stop` 뒤 `Player` 복구, 독백 재생 중 `Player` 맵 유지.
- 남은 일(후속): 튜토리얼 담당이 대사를 `SOCutscene`으로 옮기고 발판을 `PlayCutscene`으로 바꾸면, `ScriptableObjects/UIandCinematic/Dialogue`·`DialogueTable`(공용·Test)과 옛 대화 코드(`SODialogue`·`SODialogueTable`·`PlayDialogue`·`PlayCinematicDialogue`·`IsDialoguePlaying`·`StopDialogue`·`CutscenePresenter`의 여러 줄 진행·`CutsceneDirector`의 `CameraIndex`·`CinematicSequence` 대화 단계·`StageManager.PlayIntroDialogue`·Test `TestUIManagerCaller.PlayDialogue`)를 함께 지운다.
- 관련 커밋/PR: `440d8861`(이름 변경), 문서·가이드 갱신은 다음 커밋
