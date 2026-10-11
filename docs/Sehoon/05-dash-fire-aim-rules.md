# 05. 대쉬 중 사격·Aim 규칙

- **우선순위**: 높음
- **상태**: 대기
- **관련 작업**: [03. 점프 중 대쉬 1회 제약](03-air-dash-limit.md), [04. Fire 차지샷](04-fire-charge-shot.md)

## 목표
대쉬(`Roll`) 중 사격과 Aim 이 어떻게 동작하는지 규칙을 정해 적용한다.

## 확정 사항
- 대쉬 중에는 **사격할 수 없다**.
- Aim 중 대쉬를 쓰면 **Aim 이 해제된다**.
- Aim 중 대쉬를 썼고, 대쉬가 끝났을 때 Aim 입력을 **아직 떼지 않았다면 Aim 을 다시 켠다**.

## 현재 상황
- Aim 흐름: `PlayerInputComponent.OnAim`(누름/뗌 모두 호출) → `CameraMediator.CommandAim(bool)` → `PlayerBaseCamera.SetAim` (조준 카메라 on/off) (`Assets/_Project/Scripts/PlayerInput/PlayerInputComponent.cs:92`, `Assets/_Project/Scripts/Mediator/SubMediators/CameraMediator.cs:144`).
- Aim 입력이 지금 눌려 있는지 따로 기억하는 곳은 없다. 입력을 받으면 바로 카메라에 전달한다.
- 대쉬 시작/종료 신호: `MoveMediator.OnRollStarted` / `OnRollEnded`. `CharacterMediator` 에서 애니메이션·회전에 연결되어 있다 (`Assets/_Project/Scripts/Mediator/CharacterMediator.cs:272-275`).
- 대쉬 중 사격을 막는 처리는 없다. `CharacterAnimator` 는 구르는 동안 조준 회전(상체 보정)만 끈다.

## 작업 범위 (예상)
- Aim 입력 상태(눌림 여부) 보관 + 대쉬 시작 시 Aim 해제, 종료 시 입력이 눌려 있으면 Aim 복구.
- 대쉬 중 사격 차단 (스킬 조건 또는 Mediator 연결).
- 신호 연결은 `CharacterMediator` 에서 한다 (기존 Roll 이벤트 연결 방식과 같게).

## 열린 질문
- [ ] 대쉬 중 사격 차단 방식:
  - A) 재사용 가능한 스킬 조건(`ISkillCondition`, 예: "대쉬 중 아님")을 `Fire` 에 붙인다.
  - B) `CharacterMediator` 에서 대쉬 시작/종료 때 Fire 스킬을 비활성/활성(`SetSkillEnabled`) 한다.
- [ ] 차지 중 대쉬하면 차지는 **취소**되는가, **유지**되어 대쉬 후 이어지는가?
- [ ] 대쉬가 끝났을 때 사격 버튼을 계속 누르고 있으면, 그때부터 차지를 시작하는가?
- [ ] 대쉬 중에 Aim 을 새로 누르면(대쉬 전에는 Aim 아님) 대쉬가 끝난 뒤 Aim 을 켜는가?
- [ ] Aim 해제·복구 대상은 조준 카메라만인가? 이동 속도 등 Aim 에 묶인 다른 상태가 있는가?

## 완료 기준
- 플레이 모드 확인:
  - 대쉬 중 사격 입력이 무시된다.
  - Aim 유지 상태로 대쉬 → 대쉬 중 Aim 해제 → 대쉬 종료 시 Aim 복구.
  - Aim 중 대쉬 후 대쉬 도중 Aim 을 떼면 → 대쉬 종료 후 Aim 이 꺼진 상태.
- 컴파일 에러 없음.
