# 군중 세션 A — CrowdController (UNITY-1, UNITY-2)

## 작업 요약 및 방법
- `Assets/_Project/Scripts/AI/Crowd/CrowdController.cs`(+ `.meta`, `Crowd.meta`) 신규. 네임스페이스 `AI.Crowd`.
- `IMoveController`, `IBodyRotateController` 구현(`AIController`와 같은 콜백 보관 방식). 프리팹 루트에 붙인다.
- 매 프레임: 지난 프레임 이후 실제 이동량을 스플라인 접선에 투영해 진행 거리를 늘리고(길이 초과 시 0으로 감김), 진행 거리 + 앞쪽 거리 지점을 향한 수평 방향으로 이동·회전을 요청한다.
- 진행 방향 `SphereCastNonAlloc`으로 막힘 검사. 자기 콜라이더(`IsChildOf(transform)`)는 제외하고, 막히면 이동 0만 요청(회전 요청 없음).
- 컴파일: MCP `recompile` 0에러.

## 결정 사항
- 공개 입구는 `SetLane(SplineContainer lane, float startDistance)` 하나. 호출 전/길이 0이면 이동 0.
- 스펙에 없던 Inspector 필드 `_detectHeight`(기본 0.9) 추가 — SphereCast 중심 높이가 필요해서.
- 진행 거리는 목표 추종 결과(실제 이동량)로만 갱신하므로 멈추면 늘지 않는다.

## 현재 상태 및 이슈
- 동작 확인 전부 미완(프리팹·SO·레이어가 없음) — USER-5에서 확인.
- 확인 불가 항목: SphereCast가 CharacterController를 감지하는지, 시작 겹침 무시 동작.
- MCP `console` 호출이 400 오류여서 콘솔 경고는 확인하지 못했다(컴파일 결과만 확인).
- Phase 4 리뷰는 자체 점검만 했다(이벤트 해제 해당 없음, Unity Object는 `!_lane` 비교, 코루틴 없음).

## 다음 할 일
- 세션 B: UNITY-3 `CrowdLane.cs` — `SetLane` 호출 후 `CharacterMediator.IsValidAttribute` 확인 → `SetAttribute`.
- 사용자: USER-2, USER-3, 이어서 USER-4(프리팹).
