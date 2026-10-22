# 피드 맵 악성 데이터 무적 0초 구현 (intent-061)

## 작업 요약 및 방법
- 악성 데이터가 남아 있는 동안만 플레이어 피격 무적을 0초로 두도록 구현했다. 매 프레임 확인 없이 이벤트로만 처리한다.
- `Map.Gimmicks.HomingHazard`: 인스턴스 이벤트 `OnDestroyed(HomingHazard)` 추가, `OnDestroy()`에서 호출(사용자 허락).
- `Core.Stage.FeedApp`: `StartStage()`에서 `MaliciousFeed.OnCleared`·`GameManager.OnPlayerSpawned` 구독. 제거 때 `FindObjectsByType<HomingHazard>` 한 번으로 처음 보는 데이터만 `HashSet`에 담고 `OnDestroyed` 구독 → 무적 0초, 모두 사라지면 `_defaultHitInvincibilityDuration`(1초)로 복귀, 재스폰한 플레이어에 데이터가 남아 있으면 0초 재적용, `OnDestroy()`에서 구독 해제.
- `FeedApp.cs`는 CP949 인코딩이라 UTF-8로 바꿔 수정한 뒤 다시 CP949로 저장(왕복 손실 없음 확인, diff는 추가 줄만).
- 같은 프레임 동시 접촉 방지(양준범 님 허락): `HomingHazard.ScaleSpeed(float)` 추가, `MaliciousFeed._speedMultipliers`(기본 `{ 0.9, 1.0, 1.1 }`)를 소환 순서대로 적용. 기존 악성 피드 프리팹 5개는 기본값이 들어가 수정 안 함.
- 상세는 [intent-061](../../../intent/intent-061-feed-malicious-data-invincibility.md).

## 결정 사항
- 사라짐 감지는 `HomingHazard` 인스턴스 이벤트(static 아님: 구독한 데이터에서만 알림, 보내는 쪽이 사라지므로 해제 부담 적음).
- 원래 값은 `FeedApp` 필드(플레이어 프리팹 값과 맞춤).
- 동시 접촉은 `HitInvincibility`(이세훈 님 코드)를 고치지 않고 소환 속도 차이로 해결. 출발 대기 차이보다 오래 쫓을수록 간격이 벌어지는 속도 차이를 택함.
- `StageManager.StartStage`는 스테이지마다 한 번만 불리므로 재스폰은 `OnPlayerSpawned`로 처리(Tutorial과 같은 방식).

## 현재 상태 및 이슈
- 컴파일 0에러. TestScene Play에서 실행 중에 FeedApp·MaliciousFeed(데이터 `MaliciousData_Mid` 3개)를 임시로 만들어 확인(씬 저장 안 함): 제거 직후 무적 1 → 0, 데이터 3개 모두 사라진 뒤 0 → 1 복귀 확인.
- 1차 테스트에서 가만히 서 있으면 좌우 대칭 데이터 2개가 같은 프레임에 닿아 하나가 막힘(HP 100 → 90 → 90 → 80). 원인은 0초여도 `HitInvincibility.Begin()`이 한 프레임 무적을 켜기 때문.
- 속도 배율 적용 후 컴파일 0에러, Play에서 소환된 데이터 속도 5.28 / 4.8 / 4.32(Mid 기준)·무적 0초 확인. 에디터 게임 루프가 저절로 넘어가지 않아(창을 클릭해도 동일) 일시정지 후 `EditorApplication.update`에서 `EditorApplication.Step()`을 200번 불러 진행: 가만히 선 채 HP 100 → 90 → 80 → 70(프레임 93·104·107, 서로 다른 프레임), 끝난 뒤 무적 1초·데이터 0개 확인.
- 재스폰 재적용, `Map_2_Feed` 실제 플레이는 미확인. 커밋 안 함.

## 다음 할 일 (Next Steps)
- `Map_2_Feed`에서 실제 플레이로 확인, 재스폰 시 0초 유지 확인.
