---
id: intent-001
title: 이동 시스템 공용 코어(CharacterMotor/WaypointMover) 부재
part: Movement System
status: resolved
created: 2026-09-11
resolved: 2026-09-11
---

## 문제 (Problem)
Player/Enemy/이동 Platform/Crowd(군중) 등 이동이 필요한 오브젝트마다 "방향+속도를 실제 이동으로 바꾸는" 로직을 각자 새로 짜야 하는 상태였다. 이전에 한 번 시도(Intent/Motor 인터페이스 기반 구조)가 있었으나 `chore : 초기화` 커밋으로 코드와 문서 본문이 모두 삭제되어, 이번 작업 시작 시점에는 빈 템플릿(`Movement.cs`)만 남아있었다.

## 기대 결과 (Proposed outcome)
실제로 동작 코드가 겹치는 그룹끼리만 공용 컴포넌트를 공유하도록 재설계했다 (상속/인터페이스로 억지로 묶지 않음):
- **CharacterController로 움직이는 오브젝트**(Player, Enemy — 4족 동물 포함)는 `CharacterMotor`를 같은 오브젝트에 붙이고 `GetComponent<CharacterMotor>()`로 참조해 `Move(direction, speed)`만 매 프레임 호출하면, 중력 누적·지면 부착·이동 방향 회전(FaceDirection)·이동 Platform 탑승까지 전부 처리된다.
- **좌표(웨이포인트) 기반으로 움직이는 오브젝트**(이동 Platform, Crowd)는 `WaypointMover`를 붙이면 웨이포인트 순회, 도착 판정, 다음 목표 전환, 정지-대기(`IsPaused`)가 처리된다.

방향(Direction) 데이터 자체는 각 오브젝트가 스스로 산출한다(Player=입력, Enemy=AI, Platform/Crowd=웨이포인트) — 이 부분은 공유하지 않는다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 팀원 각자 Player/Enemy/Platform/Crowd 파트를 구현할 때 이 코어를 가져다 쓴다.
- 어떤 시스템/모듈이 건드려지는가: `Assets/_Project/Scripts/Systems/`(`SOMovementConfig.cs`, `CharacterMotor.cs`, `WaypointMover.cs` 신규 작성), `Assets/_Project/Scripts/Movement.cs`(빈 템플릿, 삭제).

## 제약 (Constraints)
- 이번 작업 범위는 공용 코어 3개(`SOMovementConfig`, `CharacterMotor`, `WaypointMover`)까지다. `PlayerMovement`/`EnemyMovement`/`PlatformMovement`/`CrowdMovement`처럼 실제로 이 코어를 소비하는 스크립트는 팀원이 각자 파트에서 구현한다 — 이번 커밋에는 포함되지 않는다.
- `CharacterController`는 Rigidbody 없이는 Trigger 이벤트를 안정적으로 받지 못한다(웹 검색으로 확인, [출처](https://discussions.unity.com/t/ontriggerenter-event-for-charactercontroller-without-rigidbody-is-it-possible/772777)). 그래서 이동 Platform 탑승 감지는 `CharacterController.OnControllerColliderHit`을 사용하며, 감지 주체는 Platform이 아니라 캐릭터 쪽(`CharacterMotor`)이다.
- Platform 프리팹은 `CharacterMotor` 인스펙터의 `Platform Layer Mask`에 포함된 레이어로 설정해야 탑승 처리가 동작한다. 설정하지 않으면 그냥 부딪히는 벽처럼 취급된다.
- `CharacterMotor`는 `PlatformMovement`(팀원이 나중에 만들 스크립트)가 아니라 코어에 이미 있는 `WaypointMover`를 직접 참조한다 — `PlatformMovement`가 아직 없어도 코어만으로 컴파일/동작이 완결되게 하기 위함이다. `WaypointMover`는 Crowd도 쓰므로, Platform과 Crowd를 구분하기 위해 레이어 체크가 반드시 필요하다.
- `SOMovementConfig` 에셋은 캐릭터 타입(Player, Enemy 개체별)마다 별도로 만들어 `CharacterMotor`에 할당해야 한다. `MaxSpeed`는 `CharacterMotor`가 직접 쓰지 않고, 이동 주체 스크립트가 읽어서 `Move()`의 `speed` 인자로 넘겨야 한다.
- 회전하는 Platform이라도 탑승자(Player/Enemy)의 시야 방향은 플랫폼을 따라 돌지 않는다 — 위치(델타)만 전달하고 회전은 반영하지 않는다.

## 열린 질문 (Open questions)
없음 — 세션 내 논의로 모두 해결됨.

## 해결 기록 (Resolution)
- 최종 결정: 위 "기대 결과"의 구조로 `Assets/_Project/Scripts/Systems/SOMovementConfig.cs`, `CharacterMotor.cs`, `WaypointMover.cs` 3개 파일을 신규 작성. 빈 템플릿이던 `Movement.cs`(및 `.meta`)는 삭제. 팀원이 코어를 갖다 쓸 때 필요한 규칙(Platform 레이어 설정 등)은 위 제약 항목과 각 클래스의 `<summary>` 문서 주석에 명시.
- 관련 커밋/PR: 본 intent 문서와 같은 작업 세션에서 커밋됨(커밋 로그 참고).
