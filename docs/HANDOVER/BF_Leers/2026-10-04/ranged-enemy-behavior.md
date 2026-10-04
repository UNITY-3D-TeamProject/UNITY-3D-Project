# 원거리 적 행동 — 후퇴/위치 변경 + 사선 조건 (2026-10-04)

관련 intent: [intent-013](../../../intent/intent-013-ranged-enemy-behavior.md) (`open`)

## 1. 작업 요약 및 방법
- **후퇴/위치 변경**: 플레이어가 후퇴 거리 안으로 들어오면 NavMesh 후보점을 샘플링해 물러난다.
  - `AI/RetreatPointFinder.cs` 신규. 상태 없는 정적 클래스다. 대상 반대 방향 기준으로 0°, ±30°, ±60°, ±90° 순서로 후보를 검사하고, 아래 조건을 모두 통과한 첫 후보를 쓴다.
    - NavMesh 위에 있다 (SamplePosition)
    - 대상과 최소 거리 이상 벌어진다
    - 경로가 완전하고 길이가 이동 거리의 2배 이하다
    - 도착 지점에서 대상까지 NavMesh.Raycast가 막히지 않는다
  - `AI/BT/AIRetreatAction.cs` 신규. 블랙보드 값은 `Range`(대상과 벌릴 최소 거리)와 `MoveDistance`(기본 6)다. 후보를 못 찾거나 정체되면 Failure를 반환하고 1초간 재시도를 막는다. 그동안 공격 가지로 넘어가 제자리에서 사격한다.
- **사선 조건**: `AI/BT/AIHasLineOfFireCondition.cs` 신규. 보이는 대상까지 `NavMesh.Raycast`로 판정한 트임 여부가 기대값 `IsClear`와 같으면 참이다. 패키지에 조건 반전이 없어서 기대값을 받는다.
  - Sensor 유예 시간(3초) 동안 엄폐물 뒤 대상에게 벽에 대고 쏘던 문제를 막는다.
- `AIController`에는 `Position` 속성 1줄만 추가했다.
- 검증: 열린 Editor가 자동 재컴파일했고 `Assembly-CSharp.dll`에 새 클래스 3개가 포함된 것을 확인했다. Editor.log의 `error CS`는 0건이다. Pipeline 패키지가 설치되어 있지 않아 CLI로 연결하지는 못했다.

## 2. 결정 사항
- AIController와 Sensor를 키우지 않는다. 계산은 정적 클래스가, 행동 파라미터(거리·재시도 간격)는 BT 노드가 갖는다.
- 후보 평가는 점수 없이 "선호 순서대로 검사하고 처음 통과한 후보"를 쓴다(프로토타입 범위).
- 후퇴 이동 중에는 쏘지 않는다. 몸은 이동 방향을 본다(회전 코드 무변경).
- 원거리 적에게 추격 가지는 "사거리까지 접근 + 사선 확보" 역할이다. 기존 `AIChaseTargetAction`을 그대로 쓴다.
- 근거리와 공유하는 기존 노드(`AIAttackTargetAction` 등)는 수정하지 않았다.

## 3. 현재 상태 및 이슈
- **[USER] BT 그래프 배선 미완** (`EnemyAI_Ranged.asset`):
  ```
  Try In Order
   1. Guard: 대상이 [RetreatRange] 안에 있다 (Abort Lower Priority) → 후퇴 (Range=RetreatRange)
   2. Guard (Abort Lower Priority, Requires All 체크)
        조건: 대상이 [AttackRange] 안에 있다
              대상까지 사선이 트여 있음이 [true] 이다
        → Fail (조건: 대상까지 사선이 트여 있음이 [false] 이다)
            → 공격 (Range=AttackRange)
   3. Guard: 대상이 보인다                                            → 추격
   4. Guard: 마지막 목격 위치가 있다                                  → 수색
   5. 순찰
  ```
  - Guard는 진입 조건(AND = Requires All)만 담당하고, 사격 중 사선이 막히면 Fail이 공격을 끊는다.
  - Conditional Guard는 Lower Priority만 지원한다(Self/Both 불가). Priority Abort는 조건이 거짓이면 부모를 처음부터 다시 돌려 루프에 빠지므로 진입 관문으로 쓰지 않는다.
  블랙보드 예시: `RetreatRange` = 4, `AttackRange` = 12 (Sensor 시야 15보다 작게).
- **Play 검증 미완.**
- 사선과 후보 판정은 NavMesh 근사라서, 벽이 NavMesh에서 뚫려 있지 않은 씬에서는 벽 너머로 쏠 수 있다. 과거 순찰 씬에서 같은 이슈가 있었다.
- 후퇴하는 동안 등을 보인다. 뒷걸음질 사격이 필요하면 별도로 다룬다.
- `HANDOFF_POINTER.md`의 2026-10-04 "적 원거리 공격" 항목이 가리키는 `2026-10-04/enemy-ranged-attack.md` 파일이 디스크에 없다(링크 깨짐).

## 4. 다음 할 일 (Next Steps)
1. [USER] 위 그래프 배선 후 Play로 다음을 확인한다.
   - 접근한 뒤 사격하는지
   - 가까이 붙으면 후퇴하거나 측면으로 이동하는지
   - 사방이 막혔을 때 제자리에서 사격하는지
   - 엄폐하면 사격을 멈추고 다가오는지
   - 놓치면 수색을 거쳐 순찰로 돌아가는지
2. 검증이 끝나면 intent-012와 intent-013을 resolved 처리한다.
