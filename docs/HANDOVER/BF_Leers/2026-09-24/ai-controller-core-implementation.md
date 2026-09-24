# AI 조종부 코어 구현 (BT 제외) — intent-008 1차

- **날짜**: 2026-09-24
- **브랜치**: `feature/AI/Controller`
- **관련 문서**: [intent-008 적 AI 시스템](../../../intent/intent-008-enemy-ai-system.md) · [AI 구조 설계 확정 (Q1~Q28)](./ai-architecture-design-grilling.md) · [캐릭터 공통 구조](../../../character-architecture.md)

---

## 1. 작업 요약

intent-008의 1차 범위(이동만, 공격 제외) 중 **BT를 제외한 조종부 코어**를 구현했다.

`Assets/_Project/Scripts/AI/`에 `Sensor`와 `AIController` 두 컴포넌트를 신설했고, **중재자 계층(`CharacterMediator` / `MoveMediator` / `CharacterMotor`)은 한 줄도 수정하지 않았다.** 조종 주체를 모르도록 설계해 둔 것이 의도대로 동작함을 코드로 확인한 셈이다.

BT 액션·조건 노드는 세션 시작 시점에 `com.unity.behavior`가 미설치라 작성하지 않았다. (세션 도중 사용자가 설치 — 5장 참고)

## 2. 방법 / 접근

### 2-1. 교체점은 `ICharacterController` 하나뿐

`CharacterMediator.Awake`가 같은 오브젝트의 `ICharacterController`를 찾고, `OnEnable`에서 `_controller.SetMoveRequest(_moveMediator.CommandMove)`로 **메서드 참조만 넘긴다.** 따라서 `PlayerInputComponent` 대신 `AIController`를 붙이면 그것으로 끝이다. 중재자 쪽 신규 코드가 필요 없다.

### 2-2. NavMeshAgent는 경로 계산기로만 쓴다 (Q3, Q28)

실제 이동은 기존 `CharacterMotor`가 해야 하므로(이동 코드 신규 작성 금지), Agent가 Transform을 직접 옮기지 못하게 막았다.

```csharp
_agent.updatePosition = false;
_agent.updateRotation = false;
_agent.updateUpAxis  = false;
```

매 `Update`에서 두 가지를 한다.

1. `_agent.nextPosition = transform.position` — `CharacterMotor`가 옮긴 실제 위치를 Agent에 되돌린다. **이걸 빼면 Agent의 내부 위치와 실제 위치가 즉시 어긋나 경로가 무의미해진다.**
2. `_agent.desiredVelocity`를 수평 투영·정규화해 이동 요청으로 넘긴다.

### 2-3. 월드 방향을 그대로 넘긴다

`MoveMediator`는 기준 프레임이 없으면 `CommandMove(x, y)`를 월드 `(x, 0, y)`로 해석한다 (`MoveMediator.cs:32`). 적에는 `CameraMediator`를 붙이지 않으므로 기준 프레임이 없고, 따라서 월드 방향 벡터를 `new Vector2(dir.x, dir.z)`로 바꿔 넘기기만 하면 된다. 좌표 변환 코드가 따로 필요 없다.

### 2-4. Sensor의 출력은 bool이 아니다

`CurrentTarget`(대상 Transform)과 `LastKnownPosition`(마지막 목격 위치)을 낸다. 시야를 잃어도 마지막 목격 위치는 남으며, 이것이 Q21 수색 단계의 목적지가 된다. 수색이 끝나면 `ClearLastKnownPosition()`으로 지운다.

감지 파이프라인은 `OverlapSphere`(거리, Q26) → 각도 필터 → `Raycast` 차폐 검사(Q22) → 통과한 것 중 최근접 채택 순이다. 대상 지점은 `collider.bounds.center`를 쓴다 — 발밑을 기준으로 하면 바닥에 시야가 막힌다.

성능은 두 가지로 처리했다. `_detectInterval = 0.2f` 주기(Q25)에 `Awake`에서 `Random.Range(0, _detectInterval)`로 적마다 시작 시점을 어긋내 프레임 몰림을 막고, `OverlapSphereNonAlloc` + 고정 버퍼로 GC 할당을 0으로 만들었다.

### 2-5. BT가 부를 API는 `AIController`에만 (Q27)

BT 노드는 `Sensor`나 `NavMeshAgent`를 직접 만지지 않는다.

| 구분 | API |
|---|---|
| 질의 | `HasVisibleTarget`, `VisibleTarget`, `HasLastKnownPosition`, `LastKnownPosition`, `HasArrived` |
| 명령 | `MoveTo(Vector3)`, `MoveToRandomPatrolPoint()`, `StopMove()`, `ClearLastKnownPosition()` |

명령의 반환 `bool`은 "NavMesh 위 유효 목적지를 잡았는가"라서, BT Action 노드가 Success/Failure로 그대로 쓸 수 있다.

## 3. 변경된 파일

**신규 (전부 내가 작성)**

```
Assets/_Project/Scripts/AI.meta
Assets/_Project/Scripts/AI/Sensor.cs        (+ .meta)
Assets/_Project/Scripts/AI/AIController.cs  (+ .meta)
```

**내가 만들지 않은 변경** — 세션 도중 사용자가 에디터에서 BT 패키지를 설치한 부산물이다. 되돌리지 않고 그대로 두었다.

```
M  Packages/manifest.json                      + "com.unity.behavior": "1.0.16"
M  Packages/packages-lock.json
M  ProjectSettings/EditorBuildSettings.asset   + com.unity.dt.app-ui
?? ProjectSettings/SceneTemplateSettings.json
```

**전부 미커밋 상태다.**

## 4. 결정 사항

