# EnterScene 씬 전환 실패 원인 확인

## 작업 요약 및 방법
- 저장된 Home/Lobby 씬, TestSceneTransitionBox, GameManager, PlayerState 및 프리팹 참조를 추적했다. 실행 코드와 씬은 변경하지 않았다.

## 결정 사항
- 원인 설명 요청이므로 수정 없이 진단 결과만 안내한다.

## 현재 상태 및 이슈
- 두 씬의 EnterScene에는 전환 스크립트, Trigger BoxCollider, Kinematic Rigidbody가 연결되어 있다. Build Settings에도 두 씬이 활성 등록되어 있다.
- GameManager의 SO 필드는 `[SerializeField] private readonly SOAttributeData _playerSOAttributeData`이다. readonly 필드는 Unity 직렬화 대상이 아니며 코드에서 값을 할당하지 않는다.
- GameManager 프리팹은 이전 이름 `_playerSoAttributeDataForName`으로 SO 참조를 저장하고 있다. Lobby에는 `_playerSOAttributeDataForName` 이름의 오버라이드도 남아 있다.
- SO가 null이면 PlayerState 생성자가 ArgumentNullException을 던진다. 초기화 실패 시 CurrentPlayerState가 null이므로 OnTriggerEnter가 로드 전에 반환한다.
- Play Mode는 실행하지 않았다. Editor.log는 접근 거부되어 실제 런타임 예외 확인은 하지 못했다.

## 다음 할 일
- GameManager SO 필드의 readonly를 제거하고 현재 필드 이름으로 SO 참조를 재연결한 후 Play Mode에서 플레이어 등록 및 씬 전환을 확인한다.
