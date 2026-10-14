# 07-2. 상호작용 스킬

- **우선순위**: 높음
- **상태**: 완료
- **상위 작업**: [07. 스킬 구현](../07-skill-implementation.md)

## 목표
카메라에서 Raycast 로 찾은 `IInteractable` 을 상호작용 스킬로 실행하고, "상호작용 가능" 신호를 Facade 로 알린다.

## 현재 상황
- `Interact.cs` 는 `Debug.Log` 만 찍는 빈 껍데기다 (`Assets/_Project/Scripts/Skill/Skills/Interact.cs`).
- 상호작용 인터페이스 `IInteractable` 이 있다 (`Assets/_Project/Scripts/Interaction/IInteractable.cs`).
- 참고: `PlayerBaseCamera.GetAimRay()` 가 이미 있다 (`Assets/_Project/Scripts/CameraControl/PlayerBaseCamera.cs:98`). 재사용 가능한지 작업 시 확인한다.

## 확정 사항
- **카메라에서 Raycast** 를 쏴서 맞은 `IInteractable` 을 상호작용 대상으로 판정한다.
- 대상이 있을 때 E키(`Interact` 액션, 이미 바인딩됨)를 누르면 Raycast 로 맞은 대상의 `IInteractable.Interact(interactor)` 만 호출한다. `interactor` 는 **플레이어 루트** (`transform.root.gameObject`).
- 대상이 없으면 스킬이 **발동하지 않는다** (`OnSkillUsed` 도 발행되지 않음).
  - 영향: 맵 파트의 `PlayerInteractRelay`(`OnSkillUsed("Interact")` 로 3m 안 오브젝트를 실행하는 임시 컴포넌트)는 9번 레이어 대상을 바라보고 있을 때만 동작하게 된다. 맵 파트에 9번 레이어 이전 + relay 제거를 요청해야 한다.
- 쿨타임·코스트 없음. `Interact.prefab` 의 기존 `CooldownCost` 를 제거했다 (필요 시 Inspector 에서 추가).

## 작업 범위 (예상)
- 카메라 Raycast 로 `IInteractable` 을 찾아 실행. 카메라 Ray 를 스킬에 전달하는 경로는 기존 Mediator 연결 방식(예: Fire 의 조준점 전달)을 따른다.

## 열린 질문
- [x] 대상 탐색 방식 → **카메라 Raycast**. (Ray 에 처음 맞은 대상만 보므로 "여러 개일 때 선택" 문제는 없다)
- [x] 거리 기준 → **카메라 기준**.
- [x] Raycast 대상 → **상호작용 전용 레이어**를 따로 쓴다.
- [x] 판정 시점 → **항상(매 프레임) 검사**해서 "상호작용 가능" UI 를 띄운다.
- [x] 레이어 → **9번 `Interact`** (이미 `ProjectSettings/TagManager.asset` 에 있음).
- [x] 벽에 가려진 대상 → **감지하지 않는다**. (구현: `Interact` 레이어로 1차 Raycast 후, 시작점~충돌점 구간을 가림 레이어로 다시 검사해 맞으면 가려진 것으로 본다)
- [x] UI 알림 → **`PlayerFacade` 를 통해** 알린다. 여기서는 **신호만** 만들고 UI 화면 구현은 UI 파트 담당이다.
- [x] 스킬이 꺼지면(`SetSkillEnabled(false)`) **검사하지 않는다**. 꺼질 때 대상 없음(`false`) 신호를 한 번 보낸다.
- [x] 상호작용 가능 거리 → **카메라에서 6m** (기본 카메라 거리 3m 기준 플레이어 앞 약 3m). Inspector 노출.
- [x] Facade 이벤트 → **`event Action<bool> OnInteractableChanged`**. 상호작용 가능 여부가 바뀔 때만 발행한다.
- [x] 가림 판정 레이어 → **`Default`, `Obstacle(7)`, `CameraObstacle(10)`**. 대상 레이어(`Interact(9)`)와 함께 Inspector 노출.
- [x] 입력 키 / 쿨타임 / 코스트 → **E키 / 없음 / 없음**

## 결과 (2026-10-13)
### 흐름
1. `Interact.Update` 가 매 프레임 대상을 판정한다.
   - 카메라 Ray(`CameraMediator.GetAimRay` → `PlayerBaseCamera.GetAimRay`)를 주입받아 쓴다.
   - 1차: `_interactLayers`(9번) 로 `_interactDistance`(6m) 까지 Raycast. 트리거 콜라이더도 맞힌다.
   - 2차: 시작점~1차 충돌점 구간을 `_blockingLayers`(Default, Obstacle, CameraObstacle) 로 Raycast. 트리거는 무시한다 (`Default` 레이어 트리거인 Checkpoint·FallZone 이 가리지 않도록). 맞으면 가려진 것으로 본다.
   - 통과하면 맞은 콜라이더에서 `GetComponentInParent<IInteractable>()` 로 찾은 GameObject 가 대상이다.
2. 대상 유무가 바뀌면 `HasTargetCondition.SetHasTarget` 갱신 + `SkillMediator.OnInteractableChanged` → `CharacterMediator.OnInteractableChanged` → `PlayerFacade.OnInteractableChanged(bool)` 발행.
3. E키 → `TryExecuteSkill("Interact")`. `HasTargetCondition` 이 대상 없으면 발동을 막는다. 대상이 있으면 대상의 **첫 번째** `IInteractable.Interact(transform.root.gameObject)` 를 호출하고 `OnSkillUsed` 가 발행된다.

### 변경 파일
- 추가: `Scripts/Skill/Conditions/HasTargetCondition.cs` (대상 유무를 스킬이 설정하는 `ISkillCondition`. `UseCountCondition` 과 같은 방식)
- 수정: `Scripts/Skill/Skills/Interact.cs`, `Scripts/Mediator/SubMediators/CameraMediator.cs` (`GetAimRay`), `Scripts/Mediator/SubMediators/SkillMediator.cs` (`SetGetAimRay`/`ClearGetAimRay`, `OnInteractableChanged`), `Scripts/Mediator/CharacterMediator.cs`, `Scripts/Facade/Player/PlayerFacade.cs` (`OnInteractableChanged`)
- 프리팹: `Prefabs/Skills/Interact.prefab` 에 `HasTargetCondition` 추가

### 남은 것
- 테스트용 `Debug.Log` 3개가 남아 있다 (`Interact.cs` 2개, `PlayerFacade.cs` 1개, `// TODO: 테스트용 로그` 주석). 검증이 끝나면 제거한다.
- 맵 파트 전달 사항: `PlayerInteractRelay` 는 이제 9번 레이어 대상을 바라볼 때만 동작한다. 대상 오브젝트를 9번 레이어로 옮기고 relay 를 제거해야 한다. 또 `IInteractable` 이 여러 개 붙은 오브젝트는 첫 번째 것만 실행된다 (relay 는 전부 실행).

## 완료 기준
- 플레이 모드에서 대상을 바라보면 신호가 나가고, 스킬 사용 시 `IInteractable` 이 실행된다. 가려진 대상은 감지되지 않는다. 컴파일 에러 없음.
