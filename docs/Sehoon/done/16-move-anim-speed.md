# 16. 이동속도에 맞춘 이동 애니메이션 재생 속도

- **우선순위**: 낮음
- **상태**: 완료
- **관련 작업**: [14. 사격·조준 중 이동 속도 감소](14-fire-aim-move-slow.md)

## 목표
현재 이동속도에 비례해 이동(Locomotion) 애니메이션 재생 속도를 맞춘다. 이동속도 5 일 때가 지금 재생 속도(1배)다.

## 확정 사항
- 방식(A안): `MoveSpeed` 어트리뷰트 변경을 `AnimationMediator` 가 기존 어트리뷰트 콜백으로 받아 `CharacterAnimator.SetMoveSpeed` 로 전달한다.
- `CharacterAnimator` 가 `현재 이동속도 / 기준 속도(인스펙터, 기본 5)` 를 Animator Float 파라미터 `MoveAnimSpeed` 에 넣는다.
- `PlayerAnimator.controller` 의 `Locomotion` 상태 Speed Multiplier 를 `MoveAnimSpeed` 에 연결한다. 다른 상태와 UpperBody 레이어는 영향받지 않는다.
- 배율 하한·상한은 두지 않는다.
- 속도 변경은 즉시 반영한다 (보간 없음).

## 현재 상황
- Locomotion 은 2D Blend Tree 상태 하나이며, 속도 관련 Animator 파라미터는 없었다.
- `MoveSpeed` 변경은 `CharacterMediator.OnAttributeChangeCallback` 이 모든 중재자에 알린다 (`MoveMediator` 도 같은 경로로 `Motor.Speed` 갱신).
- `MediatorBase.InitValue()` 호출이 주석 처리되어 있어 초기값은 어트리뷰트 변경 알림에 의존한다. 파라미터 기본값 1(=5 기준)이라 현재 수치에서는 문제없다.

## 열린 질문
(없음)

## 완료 기준
- 플레이 모드 확인: 이동속도 5 에서 Locomotion 재생 속도 1배, 차지(3.5) 0.7배, 조준(2) 0.4배.
- 컴파일 에러 없음.
