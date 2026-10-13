# 20. 카메라 가림 처리 (7, 10번 레이어)

- **우선순위**: 미정
- **상태**: 완료
- **선행 작업**: [18. 플레이어 카메라 뷰 수정](18-camera-fixed-screen-position.md) (완료) — 같은 `CinemachineThirdPersonFollow` 를 건드리고, 화면 고정 규칙이 정해져야 가림 해제 후 복귀 속도를 정할 수 있다.

## 목표
플레이어와 카메라 사이에 오브젝트가 있으면 카메라 위치를 조정해 플레이어가 가려지지 않게 한다. 대상은 **7번 `Obstacle`, 10번 `CameraObstacle`** 레이어다.

## 확정 사항
- 적·NPC 중 7번 `Obstacle` 레이어를 쓰는 것들은 다른 레이어로 수정된다. 따라서 7번은 맵 오브젝트용으로 보고 가림 대상에 넣는다.
  - 작업 시작 시 적·NPC 프리팹 레이어가 실제로 바뀌었는지 확인한다. 아직이면 적이 지나갈 때도 카메라가 당겨진다.

## 현재 상황
- 레이어 (`ProjectSettings/TagManager.asset`): 7 = `Obstacle`, 10 = `CameraObstacle` (10번은 커밋 `6f93118` 에서 추가).
- `BaseCamera`·`AimCamera` 의 `CinemachineThirdPersonFollow.AvoidObstacles` 는 **꺼져 있다** (`Enabled: 0`, `CollisionFilter` = Default 레이어만, `CameraRadius 0.2`, `DampingIntoCollision 0`, `DampingFromCollision 0.5`). 이 기능을 켜고 필터를 바꾸는 것만으로 처리될 가능성이 있다 (Cinemachine 3 내장 기능, 동작은 작업 시 확인).
- 18번 완료로 두 카메라의 위치·회전 `Damping` 은 모두 0 이다 (화면 고정 규칙). `CameraPivot` 은 `localPosition (0, 0.8, 0)`, `ShoulderOffset.x` 는 Base 1.2 / Aim 1.0.
- 레이어 사용처 (2026-10-13 `Assets/_Project` 기준):
  - 7 `Obstacle`: 적(`Melee`, `Ranged`), NPC(`GC`), Feed 계열 맵 프리팹(`FeedBalloon`, `MaliciousData_*`, `FeedPost_Tag1` 등)에 쓰인다. 적·NPC 의 7번 사용은 다른 레이어로 수정될 예정이다 (확정 사항 참고).
  - 10 `CameraObstacle`: 아직 이 레이어를 쓰는 오브젝트가 없다. 맵 파트(양준범)가 배치할 예정으로 보인다 (확인 필요).
- `CameraMediator.GetAimPoint()` 는 카메라와 캐릭터 사이 물체를 무시하도록 레이를 피벗 깊이에서 시작한다. 카메라가 당겨져도 조준점 계산은 영향을 받지 않을 것으로 보이나 작업 시 확인한다.

## 작업 순서 (예상)
1. `AvoidObstacles` 를 켜고 필터를 7·10 으로 설정해 플레이 모드에서 동작을 확인한다.
2. 부족하면(예: 레이어별 다른 처리) 선택지로 보고하고 승인 후 별도 구현한다.

## 열린 질문
- [x] 7번과 10번의 처리 방식이 다른가? → 둘 다 카메라 당기기 (Cinemachine 내장 `AvoidObstacles`).
- [x] 7번 레이어에 적·NPC 가 포함된 것은 의도인가? → 아니다. 적·NPC 중 `Obstacle` 레이어를 쓰는 것들은 수정될 예정이다.
- [x] `Default` 레이어(일반 지형·벽)는 가림 대상에서 빼는가? → 뺀다 (필터는 7·10 만). 2026-10-13 기준 Lobby 충돌체 1582개(트리거 제외)가 모두 Default 라, 맵 오브젝트가 7·10 으로 옮겨지기 전까지 Lobby 에서는 동작하지 않는다.
- [x] 10번 `CameraObstacle` 의 의도를 맵 파트와 확인했는가? → 확인 완료 (카메라 가림 대상). 2026-10-13 기준 사용 오브젝트는 아직 없다.
- [x] 조준 카메라(`AimCamera`)에도 적용하는가? → 적용한다.
- [x] 가림이 풀릴 때 카메라 복귀 속도(`DampingFromCollision`)는? → 0.5 유지 (가림 해제 후 약 0.5초는 화면 고정 예외로 허용). `DampingIntoCollision` 은 0 유지.
- [x] 적·NPC 레이어 변경 전에 진행하는가? → 먼저 진행한다. 2026-10-13 기준 `Melee`·`Ranged`·`GC` 루트가 아직 7번이라, 바뀌기 전까지는 적이 지나갈 때 카메라가 당겨진다.

## 결과 (2026-10-13)
`Player.prefab` 수치만 변경, 코드 변경 없음. `BaseCamera`·`AimCamera` 공통:
- `AvoidObstacles.Enabled` 0 → 1
- `AvoidObstacles.CollisionFilter` Default(1) → Obstacle + CameraObstacle (`m_Bits` 1152)
- `CameraRadius 0.2`, `DampingIntoCollision 0`, `DampingFromCollision 0.5`, `IgnoreTag` 없음 — 그대로

검증 (플레이 모드, Lobby, 런타임 큐브 0.6m 를 카메라와 플레이어 사이에 배치):

| 상황 | 카메라 거리 | 복귀 |
|---|---|---|
| 7번 큐브 | 3.0 → 1.30, 가림 없음 | 0.46초 |
| 10번 큐브 | 3.0 → 1.30, 가림 없음 | 0.47초 |
| Default 큐브 | 반응 없음 (의도) | - |
| 조준 중 7번 큐브 | 1.5 → 0.43, 가림 없음 | 0.41초 |

- 조준점은 당겨진 상태에서도 화면 중앙이고 장애물에 맞지 않는다.

## 알려진 문제 (수용, 2026-10-13)
- 장애물이 플레이어 바로 뒤에 있어 카메라가 크게 당겨지면 캐릭터가 화면 왼쪽 아래로 밀려난다. Cinemachine 이 피벗이 아니라 어깨 지점 쪽으로 당기는데 어깨·높이 오프셋은 그대로라서다.
  - 기본 카메라: 피벗 화면 위치 (0.419, 0.385) → (0.313, 0.234), 몸이 왼쪽 가장자리에 걸침.
  - 조준 카메라: (0.047, 0.098) → (-1.09, -0.91), 화면 밖.
- 실제 맵 오브젝트가 7·10번으로 배치된 뒤 다시 확인하고, 문제가 되면 별도 작업으로 처리한다. 후보: 오프셋 축소(구도 변경), 당겨진 비율만큼 어깨 오프셋을 줄이는 컴포넌트 추가(새 스크립트, Cinemachine 에서 당겨진 거리를 읽는 방법 조사 필요).

## 완료 기준
- 7·10번 레이어 오브젝트가 플레이어를 가릴 때 카메라가 앞으로 당겨져 플레이어가 보인다 (플레이 모드 확인).
- 가림이 없어지면 원래 거리로 돌아오고, 조준점 계산에 문제가 없다.
