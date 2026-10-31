# 보스A 그로기 팔 피격 이펙트 위치 수정 (AI_TODO2 보스-30)

## 작업 요약 및 방법
- 원인: 그로기(HandsOnly)에서는 `BossHitRelay.SetValue` 가 HP 하한을 적용하며 `SEffectContext` 를 `(Cursor, Effect)` 로 새로 만들어 `HitPoint`·`Direction` 을 모두 버렸다. `CharacterMediator.GetHitSource` 는 맞은 지점이 없으면 공격자(`Cursor`) 위치로 대체하므로 `HitEffectFeedback` 이 플레이어 위치에서 이펙트를 냈다.
- 수정: `BossHitRelay.cs` 에서 `HitPoint` 만 남기고 `Direction` 은 계속 뺀다. 약점 판정(`CharacterCombat.IsCriticalHit`)은 `hitPoint`·`direction` 둘 다 있어야 성립하므로 손을 때려도 가슴 코어 약점 보정이 붙지 않는 기존 의도는 유지된다.

## 결정 사항
- 맞은 지점은 그로기 중에도 항상 전달한다. 크리티컬 차단은 `Direction` 제거로 한다.

## 현재 상태 및 이슈
- 리컴파일 통과(오류 0·경고 0). 직접 플레이 미확인, 게이트 루프 미실행, 커밋 전.
- 부수 효과: `GetHitSource` 가 맞은 지점을 쓰게 되어 그로기 중 피격 방향 계산(`NotifyHit`)도 공격자 위치 대신 맞은 지점 기준이 된다. 보스A 에는 영향이 미미할 것으로 봄(미확인).

## 다음 할 일
- 그로기 구간에서 손 여러 부위를 때려 이펙트가 맞은 지점에서 나오는지, 손을 때려도 크리티컬이 안 붙는지 확인. 보스-32 마커와 함께 확인.
