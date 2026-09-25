# AI Behavior Tree 노드 계층 구현

- **날짜**: 2026-09-25
- **브랜치**: `feature/AI/Controller`
- **관련 intent**: [intent-008 적 AI 시스템](../../../intent/intent-008-enemy-ai-system.md)
- **이전 세션**: [ICharacterController 설계 확인 + 미사용 메서드 정리](../2026-09-24/icharactercontroller-naming-and-unused-methods.md)

---

## 1. 작업 요약 및 방법

`AIController`의 공개 API를 호출해 줄 두뇌가 없어 적이 제자리에 서 있던 문제를 해결하기 위해,
BT 액션·조건 노드 계층을 새로 작성했다. **기존 코드는 한 줄도 수정하지 않았다.**

신규 파일 8개 — `Assets/_Project/Scripts/AI/BT/`, namespace `AI.BT`:

| 파일 | 역할 | 편집기 표시명 |
|---|---|---|
| `AIActionBase.cs` | 액션 공통 기반. `AIController` 해석·캐싱 | (추상, 메뉴 비노출) |
| `AIConditionBase.cs` | 조건 공통 기반. 같은 패턴 | (추상, 메뉴 비노출) |
| `AIPatrolAction.cs` | 스폰 반경 내 무작위 지점 이동 | **순찰** |
| `AIChaseTargetAction.cs` | 보이는 대상 추격, 목적지 0.2초 주기 갱신 | **추격** |
| `AISearchLastKnownPositionAction.cs` | 마지막 목격 지점 이동 후 기록 삭제 | **수색** |
| `AIStopMoveAction.cs` | 이동 정지 + 목적지 비우기 | **정지** |
| `AIHasVisibleTargetCondition.cs` | 시야 대상 유무 질의 | **대상이 보인다** |
| `AIHasLastKnownPositionCondition.cs` | 목격 기록 유무 질의 | **목격 지점을 기억한다** |

방법:
- 먼저 `com.unity.behavior` 1.0.16 패키지 소스를 직접 읽어 authoring API를 확인했다
  (`NodeDescriptionAttribute`, `ConditionAttribute`, `Status` enum, `NodeRegistry`의 타입 수집 방식).
  **추측으로 쓰지 않았다** — CLAUDE.md 3장 1절.
- 내장 노드로 이미 존재하는 반복·대기·분기(`Repeat` / `Wait` / `Sequence` / `Try In Order` / `Conditional Guard`)는
  재구현하지 않고 그래프 조립에 맡겼다.
- `[USER]` 영역(`.asset` 그래프, 프리팹, 레이어, NavMesh)은 코드 대신
  [조립 가이드 문서](../../../features/enemy-ai-bt-graph-guide.md)로 넘겼다.

---

## 2. 결정 사항

### ① 노드 단위 = 행동 단위 (순찰 / 추격 / 수색)

원시 단위(`MoveTo(Vector3)` + 목적지 선택 노드 분리)도 검토했으나 기각했다.
그래프에서 Blackboard 배선이 필요해지고 노드 수가 늘어나는데, 지금 요구되는 조합 자유도는 그만큼이 아니다.

이 결정의 성격을 분명히 해 둔다 — **"이동" 상태를 셋으로 쪼갠 게 아니다.**
이동 구현은 `AIController` 하나뿐이고 세 노드 모두 같은 `MoveTo` → `CommandMove` → `CharacterMotor` 경로를 쓴다.
셋이 다른 건 **목적지를 어디서 얻는지**와 **그 결정이 언제 무효가 되는지**뿐이다.

| 노드 | 목적지 출처 | 종료 |
|---|---|---|
| 순찰 | 스폰 반경 내 무작위, 1회 결정 | 도착 → Success |
| 추격 | 대상 위치, **0.2초마다 갱신** | 스스로 안 끝남. 시야 상실 → Failure |
| 수색 | 마지막 목격 지점, 1회 결정 | 도착 → 기록 삭제 후 Success |

따라서 `AIController`는 "왜 거기로 가는지"를 모른다. 의미는 BT에만 산다.
"도망", "엄폐물로 이동" 같은 게 추가돼도 `AIController`는 손대지 않고 노드만 늘리면 된다.

### ② `AIController` 참조 = 자기 GameObject에서 `GetComponent` (A안)

`Node.GameObject`(그래프 주인)에서 `GetComponent<AIController>()`를 `OnStart`에 캐싱한다.
Blackboard `[Agent]` 변수를 두는 패키지 관례(B안)는 기각했다 — 노드마다 `Self`를 배선해야 하고
빼먹으면 조용히 null이 된다. "대장이 부하를 조종" 같은 요구가 실제로 생기면
`AIActionBase`에 필드 하나 추가하는 것으로 전환 가능하다.

### ③ 반복·대기는 노드가 아니라 그래프 책임

「순찰」은 도착 즉시 성공한다. 뽑힌 지점이 가까우면 `Repeat`이 연달아 새 지점을 뽑는데,
그래프의 `Wait 2초`가 이 폭주를 막는다. 대기 시간을 코드 상수로 박지 않고 그래프에서 조절하게 했다.

