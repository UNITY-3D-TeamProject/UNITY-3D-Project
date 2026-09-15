---
id: intent-003
title: 전투 적/보스 AI 부재 — BT 설계안을 기존 이동 코어와 맞물리게 구현
part: Enemy AI
status: open
created: 2026-09-13
resolved: null
---

## 문제 (Problem)
프로토타입(9/28) 필수 항목인 "기본 적 (체력, 공격, 이동, 사망)"이 아직 하나도 없다. 적 AI 설계안(`적AI설계정리.md`, 로컬 문서)은 정리돼 있지만, 이 설계는 **이미 develop에 들어간 이동 코어(`CharacterMotor`)를 고려하지 않고 쓰였다.** 그래서 설계를 그대로 옮기면 아래 지점에서 부딪힌다.

- 설계는 적이 `NavMeshAgent`로 움직이고 겹침 방지를 Agent의 Obstacle Avoidance에 맡긴다고 가정한다. 그런데 `intent-001`/`intent-002`는 **Enemy도 `CharacterMotor.Move(direction, speed)`로 움직인다**고 정했다. 경로만 받아 `CharacterController`로 움직이면 **물리 충돌로 서로 파고드는 것은 막히지만**("A collision constrains the Move from taking place." — `CharacterController.Move` 문서), 서로의 속도를 보고 미리 비켜 가는 **회피 조향은 쓰지 못한다.** 같은 타깃을 쫓는 적들이 한 줄로 부딪히거나 좁은 곳에서 막힌다.
- 설계는 Unity 6 Behavior 패키지를 쓰는데, `Packages/manifest.json`에 아직 없다(`com.unity.ai.navigation 2.0.12`만 설치, 에디터 `6000.4.8f1`).
- HP·데미지·쿨다운은 전투 시스템 팀원 소유인데, 프로토타입에는 "플레이어와 상호 데미지"가 들어간다. 인터페이스 합의가 아직 없다.

## 기대 결과 (Proposed outcome)

### 결정 사항: 대상별 구조
BT는 **판단 분기가 실제로 있는 대상에만** 쓴다.

| 대상 | 구조 | 이 intent 범위 |
|---|---|---|
| 전투맵 근거리/원거리 적 | BT | 포함 |
| 보스 | BT — 패턴은 Sequence, 페이즈 전환은 조건 데코레이터 | 포함 (본 개발) |
| 로비 보행자 NPC / 차량 | FSM (정지·이동) | 제외 — 상태가 두 개뿐이라 BT는 과함. 이동 적용은 [`intent-002`](./clear/intent-002-movement-core-scope.md)의 Crowd=`TransformMotor` 정리 참고 |
| 미로 경찰 / 가비지 컬렉터 | 미정 | 보류 — 기획 미확정, 요구사항이 나오면 별도 intent |

설계안의 "보안 앱 적 = BT"는 전투맵 적과 같은 대상으로 본다 (전투는 보안 앱 스테이지와 보스전에 국한되므로).

### 판단부와 실행부 분리
노드 안에서 `NavMeshAgent`를 직접 만지거나 투사체를 직접 생성하지 않는다.

**실행 모델 (2026-09-16):** Action은 컴포넌트에 명령(목적지 설정, 정지, 조준 대상 설정, 공격 시작)만 건다. 실제 실행은 컴포넌트의 `Update`가 매 프레임 한다. 노드는 결과를 보고 Running/Success/Failure를 리턴한다. 이렇게 하면 노드가 바뀌어도(예: 추격 → 공격) 중력, 외부 변위, `nextPosition` 동기화가 끊기지 않는다. `CharacterMotor.Move()`는 매 프레임 불러야 하기 때문이다(`CharacterMotor.cs:60`).

**분류 기준:** 아래 중 하나라도 해당하면 컴포넌트에 둔다. 셋 다 아니면 노드 안에 직접 쓴다. 상태가 없고 한 줄로 끝나는 로직은 노드 안에 둬도 된다.
- A. 노드 수명보다 오래 사는 상태나 매 프레임 처리가 있다
- B. BT 밖에서도 불린다 (피격, 애니메이션 이벤트, 기즈모, 재진입)
- C. 여러 노드가 공유하는 계산이나 캐시다

