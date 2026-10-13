# 18. 플레이어 카메라 뷰 수정 (화면 고정 위치)

- **우선순위**: 미정
- **상태**: 완료
- **후속 작업**: [20. 카메라 가림 처리](../20-camera-occlusion.md) — 이 작업이 20번의 선행 작업이다 (같은 `CinemachineThirdPersonFollow` 를 건드린다).

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
- [x] **(작업 전 확인)** 흔들리는 상황과 원인 → **Damping 이 유일한 원인** (2026-10-13 Lobby 플레이 모드 측정, 피벗의 뷰포트 좌표 기록).
  - `CameraPivot` 은 루트 직속 자식이고 루트 모션은 꺼져 있다 (`m_ApplyRootMotion: 0`). 애니메이션 영향 없음.
  - 현재값: 점프·착지 화면 Y 0.25~0.51 (기준 0.385), 구르기 X 0.35~0.42 (기준 0.419), 뒤로 이동 거리 2.63 (기준 3.00), 카메라 회전 X 최대 0.47.
  - 런타임에서 Damping 을 모두 0 으로 바꾸면 위 상황 + 조준 중 점프까지 전부 완전 고정 (조준 전환 블렌드 구간 제외).
- [x] "같은 자리"의 범위 → 모든 축 완전 고정 (`Damping (0, 0, 0)`).
- [x] 조준 카메라(`AimCamera`)에도 같은 규칙을 적용하는가? → 적용한다.
- [x] 회전 감쇠(`RotateWithFollowTarget Damping 0.15`)도 없애는가? → 0 으로 한다.
- [x] 대쉬·구르기처럼 빠른 이동에서도 완전 고정인가? → 완전 고정 (1번 답에 따라 감쇠 전체가 0).
- [x] (작업 중 발견) 피벗 `localPosition.x 0.2` 오프셋 — 루트는 회전하지 않고 `RotatorPrefab` 만 돌기 때문에 카메라 yaw 에 따라 화면상 몸통 위치가 최대 약 ±3% 화면 폭 움직인다 (계산값). → 처음엔 패스했으나 실제 화면에서 차이가 보여 수정했다 (아래 결과 참고).

## 결과 (2026-10-13)
`Player.prefab` 수치만 변경, 코드 변경 없음.

| 대상 | 항목 | 변경 |
|---|---|---|
| `BaseCamera` | ThirdPersonFollow `Damping` | `(0.1, 0.5, 0.3)` → `(0, 0, 0)` |
| `BaseCamera` | RotateWithFollowTarget `Damping` | `0.15` → `0` |
| `AimCamera` | ThirdPersonFollow `Damping` | `(0.1, 0.5, 0.3)` → `(0, 0, 0)` |
| `AimCamera` | RotateWithFollowTarget `Damping` | `0.15` → `0` |
| `CameraPivot` | `localPosition.x` | `0.2` → `0` (몸통 중심으로) |
| `BaseCamera` | `ShoulderOffset.x` | `1.0` → `1.2` (피벗 이동분 보정, 1.0 은 작업 중 사용자가 변경한 값) |
| `AimCamera` | `ShoulderOffset.x` | `0.8` → `1.0` (피벗 이동분 보정) |

- 검증 (플레이 모드, 이벤트 직접 호출로 입력): 이동·점프·구르기·카메라 회전·조준 중 점프에서 피벗과 몸통의 화면 좌표가 고정됨. 기본 카메라 몸통 화면 위치 (0.305, 0.385), 조준 카메라 (0.047, 0.098). 조준점은 화면 중앙, 레이 시작점은 플레이어 콜라이더 밖.
- 화면 위치 공식 (측정값과 일치): X ≈ `0.5 − ShoulderOffset.x / (2·CameraDistance·tan(FOV/2)·화면비)`, Y ≈ `0.5 − (VerticalArmLength + ShoulderOffset.y) / (2·CameraDistance·tan(FOV/2))`. 캐릭터를 중앙 쪽으로 옮기려면 이 값들을 줄인다.
- 작업 중 `PlayerInput.m_Actions` 가 비어 저장되는 일이 있었다 (원인 미상). `FID_InputSystem` 으로 복구됨을 확인.

## 완료 기준
- 확인된 모든 상황에서 플레이어가 화면의 같은 위치에 보인다 (플레이 모드 확인).
- 조준 전환·카메라 회전 조작감이 나빠지지 않는다.