### ④ 가지 우선순위 = `Try In Order` 순서 + Observer Abort

추격 > 수색 > 순찰 순으로 배치하고, 추격 가지의 `Conditional Guard`에
**Observer Abort = Lower Priority**를 건다. 이게 없으면 순찰 `Wait` 중에 플레이어를 봐도 대기가 끝날 때까지 못 움직인다.
FSM 전이표를 짜지 않은 이유이기도 하다 — 우선순위 재평가를 BT가 매 틱 알아서 한다.

### ⑤ 편집기 표시 문자열은 전부 한국어

클래스명·필드명은 C# 컨벤션대로 영어를 유지하되, `name` / `description` / `story` / `category`는 한국어로 썼다.
`story`의 `[...]` 토큰만 Blackboard 필드명과 매칭돼야 하는데 이 노드들엔 Blackboard 필드가 없어 안전하다.
내장 노드(`Repeat`, `Wait` 등)는 패키지 소유라 영어 그대로다.

### ⑥ 노드 `id` GUID는 영구 불변

`[NodeDescription(id: ...)]`의 32자리 hex는 그래프 자산이 직렬화하는 값이다.
**한 번 그래프에 배치된 뒤 바꾸면 해당 노드가 그래프에서 유실된다.** 절대 수정하지 말 것.
중복 id는 조용히 무시되므로 노드를 추가할 때도 새로 생성해야 한다.

---

## 3. 현재 상태 및 이슈

### 검증된 것

- **Unity 컴파일 0 에러.** 이전 두 세션 내내 미검증으로 남아 있던 항목이 이번에 해소됐다.
  에디터 로그에서 `Assembly-CSharp.dll`이 16:09:16에 빌드되고 `Tundra build success`, `error CS` 0건을 확인했다
  (로그에 남은 과거 `error CS8300` 머지 컨플릭트 에러들은 3243행 이전의 지난 기록이다).
- 8개 `.cs` 전부 Unity가 `.meta`를 생성했다(폴더 `BT.meta` 포함).

### 미검증 / 미해결

1. **Play 모드 루프 미검증.** 그래프 자산과 적 프리팹이 아직 없어 실제 동작을 본 적이 없다. 이게 최대 리스크다.
2. **노드가 편집기 Add 메뉴에 뜨는지 미확인.** 코드상 조건(`[Serializable]` + `[GeneratePropertyBag]` + `partial` +
   유효한 GUID)은 전부 갖췄으나 눈으로 본 적은 없다.
3. **`TagManager.asset`의 layer 6이 `Obserct` 오타.** `Obstacle`로 고쳐야 차폐 Raycast가 동작한다.
4. **몸통 회전 코드 부재.** `Sensor`의 시야 방향이 스폰 시점 `forward`에 고정된다.
   플레이테스트는 `_sightAngle`을 360 가깝게 두고 진행해야 한다. 근본 해결은 별도 티켓.
5. **미커밋 작업 트리.** `Packages/manifest.json`, `packages-lock.json`, `ProjectSettings/TagManager.asset`이
   BT 패키지 설치·레이어 추가로 수정된 채 남아 있다. 이번 신규 파일과 함께 정리 필요.
6. **`Assets/Test/AI/AITestDriver.cs`** — gitignore 대상 폴더의 임시 테스트 드라이버.
   BT 그래프가 동작하면 역할이 끝나므로 삭제 여부를 판단해야 한다.

---

## 4. 다음 할 일 (Next Steps)

우선순위 순:

1. **`[USER]` 선행 작업** — [조립 가이드](../../../features/enemy-ai-bt-graph-guide.md) 4장:
   레이어 오타 수정(`Obserct` → `Obstacle`), NavMesh 베이크
2. **`[USER]` BT 그래프 자산 조립** — 가이드 2장의 구조대로.
   **Observer Abort = Lower Priority 설정을 빠뜨리지 말 것**
3. **`[USER]` 적 프리팹 구성** — 가이드 3장.
   `AttributeToMotorAdapter._speedValueKey`를 비워 두면 속도 0이라 제자리에 선다 (가장 흔한 실수)
4. **Play 모드 루프 검증** — 순찰 → 감지 → 추격 → 상실 → 수색 → 복귀.
   그래프 편집기를 열어 두면 실행 중인 가지가 하이라이트되므로 교차 확인할 것.
   **T포즈로 미끄러지는 건 정상이다** (애니메이션·회전 미구현)
5. **검증 통과 시 intent-008을 `resolved`로 바꾸고 `docs/intent/clear/`로 이동**
6. 그 뒤 보류 중인 판단들: 몸통 회전 담당 결정(`CharacterRotator` + `MoveMediator.CommandFace` 쪽이 유력),
   `Sensor` → `SightSensor` 리네임, `ICharacterController` 리네임
7. `character-architecture.md` 소유자와 문서-코드 불일치 4건 협의 (intent-008 열린 질문 ②~⑤)