| 컴포넌트 | 역할 | 기준 |
|---|---|---|
| `EnemyMotor` | `MoveTo(pos)`, `Stop()`, `SetLookTarget(t)`, `ClearLookTarget()`. 매 프레임 `CharacterMotor.Move()` 호출, NavMeshAgent 설정·delta 이동·동기화, 회전(평소 `steeringTarget` 기준 / 조준 대상 있으면 대상 기준), `HasArrived`·`IsPathValid` 조회, 끼임 감지. **회전은 이 컴포넌트만 한다** (노드가 `transform.rotation`을 직접 건드리면 서로 덮어씀) | A, C |
| `EnemySensor` | 플레이어 참조 캐싱, `IsPlayerVisible()`(거리 + 시야각 + 레이캐스트), `DistanceToPlayer()`, 감지 범위 기즈모 | B, C |
| `EnemyCombat` | **미정 — 전투 담당과 회의 후 확정 (열린 질문 5).** 후보 책임: 공격 쿨다운, 공격 판정 적용(`SOAttributeEffect.Apply`), 모션·사운드 재생, 선딜 취소 | A, B |
| `EnemyHealth` | HP, 피격, 사망, 사망 시 BT 정지 (사망은 BT 밖). 구현 방식은 열린 질문 5를 따른다 | B |
| 적 루트 | 재진입 시 `ResetState()` (아래 예외 처리 참고) | B |

| 노드 안에 직접 쓰는 것 | 종류 |
|---|---|
| `Init Home`, `Acquire Target`, `Clear Target`, `Record Last Seen` — 블랙보드 쓰기만 | Action |
| 추격(`OnStart`에서 `MoveTo`, `OnUpdate`에서 목적지 갱신·도착 확인), 귀환(`MoveTo(HomePosition)`, 도착 시 Success), 대기(`Stop()`) | Action |
| 경로 실패 처리 — `Motor.IsPathValid`가 false면 Failure | Action |
| 스트레이프 목적지 계산, `NavMesh.SamplePosition` 검사 (원거리, 본 개발) | Action |
| 선딜 대기 타이머 — 노드 수명과 같으므로 공격 Action 안, 중간에 끊기면 `OnEnd`에서 취소. **`EnemyCombat` 확정 후 최종 결정** | Action |
| `In Attack Range`, 추격 포기(`Time.time - LastSeenTime`), 리쉬(`HomePosition`까지 거리), 수직 대응(높이 차) | Condition |

블랙보드는 범용 `Dictionary<string, object>` 대신 명시적 필드로 둔다. 저장 위치는 혼합이다 (근거는 열린 질문 3번 참고):
- **Unity Behavior 패키지 블랙보드** (프로토타입): `Self`(기본), `Target`(Transform), `HomePosition`(리쉬·재진입), `LastSeenTime`(추격 포기) — AI만 쓰는 판단 상태. **판단이 바뀌는 순간에만** 전용 Action이 기록한다: `Acquire Target`/`Clear Target`(Target 채우기/비우기), `Record Last Seen`(놓친 순간 1회), `Init Home`(루프 밖 1회). 추격·조준 노드는 `Target`을 읽기만 한다. `LastSeenPosition`은 수색 기능이 생길 때 추가한다.
- **컴포넌트 호출**: 남은 쿨다운, HP 비율, 공격 범위, 플레이어 감지는 블랙보드로 복사하지 않는다. 커스텀 Condition(`Is Player Visible`, `In Attack Range` 등)이 매 틱 `EnemySensor`/`EnemyCombat`/`EnemyHealth`에 직접 묻는다 (값 소유권 규칙과 일치).
- **Condition은 부수효과가 없다** — 블랙보드에 쓰지 않는다. `Priority Abort`/`Restart`에 걸리면 매 틱 평가되기 때문이다.
- 플레이어 참조: `EnemySensor`가 `Awake`에서 태그로 플레이어 Transform을 한 번 찾는다 (프리팹은 씬 오브젝트를 인스펙터로 참조할 수 없음).

