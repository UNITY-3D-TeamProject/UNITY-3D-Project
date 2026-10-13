# 오브젝트 풀 카테고리 축소 (2026-10-13)

## 작업 요약 및 방법
- `ObjectPoolManager`의 인스펙터 카테고리를 6종에서 3종으로 줄였다.
  - 남김: `Enemies`(근접·원거리·특수), `PlayerBullets`, `EnemyBullets`
  - 삭제: `Npcs`, `Effects`, `BossSkills` 목록과 각 MaxSize 필드, Awake의 등록 호출
- `_monsters` → `_enemies`, `_monstersMaxSize` → `_enemiesMaxSize`로 이름을 바꾸고 `[FormerlySerializedAs]`를 달아 기존 씬에 등록한 값이 유지되게 했다.
- 근접·원거리·특수는 별도 카테고리가 아니라 `Enemies` 목록 안의 키로 등록한다. (예: `MeleeEnemy`, `RangedEnemy`, `SpecialEnemy`)
- 담당별 연결 가이드의 등록 예시 표와 intent-012 카테고리 항목을 갱신했다.

## 결정 사항
- 카테고리 구분은 인스펙터 정리 + 카테고리별 maxSize 용도만 유지한다. Get은 여전히 키만 넘긴다.
- 이펙트 자동 반납(`PooledObject.OnParticleSystemStopped`)은 카테고리와 무관해서 코드에 그대로 둔다.

## 현재 상태 및 이슈
- Unity 재컴파일 에러 0.
- 삭제된 카테고리에 등록해 둔 항목이 있던 씬은 그 등록이 사라진다. TempScene은 해당 목록이 모두 비어 있었다.

## 다음 할 일 (Next Steps)
- 실제 적·탄환 프리팹이 준비되면 각 씬 매니저의 `Enemies` / `PlayerBullets` / `EnemyBullets`에 키를 등록한다.
