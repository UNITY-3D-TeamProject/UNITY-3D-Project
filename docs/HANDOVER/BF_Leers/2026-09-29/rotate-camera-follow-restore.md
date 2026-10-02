# Rotate 카메라 추종 복원 (2026-09-29)

## 1. 작업 요약 및 방법
- 카메라 연동이 포함된 Rotate 버전(`e65fe11`)을 찾았다. 이 커밋은 브랜치 reset 때문에 reflog에만 남아 있다. 그 동작을 현재 브랜치에 되살렸다.
- `e65fe11`의 파일을 그대로 checkout하지 않은 이유: 그 커밋은 옛 develop의 `CameraMediator.ReferenceFrame` / `MoveMediator.SetReferenceFrame` API 위에 있다. 이 API는 현재 develop에 없다. 되돌리면 컴파일이 깨지고, 팀원의 Camera/Move/Combat 변경까지 같이 되돌아간다.
- 대신 바라보는 방향 규칙만 옮겼고, 연결은 현재 이벤트 구조에 맞췄다.
  - `RotateMediator.cs`: `_viewForward` 필드와 `SetViewForward(Vector3)`를 추가했다. `Update`는 `_viewForward`가 있으면 그 방향을, 없으면 기존처럼 월드 기준 입력 방향을 바라본다.
  - `CharacterMediator.cs`: `[SerializeField] _rotateMediator` 슬롯을 추가했다. `SendViewForward`가 회전 중재자에도 시점 방향을 전달한다.
- `CharacterRotator.cs`는 `e65fe11`과 같아서 수정하지 않았다.

## 2. 결정 사항
- intent-009에 D14를 추가했다. 플레이어는 카메라 정면을 추종하고(입력과 무관), AI는 월드 기준 이동 방향을 따른다(D9 유지).

## 3. 현재 상태 및 이슈
- **Unity 컴파일과 Play 모드 검증을 아직 하지 않았다.**
- 몸통은 `CameraMediator`가 이벤트를 보낼 때(시점 입력 시, OnEnable 초기 1회)만 새 방향을 받는다. `e65fe11`은 매 프레임 피벗을 직접 읽었지만, 이벤트 방식이라도 몸통이 따라가는 결과는 같다.

## 4. 다음 할 일 (Next Steps)
- `[USER]` 플레이어 `CharacterMediator` 인스펙터의 `Rotate Mediator` 슬롯에 RotatorPrefab의 `RotateMediator`를 연결한다.
- Unity 컴파일을 확인한다. Play 모드에서 플레이어 몸통이 카메라 정면을 따라가는지, 근거리 적이 이동 방향을 바라보는지 확인한다.