### 값 소유권
- **AI 스탯 SO (AI 소유)**: 감지 거리, 시야각, 선호 거리(stoppingDistance), 이동 속도, 회전 속도, 추격 포기 시간, 리쉬 거리, 재배치 거리
- **전투 시스템 (팀원 소유)**: HP, 공격력, 방어력, 데미지, 공격 쿨다운, 선딜, 공격 범위
- BT는 전투 값을 직접 읽지 않고 `EnemyCombat`에 묻는다. 경계가 애매한 값(공격 범위, 선딜)은 전투 쪽에 둔다.
- 적 타입 추가 = 양쪽 에셋을 각각 복제 후 숫자만 수정.

### 전투 적 트리
```
Start
└─ Sequence
   ├─ Init Home                                  ← 루프 밖, 1회
   └─ Repeat
      └─ Try In Order
         ├─ [Priority Abort: Is Player Visible]
         │  Sequence: Acquire Target
         │            └─ Try In Order
         │               ├─ Sequence: [In Attack Range] [쿨다운 끝] → 공격
         │               └─ 추격 / 재배치        ← Target 읽음
         ├─ Sequence: [Target 있음] → Record Last Seen → [놓친 지 N초] → Clear Target → 귀환
         └─ 대기
```
- `Try In Order`는 반응형이 아니다(자식이 Running이면 기다리기만 함, `SelectorComposite.cs:34`). 상위 가지로 되돌아가려면 `Priority Abort`/`Restart` 모디파이어를 붙인다. Abort/Restart는 `Repeat` 루프 안에만 둔다(`Init Home` 재실행 방지).
- 위 다이어그램은 블랙보드 흐름을 보이기 위한 뼈대다. 추적 실패 가지의 정확한 노드 배치(대기 시간 처리 등)는 그래프 작성 시 확정한다.
- **근거리**: stoppingDistance 2m, 쿨다운 중에도 추격
- **원거리**: stoppingDistance 9m, 쿨다운 중 플레이어를 바라본 채 좌우 스트레이프 (너무 가까우면 후퇴 / 너무 멀면 접근 / 목적지가 NavMesh 밖·낙사 지점이면 반대 방향)
- **수직 이동 대응**: 플레이어가 일정 높이 이상(이단 점프·활공)이면 근거리는 아래에서 대기, 원거리만 사격
- **낙사 지형**: NavMesh 가장자리 회피

### 반드시 챙길 것
1. **선딜(telegraph)**: 공격 직전 0.5초 모션 + 사운드
2. **디버그 표시**: 활성 노드 이름, 감지/공격 범위 기즈모, 목적지 선
3. **예외 처리**: 끼임 감지(N초간 이동량 0이면 리셋), 경로 실패 폴백, 리쉬, 사망 중 로직 정지, 스테이지 재진입 시 상태 초기화 (프로토타입: 파괴 후 재생성 → 블랙보드도 새로 생성. 최적화 단계에서 오브젝트 풀링으로 전환)

### (보류) 미로 술래 AI 참고용 설계안
기획이 확정되면 별도 intent에서 검토한다. 설계안의 초안만 남겨둔다.
```
Selector
├─ Sequence: [플레이어 보임] → 추격
├─ Sequence: [마지막 목격 위치 있음] → 이동 → 주변 수색
└─ 웨이포인트 순찰
```
시야각 제한, 시야 시각화, 추격 포기 조건, 랜덤 배회 금지.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 전투 적/보스 AI 담당, 전투 시스템 담당 팀원, 레벨 디자인(전투 방 구성).
- 어떤 시스템/모듈이 건드려지는가:
  - 신규: `EnemyMotor` / `EnemySensor` / `EnemyCombat` / `EnemyHealth`, AI 스탯 SO, BT 그래프 에셋
  - 연동: `Assets/_Project/Scripts/Movement/CharacterMotor.cs` (Enemy 이동 적용, 회전은 하지 않음), `NavMeshAgent`
  - 연동: 전투 시스템의 데미지 인터페이스 (팀원 작업물)
  - 패키지: `Packages/manifest.json`에 Unity Behavior 추가

