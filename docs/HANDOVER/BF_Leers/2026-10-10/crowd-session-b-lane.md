# 군중 세션 B — CrowdLane (UNITY-3)

## 작업 요약 및 방법
- `Assets/_Project/Scripts/AI/Crowd/CrowdLane.cs`(+ `.meta`) 신규. 네임스페이스 `AI.Crowd`, `[RequireComponent(typeof(SplineContainer))]`.
- `Start`에서: 프리팹·스플라인 검증(길이 0이면 에러, 닫히지 않았으면 경고) → 최소 간격 기준 개수 계산(초과 시 줄이고 경고) → 균등 간격으로 레인의 자식에 Instantiate(스플라인 위치, 수평 접선 방향).
- 각 개체의 프리팹 루트에서 `CrowdController`/`CharacterMediator`를 `TryGetComponent`로 찾아 `SetLane(_lane, distance)` → `IsValidAttribute(키)` 확인 → `SetAttribute(키, 속도)`.
- 컴파일: MCP `recompile` 0에러.

## 결정 사항
- 간격은 `길이 ÷ 실제 개수`로 균등 배치(최소 간격 이상 보장).
- 필수 컴포넌트가 없는 개체는 `Destroy`(에러 로그 1회/개체). 속성 키가 없으면 개체는 남기고 경고만(속도 0이라 정지).
- 스펙 외 추가 없음. `Reset()`으로 `_lane` 자동 지정만 편의로 둠.

## 현재 상태 및 이슈
- 동작 확인 전부 미완(프리팹·SO·레이어 없음) — USER-5에서 확인.
- Phase 4 자체 점검만 수행(이벤트·코루틴 없음, Unity Object는 `!` 비교).
- 작업 중 `ProjectSettings/EditorBuildSettings.asset`이 수정된 상태로 보임 — 이번 작업과 무관(에디터가 자동 변경한 것으로 추정). 커밋 시 포함 여부 확인 필요.

## 다음 할 일
- 사용자: USER-2(레이어/충돌 행렬), USER-3(SOAttributeData_NPC1/2), USER-4(프리팹), USER-5(테스트 씬 검증 + 150개 성능 관문).
- 세션 C: USER-5에서 발견된 수정, 필요 시 성능 티켓(감지 주기 분산 → 거리 기반 비활성화).
