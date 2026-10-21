---
id: intent-058
title: 대화 중에는 대화창만 보이고 나머지 인게임 UI 숨기기
part: UI / Dialogue
status: resolved   # open | resolved
created: 2026-10-22
resolved: 2026-10-22 # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
대화(Dialogue)가 진행되는 동안에도 `Canvas_InGame`의 왼쪽 위 미션 패널(`Active Mission`), 가운데 크로스헤어(`Crosshair`), 체력·스킬·열·배터리 HUD(`HpPanel`·`Skills`·`HeatLabel`·`BatteryLabel`), 보스 체력바(`BossHealth`)가 그대로 떠 있다.
연출 카메라로 플레이어·펫을 비추는데 화면이 HUD로 가려져 대화에 집중하기 어렵다. 지금은 `[E] 상호작용` 안내(`Crosshair/InteractLabel`)만 `PlayerHudPresenter.SetInteractLabelBlocked`로 숨긴다.

조사한 구조:
- `PlayerHudView._root`는 빈 오브젝트 `PlayerHudRoot/PlayerHUD`라서 `Hide()`를 불러도 크로스헤어·스킬·HP는 숨겨지지 않는다 (모두 `Canvas_InGame` 바로 아래 형제 오브젝트).
- `InfoPanelPresenter`는 진행 알림이 오면 `Show()`, `BossHealthPresenter`는 매 프레임 보스전 여부로 `Show()`/`Hide()`를 스스로 부른다. 그래서 `BaseView.Hide()`로 끄면 다시 켜질 수 있다.
- 대화창은 `Canvas_InGame/PetGuidePanel`(PetGuideView·DialoguePresenter), 설정창은 `SettingsView/SettingsPanel`(자체 Canvas, override sorting)이다.

## 기대 결과 (Proposed outcome)
- 대화가 시작되면 대화창(`PetGuidePanel`)만 보이고 나머지 인게임 UI는 모두 안 보인다.
- 대화가 끝나면(끝까지 보거나 `StopDialogue`) 원래 보이던 UI가 그대로 다시 보인다. 원래 숨어 있던 UI(예: 보스전이 아닐 때의 보스 체력바)는 계속 숨어 있다.
- 대화 흐름은 UIManager → DialogueDirector 한 방향(intent-049)을 유지하고, 숨기기·복구는 UIManager의 대화 시작·끝 처리에서 한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 대화를 보는 플레이어, 대화를 배치하는 팀원(`UIManager.PlayDialogue`)
- 어떤 시스템/모듈이 건드려지는가: `UIManager`(대화 시작·끝), `Canvas_InGame.prefab`(숨김 방식에 따라 컴포넌트 추가)

## 제약 (Constraints)
- 예산/기한: 없음
- 지켜야 할 정책: 다른 사람 스크립트(PlayerFacade 등)는 수정하지 않는다. 확인은 개인 TestScene에서만 한다.
- 쓰지 말아야 할 것: `Canvas_InGame` 계층 구조 재배치(팀원 프리팹 병합 충돌 위험)

## 열린 질문 (Open questions)
- (없음) 사용자 확정: 숨기는 방식은 (A) CanvasGroup 일괄, 대화 중 연 설정창은 설정창만 보임(HUD는 계속 숨김), 펫 안내 중에는 숨기지 않음(대화만).
- 검토했던 다른 방식: (B) 숨길 오브젝트 목록 SetActive — InfoPanel·BossHealth Presenter가 같은 오브젝트라 꺼진 동안 갱신이 멈추고 UI 추가 때마다 목록 수정 필요 / (C) 각 View `Hide()` — PlayerHud 루트 재배치와 Presenter 수정 필요.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
  - `UIManager`에 `_inGameCanvasGroup`(CanvasGroup) 필드 추가. `StartDialogue`에서 alpha 0, `HandleDialogueEnded`에서 alpha 1 (끝까지 보기·`StopDialogue`·UIManager 비활성화 모두 이 경로).
  - `Canvas_InGame.prefab`: 루트에 CanvasGroup 추가 후 UIManager 필드에 연결, `PetGuidePanel`과 `SettingsView/SettingsPanel`에 `Ignore Parent Groups` CanvasGroup 추가.
  - `PetGuidePanel`에 중첩 Canvas(Override Sorting 끔) 추가. Play에서 확인해 보니 Canvas 루트 오브젝트에 붙은 CanvasGroup은 같은 Canvas 안의 자식이 `Ignore Parent Groups`를 켜도 무시되지 않았다. 자체 Canvas를 가진 SettingsPanel은 처음부터 잘 보였고, PetGuidePanel도 중첩 Canvas를 주니 보였다.
  - 기존 `SetInteractLabelBlocked`(대화 중 상호작용 안내 숨김)는 그대로 둠.
  - 숨긴 동안 각 Presenter(InfoPanel·BossHealth·HUD)는 계속 갱신하므로, 대화가 끝나면 그 사이 바뀐 상태 그대로 보인다.
- 확인: 컴파일 0에러. TestScene Play에서 `PlayDialogue(1)` → 대화창만 보임(화면 캡처), 대화 중 `OpenSettings` → 설정창·대화창만 보임, 닫고 끝까지 넘김 → HUD·미션 패널·크로스헤어 복구·timeScale 1, `StopDialogue`로도 복구, `ShowPetGuide(1)` → HUD 유지.
- 관련 커밋/PR: (커밋 전)