## 제약 (Constraints)
- 예산/기한: **프로토타입 9/28.** 범위는 BT 뼈대 + 감지·추격·공격·사망 노드, AI 스탯 SO, **근거리 1종**(원거리는 여유 있으면), 선딜 + 디버그 기즈모, 플레이어와 상호 데미지. 원거리 재배치·보스 패턴은 본 개발로 넘긴다.
- 지켜야 할 정책:
  - 전투 수치(HP/공격력/쿨다운/선딜/공격 범위)를 AI 쪽에서 중복 정의하지 않는다.
  - BT 노드는 실행 컴포넌트를 호출만 한다.
  - 이동 코어(`CharacterMotor`/`TransformMotor`)는 수정하지 않는다 (`intent-002` 범위 결정 유지).
  - 전투 구역은 평평하고 넓은 방으로 만들어 플랫포밍 구간과 분리한다 (레벨 디자인으로 AI 난이도를 낮춤).
- 쓰지 말아야 할 것 (의도적 제외, 필요해지면 그때 얹는다):
  - 공격 토큰 / 인카운터 디렉터 → 한 방에 3~4마리 배치로 대체
  - 슬롯 기반 포위, 엄폐 시스템, 소리 감지, 유틸리티 AI 위치 선정
  - 별도 스티어링 코드
  - 자체 BT 에디터 / 노드 직렬화

