---
id: intent-068
title: 튜토리얼에서 첫 상호작용 대화가 끝나야 펫이 따라오게 하기
part: Core / Stage / Tutorial / Pet
status: open   # open | resolved
created: 2026-10-22
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
튜토리얼에 처음 들어가면 플레이어를 따라다니는 펫이 바로 켜져 있다. 기획상 펫은 처음엔 없고, 맨 처음 E 상호작용(`ResumeSpawnerCube`, InteractButton → `UIManager.PlayDialogue`)의 대화가 끝난 뒤에 나타나야 한다.
튜토리얼 씬(`Map_0_Tutorial`)은 지금 고치면 안 되므로, 코드와 TestScene 시험까지 해 두고 씬 담당자는 연결만 하게 해야 한다.

## 기대 결과 (Proposed outcome)
- `Core.Stage.Tutorial`(튜토리얼 StageBase)이 `StartStage`에서 펫을 끄고(`PlayerFacade.SetPetEnabled(false)`), `GameManager.OnPlayerSpawned`로 다시 생긴 플레이어에도 같은 상태를 적용한다.
- 공개 메서드 `Tutorial.UnlockPet()`: 재생 중인 대화가 끝날 때까지(`CutsceneController.Current.IsDialoguePlaying`) 기다렸다가 펫을 켠다. 대화 끝 이벤트가 없어 상태를 보고 기다린다.
- 씬 담당자는 `ResumeSpawnerCube`의 `On Interacted` 끝에 `Tutorial.UnlockPet`만 추가한다.
- TestScene `TutorialPetTest` 구역(기본 꺼진 Tutorial 스테이지 + 시험 큐브)으로 시험한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 튜토리얼 씬 담당자(이벤트 연결), 플레이어
- 어떤 시스템/모듈이 건드려지는가: `Core/Stage/Tutorial.cs`만. `PlayerFacade.SetPetEnabled`·`GameManager.OnPlayerSpawned`·`CutsceneController`는 호출만 한다.

## 제약 (Constraints)
- 지켜야 할 정책: `Map_0_Tutorial`은 열지도 고치지도 않는다. 스크립트는 Core/UI/UIandCinematic 안에서만 작성.
- 쓰지 말아야 할 것: Pet·Facade·Mediator 코드 수정, 펫 오브젝트 직접 접근(PlayerFacade만 사용).

## 열린 질문 (Open questions)
- 펫 안내(`PetGuide_1_MoveJump` 등)·스캔 안내가 첫 상호작용보다 앞에 있으면 펫 없이 뜬다. 튜토리얼 배치 순서 확인 필요(씬 담당자).

## 진행 기록
- 처음엔 맵 펫 모델에 붙이는 `PetUnlockPoint`(컷씬 재생)로 만들었으나, 실제 튜토리얼 첫 상호작용이 큐브 + `UIManager.PlayDialogue`라 맞지 않아 삭제하고 `Tutorial.cs`로 옮김(사용자 결정).

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
