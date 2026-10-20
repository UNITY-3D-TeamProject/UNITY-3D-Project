---
id: intent-046
title: 컷씬을 번호로 UIManager에서 재생
part: UI / Cutscene
status: open   # open | resolved
created: 2026-10-20
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
컷씬을 하나 추가하려면 씬에 `DialogueCutscene` 오브젝트를 두고 대화 SO·Director·고정 카메라를 연결한 뒤, 발판 UnityEvent나 `StageIntroDialogue`로 `Play`를 연결해야 한다.
원하는 타이밍에 코드 한 줄로 부를 수 없고, 컷씬이 늘어날수록 씬 오브젝트가 늘어난다.

## 기대 결과 (Proposed outcome)
- 펫 안내(`ShowPetGuide(id)`)처럼 `UIManager.Instance.PlayCutscene(id)`를 부르면 그 번호의 컷씬이 바로 재생된다.
  - (2026-10-20) 이름을 `UIManager.Instance.PlayDialogue(id)`로 바꿨다. 기존 `PlayDialogue(SODialogue, …)`는 `StartDialogue`.
- 번호 → 대화 SO는 `SOCutsceneTable`에서 정한다. 컷씬 추가 = 대화 SO 만들기 + 테이블에 번호 추가.
- 펫 세우기·말하는 사람 카메라 연출은 지금 `DialogueDirector` 그대로 쓴다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 컷씬을 띄우는 쪽(스테이지 시작, 발판, 이후 퀘스트 등)
- 어떤 시스템/모듈이 건드려지는가:
  - `UI/Cutscene`: `SOCutsceneTable` 추가, `DialogueDirector` 수정, `StageIntroDialogue` 수정, `DialogueCutscene` 삭제
  - `UI/UIManager`: `PlayCutscene(id)` 추가, 테이블 연결 (`Canvas_InGame` 프리팹)
  - `Map_0_Tutorial` 씬: **이번 작업에서 건드리지 않는다 (사용자 지시)**. 테스트는 로컬 TestScene에서만 한다.
    - 그대로 두면 `Cutscene_StageIntro`에 지운 `DialogueCutscene`이 Missing Script로 남고, Director의 고정 카메라 목록이 비어 있어 CameraIndex 0·1 줄은 말하는 사람 카메라로 대신 나온다. 시작 대화는 `StageIntroDialogue._cutsceneId` 기본값 1(SODialogue_Tutorial)로 재생된다. 씬 정리는 담당자가 따로 한다.

## 제약 (Constraints)
- 고정 카메라는 씬 오브젝트라 SO에 넣을 수 없다 → 씬의 `DialogueDirector`에 고정 카메라 목록을 두고, 대사의 `CameraIndex`가 그 목록 번호를 가리킨다. (사용자 결정)
- 본 기록(PlayerPrefs)은 남기지 않는다. 부를 때마다 재생하고, 한 번만 띄우는 건 부르는 쪽이 정한다. (사용자 결정)
- 기존 `DialogueCutscene`은 새 방식으로 대체하고 지운다. (사용자 결정)
- `UIManager`는 `Canvas_InGame` 프리팹에 있고 `DialogueDirector`는 씬에 있으므로, UIManager는 `DialogueDirector.Instance`(씬당 하나)로 Director를 찾는다.

## 열린 질문 (Open questions)
- 없음

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
