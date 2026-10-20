---
id: intent-045
title: 상호작용 대상을 조준하면 "E 상호작용" 안내 표시
part: UI / Interaction
status: resolved
created: 2026-10-19
resolved: 2026-10-19
---

## 문제 (Problem)
Interact 레이어(9번) 오브젝트(ItemBox, ChargeStation, InteractButton 등)를 조준해도 화면에 아무 안내가 없어서, 플레이어는 E로 상호작용할 수 있는지 알 수 없다.
감지(`Interact` 스킬 레이캐스트)와 전달(`SkillMediator` → `CharacterMediator` → `PlayerFacade.OnInteractableChanged`)은 이미 구현돼 있지만, 이 이벤트를 구독하는 UI가 없다.

## 기대 결과 (Proposed outcome)
- 상호작용 가능한 대상을 조준하는 동안 크로스헤어 근처에 "E 상호작용" 안내가 뜨고, 조준을 떼거나 거리를 벗어나거나 벽에 가려지면 사라진다.
- 대화(컷씬) 중에는 안내를 숨기고, 대화가 끝났을 때 여전히 대상을 보고 있으면 다시 띄운다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 플레이어 (인게임 HUD)
- 어떤 시스템/모듈이 건드려지는가:
  - `UI/PlayerHud/PlayerHudView.cs` — 안내 오브젝트 켜고 끄기
  - `UI/PlayerHud/PlayerHudPresenter.cs` — `PlayerFacade.OnInteractableChanged` 구독 (사격 차지 바와 같은 패턴)
  - `UI/UIManager.cs` — 대화 시작·종료 시 안내 막기/풀기
  - `Canvas_InGame` 프리팹 — `InteractPrompt` 텍스트 추가

## 제약 (Constraints)
- 지켜야 할 정책: PlayerFacade·Mediator·Interact 스킬은 수정하지 않는다 (이미 필요한 이벤트가 있음).
- 쓰지 말아야 할 것: Presenter가 UIManager를 직접 참조하지 않는다 (UIManager → Presenter 호출 방향 유지).

## 열린 질문 (Open questions)
- 없음. 아래는 사용자 결정 (2026-10-19):
  - 문구는 고정 "E 상호작용" (대상별 문구 없음)
  - 이미 쓴 대상(열린 ItemBox, 한 번만 누르는 버튼)에도 안내가 뜨는 것은 이번엔 그대로 둠
  - 대화 중에는 숨김

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
  - `PlayerHudPresenter`가 `PlayerFacade.OnInteractableChanged`를 구독. 상태 2개(`_hasInteractTarget`, `_isInteractPromptBlocked`)로 "대상 있음 && 안 막힘"일 때만 표시
  - `UIManager.PlayDialogue`에서 막고 `HandleDialogueEnded`에서 풂 → 대화가 끝났을 때 계속 조준 중이면 바로 다시 뜸
  - `Canvas_InGame/Crosshair/InteractPrompt` (크로스헤어 아래 y -120, 평소 꺼짐): 스킬 카드 `Key` 배지(Bg·Frame·Text "E") 복제 + `Label`(Pretendard-SemiBold 22 "상호작용"), `PlayerHudView._interactPromptRoot`에 연결
  - PlayerFacade·Mediator·Interact 스킬 수정 없음
- 검증 (TestScene Play): 컴파일 에러 0, 대화 중 대상 조준 → 숨김, 대화 종료 후 조준 유지 → 표시, 대상 치움 → 숨김, 콘솔 에러 0
- 관련 커밋/PR: (커밋 전)
