# Movement System 코어 범위 재정렬 (WaypointMover → TransformMotor)

- 날짜: 2026-09-12
- 관련 intent: [intent-002 이동 코어에 경로/회전/탑승 정책이 섞여 있음](../../../intent/intent-002-movement-core-scope.md)
- 대상: `Assets/_Project/Scripts/Systems/`

---

## 1. 작업 요약 및 방법

`intent-001`로 만든 이동 코어를 리뷰한 결과, `WaypointMover`가 **"이동 적용"이 아닌 것들(경로 산출·회전·정지·탑승)을 함께 들고 있었고**, 그 파급으로 `CharacterMotor`까지 오염되어 있었다. 코어를 "한 프레임 분량의 이동을 적용한다"까지로 축소했다.

### 판단 기준
`intent-001`은 이미 원칙을 적어두었다 — *"방향 데이터 자체는 각 오브젝트가 스스로 산출한다. 이 부분은 공유하지 않는다."* `CharacterMotor`는 이를 지켰으나 `WaypointMover`는 같은 문서의 같은 원칙을 스스로 어기고 "웨이포인트"라는 경로 산출 방식을 코어에 박아넣었다.

### 변경 내용

| 파일 | 변경 |
|---|---|
| `WaypointMover.cs` → `TransformMotor.cs` | `git mv`로 이력 보존. `MoveTo(targetPosition, speed)` + `DeltaThisFrame`만 남기고 전부 제거. **직렬화 필드 0개** |
| `CharacterMotor.cs` | `_platformLayerMask`, `OnControllerColliderHit()`, `_platformDelta`, `WaypointMover` 참조 제거. `AddExternalDisplacement()` 신설. `Awake`에 `_config` Assert 추가. `<summary>` 수정 |
| `SOMovementConfig.cs` | 변경 없음 |

### 코어에서 빠진 것과 그 이유
- **경로 산출**(`_waypoints`, `_loopMode`, `EWaypointLoopMode`, 도착 판정, `AdvanceWaypoint()`, `Update()`) — 담당자마다 웨이포인트/Spline/NavMesh/타겟추적 중 다른 것을 고른다.
- **회전**(`_shouldRotate`, `_rotationSpeed`, `RotateTowardsMovement()`) — 결정적으로 **회전 소스가 클라이언트마다 다르다.** 코어는 `LookRotation(DeltaThisFrame)`을 썼지만 Spline은 `EvaluateTangent(t)`, NavMesh는 `agent.velocity`, 포탑형 플랫폼은 이동과 무관한 방향을 본다. 코어가 가질 수 없는 지식이다. `_shouldRotate` bool 자체가 두 클라이언트 의견 충돌의 산물이었다.
- **`IsPaused`** — 호출자가 구동하는 구조에서 "정지"는 `MoveTo()`를 안 부르는 것이다.
- **`_speed`** — 인자로 받는다 (`CharacterMotor`와 동일한 패턴).
- **탑승 감지** — 두 오브젝트 간 상호작용이지 이동 적용이 아니다.

### `DeltaThisFrame`을 남긴 이유
탑승 지원용이 아니라 **이동의 결과물**이기 때문이다. 목표 위치만 넘기는 호출자는 자기가 실제로 얼마나 움직였는지 알 수 없다(이전 위치 캐싱을 스스로 해야 함). 애니메이션 속도 산출, 이펙트, 탑승 처리가 모두 이 값을 읽는다.

### `AddExternalDisplacement()`가 필수인 이유
편의 기능이 아니다. `Move()`는 프레임당 한 번만 호출해야 한다는 제약(`CharacterController.Move()`를 여러 번 부르면 충돌 판정이 깨짐)을 **코어가 소유**하므로, 담당자가 캐릭터를 추가로 밀 방법을 코어가 제공하지 않으면 탑승 구현 자체가 불가능하다. 코어는 원인(플랫폼/컨베이어/넉백/바람)을 알지 않고 단일 `Move()` 호출에 합산되는 것만 보장한다.

---

## 2. 결정 사항 (채택된 아키텍처 규칙)

1. **코어는 두 개이고 대칭이다.** 둘 다 "어디로 갈지"도 "왜 밀리는지"도 모른다.
   - `CharacterMotor` — `CharacterController`에 적용. `Move(direction, speed)` + `AddExternalDisplacement(displacement)`
   - `TransformMotor` — `Transform`에 직접 적용. `MoveTo(targetPosition, speed)` + `DeltaThisFrame`
