---
id: intent-061
title: 피드 맵 악성 데이터 여러 개에 닿아도 피격 무적 때문에 데미지를 1번만 입음
part: Stage / FeedApp / Player
status: open   # open | resolved
created: 2026-10-22
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
악성 피드를 제거하면 악성 데이터(`HomingHazard`, 프리팹 `MaliciousData_Fast/Mid/Slow`)가 여러 개(기본 3개) 생겨 플레이어를 쫓아온다.
여러 개에 연달아 닿으면 그만큼 데미지를 입어야 하는데, 실제로는 1번만 입는 경우가 많다.

원인:
- 플레이어 프리팹의 `HitInvincibility._duration`이 1초다. 첫 피격 뒤 1초 동안 `CombatMediator.IsDamageBlocked`가 체력 감소를 막는다.
- `HomingHazard`는 닿으면 데미지가 막혔는지와 상관없이 `Destroy(gameObject)`로 사라진다. 그래서 무적 중에 닿은 악성 데이터는 데미지 없이 소모된다.

무적 시간을 바꾸는 기능은 이미 있다: `PlayerFacade.SetHitInvincibilityDuration(float)` → `CharacterMediator` → `CombatMediator` → `HitInvincibility.SetDuration`. 다음 피격부터 적용된다. 지금 이 기능을 부르는 곳은 없다.

## 기대 결과 (Proposed outcome)
피드 맵에서는 악성 데이터에 닿을 때마다 데미지를 입는다.

설계안 1 — 피드 맵 전체 (처음 안):
- `Core.Stage.FeedApp`에 `[SerializeField] float _hitInvincibilityDuration`을 두고, `StartStage()`에서 `GameManager.Instance.CurrentPlayerFacade.SetHitInvincibilityDuration(값)`을 부른다.
- `StartStage()`는 `GameManager.CompletePlayerSpawn`에서 플레이어가 생성될 때마다(씬 입장·사망 후 재스폰) 불리므로, 새로 생성된 플레이어에도 매번 다시 적용된다.
- 플레이어는 씬마다·재스폰마다 프리팹에서 새로 만들어지므로(기본 1초) 다른 맵으로 나갈 때 되돌리는 코드는 필요 없다.

설계안 2 — 악성 데이터가 있을 때만 0초 (사용자 희망, 현재 검토 중):
- 매 프레임 `FindObjectsByType`으로 찾는 방식은 비용 때문에 쓰지 않는다.
- 생길 때: 맵은 `MapGeneratorBase.Awake`에서 생성되므로 `FeedApp.StartStage()`에서 `MaliciousFeed`를 한 번 찾아 `OnCleared`를 구독한다. `MaliciousFeed.Clear()`는 `SpawnData()` 다음에 `OnCleared`를 부르므로, 이때 생긴 `HomingHazard`를 한 번만 찾아 목록에 담고 무적을 0초로 바꾼다.
- 사라질 때: `HomingHazard`는 `Destroy`될 때 알리는 이벤트가 없다.
  - A안: `FeedApp.Update`에서 목록이 비어 있으면 바로 return, 있을 때만 참조 몇 개의 null 여부를 보고, 다 사라지면 원래 값으로 되돌린다. Map 스크립트 수정 없음.
  - B안: `HomingHazard`에 파괴 이벤트를 추가해 이벤트만으로 처리한다. Map 스크립트 수정 필요.
- 데이터가 남은 채 사망·재스폰하면 새 플레이어는 1초로 돌아오므로, `StartStage`에서 살아 있는 데이터가 있으면 0초를 다시 넣는다.
- 되돌릴 원래 값: Facade에 현재 무적 시간 조회 기능 추가(`HitInvincibility.Duration` → CombatMediator → CharacterMediator → PlayerFacade) vs `FeedApp`에 평소 값 필드.

채택안 (2026-10-22 구현, 매 프레임 확인 없이 이벤트로만):
- `HomingHazard`에 인스턴스 이벤트 `OnDestroyed(HomingHazard)` 추가, `OnDestroy()`에서 호출(닿아서·공격받아 사라질 때 모두).
- `FeedApp.StartStage()`: `MaliciousFeed`를 모두 찾아 `OnCleared` 구독, `GameManager.OnPlayerSpawned` 구독.
- `OnCleared` 때 `FindObjectsByType<HomingHazard>`를 한 번 불러 처음 보는 데이터만 `HashSet`에 넣고 `OnDestroyed` 구독, 하나라도 있으면 무적 0초.
- `OnDestroyed` 때 목록에서 빼고, 비면 `_defaultHitInvincibilityDuration`(기본 1초, 플레이어 프리팹 값과 맞춤)으로 되돌림.
- 재스폰: `StageManager.StartStage`는 `IsRunning` 때문에 스테이지마다 한 번만 불린다(처음 적은 "재스폰마다 StartStage" 설명은 틀림). 그래서 `OnPlayerSpawned`에서 데이터가 남아 있으면 새 플레이어에 0초를 다시 넣는다.
- `FeedApp.OnDestroy()`: 악성 피드·남은 데이터·스폰 이벤트 구독 해제.
- 같은 프레임 동시 접촉 방지: `MaliciousFeed._speedMultipliers`(기본 `{ 0.9, 1.0, 1.1 }`)를 소환 순서대로 `HomingHazard.ScaleSpeed`에 넘겨, 좌우 대칭 위치의 데이터 속도를 다르게 한다(양준범 님 허락). 평균 속도는 프리팹 값과 같다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 피드 맵(`Map_2_Feed`)을 플레이하는 플레이어
- 어떤 시스템/모듈이 건드려지는가: `Core.Stage.FeedApp`, `Map.Gimmicks.HomingHazard`(파괴 이벤트·`ScaleSpeed` 추가), `Map.Feed.MaliciousFeed`(속도 배율) — 양준범 님 허락
- 피드 맵의 다른 피해원도 무적 시간이 함께 짧아진다. 연속 접촉 피해인 `KnockbackHazard`는 자체 `_hitCooldown`(0.6초)이 있어 매 프레임 피해는 나지 않는다.

## 제약 (Constraints)
- 지켜야 할 정책: `HitInvincibility`(이세훈 님)는 수정하지 않는다. 무적 시간은 Facade에 이미 있는 기능만 쓴다.
- `FeedApp.cs`는 UTF-8이 아닌 인코딩(EUC-KR 추정)으로 저장돼 있어, 수정 시 기존 한글 주석이 깨지지 않게 인코딩을 유지해야 한다.
- 무적 0초여도 `HitInvincibility.Begin()`이 `IsInvincible`을 켜고 다음 `Update`에서 끄므로, 같은 프레임에 둘이 동시에 닿으면 하나는 여전히 막힌다. TestScene에서 플레이어가 서 있을 때 좌우 대칭 위치의 데이터 2개가 같은 프레임에 닿아 실제로 1번 막혔다(드물지 않음).
- 무적이 짧아지면 피격 깜빡임(`PlayerAnimationMediator`)도 그만큼 짧아진다.

## 열린 질문 (Open questions)
- 결정됨: 적용 범위는 악성 데이터가 있을 때만, 사라짐 감지는 `HomingHazard` 인스턴스 이벤트, 원래 값은 `FeedApp` 필드, 동시 접촉은 `HitInvincibility` 대신 소환 속도 차이로 해결.
- 확인됨: TestScene에서 가만히 선 채 3번 피격(HP 100 → 90 → 80 → 70, 프레임 93·104·107), 모두 사라진 뒤 무적 1초 복귀.
- 남은 확인: 재스폰 시 0초 유지, `Map_2_Feed` 실제 플레이.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