## 열린 질문 (Open questions)
1. **이동 방식** — 전투 적은 무엇으로 움직이는가? **(c)로 확정 (2026-09-14 스파이크, 아래 결과 참고).**

   | | 파고듦 | 회피 조향 | CharacterMotor 유지 |
   |---|---|---|---|
   | (a) `NavMesh.CalculatePath()`로 경로만 계산 + `CharacterMotor` | 막힘 | 없음 | O |
   | (b) `NavMeshAgent`가 직접 이동 | 막힘 | O | X — `intent-001`/`002`의 "Enemy = `CharacterMotor`" 결정을 뒤집음 |
   | (c) `NavMeshAgent`(`updatePosition = false`)는 계산만, 이동은 `CharacterMotor` | 막힘 | O (검증 필요) | O |

   (c)의 흐름: `agent.desiredVelocity`(회피 반영) → `CharacterMotor.Move(방향, 속도)` → `agent.nextPosition = transform.position`으로 동기화. `updatePosition = false`는 "the simulated position will not be applied to the transform position and vice-versa" (ScriptReference).

   스파이크 검증 항목 (`Assets/Test/Scripts/NavAgentMotorSpike.cs`, 적 4마리):
   1. 넓은 곳 추격 — 한 줄로 막히지 않고 퍼져서 접근하는가. **겹쳐 놓고 시작하지 않는다**(겹침 해소를 보게 됨). 배치: (A) 2m 간격 가로 일렬 + 타깃 10m 이상 앞 한 점, (B) 타깃 중앙 + 사방 10m
   2. 회피 끔(대조군) — 1번과 눈에 띄는 차이가 있는가 (없으면 (c)의 이점이 없으므로 (a) 재검토)
   3. 좁은 통로 — 떨림/영구 정지 없이 통과하는가
   4. 타깃 정지 — stoppingDistance 근처에서 멈추고 떨지 않는가 (실패 시 도착 판정/감속을 적 스크립트에서 처리)
   5. 타깃을 벽 너머로 — 돌아서 추격하고 Agent 위치와 실제 위치가 벌어지지 않는가

   알려진 한계: `agent.radius`/`speed`를 `CharacterController.radius`/이동 속도와 코드로 맞춰야 함, NavMesh 밖으로 밀려나면(넉백·공중) `nextPosition`이 NavMesh 위로 보정되어 위치가 어긋남, Off-Mesh Link 자동 통과 불가, 벽·장애물을 놓거나 옮긴 뒤 NavMesh를 다시 굽지 않으면 적이 벽 앞에서 에러 없이 멈춤.

   스파이크 중간 결과 (2026-09-14):
   - **이동량 계산은 `desiredVelocity`가 아니라 `agent.nextPosition - transform.position`(delta)으로 한다.** 밀집 상태에서 비교한 결과, `nextPosition`에는 겹친 Agent를 밀어내는 보정이 들어 있어 겹침이 풀린다. `desiredVelocity`로 움직이면 이 보정이 빠져 적끼리 겹친다. (Enemy끼리의 3D 충돌은 끈 상태)
   - **3번 좁은 통로(폭 3m)에서 적이 몰리면 몸통이 좌우로 떨린다.** 위치는 거의 제자리인데 Rotation Y만 프레임마다 크게 튄다.
     - W1(NavMesh Bake 반경 < CC Radius + Skin Width) 기각: Bake 반경을 0.7(≥ 0.51 + 0.08)로 올려 다시 구워도 그대로였다.
     - 최소 이동 속도 기준값으로 거르는 방식도 효과 없음: 떨릴 때도 이동량이 1 m/s 이상이었다.
     - 원인: 막힌 적은 밀어내기와 회피 계산 때문에 delta 방향이 매 프레임 좌우로 뒤집힌다. 위치 이동은 서로 상쇄되지만, `CharacterMotor.Move()`가 매번 그 방향으로 몸을 돌려 회전은 상쇄되지 않는다.
     - 스파이크에서 회전만 `agent.steeringTarget`(경로의 다음 꼭짓점) 기준으로 따로 계산하자 떨림이 사라졌다. → 7번의 근거.
   - 남은 현상: 대열 맨 뒤 적의 `desiredVelocity` 기즈모는 여전히 좌우로 흔들린다. 밀집 상태에서 회피 계산 자체가 흔들리는 것이고, 몸 위치·회전에는 드러나지 않아 **허용한다.** 이 흔들림을 없애려고 `desiredVelocity` 이동으로 바꾸면 겹침이 생기므로 delta를 유지한다. 대기 모습을 더 다듬어야 하면 Avoidance Priority/Quality 조정으로 따로 다룬다.
   - **2번 회피 끔 대조군:** 넓은 곳(1-A/1-B)에서는 차이가 없었다. 밀집(1-B 변형)과 좁은 통로(3번)에서는 차이가 확실했다. 회피를 켜면 어느 정도 비켜서 타깃까지 가지만, 끄면 그러지 못한다. → 회피 조향의 이점이 확인되어 (a)는 재검토하지 않고 **(c)로 확정한다.**
   - **5번 벽 너머 타깃:** 통과. 적이 벽을 돌아서 추격한다(`remain` > `straight`, 경로가 벽 끝에서 꺾임). `nextPosition`과 실제 위치도 벌어지지 않는다. 처음 실패는 벽을 놓은 뒤 NavMesh를 다시 굽지 않은 탓이었다. 그때는 경로가 벽을 관통하는 직선(`remain` == `straight`)으로 나왔고, CharacterController가 벽에 막혀 멈췄다(`actual` 0).
   - **해결 (2026-09-14):** 스파이크 검증 항목 1~5를 모두 통과했다. 이동 방식은 (c)로 확정한다. 이동량은 delta, 회전은 `steeringTarget` 기준, 적끼리의 3D 충돌은 끈다.
2. **Unity Behavior 패키지** — `6000.4.8f1`에서 정식(비프리뷰) 버전으로 설치 가능한가? 그래프 에셋이 텍스트 직렬화라 머지 가능한가?
   - **해결 (2026-09-14):** Package Manager에 프리뷰 표시 없음 → 정식 버전 설치 가능. 이 프로젝트는 Asset Serialization이 바이너리라(씬도 동일 이유로 Test 폴더 전용 규칙이 있음) 애초에 텍스트 diff로 병합하는 방식을 쓰지 않는다. 대신 담당 파트가 Player / Enemy(AI·NPC) / System·UI / Map으로 나뉘어 있어 BT 그래프는 Enemy AI 담당자(본인) 혼자 건드리므로 동시 편집 충돌 자체가 없다.
