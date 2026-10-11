# 07. 스킬 구현 (블루투스, 상호작용)

- **우선순위**: 높음
- **상태**: 대기
- **관련 작업**: 멀티점프는 [02. 멀티점프](02-multi-jump.md)로 분리했다.

## 목표
블루투스, 상호작용 스킬이 실제로 동작하도록 구현한다.

## 현재 상황
- `Interact.cs`, `SpawnVehicle.cs` 는 `Debug.Log` 만 찍는 빈 껍데기다 (`Assets/_Project/Scripts/Skill/Skills/`).
- 상호작용 인터페이스 `IInteractable` 이 있다 (`Assets/_Project/Scripts/Interaction/IInteractable.cs`).
- 배터리 어트리뷰트 `CurrentBattery` / `MaxBattery` 가 있고 HUD 에 표시된다. `ResourceCost` 는 발동 1회당 고정량 지불만 지원한다 (사용 중 지속 소모 없음).

## 확정 사항
- **블루투스**: 기존 `SpawnVehicle` 스킬이 블루투스다. 탈것을 소환해 탑승한다. 탑승 중 이동속도 증가 + 점프 파워 증가 + 중력 감소. 사용하는 동안 배터리를 계속 소모한다.
- **상호작용**: **카메라에서 Raycast** 를 쏴서 맞은 `IInteractable` 을 상호작용 대상으로 판정한다.
  - 참고: `PlayerBaseCamera.GetAimRay()` 가 이미 있다 (`Assets/_Project/Scripts/CameraControl/PlayerBaseCamera.cs:98`). 재사용 가능한지 작업 시 확인한다.

## 작업 범위 (예상)
- 블루투스: 탈것 소환·탑승, 스탯 버프 적용/해제, 사용 중 배터리 지속 소모.
- 상호작용: 카메라 Raycast 로 `IInteractable` 을 찾아 실행. 카메라 Ray 를 스킬에 전달하는 경로는 기존 Mediator 연결 방식(예: Fire 의 조준점 전달)을 따른다.

## 열린 질문
### 블루투스
- [ ] 이동속도·점프 파워·중력 변화량은? (배율인가, 고정 가산인가)
- [ ] 버프 적용 방식: `SOAttributeEffect` 로 어트리뷰트(`MoveSpeed`, `JumpPower` 등)를 바꾸는가? 중력은 어트리뷰트가 없으면 어떻게 처리하나?
- [ ] 배터리 소모량(초당)은? 소모 방식: 지속 소모용 코스트를 새로 만들지, `AttributeRegenerator` 같은 기존 구조를 쓸지?
- [ ] 해제 조건: 같은 키를 다시 누르면 하차? 배터리 0 이면 강제 하차? 피격 시 하차?
- [ ] 탈것 프리팹/모델은? 탑승 중 사격·구르기·멀티점프가 가능한가?

### 상호작용
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

### 공통
- [ ] 두 스킬의 입력 키 / 쿨타임 / 코스트 값

## 완료 기준
- 플레이 모드에서 각 스킬이 의도대로 동작. 컴파일 에러 없음.