이번 세션에서 새로 정한 것만 적는다. Q1~Q28은 [설계 문서](./ai-architecture-design-grilling.md)에 있다.

| # | 결정 | 근거 |
|---|---|---|
| 1 | **BT 없는 코어를 먼저 만든다** | 세션 시작 시 `com.unity.behavior` 미설치. 지금 작성 가능한 것을 컴파일 가능한 상태로 먼저 확보하고, `AIController`의 공개 API를 미리 확정해 두면 BT 노드는 얇은 래퍼만 남는다 |
| 2 | **몸통 회전은 이번에 넣지 않는다** | 회전 주체를 정하는 것 자체가 별도 결정 건이라 1차 구현과 분리 (5장 이슈 참고) |
| 3 | **에디터를 열지 않고 `.cs` + `.meta`만 만진다** | 에디터가 프로젝트를 열면 지시하지 않은 에셋까지 재직렬화되어 diff가 생긴다. `.prefab`/`.asset` 직접 편집 금지(CLAUDE.local.md 0장)를 Unity CLI로 우회하지 않기 위함 |
| 4 | `ARRIVAL_THRESHOLD = 0.25f`로 도착 판정에 여유를 둔다 | Agent가 아니라 `CharacterMotor`가 움직이므로 목적지에 정확히 멈추지 않는다. 여유가 없으면 목적지 주변에서 계속 진동한다 |
| 5 | `AIController.OnDisable`에서 `CommandMove(Vector2.zero)`를 보낸다 | 이동 명령은 래치(latch)라, 조종부가 꺼져도 마지막 값이 남아 캐릭터가 계속 미끄러진다 |

## 5. 현재 상태 및 이슈

### ⚠️ Unity 컴파일 미확인 — 다음 세션 최우선

에디터를 열지 않는 범위로 합의했으므로 **정적 검토까지만** 했다. `MoveMediator.CommandMove` / `ICharacterController` / `CharacterMotor`의 실제 시그니처 대조와 컨벤션 확인은 마쳤으나, 컴파일러를 통과시킨 적이 없다.

### ⚠️ 회전 주체가 없어 시야가 스폰 방향에 고정된다

`Sensor`는 `transform.forward`로 시야 각도를 판정한다. 그런데 **이 프로젝트에는 캐릭터 몸통을 회전시키는 코드가 하나도 없다.** 플레이어는 카메라 기준 이동이라 이 공백이 가려져 있었다.

결과: 적은 순찰로 이동해도 스폰 당시 바라보던 방향의 부채꼴만 감지한다.

1차 Play 테스트에서는 `_sightAngle`을 크게(예: 360) 잡아 **감지 → 추격 → 수색 루프 자체를 먼저 검증**하고, 회전을 도입한 뒤 정상 값으로 되돌린다. intent-008의 "T포즈 미끄러짐이 정상"과 같은 성격의 알려진 미완성이다.

### ⚠️ 타입 이름 충돌 위험 — `AI.Sensor` ↔ `UnityEngine.InputSystem.Sensor`

지금은 두 네임스페이스를 함께 `using`하는 파일이 없어 문제되지 않는다(InputSystem을 쓰는 것은 `PlayerInputComponent` 하나뿐). 그러나 한 파일에서 둘을 같이 쓰면 모호성 에러가 난다. intent-008이 이름을 `Sensor`로 지정해 뒀기에 임의로 바꾸지 않았다. **`SightSensor`로 개명할지 확인 필요.**

### 그 밖에

- `AIActionBase` 및 BT 액션·조건 노드 미작성
- 애니메이션 컴포넌트가 없어 **T포즈로 미끄러지는 것이 정상**이다 (동작 실패로 오해하지 말 것)
- `MoveMediator.CommandJump()`는 여전히 비어 있으나 1차 AI에 점프가 없어 영향 없음
- intent-008은 `open` 유지 (1차 부분 완료)

## 6. 다음 할 일

1. **Unity 에디터로 컴파일 0에러 확인** (최우선)
2. **BT 노드 작성** — 패키지가 설치됐으므로 더 이상 막혀 있지 않다. `AIActionBase` + 액션·조건 노드를 `AIController`의 공개 API를 감싸는 얇은 래퍼로 만든다
3. **`[USER]` 선행 작업** (전부 끝나야 Play 검증 가능)
   1. `ProjectSettings/TagManager.asset`에 `Player` / `Obstacle` 레이어 추가 후 플레이어·벽에 지정
   2. 맵 NavMesh 베이크
   3. 적 프리팹 구성 — `CharacterController` + `CharacterMotor` + `MoveMediator` + `CharacterMediator`(Move만 연결, Camera는 비움) + `AttributeSet`(`SOAttributeData_Enemy`) + `AttributeToMotorAdapter`(`_speedValueKey` 설정) + `NavMeshAgent` + `Sensor` + `AIController`
   4. 튜닝 — `_patrolRadius`, `_sightRange`, `_sightAngle`(1차는 크게), `_detectInterval`
   > `AttributeToMotorAdapter`의 속도 키가 비어 있거나 값이 0이면 적은 목적지를 잡고도 제자리에 선다. 과거 `JumpPower: 0` 사례와 같은 함정이니 먼저 확인할 것.
4. **몸통 회전 주체 결정** — 중재자 계층에 회전 추가(`CharacterRotator` + `MoveMediator.CommandFace`) vs 조종부가 직접 회전. 전자가 구조 문서에 맞고 나중에 플레이어도 재사용할 수 있다
5. **`Sensor` 개명 여부 확인** (5장)
6. **문서-코드 불일치 협의** — intent-008 열린 질문 ②~⑤는 `character-architecture.md` 소유자와의 협의 항목이라 BF_Leers 단독으로 확정하지 않는다
