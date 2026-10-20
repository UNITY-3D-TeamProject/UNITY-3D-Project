---
id: intent-047
title: 스테이지 시작 대화를 씬 오브젝트 없이 재생
part: UI / Cutscene / Stage
status: resolved   # open | resolved
created: 2026-10-20
resolved: 2026-10-20 # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
스테이지 시작 대화를 띄우려면 씬마다 `DialogueManager`(+ 말하는 사람 카메라 2대)와 `StageIntroDialogue` 오브젝트를 배치해야 한다.
develop 머지(PR #97, `feature/Map/Tutorial`)로 튜토리얼 씬이 팀원 버전으로 덮어써지면서 이 오브젝트가 모두 빠졌고, 튜토리얼 시작 대화가 나오지 않는다.
튜토리얼 씬은 다른 팀원이 작업하는 씬이라 이 브랜치에서 수정하지 않는다.

## 기대 결과 (Proposed outcome)
- `StageManager.StartStage()`가 지금 스테이지 종류(`EStageType`)를 보고 시작 대화 번호를 골라 `UIManager.Instance.PlayDialogue(번호)`를 부른다. 번호는 코드에서 정한다. 지금은 사용자 요청으로 모든 스테이지에서 꺼 둔다 (켤 스테이지만 switch에 추가, 예: Tutorial → 1).
- `DialogueManager`와 말하는 사람 카메라 2대를 `Canvas_InGame` 프리팹 안에 둔다. 이 프리팹이 있는 맵 씬(튜토리얼·로비·Feed·File·Security)은 씬 수정 없이 대화 연출이 된다.
- `StageIntroDialogue`는 필요 없어져 삭제한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 스테이지 시작 대화, 발판 등에서 `PlayDialogue(번호)`를 부르는 쪽
- 어떤 시스템/모듈이 건드려지는가:
  - `Core/Stage/StageManager.cs`: 시작 대화 호출 추가 (Core가 UI를 참조하게 됨, 사용자 승인)
  - `Prefabs/UI/Canvas_InGame.prefab`: `DialogueManager` + `SpeakerCam_Player`, `SpeakerCam_Pet` 추가
  - `UI/Cutscene/StageIntroDialogue.cs`: 삭제
  - 개인 TestScene(Git 미포함): 씬의 `DialogueDirector`, `Cutscene_StageIntro` 제거

## 제약 (Constraints)
- `Map_0_Tutorial` 등 맵 씬 파일은 수정하지 않는다.
- 씬마다 두던 고정 카메라(대사 CameraIndex 0 이상)는 프리팹에 넣을 수 없으므로 없앤다. 해당 줄은 말하는 사람 카메라로 나온다. (사용자 승인)
- 팀원 파일(FeedApp 등 스테이지 클래스)은 수정하지 않는다.

## 열린 질문 (Open questions)
- 없음

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: 위 기대 결과대로 구현(2026-10-20). `StageManager.PlayIntroDialogue`(switch, 지금은 모두 0), `Canvas_InGame/DialogueManager`(SpeakerCam 2대, 고정 카메라 없음, 카메라 컴포넌트는 꺼진 상태로 저장), `StageIntroDialogue.cs` 삭제, TestScene의 `DialogueDirector`·`Cutscene_StageIntro` 삭제. 확인: 컴파일 0에러, TestScene Play에서 시작 대화 없음·발판 `PlayDialogue(2)` 재생(프리팹 DialogueManager, 펫 카메라 켜짐). 튜토리얼 Play는 미확인(씬 미수정).
- 관련 커밋/PR: 2824eb23 (PlayCutscene → PlayDialogue 이름 변경), b99ca76c (StageManager 시작 대화·Canvas_InGame DialogueManager·StageIntroDialogue 삭제)