2. **경로·회전·정지·상호작용은 전부 파트 담당자 몫이다.** 코어를 고치지 않고 구현할 수 있어야 하며, 실제로 그렇게 설계했다.
3. **`CharacterMotor.FaceDirection`은 유지한다.** `Move(direction, speed)`는 호출자가 방향을 이미 넘겨주므로 회전 소스가 명확하다 — `MoveTo(위치, speed)`가 방향을 *추측*해야 했던 것이 문제였다. API 모양의 차이가 이 비대칭을 정당화한다.
4. **탑승 여부는 타입으로 구분한다** (아래 `RideablePlatform`). `intent-001`의 LayerMask 방식은 폐기 — 코어가 Platform/Crowd 구분을 잃어버려서 런타임에 복원해야 했던 우회책이었고, Crowd를 실수로 Platform 레이어에 두면 군중이 발판이 되는 사고가 났다.
5. **`OnControllerColliderHit`은 담당자가 캐릭터에 붙인 컴포넌트가 받는다.** 이 메시지는 `CharacterController`가 붙은 GameObject의 **모든 MonoBehaviour**에 전달되므로 코어를 고칠 필요가 없다.

---

## 3. 담당자용 스니펫 (코어에서 빠진 것 전부)

복사해서 각자 파트 폴더에 넣고 네임스페이스만 바꾸면 된다. 리뷰에서 찾은 버그는 이미 반영되어 있다.

### 3-1. Platform 이동 (웨이포인트 순회 + 회전)

```csharp
using UnityEngine;

namespace Project.Systems   // 담당자 폴더에 맞게 변경
{
    public enum EWaypointLoopMode
    {
        Once,
        Loop,
        PingPong,
    }

    /// <summary>웨이포인트를 순서대로 따라가며 TransformMotor를 구동한다.</summary>
    [DefaultExecutionOrder(-100)]   // 탑승자보다 먼저 움직여야 최신 델타가 전달된다
    [RequireComponent(typeof(TransformMotor))]
    public class PlatformMovement : MonoBehaviour
    {
        #region Constants
        private const float ARRIVAL_THRESHOLD = 0.05f;
        #endregion

        #region Serialized Fields
        [Header("Waypoints")]
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private float _speed = 3.0f;
        [SerializeField] private EWaypointLoopMode _loopMode = EWaypointLoopMode.Loop;

        [Header("Rotation")]
        [Tooltip("이동 방향을 바라보도록 회전할지 여부. 일부 Platform은 회전이 필요 없다.")]
        [SerializeField] private bool _shouldRotate;
        [SerializeField] private float _rotationSpeed = 5.0f;
        #endregion

        #region Private Fields
        private TransformMotor _motor;
        private int _currentIndex;
        private int _direction = 1;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _motor = GetComponent<TransformMotor>();

            Debug.Assert((_waypoints != null) && (_waypoints.Length > 0),
                $"[{name}] Waypoints가 비어 있습니다.");

            for (int i = 0; i < _waypoints.Length; i++)
            {
                Debug.Assert(_waypoints[i] != null, $"[{name}] Waypoints[{i}]가 연결되지 않았습니다.");
            }
        }

        private void Update()
        {
            bool hasNoWaypoints = (_waypoints == null) || (_waypoints.Length == 0);
            if (hasNoWaypoints)
            {
                return;   // 정지는 MoveTo()를 호출하지 않는 것으로 표현한다
            }

            Vector3 targetPosition = _waypoints[_currentIndex].position;
            _motor.MoveTo(targetPosition, _speed);

            if (_shouldRotate)
            {
                RotateTowardsMovement();
            }

            bool hasArrived = Vector3.Distance(transform.position, targetPosition) <= ARRIVAL_THRESHOLD;
            if (hasArrived)
            {
                AdvanceWaypoint();
            }
        }
        #endregion

        #region Private Methods
        private void RotateTowardsMovement()
        {
            // 회전 소스는 이동 방식에 맞게 고른다:
            //   웨이포인트 → _motor.DeltaThisFrame
            //   Spline     → spline.EvaluateTangent(t)
            //   NavMesh    → agent.velocity
            Vector3 facing = _motor.DeltaThisFrame;
            if (facing == Vector3.zero)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(facing.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }

        private void AdvanceWaypoint()
        {
            // 지점이 1개뿐이면 진행할 곳이 없다.
            // 이 가드가 없으면 PingPong에서 _currentIndex가 -1이 되어 다음 프레임에 IndexOutOfRange로 터진다.
            if (_waypoints.Length < 2)
            {
                return;
            }

            switch (_loopMode)
            {
                case EWaypointLoopMode.Once:
                    _currentIndex = Mathf.Min(_currentIndex + 1, _waypoints.Length - 1);
                    break;

                case EWaypointLoopMode.Loop:
                    _currentIndex = (_currentIndex + 1) % _waypoints.Length;
                    break;

                case EWaypointLoopMode.PingPong:
                    bool isAtEnd = ((_currentIndex + _direction) >= _waypoints.Length) ||
                                   ((_currentIndex + _direction) < 0);
                    if (isAtEnd)
                    {
                        _direction *= -1;
                    }
                    _currentIndex += _direction;
                    break;
            }
        }
        #endregion
    }
}
```

