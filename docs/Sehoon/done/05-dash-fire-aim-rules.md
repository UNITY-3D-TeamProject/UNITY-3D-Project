# 05. 대쉬 중 사격·Aim 규칙

- **우선순위**: 높음
- **상태**: 완료
- **관련 작업**: [03. 점프 중 대쉬 1회 제약](03-air-dash-limit.md), [04. Fire 차지샷](04-fire-charge-shot.md)

## 목표
대쉬(`Roll`) 중 사격과 Aim 이 어떻게 동작하는지 규칙을 정해 적용한다.

## 확정 사항
- 대쉬 중에는 **사격할 수 없다**.
- Aim 중 대쉬를 쓰면 **Aim 이 해제된다**.
- Aim 중 대쉬를 썼고, 대쉬가 끝났을 때 Aim 입력을 **아직 떼지 않았다면 Aim 을 다시 켠다**.
- 대쉬 중 사격 차단은 `CharacterMediator` 에서 대쉬 시작/종료 때 Fire 스킬을 비활성/활성(`SetSkillEnabled`) 하는 방식으로 한다.
- 차지 중 대쉬하면 차지는 **취소**된다 (Fire 비활성화 시 `Cancel`).
- 대쉬가 끝났을 때 사격 버튼을 계속 누르고 있어도 차지를 **시작하지 않는다**. 다시 눌러야 한다.
- 대쉬 중에 Aim 을 새로 눌렀어도, 대쉬 종료 시 Aim 입력이 눌려 있으면 **Aim 을 켠다** ("종료 시 눌려 있으면 켠다" 한 규칙으로 처리).
- Aim 해제·복구 대상은 **조준 카메라뿐**이다 (코드 확인: Aim 입력은 `PlayerBaseCamera.SetAim` 에만 연결됨).

## 현재 상황
- Aim 흐름: `PlayerInputComponent.OnAim`(누름/뗌 모두 호출) → `CameraMediator.CommandAim(bool)` → `PlayerBaseCamera.SetAim` (조준 카메라 on/off) (`Assets/_Project/Scripts/PlayerInput/PlayerInputComponent.cs:92`, `Assets/_Project/Scripts/Mediator/SubMediators/CameraMediator.cs:144`).
- Aim 입력이 지금 눌려 있는지 따로 기억하는 곳은 없다. 입력을 받으면 바로 카메라에 전달한다.
- 대쉬 시작/종료 신호: `MoveMediator.OnRollStarted` / `OnRollEnded`. `CharacterMediator` 에서 애니메이션·회전에 연결되어 있다 (`Assets/_Project/Scripts/Mediator/CharacterMediator.cs:274-277`).
- 대쉬 중 사격을 막는 처리는 없다. `CharacterAnimator` 는 구르는 동안 조준 회전(상체 보정)만 끈다.
- 차지샷([04](04-fire-charge-shot.md)): 차지는 `Fire` 스킬이 갖는다. 누름(실행 요청)에 차지 시작, 1초가 차면 차지샷 자동 발사, 그 전에 뗌(Stop 요청)이면 비차지샷. `Fire` 를 비활성화(`SetSkillEnabled`)하면 쏘지 않고 차지를 취소한다. 코스트는 발사 시점에 지불한다.

## 작업 범위 (예상)
- Aim 입력 상태(눌림 여부) 보관 + 대쉬 시작 시 Aim 해제, 종료 시 입력이 눌려 있으면 Aim 복구.
- 대쉬 중 사격 차단 (스킬 조건 또는 Mediator 연결).
- 신호 연결은 `CharacterMediator` 에서 한다 (기존 Roll 이벤트 연결 방식과 같게).

## 열린 질문
- [x] 대쉬 중 사격 차단 방식 → B) `SetSkillEnabled` 비활성/활성
- [x] 차지 중 대쉬 → 취소
- [x] 대쉬 종료 시 사격 버튼 유지 → 차지 시작하지 않음
- [x] 대쉬 중 새로 누른 Aim → 대쉬 종료 시 켬
- [x] Aim 해제·복구 대상 → 조준 카메라뿐

## 완료 기준
- 플레이 모드 확인:
  - 대쉬 중 사격 입력이 무시된다.
  - Aim 유지 상태로 대쉬 → 대쉬 중 Aim 해제 → 대쉬 종료 시 Aim 복구.
  - Aim 중 대쉬 후 대쉬 도중 Aim 을 떼면 → 대쉬 종료 후 Aim 이 꺼진 상태.
- 컴파일 에러 없음.
