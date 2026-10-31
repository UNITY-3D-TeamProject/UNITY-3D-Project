# AI_TODO2 보스-35: 도약 충격파 예고 화살표 제거

## 작업 요약 및 방법
- `LeapWarnning.prefab` 은 `Left/Right Side Mesh`(화살표)를 가져 예고에 화살표가 보였다.
- 이미 있던 화살표 없는 원형 `CircleWarnning.prefab`(DTT `CircleRegion`)으로 `BossB.prefab` 의 `BossBLeap`(Leap) `_circlePrefab` 만 교체(MCP eval). `BossBTelegraph` 가 `CircleRegion` 을 지원해 코드 변경 없음.

## 결정 사항
- 새 프리팹을 만들지 않고 기존 `CircleWarnning` 재사용. `LeapWarnning` 은 삭제하지 않음.

## 현재 상태 및 이슈
- 리컴파일 통과. Play·직접 플레이·게이트 루프 미실행, 커밋 전.
- `CircleWarnning` 은 DTT(ThirdParty) 의존. 반경은 `_shockwaveTiles` 기준으로 코드가 맞춤.

## 다음 할 일
- 직접 플레이로 화살표 없음, 원 범위·시점이 충격파 판정과 맞는지 확인(이펙트-3 과 함께).