### 3-2. 탑승 — 플랫폼 쪽 마커

**Crowd에는 절대 붙이지 말 것.** 이 컴포넌트의 유무가 "탈 수 있는 것"과 "그냥 걸어다니는 것"을 구분한다.

```csharp
using UnityEngine;

namespace Project.Systems   // 담당자 폴더에 맞게 변경
{
    /// <summary>캐릭터가 올라탈 수 있는 이동 오브젝트임을 표시하는 마커.</summary>
    [RequireComponent(typeof(TransformMotor))]
    public class RideablePlatform : MonoBehaviour
    {
        #region Private Fields
        private TransformMotor _motor;
        #endregion

        #region Properties
        /// <summary>이번 프레임 플랫폼의 위치 변화량. 탑승자가 읽어간다.</summary>
        public Vector3 DeltaThisFrame => _motor.DeltaThisFrame;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _motor = GetComponent<TransformMotor>();
        }
        #endregion
    }
}
```

### 3-3. 탑승 — 캐릭터 쪽 (⚠️ 슬라이딩 버그의 핵심)

```csharp
using UnityEngine;

namespace Project.Systems   // 담당자 폴더에 맞게 변경
{
    /// <summary>이동 Platform에 올라탔을 때 플랫폼의 변위를 CharacterMotor에 전달한다.</summary>
    [DefaultExecutionOrder(-50)]   // Platform(-100) 다음, 캐릭터 구동 스크립트(0) 이전
    [RequireComponent(typeof(CharacterMotor))]
    public class PlatformRider : MonoBehaviour
    {
        #region Constants
        private const float FLOOR_NORMAL_THRESHOLD = 0.5f;
        private const int CONTACT_GRACE_FRAMES = 2;
        #endregion

        #region Private Fields
        private CharacterMotor _motor;
        private RideablePlatform _currentPlatform;
        private int _contactFrame = -1;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
        }

        private void Update()
        {
            bool isOnPlatform = (_currentPlatform != null) &&
                                ((Time.frameCount - _contactFrame) <= CONTACT_GRACE_FRAMES);

            if (!isOnPlatform)
            {
                _currentPlatform = null;
                return;
            }

            // 누적본이 아니라 "이번 프레임" 값을 읽는 것이 핵심이다.
            _motor.AddExternalDisplacement(_currentPlatform.DeltaThisFrame);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            bool isFloorHit = hit.normal.y > FLOOR_NORMAL_THRESHOLD;
            if (!isFloorHit)
            {
                return;
            }

            // 여기서는 참조와 프레임 번호만 기록한다. 델타를 쌓으면 한 프레임 늦게 적용된다.
            if (hit.collider.TryGetComponent(out RideablePlatform platform))
            {
                _currentPlatform = platform;
                _contactFrame = Time.frameCount;
            }
        }
        #endregion
    }
}
```

#### 왜 이렇게 짜야 하는가 (기존 "T6 슬라이딩"의 원인)

기존 코어는 `OnControllerColliderHit`에서 **델타를 직접 누적**했다. 그런데 이 메시지는 `_controller.Move()` **도중**에 발생하므로, 수집한 델타는 **다음 프레임** `Move()`에서야 적용됐다. 여기에 실행 순서 미지정까지 겹쳐 최대 2프레임 지연 + 비결정적 지터가 생겼다. 이것이 `HANDOFF_POINTER.md`에 미해결로 남아 있던 "T6 슬라이딩"의 정체로 보인다.

위 구현의 프레임 흐름:

| 순서 | 컴포넌트 | 하는 일 |
|---|---|---|
| `-100` | `PlatformMovement` | 플랫폼 이동 → `DeltaThisFrame` 갱신 (이번 프레임 값) |
| `-50` | `PlatformRider.Update` | **갱신된 값을 읽어** `AddExternalDisplacement()` |
| `0` | 캐릭터 구동 스크립트 | `CharacterMotor.Move()` → 변위 합산 적용. 이 도중 `OnControllerColliderHit` 발생 → 다음 프레임용 접촉 기록 |

정상 상태에서 `Time.frameCount - _contactFrame`은 항상 `1`이다. `CONTACT_GRACE_FRAMES = 2`는 접촉 보고가 한 프레임 누락될 때의 여유분이며, 대신 플랫폼에서 내려온 뒤 최대 2프레임 잔여 운반이 생긴다(체감 불가 수준). `1`로 줄이면 여유가 사라져 한 프레임만 놓쳐도 지터가 보인다.

