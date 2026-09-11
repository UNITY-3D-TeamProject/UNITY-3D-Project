# Movement System 코어 최종 구조

## 작업 요약
Player/Enemy/이동 Platform/Crowd 등 이동이 필요한 오브젝트가 공용으로 쓸 이동 코어 3종(`SOMovementConfig`, `CharacterMotor`, `WaypointMover`)을 신규 작성했다. 09-09~09-10에 진행하던 `IMoveMotor`/`CharacterControllerMotor` 기반 구조(인터페이스로 억지로 묶는 방식)는 PlayMode 검증 중 문제가 발생해 원인 미확정 상태로 보류되었고, 이후 세션에서 **폐기**하고 현재 구조로 재설계했다.

## 방법/접근
상속/인터페이스로 억지로 묶지 않고, 실제로 동작 코드가 겹치는 그룹끼리만 공용 컴포넌트를 공유하도록 설계했다:
- **CharacterController로 움직이는 오브젝트**(Player, Enemy — 4족 동물 포함): `CharacterMotor`를 붙이고 `GetComponent<CharacterMotor>()`로 참조해 `Move(direction, speed)`만 매 프레임 호출. 중력 누적·지면 부착·이동 방향 회전(FaceDirection)·이동 Platform 탑승까지 전부 처리.
- **좌표(웨이포인트) 기반으로 움직이는 오브젝트**(이동 Platform, Crowd): `WaypointMover`를 붙이면 웨이포인트 순회, 도착 판정, 다음 목표 전환, 정지-대기(`IsPaused`) 처리.
- 방향(Direction) 데이터는 각 오브젝트가 스스로 산출(Player=입력, Enemy=AI, Platform/Crowd=웨이포인트) — 이 부분은 공유하지 않음.

## 변경된 파일
- 신규: `Assets/_Project/Scripts/Systems/SOMovementConfig.cs`, `CharacterMotor.cs`, `WaypointMover.cs`
- 삭제: `Assets/_Project/Scripts/Movement.cs`(빈 템플릿, `.meta` 포함)

## 결정 사항
- Platform 탑승 감지는 `CharacterController.OnControllerColliderHit`을 사용하며, 감지 주체는 Platform이 아니라 캐릭터 쪽(`CharacterMotor`).
- Platform 프리팹은 `CharacterMotor` 인스펙터의 `Platform Layer Mask`에 포함된 레이어로 설정해야 탑승 처리가 동작함.
- `CharacterMotor`는 (팀원이 나중에 만들) `PlatformMovement`가 아니라 코어의 `WaypointMover`를 직접 참조 — 코어만으로 컴파일/동작이 완결되도록. `WaypointMover`는 Crowd도 쓰므로 레이어 체크로 Platform/Crowd 구분 필요.
- `SOMovementConfig`는 캐릭터 타입마다 별도 에셋으로 만들어 `CharacterMotor`에 할당. `MaxSpeed`는 `CharacterMotor`가 직접 쓰지 않고 이동 주체 스크립트가 읽어 `Move()`의 `speed` 인자로 전달.
- 회전하는 Platform이라도 탑승자의 시야 방향은 플랫폼 회전을 따라가지 않음(위치 델타만 전달).
- 상세 배경/제약은 `docs/intent/clear/intent-001-movement-system-core.md` 참고.

## 현재 상태 및 이슈
- 공용 코어 3개는 작업 완료 및 resolved 처리됨.
- `PlayerMovement`/`EnemyMovement`/`PlatformMovement`/`CrowdMovement` 등 코어를 실제로 소비하는 스크립트는 아직 미구현 — 각 팀원이 자기 파트에서 구현 예정.
- **09-09/09-10 HANDOVER 기록(`IMoveMotor`, `CharacterControllerMotor` 기반 구조)은 폐기된 이전 설계이므로 참고하지 말 것.**

## 다음 할 일
- 팀원별로 `PlayerMovement`/`EnemyMovement`/`PlatformMovement`/`CrowdMovement`를 코어(`CharacterMotor`/`WaypointMover`) 위에 구현.
- Platform 프리팹에 `Platform Layer Mask` 레이어 설정이 필요함을 팀원에게 공지.
