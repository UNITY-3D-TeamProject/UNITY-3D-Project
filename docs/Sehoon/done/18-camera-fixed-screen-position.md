# 18. 플레이어 카메라 뷰 수정 (화면 고정 위치)

- **우선순위**: 미정
- **상태**: 대기
- **후속 작업**: [20. 카메라 가림 처리](20-camera-occlusion.md) — 이 작업이 20번의 선행 작업이다 (같은 `CinemachineThirdPersonFollow` 를 건드린다).

## 목표
이동·점프·회전 중에도 플레이어가 **항상 화면의 같은 자리**에 보이도록 카메라를 수정한다.

## 현재 상황 (2026-10-13 프리팹 기준)
- 카메라는 `Player.prefab` 안의 시네머신 카메라 2개다. 둘 다 `TrackingTarget` 은 `CameraPivot`.
  - `BaseCamera`: `CinemachineThirdPersonFollow` (`ShoulderOffset (0.5, 0, 0)`, `VerticalArmLength 0.4`, `CameraDistance 3`, `CameraSide 1`), FOV 60
  - `AimCamera`: `CinemachineThirdPersonFollow` (`ShoulderOffset (0.8, 0.1, 0)`, `CameraDistance 1.5`), FOV 45. 조준 중에만 켜진다.
  - 두 카메라 모두 **`Damping (0.1, 0.5, 0.3)`** 이 걸려 있고, `CinemachineRotateWithFollowTarget` 의 **`Damping 0.15`** 가 있다.
- 감쇠(Damping) 때문에 카메라가 타깃을 늦게 따라가므로 이동·점프 중 플레이어의 화면 위치가 흔들린다 (설정값 기준 추정, 플레이 모드 미확인). 특히 Y 0.5 는 점프·낙하 시 크게 보일 수 있다.
- 피벗 회전은 `PlayerBaseCamera.ApplyLook` 이 즉시 적용한다 (`Assets/_Project/Scripts/CameraControl/PlayerBaseCamera.cs:69`).
- 카메라 전환 블렌드는 `Main Camera.prefab` 의 `CinemachineBrain` 이 담당한다 (0.2초, 커밋 `470714c`).
- `CameraPivot` 이 캐릭터 어느 위치(루트/본)에 붙어 있는지, 애니메이션 루트 모션의 영향을 받는지는 아직 확인하지 않았다.

## 작업 순서 (예상)
1. 플레이 모드에서 화면 위치가 흔들리는 상황(이동 / 점프·낙하 / 대쉬·구르기 / 카메라 회전 / 조준 전환)을 재현해 원인을 특정한다.
2. 원인별 수정안(감쇠 0 / 축별 일부만 0 / 피벗 위치 변경 등)을 선택지로 보고하고 승인 후 수정한다.

## 열린 질문
- [ ] **(작업 전 확인)** 흔들리는 상황과 원인: 위 Damping 때문인지, 피벗 위치(본 추종·애니메이션) 때문인지 플레이 모드에서 확인한다.
- [ ] "같은 자리"의 범위: 모든 축(좌우·상하·거리)을 완전 고정하는가, 아니면 점프 시 상하는 약간 허용하는가?
- [ ] 조준 카메라(`AimCamera`)에도 같은 규칙을 적용하는가?
- [ ] 회전 감쇠(`RotateWithFollowTarget Damping 0.15`)도 없애는가? (없애면 마우스 회전이 딱딱해질 수 있다)
- [ ] 대쉬·구르기처럼 빠른 이동에서도 완전 고정인가?

## 완료 기준
- 확인된 모든 상황에서 플레이어가 화면의 같은 위치에 보인다 (플레이 모드 확인).
- 조준 전환·카메라 회전 조작감이 나빠지지 않는다.
