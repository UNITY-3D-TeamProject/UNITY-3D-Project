# 가비지 컬렉터(GC) 설계 확정 + 스펙·티켓 분할 (코드 변경 없음)

## 1. 작업 요약 및 방법
- `/start-feature` 파이프라인 Phase 1~2.5를 진행했다.
- Phase 1: grilling 4라운드. 질의응답은 `docs/planning/garbage-collector.md`(개인 기획 폴더, Git 제외)에 있다.
- Phase 2: intent 문서와 스펙·티켓을 작성했다.
  - intent: `docs/intent/intent-021-garbage-collector.md`
  - 스펙: `spec/garbage-collector.md` (Git 제외). UNITY 4개, USER 7개, 세션 A~E로 나눴다.
- Phase 2.5: 서브 에이전트(`feature-dev:code-explorer`, 읽기 전용)가 독립 검증했다. 결과는 스펙의 `## 구현 전 검증`에 15건으로 기록했다. `틀림` 항목은 모두 반영했다.
- 코드·에셋 변경은 0건이다.

## 2. 결정 사항
- GC는 기존 적 골격을 재사용한다: `Melee.prefab` 복제 + `AIController` + `Sensor` + 복제 그래프. 무적은 `CombatPrefab`을 제거해서 만든다.
- 순찰은 `AIController`를 그대로 두고 순찰 지점만 `WaypointPatrolRoute` 컴포넌트에서 받는다. 조종부를 따로 만들지 않은 이유는 BT 노드 13개가 모두 `AIController` 구체 클래스에 묶여 있기 때문이다.
- 공격 후 멈춤은 범용 `AIAttackOnceAction`과 기본 `Wait` 노드를 그래프에서 조립해서 만든다. GC 전용 노드는 만들지 않는다.
- 속도는 `SpeedOverTime`이 `AnimationCurve` 값으로 `CharacterMediator.SetAttribute("MoveSpeed")`를 부르는 방식이다.
- 생성·리셋·제거는 `GarbageCollectorSpawner`가 맡는다. `StageManager`는 읽기만 하고 수정하지 않는다.
- 웨이포인트 생성·NavMesh 런타임 베이크·다리 Not Walkable·벽 레이어는 **맵 담당에게 요청**한다. 계약은 `IReadOnlyList<Vector3>`이고, 그 전까지는 임시로 `Transform[]`을 받는다.
- 피해량은 스폰 시작 HP 50 ÷ n이다(n=3 → 17). Phase 2.5에서 시작 HP가 50이라는 것을 발견해 최대 HP 기준에서 바꿨다.
- `AIController`의 목적지 제한 시간을 Inspector 값으로 바꾼다(기본 10, GC만 40).
- 낙하로 살아서 복귀할 때는 GC를 리셋하지 않는다.

## 3. 현재 상태 및 이슈
- intent-021은 `open` 상태다. 구현 전이다.
- 확인하지 못한 것: 그래프 에셋을 복제할 때 `AssetID` 중복이 무해한지, 라운드 인덱스 1이 실제로 `Checkpoint_Round2_Start`인지. 둘 다 USER-3·USER-5에서 확인한다.
- 알려진 위험: 센서 마스크에 `Default`를 넣으면 출구 문 트리거가 시야를 막을 수 있다. 벽 레이어 요청(USER-6)이 처리되면 해소된다.
- 별건: 미로 벽이 `Default` 레이어라 플레이어 총알이 벽을 뚫는 것으로 보인다. 맵 담당에게 알릴 것.

## 4. 다음 할 일 (Next Steps)
- **새 세션에서** `spec/garbage-collector.md`의 세션 표에서 `todo`인 첫 행(세션 A: UNITY-1~3)부터 시작한다.
- 사용자(언제든): USER-1(어트리뷰트 SO), USER-6(맵 담당 요청 전달).
