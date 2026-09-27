# UI 생명주기 보완 적용 가이드

## 작업 요약 및 방법
- 최신 소스를 확인한 뒤 사용자가 요청한 세 가지 보완의 파일별 적용 코드와 설명을 docs/HANDOVER/SungJun/2026-09-26/guides/ui-lifecycle-implementation-guide.md에 작성했다.
- View 검사/연결 해제 시 게이지 초기화, 플레이어 삭제 감지 및 등록 해제 알림, GameManager 초기화 대기를 다뤘다.
- 늦게 생성되는 GameManager 지원에는 UIManager뿐 아니라 PlayerSpawner의 대기도 필요함을 반영했다.

## 결정 사항
- 설명 요청이므로 런타임 소스는 변경하지 않았다. 문서만 생성했다.
- 예시는 HUD 자동 숨김 대신 게이지 초기화를 사용해 UIManager까지 비활성화되는 문제를 피한다.
- 플레이어에 PlayerLifetime 컴포넌트를 연결하고 등록에 사용한 GameManager를 기억한다.
- 이전 플레이어 삭제가 새 플레이어 등록을 지우지 않도록 참조 일치를 확인한다.
- TryGetInstance 및 IsInitialized로 오류 로그 없는 대기를 제공한다. 대기 코루틴은 비활성화 시 명시적으로 중단한다.

## 현재 상태 및 이슈
- 제시한 예시는 적용·컴파일·Play Mode 검증하지 않았다.
- GameManager 유지, 활성 플레이어 프리팹 생성 및 플레이어 GameObject 전체 Destroy를 전제로 한다. 풀링/매니저 런타임 교체/AttributeSet 단독 제거는 범위 밖이다.
- 씬 전환 저장은 실제 전환 호출부에 별도로 연결해야 한다.

## 다음 할 일
- 적용 시 새 기능 규모의 intent를 먼저 작성하고 가이드의 7개 파일 변경을 반영한다.
- 가이드의 생성 지연, 비활성화, 재활성화, A/B 플레이어 교체 후 삭제 검증 시나리오를 실행한다.
