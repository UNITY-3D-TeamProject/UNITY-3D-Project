# Player 자식 CharacterController 배치 오류 확인

## 작업 요약 및 방법
- CharacterMotor.Awake 및 Player/MovePrefab 프리팹을 읽었다.
- Player의 자식 MovePrefab에 CharacterMotor와 CharacterController가 함께 있고, Player 루트에는 CharacterController가 없다.
- CharacterMotor는 GetComponentInParent로 자기 자신의 Controller를 우선 선택하고 자식 배치 오류를 출력한다. 해당 분기는 로그만 출력하며 이동을 비활성화하지 않는다.

## 결정 사항
- Player 프리팹 편집 모드에서 CharacterController만 Player 루트로 옮기는 방법을 안내한다. CharacterMotor/MoveMediator/Adapter는 자식에 유지할 수 있다.
- 기존 Controller 설정을 복사하고 자식 Controller는 제거해야 한다. 비활성화만 하면 탐색에서 다시 선택될 수 있다.
- 현재 Player의 Controller 오버라이드: Height 1.5, Radius 0.23, Center (0, 0.75, 0). MovePrefab의 로컬 위치/회전은 원점/단위 회전이다.

## 현재 상태 및 이슈
- 소스와 프리팹을 직접 수정하지 않았으며 Play Mode 검증도 수행하지 않았다.
- Player 프리팹의 MoveMediator motor 오버라이드가 CharacterMotor 대신 AttributeToMotorAdapter를 참조하는 것으로 확인된다. Inspector에서 Motor를 같은 MovePrefab의 CharacterMotor로 다시 연결할 필요가 있다.

## 다음 할 일
- Player 프리팹에서 위 두 설정을 수정한 후 재스폰하여 이동과 씬 전환을 확인한다.
