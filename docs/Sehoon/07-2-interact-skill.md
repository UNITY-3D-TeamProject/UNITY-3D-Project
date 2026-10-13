# 07-2. 상호작용 스킬

- **우선순위**: 높음
- **상태**: 대기
- **상위 작업**: [07. 스킬 구현](07-skill-implementation.md)

## 목표
카메라에서 Raycast 로 찾은 `IInteractable` 을 상호작용 스킬로 실행하고, "상호작용 가능" 신호를 Facade 로 알린다.

## 현재 상황
- `Interact.cs` 는 `Debug.Log` 만 찍는 빈 껍데기다 (`Assets/_Project/Scripts/Skill/Skills/Interact.cs`).
- 상호작용 인터페이스 `IInteractable` 이 있다 (`Assets/_Project/Scripts/Interaction/IInteractable.cs`).
- 참고: `PlayerBaseCamera.GetAimRay()` 가 이미 있다 (`Assets/_Project/Scripts/CameraControl/PlayerBaseCamera.cs:98`). 재사용 가능한지 작업 시 확인한다.

## 확정 사항
- **카메라에서 Raycast** 를 쏴서 맞은 `IInteractable` 을 상호작용 대상으로 판정한다.

## 작업 범위 (예상)
- 카메라 Raycast 로 `IInteractable` 을 찾아 실행. 카메라 Ray 를 스킬에 전달하는 경로는 기존 Mediator 연결 방식(예: Fire 의 조준점 전달)을 따른다.

## 열린 질문
- [x] 대상 탐색 방식 → **카메라 Raycast**. (Ray 에 처음 맞은 대상만 보므로 "여러 개일 때 선택" 문제는 없다)
- [x] 거리 기준 → **카메라 기준**.
- [x] Raycast 대상 → **상호작용 전용 레이어**를 따로 쓴다.
- [x] 판정 시점 → **항상(매 프레임) 검사**해서 "상호작용 가능" UI 를 띄운다.
- [x] 레이어 → **9번 `Interact`** (이미 `ProjectSettings/TagManager.asset` 에 있음).
- [x] 벽에 가려진 대상 → **감지하지 않는다**. (Raycast 는 `Interact` + 가림 판정용 레이어를 함께 검사하고, 처음 맞은 것이 `Interact` 일 때만 대상으로 본다)
- [x] UI 알림 → **`PlayerFacade` 를 통해** 알린다. 여기서는 **신호만** 만들고 UI 화면 구현은 UI 파트 담당이다.
- [x] 스킬이 꺼지면(`SetSkillEnabled(false)`) **검사하지 않는다**. (꺼질 때 대상 없음 신호를 보낼지는 구현 시 확인)
- [ ] **(구현 시 확정)** 상호작용 가능 거리(카메라에서 m).
- [ ] **(구현 시 확정)** Facade 이벤트 이름·시그니처와 넘길 정보. 공개 인터페이스이므로 구현 전에 확인받는다.
- [ ] **(구현 시 확정)** 가림 판정에 포함할 레이어 (예: `Default`, `Obstacle`).
- [ ] 입력 키 / 쿨타임 / 코스트 값

## 완료 기준
- 플레이 모드에서 대상을 바라보면 신호가 나가고, 스킬 사용 시 `IInteractable` 이 실행된다. 가려진 대상은 감지되지 않는다. 컴파일 에러 없음.