3. **블랙보드** — Unity Behavior 자체 Blackboard 변수를 쓸지, 컴포넌트의 명시적 필드를 노드가 읽을지.
   - **해결 (2026-09-14):** 혼합으로 결정한다. AI만 쓰는 판단 상태(`Target`, `HomePosition`, `LastSeenTime`)는 패키지 블랙보드에 둔다 — 그래프 디버거에서 값이 보이고, 노드끼리 판단 상태를 공유할 수 있다. 반대로 쿨다운·HP·공격 범위처럼 전투 시스템이 소유한 값은 블랙보드로 복사하지 않는다 — 복사하면 "전투 수치를 AI 쪽에서 중복 정의하지 않는다"(제약 절)를 어기고 원본과 어긋날 위험이 생기므로, 커스텀 조건 노드가 `EnemyCombat`/`EnemyHealth`에 직접 묻는다. 감지 거리·시야각 등 AI 스탯 SO도 블랙보드로 복사하지 않는다. `EnemySensor`는 블랙보드에 직접 쓰지 않는다(`SetVariableValue` 문자열 키 지양). 기록은 전용 Action 노드가 자기 `BlackboardVariable<T>` 필드로 한다 — 타입이 있어 필드명 오타를 컴파일러가 잡는다.
   - **보완 (2026-09-14, Behavior 1.0.16 소스 대조):**
     - ~~기본 노드(Variable Comparison, Check Distance) 재사용~~ 근거 철회: `Check Distance`의 기준값이 `BlackboardVariable<float>`(`CheckDistanceCondition.cs:19`)라 공격 범위·감지 거리를 그래프에 적거나 블랙보드에 복사해야 하고, 둘 다 중복 정의 금지에 걸린다. 거리·범위 판정은 커스텀 Condition(`In Attack Range` 등)으로 한다. ~~`Variable Value Changed` 흐름~~ 근거도 뺀다 — 감지를 Condition이 직접 하므로 쓸 곳이 없다.
     - 감지: "Sense Player" Action은 쓰지 않는다. `Try In Order`가 반응형이 아니라(`SelectorComposite.cs:34`) Action은 실행 흐름이 도달할 때만 돌고, 대기·추격이 Running인 동안 값이 굳는다. 대신 `Is Player Visible` Condition이 `Priority Abort`/`Restart`에서 매 틱 `EnemySensor`에 묻는다. Condition은 블랙보드에 쓰지 않는다.
     - 기록 시점: 판단이 바뀌는 순간에만 Action이 쓴다 — `Acquire Target`/`Clear Target`, `Record Last Seen`(놓친 순간 1회), `Init Home`(`Repeat` 루프 밖 1회). 값 변경 판정이 동등 비교라(`BlackboardVariable.cs:144`) 매 프레임 바뀌는 값(시각·위치)을 계속 쓰면 변경 이벤트가 매 틱 발생한다.
     - 추격 포기: 커스텀 Condition이 `Time.time - LastSeenTime`을 AI SO의 추격 포기 시간과 비교한다.
     - 재진입: 프로토타입은 파괴 후 재생성. 풀링 전환 시 주의 — `BehaviorGraphAgent.Restart()`는 블랙보드를 초기화하지 않고(`BehaviorGraphAgent.cs:719`, 새 블랙보드는 `Init()`에서만 생성 `:368`), `Init Home`도 루프 밖이라 다시 돌지 않는다. 적 쪽 `ResetState()`에서 위치 복원 + `Target`/`LastSeenTime` 초기화 + `HomePosition` 재기록 + `Restart()`를 직접 처리해야 한다.
     - 주의: 변수↔노드 필드 연결은 바이너리 그래프 에셋에 저장되어 Git diff로 안 보인다. 노드 필드 이름을 바꾸거나 변수를 지운 뒤에는 에디터에서 연결을 확인한다.
     - 패키지 기본 `Navigate To Target`/`Navigate To Location` 노드는 `NavMeshAgent.SetDestination`으로 직접 이동시켜 열린 질문 1번의 (c) 구조와 맞지 않으므로 쓰지 않는다. 이동 Action은 직접 작성한다.
