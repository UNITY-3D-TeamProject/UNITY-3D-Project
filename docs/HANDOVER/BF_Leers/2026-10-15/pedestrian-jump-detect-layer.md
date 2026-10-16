# 보행자 추월 점프 안 되던 문제 (감지 레이어 비트 누락)

## 작업 요약 및 방법
- 증상: 애니메이션 배선(intent-025) 뒤로 빠른 보행자가 느린 보행자를 만나도 점프하지 않고 멈춘다.
- 원인: `Pedstrian.prefab`의 `CrowdController._detectLayers`가 `2147483712`(Player + 31번 레이어) → `64`(Player만)로 바뀌어 있었다. 보행자 루트는 31번 레이어라 앞쪽 SphereCast가 다른 보행자를 감지하지 못했고, `TryPassAhead`의 `hasJumpTarget`이 false로 남아 `StartJumpOver`가 불리지 않았다.
- 수정: 프리팹 에셋의 `_detectLayers`를 MCP eval(SerializedObject)로 `2147483712`로 되돌리고 저장했다. 코드·애니메이션 배선은 그대로다.
- 검증: 컴파일 에러 0, Play에서 살아 있는 인스턴스의 `DetectLayers`가 2147483712임을 확인했고, 보행자 22개의 `MoveMediator.OnJumpStarted`에 로그를 걸어 `CrowdController.TryPassAhead → StartJumpOver` 경로로 점프가 시작되는 것을 확인했다(t=72.4). 콘솔 에러 0.

## 결정 사항
- 감지 레이어 값은 프리팹에서 그대로 복구만 했다. 레이어 구조 변경은 하지 않았다.

## 현재 상태 및 이슈
- 보행자가 이름 없는 31번 레이어에 있어서 Inspector LayerMask 드롭다운에 비트가 보이지 않는다. `_detectLayers`를 에디터에서 건드리면 31번 비트가 또 빠질 수 있다.
- 에디터가 포커스를 잃으면 `Application.runInBackground`가 꺼져 있어 Play 루프가 멈춘다(MCP 검증 시 런타임에서만 켜서 우회, 프로젝트 설정은 그대로).
- LilRobot Animator의 `Apply Root Motion`이 아직 켜져 있다(가이드상 [USER] 항목).
- 점프 애니메이션 모습 자체는 육안 확인하지 않았다(이벤트·상태만 확인).

## 다음 할 일 (Next Steps)
1. 보행자를 `Crowd` 레이어로 옮기고 `_detectLayers`를 이름 있는 레이어로 바꿀지 결정한다(충돌표 영향 확인 필요: Crowd↔Obstacle 해제, Crowd↔Vehiclebody 해제).
2. 에디터에서 Play하며 점프 애니메이션이 실제로 자연스럽게 재생되는지 확인한다.
