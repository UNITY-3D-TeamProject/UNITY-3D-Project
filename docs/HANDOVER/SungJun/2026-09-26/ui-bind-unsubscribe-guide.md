# HUD Bind 구독 해제 설명

## 작업 요약 및 방법
- 현재 PlayerHudPresenter.Bind와 UIManager.OnDisable을 다시 읽고 구독 해제가 누락될 수 있는 호출 경로를 설명했다.
- 후속 질문에서 UIManager 싱글톤의 씬 참조 수명 문제, AttributeSet 참조의 역할, 스폰부터 HUD 갱신까지의 흐름, 실행 순서 지정의 한계를 안내했다.

## 결정 사항
- 사용자는 문제와 수정 방법을 질문했으므로 소스는 직접 변경하지 않고 교체할 Bind 예시를 제시한다.
- 기존 참조의 구독 해제는 활성 상태와 무관하게 수행한다.
- 새 구독은 Presenter가 활성 상태이고 AttributeSet과 View가 유효할 때 수행한다.
- 현재 플레이어 조회와 스폰 이벤트 구독을 유지하며 실행 순서 설정을 추가하지 않는다.

## 현재 상태 및 이슈
- 현재 코드에는 기존 구독 해제의 isActiveAndEnabled 조건이 남아 있다.
- UIManager와 Presenter가 함께 비활성화되고 UIManager.OnDisable이 먼저 실행되는 경우, Bind(null)이 기존 이벤트를 해제하지 않고 참조를 지울 수 있다. 해당 경로는 정적 분석이며 Play Mode 재현은 수행하지 않았다.
- 런타임 파일 및 사용자의 추가 주석 변경을 보존했다. 이번 설명에서 새 빌드는 실행하지 않았다.

## 다음 할 일
- 제시한 Bind 수정안을 적용한 뒤 HUD 전체 비활성화, HP 변경, 재활성화를 Play Mode에서 확인한다.
