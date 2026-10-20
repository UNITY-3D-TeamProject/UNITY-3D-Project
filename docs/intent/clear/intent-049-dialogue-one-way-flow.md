---
id: intent-049
title: 대화 흐름을 UIManager → DialogueDirector 한 방향으로 정리
part: UI / Dialogue
status: resolved   # open | resolved
created: 2026-10-20
resolved: 2026-10-20 # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
`UIManager.PlayDialogue(번호)` → `DialogueManager.Play` → `UIManager.StartDialogue(대화, 줄 콜백, 끝 콜백)` → `DialoguePresenter` 순서로, UIManager와 DialogueManager가 서로를 부른다.
`DialogueManager.Play`는 UIManager만, `UIManager.StartDialogue`는 DialogueManager만 부르므로 UIManager 안에서 끝날 흐름이 한 바퀴 돌아오고, 그 때문에 콜백 전달용 필드가 양쪽에 생겼다(`_dialogueLineChanged`·`_dialogueFinished`, `_onFinished`). `PlayDialogue`는 끝 콜백으로 null을 넘겨 실제로 쓰지도 않는다.

## 기대 결과 (Proposed outcome)
- 대화 진행은 UIManager가 한 곳에서 한다. 호출은 UIManager → DialogueDirector 한 방향.
- `DialogueManager`를 `DialogueDirector`로 이름을 바꾸고(GUID 유지), 연출만 하는 부품으로 만든다: `BeginStaging()`, `ShowLine(SDialogueLine)`, `EndStaging()`. UIManager 참조·`Play`·`_onFinished`·`Instance`를 없앤다.
- UIManager는 DialogueDirector를 `[SerializeField]`로 연결한다(둘 다 Canvas_InGame 프리팹 안). `StartDialogue`는 콜백 없는 private 함수가 되고 콜백 필드 2개를 없앤다.
- 바깥 API(`PlayDialogue(번호)`, `StopDialogue`, `IsDialoguePlaying`)와 동작은 그대로다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: `PlayDialogue`를 부르는 StageManager, 발판(TriggerPlate) — 수정 없음
- 어떤 시스템/모듈이 건드려지는가:
  - `UI/Dialogue/DialogueManager.cs` → `DialogueDirector.cs`
  - `UI/UIManager.cs`
  - `Prefabs/UI/Canvas_InGame.prefab`: UIManager `_dialogueDirector` 연결, 오브젝트 이름 `DialogueManager` → `DialogueDirector`
  - 주석: `DialoguePresenter.cs`, `SODialogue.cs`의 DialogueManager 언급

## 제약 (Constraints)
- DialogueDirector가 연결되지 않은 UIManager(Canvas_Legacy 등)는 지금처럼 대화를 재생하지 않는다(경고만). (사용자 결정)
- 이름은 `DialogueDirector` (사용자 결정, Instance를 없애므로 Manager를 쓸 이유가 없음).
- 맵 씬은 열지 않는다. 확인은 TestScene만.

## 열린 질문 (Open questions)
- 없음

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: 기대 결과대로 구현(2026-10-20). `DialogueDirector`(GUID 유지)는 `BeginStaging`·`ShowLine`·`EndStaging`만 public, Awake에서 연출 카메라 끄기만 남김. UIManager는 `_dialogueDirector`(SerializeField)로 연결, `PlayDialogue` → private `StartDialogue(sequence)`에서 BeginStaging, 줄 바뀜 → `ShowLine`, 끝 → `EndStaging`. 콜백 필드·`using System` 삭제. Canvas_InGame: `_dialogueDirector` 연결, 오브젝트 이름 `DialogueDirector`.
- 확인: 컴파일 0에러, 프리팹 diff 2줄. TestScene Play에서 `PlayDialogue(2)` → Dialogue 맵·펫 카메라·펫 이동, 대화 중 ESC → UI 맵, 닫기 → Dialogue 맵, 줄마다 말하는 사람 카메라 교대, 끝나면 Player 맵·카메라 끔·펫 원래 자리, 재생 후 `StopDialogue` 정리, 없는 번호 경고, Director 미연결 시 경고만 하고 재생 안 함.
- 관련 커밋/PR: (커밋 전)