#### 씬 세팅 필수 사항
- **Platform 프리팹에 Kinematic Rigidbody를 붙일 것.** Rigidbody 없는 Collider의 `transform.position`을 직접 수정하면 PhysX가 static collider 이동으로 취급해 매 프레임 AABB 트리를 재구축한다 (비용 + `CharacterController` 충돌 신뢰도 저하).
- **레이어 설정은 더 이상 필요 없다.** `RideablePlatform` 컴포넌트의 유무가 구분을 담당한다.
- 회전하는 Platform이라도 탑승자의 시야 방향은 따라 돌지 않는다 (위치 변위만 전달 — `intent-001`의 결정 유지).

---

## 4. 현재 상태 및 이슈

### 검증 상태
- **미검증.** 이 세션에서는 Unity Editor를 실행하지 못했다. 컴파일 및 PlayMode 검증은 아래 "다음 할 일"의 체크리스트를 따라 진행해야 한다.
- 정적 확인은 완료: `Assets/_Project/` 전체에 `WaypointMover` 잔존 참조 **0건**, 코어 두 파일에 경로/회전/플랫폼/레이어 관련 **코드** 심볼 0건(주석의 예시 문구만 남음).

### 알려진 이슈
1. **슬라이딩 버그는 "수정"이 아니라 "이관"됐다.** 코어에서 탑승 로직을 들어냈으므로, 위 3-3 스니펫을 그대로 쓰지 않으면 같은 증상이 재발한다. 프레임 순서가 핵심이다.
2. **`Assets/Test/Test.unity`의 `WaypointMover` 컴포넌트가 Missing Script가 된다.** 해당 폴더는 Git 미추적 스크래치이므로 무시하고 새로 세팅하면 된다.
3. **`SOMovementConfig` 에셋이 프로젝트에 하나도 없다.** `CharacterMotor`를 쓰려면 `Create > Movement > Movement Config`로 먼저 만들어 할당해야 한다. 이번에 `Awake` Assert를 넣어 미할당 시 NRE 대신 경고가 뜬다.
4. **이 포인터의 2026-09-09 / 2026-09-10 항목 링크 3개가 전부 깨져 있다.** `chore : 초기화` 커밋으로 대상 파일들이 삭제됐다.
5. **이 포인터의 2026-09-10 항목이 intent-001을 `open`으로 적고 있으나 실제로는 `resolved`다** (`docs/intent/clear/`로 이동 완료).

---

## 5. 다음 할 일 (Next Steps)

### 즉시 — Unity Editor 검증 (이 세션의 미완 부분)
3장의 스니펫들을 `Assets/Test/`(Git 미추적)에 넣고 구동하면, 코어에 담당자 몫 코드를 만들지 않으면서 검증할 수 있고 **넘길 스니펫이 실제로 도는지 동시에 확인**된다.

1. 컴파일 — Console 에러 0건
2. 기본 이동 — 플랫폼이 왕복하고 목표 지점을 **지나치지 않을 것**(오버슛 0)
3. **슬라이딩 회귀 테스트 (최우선)** — 플랫폼 위에 캐릭터를 올리고 **입력 없이** 관찰. 표면에 대해 미끄러지지 않고 고정되어야 한다. 수정 전에는 프레임당 약 5cm씩 밀렸다
4. 합성 이동 — 플랫폼 진행 방향/역방향으로 걸어보기
5. 레이어 무관 — 플랫폼 레이어를 `Default`로 되돌려도 탑승 정상 동작
6. Crowd 오염 방지 — `RideablePlatform`이 **없는** `TransformMotor` 오브젝트 위에서는 끌려가지 않을 것
7. PingPong + 웨이포인트 1개 — `IndexOutOfRangeException` 없이 제자리 유지
8. 회전 이관 — 드라이버의 Slerp를 꺼도 이동은 정상
9. Assert — `_config` 미할당 상태로 Play 시 NRE 대신 Assert 메시지

### 검증 완료 후
- `intent-002`의 "해결 기록"을 채우고 `status: resolved`로 바꾼 뒤 `docs/intent/clear/`로 이동, `INTENT_POINTER.md` 갱신
- 팀원들에게 이 문서의 3장 스니펫 위치를 공유. 특히 **Platform 담당자에게 3-3의 프레임 순서 설명을 반드시 읽게 할 것** — 안 읽으면 슬라이딩이 재발한다

### 팀원 파트 (이번 범위 밖)
- `PlayerMovement` / `EnemyMovement` — `CharacterMotor.Move()` 소비
- `PlatformMovement` — 3-1 스니펫 기반
- `CrowdMovement` — 경로 방식 미정. `com.unity.splines`는 **미설치**, `com.unity.ai.navigation 2.0.12`는 **설치됨**. 군중이 서로/플레이어를 피해야 하면 NavMesh, 배경 장식이면 Spline이 훨씬 싸다(경로 1개를 N명이 `t`만 다르게 공유). 어느 쪽이든 코어 변경 없이 `TransformMotor.MoveTo()`로 구동 가능하다
