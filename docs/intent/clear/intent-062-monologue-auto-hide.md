---
id: intent-062
title: 시간을 멈추지 않는 독백(게이지가 다 줄면 꺼지는 대사)
part: UI / Dialogue
status: resolved   # open | resolved
created: 2026-10-22
resolved: 2026-10-22 # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
지금 대사를 띄우는 방법은 두 가지뿐이고, 둘 다 게임을 멈춘다.

- `UIManager.PlayDialogue(id)` (컷씬): `PauseGameTime()`(timeScale 0) + Dialogue 입력 맵 + 연출 카메라 + 다른 인게임 UI 숨김. E로 넘긴다.
- `UIManager.ShowPetGuide(id)` (펫 안내): `PauseGameTime()` + Dialogue 입력 맵. E로 닫는다.

플레이 중 "혼잣말"이나 펫의 짧은 한마디처럼 **흐름을 끊지 않고 잠깐 보여 주고 사라지는 대사**를 띄울 방법이 없다.

## 기대 결과 (Proposed outcome)
`UIManager.Instance.PlayMonologue(id)`를 부르면:

- 게임 시간·입력·카메라는 그대로다. 플레이어는 계속 움직이고 싸운다.
- 컷씬·펫 안내와 같은 패널(이름 + 초상화 + 대사)에 대사가 뜨고, 패널 아래 게이지가 줄어든다.
- 줄마다 게이지가 가득 찬 상태에서 0까지 줄면 다음 줄로 넘어가고, 마지막 줄의 게이지가 다 줄면 패널이 사라진다.
- 끄는 키는 없다(Player 맵에서 E는 `Interact`라 겹친다).

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 기획(독백 SO·테이블 작성), 맵·스테이지 담당(발판 UnityEvent·스크립트에서 호출).
- 어떤 시스템/모듈이 건드려지는가:
  - 새 파일: `UI/Dialogue/MonologuePresenter.cs`, `UI/Dialogue/SOMonologueTable.cs`
  - 수정: `UI/UIManager.cs`, `UI/PetGuide/PetGuideView.cs`(게이지)
  - 프리팹: `Canvas_InGame` (`PetGuidePanel/Gauge`, `MonologuePresenter` 오브젝트)
  - 에셋: `ScriptableObjects/UI/SOMonologueTable.asset`(빈 테이블)

## 제약 (Constraints)
- 게임 시간(`Time.timeScale`)·입력 맵·카메라를 건드리지 않는다.
- `DialoguePresenter`·`PetGuidePresenter`의 동작은 바꾸지 않는다.
- 컷씬용 `SODialogue`·`SODialogueTable`에 독백 전용 칸을 늘리지 않는다.
- 작업 씬은 개인 TestScene만 사용한다.

## 열린 질문 (Open questions)
- (모두 결정됨, 아래 해결 기록 참고)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
  - **패널은 같이 쓴다.** 같은 모양의 대화창 두 개가 동시에 뜨면 헷갈리므로 `PetGuidePanel` 하나를 쓰고, 컷씬·펫 안내가 시작되면 독백을 끈다. 컷씬·펫 안내 중 독백 요청은 무시한다. 독백 중 새 독백이 오면 새것으로 바꾼다.
  - **게이지**: `PetGuidePanel/Gauge`(Image Filled, 가로, 왼쪽 기준, 기본 꺼짐). 독백일 때만 켜고 `E ▶ 다음` 안내는 숨긴다. 게이지는 게임 시간으로 줄므로 설정창 등으로 게임이 멈추면 같이 멈춘다.
  - **시간**: 모든 줄이 같은 시간. 기본값은 `MonologuePresenter._defaultLineDuration`(3초), 독백마다 `SOMonologueTable` 항목의 `LineDuration`으로 바꿀 수 있다(0이면 기본값).
  - **데이터**: 대사 내용은 컷씬과 같은 `SODialogue`를 쓰고(`CameraIndex`는 무시), 번호로 찾는 테이블만 독백 전용 `SOMonologueTable`(Id, Sequence, LineDuration)로 둔다. 튜토리얼은 기존 `SOPetGuideTable` 그대로.
  - `MonologuePresenter`는 꺼져 있는 `PetGuidePanel`에서 코루틴을 돌릴 수 없으므로 `Canvas_InGame/MonologuePresenter`(늘 켜짐)에 둔다.
- 관련 커밋/PR: (커밋 전)
