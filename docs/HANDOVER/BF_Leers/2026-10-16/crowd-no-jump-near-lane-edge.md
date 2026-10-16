# 군중 점프 금지: 레인 시작·끝 근처

## 작업 요약 및 방법
- 문제: 속도가 빠른 보행자가 스폰 직후와 반납 직전에도 앞 개체를 점프로 넘어, 점프하며 튀어나오거나 사라졌다.
- `CrowdController` 에 `_noJumpEdgeDistance`(기본 3m)를 추가하고, `CanJumpOver` 가 `IsNearLaneEdge()` 일 때 false 를 돌려주게 했다.
  - 시작 구간: `_splineIndex == 0 && _progress <= _noJumpEdgeDistance`
  - 끝 구간: `_nextForkKnot < 0 && _laneLength - _progress <= _noJumpEdgeDistance`
  - 닫힌 레인은 둘 다 해당 없음
- 막히면 기존 경로(`TryPassAhead` false → 이동 0)로 뒤에서 멈춰 기다린다. `CrowdLane` 은 수정하지 않았다.
- 씬에 박스를 놓는 기존 `CrowdNoJumpZone` 은 쓰지 않았다. 조종부가 이미 진행 거리·길이를 알고 있어 레인을 추가해도 자동 적용된다.

## 결정 사항
- 시작·끝 근처 점프 금지는 거리 기준으로 코드에서 처리한다 (`CrowdNoJumpZone` 은 별개로 유지).
- 시작 구간은 0번 스플라인만 본다. 갈래로 갈아탈 때 진행 거리가 0 이 되는 것은 스폰이 아니기 때문이다.

## 현재 상태 및 이슈
- 컴파일 에러 0.
- Play(Map_1_Lobby) 콘솔 에러는 내 `wait_for` 호출 인자 누락 1건뿐이고 게임 쪽 에러는 없었다.
- **경계 구간 관찰 검증은 약하다**: 한 시점 샘플에서 점프 중이던 보행자의 진행 거리가 13.0m(시작 3m 밖)였다. 시작·끝 3m 안에서 실제로 점프가 0건인지 연속 관찰은 하지 않았다.
- 기본값 3m 가 몸 길이·감지 거리(1.5m)에 비해 적절한지는 눈으로 확인이 필요하다.

## 다음 할 일 (Next Steps)
- 로비에서 시작점·끝점 부근을 눈으로 보며 `_noJumpEdgeDistance` 를 조정한다 (`Pedstrian.prefab` 에서 값을 바꾸면 된다).
- 위 값이 길면 스폰 직후 뒤 개체가 막혀 정체될 수 있으니 같이 확인한다.
