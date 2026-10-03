# 플레이어 스폰 흐름 정리

## 작업 요약 및 방법
- PlayerSpawner, GameManager, PlayerState, AttributeSet, UIManager, PlayerHudPresenter 코드를 읽어 생성부터 능력치 초기화·복원·HUD 연결까지 호출 흐름을 번호로 설명했다.

## 결정 사항
- 코드 변경 없이 현재 구현을 기준으로 설명했다.
- GameManager의 SO는 PlayerState의 저장 대상 이름 목록이며, 최초 능력치 초기값은 플레이어 AttributeSet의 SO에서 읽는다.

## 현재 상태 및 이슈
- PlayerState 등록 및 저장값 복원 이후 OnPlayerSpawned 이벤트가 발생한다.
- UI가 늦게 활성화되면 CurrentPlayerState 조회로 기존 플레이어를 연결한다.
- GameManager.HandleBeforeSceneChange는 현재 코드에서 이벤트에 연결되어 있지 않다. 저장값 복원은 SaveCurrentAttributes가 실제 호출된 경우에만 가능하다.
- 런타임 실행 없이 코드로 확인했다.

## 다음 할 일
- 별도 요청 없음.
