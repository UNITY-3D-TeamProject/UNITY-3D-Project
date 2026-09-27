# 씬 전환 중심으로 UI 처리 범위 정정

## 작업 요약 및 방법
- 사용자는 별도 플레이어 삭제 기능이 아직 없으며 씬 전환 시 자동 삭제를 예상한다고 설명했다.
- 최신 GameManager에는 안내했던 LateUpdate 검사가 추가되어 있음을 확인했다. 런타임 소스를 직접 변경하지 않았다.

## 결정 사항
- 일반적인 Single 씬 전환에서 플레이어와 HUD가 이전 씬과 함께 제거되고 GameManager만 유지되는 전제라면 단독 플레이어 삭제 감지는 불필요하다.
- PlayerLifetime 및 매 프레임 파괴 검사를 기본 권장안에서 제외한다. 기존 UIManager/Presenter.OnDisable 구독 해제와 다음 씬 플레이어 등록 흐름을 유지한다.
- 기존 종합 가이드 상단에 이후 확인된 범위를 반영하여 전체 적용이 필수가 아님을 명시했다.

## 현재 상태 및 이슈
- DontDestroyOnLoad는 Singleton에서만 확인된다. 실제 HUD 씬 배치는 별도 확인이 필요하다.
- GameManager.HandleBeforeSceneChange는 아직 호출/구독 연결이 없어 저장 기능이 자동 실행되지 않는다.
- 씬 전환 직전에 SaveCurrentAttributes를 호출하고 필요하면 현재 플레이어 등록을 해제한 다음 씬을 교체해야 한다.
- Additive 로딩이나 플레이어/HUD의 영속화는 동일한 자동 제거 전제가 적용되지 않는다.

## 다음 할 일
- 새로 추가한 GameManager.LateUpdate는 이 씬 전환 전제에서는 제거 가능함을 사용자에게 안내한다.
- 실제 씬 전환 담당 구현이 정해지면 저장 및 전환 호출을 연결한다.