4. **SO 이름과 속도 중복** — 설계안 안에서도 `EnemyAIStatsSO`/`EnemyStatsSO`가 섞여 있다. 컨벤션상 `SOEnemyAIStats`로 가는가? ~~현재 `SOMovementConfig`에는 `Gravity`, `RotationSpeed`만 있고 **이동 속도는 어느 SO에도 없다.** 적 이동 속도를 AI SO에 둘지, 회전 속도는 `SOMovementConfig`와 중복되는데 어느 쪽에 둘지 (7번 결과에 따라 달라짐).~~ → **(2026-09-14) `SOMovementConfig` 삭제됨**(7번 해결). 코어에 이동·회전 속도가 없으므로 둘 다 적 쪽(AI SO 등)이 소유한다. 중력은 `CharacterMotor` 인스펙터 필드.
5. **전투 인터페이스** — `EnemyHealth.TakeDamage()`와 `EnemyCombat.CanAttack()/GetAttackRange()`가 기대하는 계약(`IDamageable` 등)을 전투 담당 팀원과 어떻게 합의하는가? 합의 전 프로토타입은 임시 구현으로 가는가?
   - **부분 진전 (2026-09-15):** 팀원 쪽에 이미 `AttributeSet`/`SOAttributeData`(`Assets/_Project/Scripts/Attribute/Core/`)가 있다. 이름-값(`string`, `float`) 속성을 들고 있고, `AddOnAttributeChangedCallback(name, newValue, oldValue)`으로 변경을 구독할 수 있으며, `SOAttributeEffect.Apply(target, cursor)`(`Attribute/Effect/SOAttributeEffect.cs`)로 한 쪽의 속성값을 다른 쪽에 가감할 수 있다. 이걸 쓰면 `IDamageable`을 새로 만들 필요가 없다 — `EnemyCombat.Attack()`은 `_attackEffect.Apply(playerAttributeSet, myAttributeSet)`만 호출하면 되고, `EnemyHealth`는 `TakeDamage()`를 따로 두지 않고 `"HP"` 속성의 변경 콜백에서 `newValue <= 0`이면 `OnDeath`를 발동하는 식으로 대체 가능해 보인다.
   - **남은 확인 (팀원 대상):**
     1. 공격범위/선딜처럼 전투 중 안 바뀌는 상수 값도 `AttributeSet`(문자열 키, 변경 콜백)에 넣을지, 아니면 별도 typed SO/struct로 뺄지. 안 바뀌는 값에 변경 콜백을 거는 건 낭비이고, 문자열 키는 오타를 컴파일러가 못 잡는다(블랙보드를 명시적 필드로 둔 것과 같은 이유, 위 3번 항목 참고).
     2. 플레이어 쪽도 같은 `AttributeSet` + 같은 속성 이름(`"HP"` 등)을 쓰는지 — 안 그러면 `Apply()`가 플레이어를 못 찾는다.
     3. **이보다 먼저, 게임 디자인 확인이 필요하다:** 이 적이 선딜(telegraph)을 갖는 몹인지(몹 타입마다 다를 수 있음), 공격 간격을 고정 간격(마지막 시도 시각 기준)으로 볼지 쿨다운(마지막 성공/발동 시각 기준, 회피 시 리셋 여부 등)으로 볼지. `CanAttack()`의 판정 로직과 Attack 노드의 트리 구조(선딜 단계 유무)가 이 답에 따라 달라진다.
   - **(2026-09-16)** "판단부와 실행부 분리" 절의 `EnemyCombat` 책임 범위도 이 회의 결과에 따라 확정한다. `AttributeSet`에 어떤 값을 넣을지 아직 정해지지 않아 미해결로 둔다.
