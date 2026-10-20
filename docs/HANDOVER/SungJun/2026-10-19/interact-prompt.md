# 상호작용 안내 UI ("[E] 상호작용") — 2026-10-19

관련 intent: [intent-045](../../../intent/clear/intent-045-interact-prompt.md)

## 1. 작업 요약 및 방법
- Interact 레이어 대상을 조준하면 크로스헤어 아래에 `[E] 상호작용` 안내가 뜨도록 함.
- 조사해 보니 감지·전달은 이미 끝까지 있었음: `Interact` 스킬(카메라 레이) → `SkillMediator` → `CharacterMediator` → `PlayerFacade.OnInteractableChanged(bool)`. 구독하는 UI만 없었음.
- 코드
  - `PlayerHudView`: `_interactPromptRoot`(GameObject), `SetInteractPromptVisible(bool)`
  - `PlayerHudPresenter`: `OnInteractableChanged` 구독/해제, `_hasInteractTarget`·`_isInteractPromptBlocked`, `SetInteractPromptBlocked(bool)`(public), `UpdateInteractPrompt`·`ResetInteractPrompt`. OnEnable·OnDisable·SetPlayer에서 대상 유무 초기화(막힘 상태는 유지)
  - `UIManager`: `PlayDialogue`에서 `SetInteractPromptBlocked(true)`, `HandleDialogueEnded`에서 `false`
- 프리팹 `Canvas_InGame`: `Crosshair/InteractPrompt`(y -120, 180×42, 평소 꺼짐) 추가
  - `Key`: `Skills/SkillCard_Scan/Key` 복제(Bg·Frame·Text), 글자 "E"
  - `Label`: Pretendard-SemiBold SDF 22, "상호작용", 키 글자와 같은 색
  - `PlayerHudView._interactPromptRoot`에 연결. 에디터 eval(LoadPrefabContents → SaveAsPrefabAsset)로 작업

## 2. 결정 사항
- PlayerFacade·Mediator·Interact 스킬은 수정하지 않음 (필요한 이벤트가 이미 있음)
- 문구는 고정 (대상별 문구 없음)
- 대화 중에는 숨김. Presenter는 UIManager를 참조하지 않고, UIManager가 Presenter를 호출하는 기존 방향 유지
- 이미 쓴 대상(열린 ItemBox, 한 번만 누르는 InteractButton)에도 안내가 뜨는 건 그대로 둠

## 3. 현재 상태 및 이슈
- 컴파일 에러 0. TestScene Play에서 확인: 대화 중 조준 → 숨김, 대화 끝난 뒤 계속 조준 → 표시, 대상 치움 → 숨김, 콘솔 에러 0. 화면 캡처로 위치·모양 확인
  - TestScene에 상호작용 오브젝트가 없어서 Play 중에 레이어 9 큐브 + `InteractButton`을 조준 위치에 임시로 만들어 확인함 (씬에는 저장 안 됨)
- 실제 E 입력으로 상호작용하는 동작은 이번에 바꾸지 않았고 따로 확인하지 않음
- 커밋 안 함
- `Interact.cs`·`PlayerFacade.cs`에 남아 있는 `// TODO: 테스트용 로그`는 건드리지 않음

## 4. 다음 할 일 (Next Steps)
- 실제 맵(ItemBox·ChargeStation 등)에서 E 안내 위치·크기를 보고 조정
- 필요하면 다음 단계: 이미 쓴 대상에서는 안내 숨기기(IInteractable에 사용 가능 여부 추가), 대상별 문구("E 열기" 등)
