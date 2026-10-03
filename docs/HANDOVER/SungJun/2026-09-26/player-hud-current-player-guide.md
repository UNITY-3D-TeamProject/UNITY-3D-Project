# 현재 플레이어 조회 및 HUD 연결 안내

## 작업 요약 및 방법
- 실제 PlayerState, GameManager, UIManager, PlayerHudPresenter와 Singleton 코드를 읽고 사용자가 제시한 2~4번 변경의 적용 위치를 설명했다.
- 구현 요청이 아닌 적용 방법 질문이므로 런타임 소스는 수정하지 않았다.

## 결정 사항
- PlayerState의 기존 참조를 읽기 전용 프로퍼티로 공개한다.
- GameManager는 저장값 적용 뒤 기존 OnPlayerSpawned 이벤트로 알린다.
- UIManager는 이벤트 구독과 현재 플레이어 조회를 함께 사용하고, 구독한 GameManager 참조로 구독을 해제한다.

## 현재 상태 및 이슈
- 제안된 변경은 아직 소스에 적용하지 않았다. 실행 검증은 수행하지 않았다.
- 현재 PlayerHudPresenter.Bind(null)은 플레이어 연결을 해제하며 표시값 초기화는 하지 않는다.

## 다음 할 일
- 안내에 따라 2~4번을 적용하고 Inspector 참조 및 UI 재활성화 시 연결을 확인한다.
