# 17. 조준 중 사격 자세 유지

- **우선순위**: 낮음
- **상태**: 완료
- **관련 작업**: [14. 사격·조준 중 이동 속도 감소](14-fire-aim-move-slow.md), [04. Fire 차지샷](04-fire-charge-shot.md)

## 목표
조준 중에는 상체가 사격 자세(FireHold, 손을 앞으로 내민 자세)를 유지한다.

## 확정 사항
- 조준 판정은 실제 조준 카메라 상태(`CameraMediator.OnAimChanged`)를 쓴다. 구르기로 조준이 잠기면 조준 해제로 본다.
- 흐름: `CameraMediator.OnAimChanged` → `CharacterMediator` → `AnimationMediator.NotifyAim` → `CharacterAnimator.SetAiming`.
- 조준 중에는 차지 중과 같이 `IsFiring` 을 켜 FireHold 와 총신 조준 보정을 유지하고, 사격 자세 유지 시간(3초)이 지나도 끄지 않는다. 조준 시작 시 특수 Idle 을 해제한다.
- 조준 해제 시(B안): 즉시 사격 자세를 내린다. 단, 마지막 사격·차지 종료 후 유지 시간(3초)이 남아 있으면 그때까지 유지한다.
- Animator Controller 는 바꾸지 않는다 (기존 `Empty → FireHold` 전환 사용).

## 현재 상황
- 사격 자세 유지는 `CharacterAnimator._isFiring` / `_isCharging` / `_lastFireTime` 으로 판단한다.
- `CameraMediator.OnAimChanged` 는 14 에서 추가되어 `CombatMediator` 에만 연결되어 있다.

## 열린 질문
(없음)

## 완료 기준
- 플레이 모드 확인: 조준 시작 시 UpperBody 가 FireHold 로 가고, 조준 중 3초가 지나도 유지되며, 해제 시 규칙대로 내려간다.
- 컴파일 에러 없음.