6. **폴더/네임스페이스** — `docs/coding-convention.md` 예시(`Assets/_Project/Scripts/Enemy/AI/`, `Project.Enemy.AI`)를 따르는가? 기존 이동 코어는 `namespace Movement`다.
   - **해결 (2026-09-15):** `Enemy` 밑에 `AI`를 두지 않고, **`AI`를 최상위로 두고 그 밑에 `Core`/`Enemy`(미래: `Police`/`GC`)를 둔다.** 이 AI(BT) 구조가 전투 적뿐 아니라 미로의 GC/경찰(보류 항목, 위 참고)에도 재사용될 예정이라, `Enemy.AI`처럼 AI를 Enemy 하위로 두면 그 재사용이 어색해진다. 현재 브랜치(`feature/AI-Core`)가 만드는 것도 "Enemy/Police/GC가 공통으로 쓸 Core"이므로 `AI/Core`를 별도 하위 폴더로 둔다.
     - 폴더: `Assets/_Project/Scripts/AI/Core/`(공유 기반) + `AI/Enemy/`(Enemy 전용, `Core` 소비)
     - 네임스페이스: `Project.AI.Core`, `Project.AI.Enemy`
     - `Core`/`Enemy` 경계에 정확히 어떤 파일이 들어가는지(예: `EnemyMotor`/`EnemySensor`를 범용화해 `Core`로 올릴지)는 구현 단계에서 정한다.
     - `docs/coding-convention.md` 3-6("네임스페이스는 폴더 구조를 그대로 반영")과 충돌하지 않는다 — 문서의 `Enemy/AI` 예시는 예시일 뿐, 규칙 자체가 그 순서를 강제하지는 않는다.
7. **`CharacterMotor`의 이동 방향 회전** — `Move()`가 이동 방향으로 몸을 돌린다(`CharacterMotor.cs:64-67`, `FaceDirection`). 원거리 적의 "플레이어를 바라본 채 좌우 스트레이프"와 충돌하며, (a)/(c) 어느 쪽이든 발생한다. 회전 기준이 쓰는 쪽마다 다르다는 점에서 `intent-002`가 `TransformMotor`에서 회전을 뺀 근거와 같다. **기획자에게 플레이어 이동 방식(평소 회전 기준 / 조준 중 스트레이프 여부 / 공중 방향 전환)을 확인한 뒤** 코어에서 회전을 뺄지 결정한다. 빼면 `intent-002`의 "`FaceDirection` 유지" 결정이 뒤집힌다. ~~프로토타입(근거리 1종)에는 영향 없음.~~ → **근거리 적에도 영향 있음 (2026-09-14 스파이크):** 좁은 곳에 몰리면 이동량 방향이 좌우로 뒤집혀 몸통이 떨리고, 회전을 `steeringTarget` 기준으로 분리해야 해소된다(1번 스파이크 중간 결과 참고). 프로토타입 전에 코어 API 방향(`Move()`에서 회전 빼기 / 회전 방향을 따로 받기)을 정해야 한다.
   - **해결 (2026-09-14):** 회의 결과 플레이어는 WASD 이동 + 마우스 회전으로 확정. → `CharacterMotor`에서 회전(`FaceDirection`)을 뺐다. `Move(direction, speed)` 시그니처는 그대로이고 이동·중력만 적용한다. 회전은 호출자가 `transform.rotation`을 직접 다룬다(적 = `steeringTarget` 기준). 회전 속도만 남았던 `SOMovementConfig`는 삭제하고 중력은 `CharacterMotor`의 `[SerializeField] _gravity`로 옮겼다.
8. **AI 스탯 SO 접근 방식** — Condition/Action이 추격 포기 시간, 리쉬 거리 같은 SO 값을 어떻게 읽는가?
   - (a) 블랙보드에 SO **참조** 하나만 둔다. 값 복사가 아니므로 중복 정의 금지에 걸리지 않는다. Behavior 패키지 블랙보드가 ScriptableObject 타입 변수를 지원하는지 확인이 필요하다.
   - (b) 컴포넌트가 SO를 들고 있고, 노드는 컴포넌트를 통해 읽는다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
