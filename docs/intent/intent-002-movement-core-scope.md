---
id: intent-002
title: 이동 코어에 경로/회전/탑승 정책이 섞여 있음
part: Movement System
status: open
created: 2026-09-12
resolved: null
---

## 문제 (Problem)
`intent-001`로 만든 이동 코어 3종(`SOMovementConfig`, `CharacterMotor`, `WaypointMover`) 중 `WaypointMover`가 **"이동 적용"이 아닌 것들을 함께 들고 있다.**

`intent-001`은 원칙을 이렇게 명시했다:

> 방향(Direction) 데이터 자체는 각 오브젝트가 스스로 산출한다(Player=입력, Enemy=AI, Platform/Crowd=웨이포인트) — 이 부분은 공유하지 않는다.

`CharacterMotor`는 이 원칙을 지킨다 — "어디로"는 호출자가 정하고 `Move(direction, speed)`는 적용만 한다. 그러나 `WaypointMover`는 **같은 문서의 같은 원칙을 스스로 어긴다.** "웨이포인트"라는 *경로 산출 방식*을 공용 컴포넌트 안에 박아넣었다.

그 파급으로 `CharacterMotor`까지 오염됐다:
- `CharacterMotor`가 `WaypointMover`를 직접 참조한다 (`CharacterMotor.cs:101`). Platform이 나중에 스플라인/NavMesh/애니메이션으로 움직이면 코어를 고쳐야 한다.
- Platform과 Crowd 모두 `WaypointMover`를 쓰게 되어 **타입으로 표현되던 구분("탈 수 있는 것" / "그냥 걸어다니는 것")이 사라졌고**, 그 구분을 런타임 `LayerMask`로 다시 복원해야 했다. Crowd를 실수로 Platform 레이어에 두면 **군중이 발판이 되는** 조용한 버그가 난다.
- `DeltaThisFrame`은 Platform만, `IsPaused`는 Crowd만 쓴다. `_shouldRotate` bool은 두 클라이언트의 의견 충돌 때문에 생긴 스위치다.

리뷰 중 발견한 동작 버그도 함께 있다:
1. **탑승자 슬라이딩** — `OnControllerColliderHit`은 `_controller.Move()` *도중* 발생하므로 수집한 델타가 **다음 프레임** `Move()`에서야 적용된다. 실행 순서(`[DefaultExecutionOrder]`) 미지정까지 겹쳐 최대 2프레임 지연 + 비결정적 지터. `docs/HANDOVER/BF_Leers/HANDOFF_POINTER.md`에 미해결로 남은 "T6 슬라이딩"과 증상이 일치한다.
2. **PingPong + 웨이포인트 1개** → `_currentIndex`가 `-1`이 되어 다음 프레임 `IndexOutOfRangeException`.
3. `_waypoints` 배열에 null 원소가 있으면 매 프레임 NRE. `Awake` 검증 없음 (컨벤션 §5-3 위반).
4. `CharacterMotor._config` null 가드 없음. 프로젝트에 `SOMovementConfig` 에셋이 하나도 없어 현재는 붙이는 즉시 NRE.
5. **문서가 코드와 반대** — 두 클래스의 `<summary>`가 "같은 오브젝트에 `WaypointMover`를 붙여야 한다"고 적고 있으나, 실제로는 부딪힌 콜라이더(플랫폼 쪽)에서 읽는다. `Assets/Test/Test.unity`가 이미 이 잘못된 설명대로 세팅되어 있다.

## 기대 결과 (Proposed outcome)
코어는 **"한 프레임 분량의 이동을 적용한다"까지만** 책임진다. 경로를 어떻게 산출하는지, 회전을 할지, 누가 올라타는지는 전부 각 파트 담당자가 구체화한다.

코어에 남는 것과 나가는 것을 "이동 적용인가?" 기준으로 분류하면:

| 항목 | 판정 | 위치 |
|---|---|---|
| 목표를 향해 한 프레임 전진 | 이동 적용 | 코어 |
| 실제 이동량 기록 (`DeltaThisFrame`) | 이동의 결과물 | 코어 |
| 중력 누적 / 지면 부착 | 이동 적용 | 코어 |
| 단일 `Move()` 보장 + 외부 변위 합산 | 코어가 소유한 제약 | 코어 |
| 웨이포인트 순회 / 도착 판정 / Loop·PingPong·Once | 경로 정책 | 담당자 |
| 진행 방향 회전 (`_shouldRotate`) | 회전 정책 | 담당자 |
| 정지-대기 (`IsPaused`) | 구동 정책 | 담당자 |
| 플랫폼 탑승 감지 (`OnControllerColliderHit`, LayerMask) | 두 오브젝트 간 상호작용 | 담당자 |

결과적으로 코어는 두 개가 대칭을 이루며, 둘 다 "어디로 갈지"도 "왜 밀리는지"도 모른다:
- **`CharacterMotor`** — `CharacterController`에 적용. `Move(direction, speed)` + `AddExternalDisplacement(displacement)`
- **`TransformMotor`** (`WaypointMover`를 대체) — `Transform`에 직접 적용. `MoveTo(targetPosition, speed)` + `DeltaThisFrame`

