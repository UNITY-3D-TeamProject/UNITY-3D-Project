# 20. 카메라 가림 처리 (7, 10번 레이어)

- **우선순위**: 미정
- **상태**: 대기
- **선행 작업**: [18. 플레이어 카메라 뷰 수정](done/18-camera-fixed-screen-position.md) (완료) — 같은 `CinemachineThirdPersonFollow` 를 건드리고, 화면 고정 규칙이 정해져야 가림 해제 후 복귀 속도를 정할 수 있다.

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
- [ ] 7번과 10번의 처리 방식이 다른가? (예: 7번은 카메라 당기기, 10번은 오브젝트 반투명 처리 등) 아니면 둘 다 카메라 당기기인가?
- [x] 7번 레이어에 적·NPC 가 포함된 것은 의도인가? → 아니다. 적·NPC 중 `Obstacle` 레이어를 쓰는 것들은 수정될 예정이다.
- [ ] `Default` 레이어(일반 지형·벽)는 가림 대상에서 빼는가? (현재 필터 기본값은 Default)
- [ ] 10번 `CameraObstacle` 의 의도(어떤 오브젝트에 붙이는지)를 맵 파트와 확인했는가?
- [ ] 조준 카메라(`AimCamera`)에도 적용하는가?
- [ ] 가림이 풀릴 때 카메라 복귀 속도(`DampingFromCollision`, 현재 0.5)는? 18번에서 일반 추종 감쇠를 0 으로 정했으므로, 0.5 를 유지하면 가림이 풀린 뒤 약 0.5초 동안은 화면 위치가 고정되지 않는다. 그 예외를 허용할지 정한다.

## 완료 기준
- 7·10번 레이어 오브젝트가 플레이어를 가릴 때 카메라가 앞으로 당겨져 플레이어가 보인다 (플레이 모드 확인).
- 가림이 없어지면 원래 거리로 돌아오고, 조준점 계산에 문제가 없다.
