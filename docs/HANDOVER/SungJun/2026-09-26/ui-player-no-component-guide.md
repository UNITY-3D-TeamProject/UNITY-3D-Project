# 플레이어 컴포넌트 추가 금지 제약 반영

## 작업 요약 및 방법
- 사용자가 플레이어에 컴포넌트를 붙일 수 없다고 명시했다. 런타임 AddComponent도 사용하지 않는 안내로 변경했다.
- 최신 GameManager와 PlayerState에는 앞서 안내한 UnregisterPlayer 및 제거 이벤트가 적용됐음을 확인했다. PlayerSpawner는 기존 Start 방식이다.
- Unity 공식 Object 문서에서 파괴된 객체의 관리 참조와 == null 차이를 확인했다.

## 결정 사항
- PlayerLifetime 제안을 철회하고 GameManager.LateUpdate에서 CurrentPlayerState 참조의 파괴 여부를 확인하는 코드를 안내한다.
- 기존 UnregisterPlayer와 OnPlayerDespawned, UIManager의 Bind(null) 흐름은 유지한다.
- docs/HANDOVER/SungJun/2026-09-26/guides/ui-lifecycle-implementation-guide.md의 PlayerLifetime 섹션과 스포너 예시도 이에 맞춰 갱신했다.

## 현재 상태 및 이슈
- 런타임 소스는 변경하지 않았고 실행 검증하지 않았다.
- 삭제 완료 후 다음 LateUpdate에서 알림을 보내므로 다음 프레임에 반영될 수 있다.
- HP 0 및 풀링 비활성화는 파괴가 아니므로 이 방법의 감지 대상이 아니다.

## 다음 할 일
- GameManager에 안내한 LateUpdate를 적용하고 현재 플레이어 파괴 시 HUD 초기화를 검증한다.
- 지연 생성 지원을 적용한다면 스포너 예시에서 Lifetime 관련 코드를 제외한다.