`AddExternalDisplacement`는 편의 기능이 아니라 **필수**다. `Move()`는 프레임당 한 번만 호출해야 한다는 제약을 코어가 소유하므로, 담당자가 캐릭터를 추가로 밀 방법을 코어가 제공하지 않으면 탑승 구현 자체가 불가능하다. 코어는 원인(플랫폼/컨베이어/넉백/바람)을 알지 않고 단일 `Move()` 호출에 합산되는 것만 보장한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: Player/Enemy/Platform/Crowd 파트 담당 팀원 전원.
- 어떤 시스템/모듈이 건드려지는가: `Assets/_Project/Scripts/Systems/`의 `WaypointMover.cs`(→ `TransformMotor.cs`로 개명 및 축소), `CharacterMotor.cs`(탑승 로직 제거, 외부 변위 창구 신설). `SOMovementConfig.cs`는 변경 없음.
- 씬/프리팹에서 `CharacterMotor`를 참조하는 곳이 **0건**이고 팀원들의 소비 스크립트가 아직 하나도 없어, 시그니처를 바꿔도 직렬화 데이터 손실이 없다. 지금이 가장 싼 시점이다.

## 제약 (Constraints)
- 이번 범위는 `Assets/_Project/Scripts/Systems/` 코어뿐이다. `PlayerMovement`/`EnemyMovement`/`PlatformMovement`/`CrowdMovement`는 `intent-001`과 동일하게 팀원 몫으로 남긴다.
- 코어에서 빠지는 것은 **동작하는 코드 스니펫으로 HANDOVER 문서에 넘긴다**(웨이포인트 순회 / 회전 Slerp / 플랫폼 탑승). 담당자가 복사해 쓰면 되므로 잃는 것이 없다.
- 이 변경으로 **슬라이딩 버그가 수정되는 것이 아니라 담당자 쪽으로 이관된다.** 대신 프레임 순서까지 해결한 정답 구현을 스니펫으로 넘겨 같은 함정을 막는다.
- `CharacterMotor.FaceDirection`은 **유지한다.** `Move(direction, speed)`는 호출자가 방향을 이미 넘겨주므로 회전 소스가 명확하다 — `MoveTo(위치, speed)`가 방향을 *추측*해야 했던 것이 문제였다. API 모양의 차이가 이 비대칭을 정당화한다.
- 두 모터를 **공통 추상 클래스나 인터페이스로 통합하지 않는다.** 검토했으나 기각한다: ①`Move(direction, speed)`와 `MoveTo(targetPosition, speed)`는 둘 다 `(Vector3, float)`지만 인자 의미가 반대여서(상대 방향 vs 월드 절대 좌표) 공통 시그니처가 그 구분을 지운다. ②두 모터를 구현 종류를 모른 채 소비하는 호출자가 없다 — Player/Enemy는 항상 `CharacterMotor`, Platform/Crowd는 항상 `TransformMotor`이며 런타임 교체가 없다. ③공통 베이스 타입은 위 "Platform과 Crowd가 같은 타입을 쓰게 되어 구분이 사라진" 문제를 그대로 되살린다. ④겹치는 것은 메서드 이름뿐이고 베이스로 올릴 실제 코드가 없다.
- 위 결정이 **뒤집히는 조건**: 대상이 어느 모터인지 모른 채 다뤄야 하는 실제 소비자(넉백/스턴, 애니메이션 드라이버 등)가 생겼을 때. 그때도 묶는 축은 `Move()`가 아니라 그 소비자가 실제로 필요로 하는 최소 계약이다 — `IDisplacementReceiver { void AddExternalDisplacement(Vector3); }` 또는 `IMotorReadout { Vector3 DeltaThisFrame { get; } }` (후자를 쓰려면 `CharacterMotor`에도 `DeltaThisFrame`을 추가해야 한다).
- 회전 정책을 코어에서 빼는 결정적 근거: **회전 소스가 클라이언트마다 다르다.** 코어는 `LookRotation(DeltaThisFrame)`을 쓰지만 스플라인은 `EvaluateTangent(t)`, NavMesh는 `agent.velocity`, 포탑형 플랫폼은 이동과 무관한 방향을 본다. 코어가 가질 수 없는 지식이다.

### intent-001의 결정 중 대체(supersede)되는 것
- ~~"Platform 프리팹은 `CharacterMotor` 인스펙터의 `Platform Layer Mask`에 포함된 레이어로 설정해야 한다"~~ → **레이어 설정 불필요.** 탑승 여부를 타입(담당자가 만드는 마커 컴포넌트)으로 구분한다.
- ~~"`CharacterMotor`는 `WaypointMover`를 직접 참조한다"~~ → **참조하지 않는다.** `AddExternalDisplacement()`를 통해 담당자 코드가 변위를 넣는다.
- ~~"`WaypointMover`를 붙이면 웨이포인트 순회, 도착 판정, 다음 목표 전환, 정지-대기가 처리된다"~~ → **전부 담당자 몫으로 이관.**
- ~~"탑승 감지 주체는 `CharacterMotor`"~~ → **담당자가 캐릭터에 붙이는 별도 컴포넌트.** `OnControllerColliderHit`은 `CharacterController`가 붙은 GameObject의 **모든 MonoBehaviour**에 전달되므로 코어를 고치지 않고도 받을 수 있다.

### intent-001의 결정 중 유지되는 것
- `CharacterController`는 Rigidbody 없이 Trigger 이벤트를 안정적으로 받지 못하므로 탑승 감지는 `OnControllerColliderHit`을 쓴다 (감지 주체만 코어 → 담당자로 이동).
- 회전하는 Platform이라도 탑승자의 시야 방향은 따라 돌지 않는다 — 위치 변위만 전달한다.
- `SOMovementConfig`는 캐릭터 타입별로 에셋을 만들어 할당하고, `MaxSpeed`는 이동 주체가 읽어 `Move()`의 `speed` 인자로 넘긴다.

## 열린 질문 (Open questions)
없음 — 세션 내 논의로 모두 해결됨.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
