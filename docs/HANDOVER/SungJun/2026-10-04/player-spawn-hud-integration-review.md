# 플레이어 스폰과 HUD 수치 연동 확인

## 작업 요약 및 방법
- PlayerSpawner → GameManager.CompletePlayerSpawn → OnPlayerSpawned → UIManager → PlayerHudPresenter → PlayerHudView 호출 및 AttributeSet 변경 콜백을 추적했다.
- 저장된 KSJ_Home/KSJ_Lobby 씬, Player_TPS_Attribute 및 Canvas 프리팹의 참조를 대조했다.
- Unity Play Mode 실행 검증은 하지 않았다. 저장되지 않은 Inspector 변경은 확인 범위 밖이다.

## 결정 사항
- 현재 코드는 씬에 배치된 HUD에 플레이어를 연결한다. 스폰 이벤트로 UI 프리팹을 생성하지 않는다.
- 연결 직후 HP/Battery/Heat 전체 게이지를 갱신하고, 이후 각 Current/Max 변경 이벤트로 해당 게이지를 갱신한다.
- 요청 범위는 확인이며 코드와 에셋은 수정하지 않았다.

## 현재 상태 및 이슈
- Canvas의 UIManager → Presenter → View 및 세 Slider 참조와 여섯 Attribute 키는 연결돼 있다.
- GameManager와 스폰 프리팹의 AttributeSet은 SOAttributeData_Player를 참조한다.
- 저장된 두 테스트 씬의 PlayerSpawner에는 _playerEffects 연결이 없다. SO의 CurrentHp/CurrentBattery/CurrentHeat는 모두 0이고 각 Max는 100이다. 이 상태의 새 게임에서는 세 게이지가 0으로 시작한다.
- 스폰 Effect 에셋 세 개의 Target Attribute가 비어 있다. 그대로 연결하면 Apply에서 예외가 발생하여 스폰 완료 통지에 도달하지 못한다.
- Player_TPS_Attribute에 PlayerAttributeTester가 연결돼 있어 1번 HP 감소, 2번 배터리 감소, 3번 Heat 증가 경로가 존재한다.

## 다음 할 일
- 원하는 시작값을 스폰 Effect에 설정하고 씬 스포너에 연결한다.
- Play Mode에서 최초 게이지와 1/2/3번 입력에 따른 변화, 씬 이동 후 복원값 표시를 확인한다. HP/배터리 감소 확인에는 양수 초기값이 필요하다.

## 변경된 파일
- 이 작업 기록과 HANDOFF_POINTER.md만 추가/갱신했다.
