# RotateMediator.Update → CharacterRotator 이전

## 1. 작업 요약 및 방법
- `RotateMediator.Update()`가 매 프레임 "카메라 시점(`_viewForward`)이 있으면 그 방향, 없으면 입력 방향"을 골라 `CharacterRotator.SetLookDirection`을 호출하던 판단을 `CharacterRotator`로 옮겼다.
- `CharacterRotator.cs`: `_viewForward` 필드와 `SetViewForward(Vector3)`를 추가했다. `Update` 맨 앞에서 `_viewForward`가 있으면 `SetLookDirection(_viewForward)`를 호출하고, 그 뒤의 yaw 계산은 그대로 둔다.
- `RotateMediator.cs`: `Update`, `_rotateInput`, `_viewForward`를 삭제했다. `CommandRotate`는 `SetLookDirection`을 바로 호출하고, `SetViewForward`는 `_rotator.SetViewForward`로 값만 넘긴다. `OnDisable`의 입력 리셋은 필드가 없어져서 함께 삭제했다. zero 입력은 원래 no-op이었으므로 동작은 같다.
- intent-009에 D15를 추가했다.

## 2. 결정 사항
- 중재자는 전달만 하고 판단은 하지 않는다. 회전 방향의 우선순위는 회전 실행 컴포넌트(`CharacterRotator`)가 정한다. `MoveMediator`가 판단 없이 전달만 하는 구성과 같은 방식이다.

## 3. 현재 상태 및 이슈
- Unity 재컴파일에서 에러 0건을 확인했다.
- Play 모드 검증은 하지 않았다.

## 4. 다음 할 일 (Next Steps)
- Play 모드에서 확인한다.
  - 플레이어 몸통이 카메라 정면을 따라 도는지
  - AI가 이동 방향으로 돌고, 멈췄을 때 직전 방향을 유지하는지
