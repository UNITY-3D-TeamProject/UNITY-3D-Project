# 피드 맵 악성 데이터 피격 무적 설계 (intent-061)

## 작업 요약 및 방법
- 피드 맵에서 악성 데이터 여러 개에 닿아도 데미지를 1번만 입는 원인을 확인하고 설계만 정리했다. 코드는 수정하지 않았다.
- 원인: 플레이어 프리팹 `HitInvincibility._duration`이 1초라 첫 피격 뒤 `CombatMediator.IsDamageBlocked`가 체력 감소를 막는데, `HomingHazard`는 막혔는지와 상관없이 닿으면 `Destroy`된다. 무적 중 닿은 데이터는 데미지 없이 소모된다.
- 무적 시간 변경 기능 `PlayerFacade.SetHitInvincibilityDuration`은 이미 있지만 부르는 곳이 없다. 현재 값을 읽는 기능은 없다.
- 상세 설계는 [intent-061](../../../intent/intent-061-feed-malicious-data-invincibility.md).

## 결정 사항
- 사용자는 "악성 데이터가 있을 때만 0초"를 원한다.
- 매 프레임 `FindObjectsByType`로 찾는 방식은 쓰지 않는다(비용).
- 생성 감지는 `MaliciousFeed.OnCleared` 이벤트로 한다(`FeedApp.StartStage`에서 구독).

## 현재 상태 및 이슈
- 구현 전. 사용자가 구현하지 말라고 해 문서만 커밋했다.
- 사라짐 감지 방식(A: 데이터 있을 때만 참조 null 확인 / B: `HomingHazard` 파괴 이벤트 추가)과 원래 값 출처(Facade 조회 추가 / `FeedApp` 필드) 미결정.
- `FeedApp.cs`는 UTF-8이 아닌 인코딩이라 수정 시 인코딩 유지 필요.

## 다음 할 일 (Next Steps)
- 위 두 가지 결정 후 `FeedApp`에 구현, 사망·재스폰 시 0초 재적용 포함.
- TestScene에서 악성 데이터 3개 연속 피격 시 3번 데미지 확인.
