# 스캔 약점 데미지 배율 — 타 파트 요청 목록 (2026-10-10)

관련 intent: [intent-019](../intent/intent-019-scan-system.md)

## 현재 준비된 것 (Core 쪽, 완료)
- `Core.Scanning.WeakPoint` : 약점 부위 콜라이더가 있는 오브젝트에 붙이는 컴포넌트. `DamageMultiplier`(기본 2, 최소 1)를 가진다.
- `WeakPoint.GetMultiplier(Collider hitCollider)` : 맞은 콜라이더가 약점이면 그 배율, 아니면 `1`을 돌려준다.
- **[2026-10-11 정정]** 배율은 **스캔 중에만** 적용된다. `WeakPointReaction`이 스캔 중에만 약점 오브젝트를 활성화(`SetActive`)하므로, 평소에는 약점 콜라이더가 꺼져 총알이 맞히지 못하고 몸통으로 판정되어 배율 1이다. 그래서 `WeakPoint`나 피격 코드에서 스캔 여부를 따로 확인할 필요가 없다. (이전 문서의 "항상 적용" 가정은 폐기)

## 요청 1 — Attribute (이세훈 영역): 피해량에 배율을 실을 통로
피해는 `SOAttributeEffect.Apply(target, SEffectContext)`에서 계산된다 (`float amount = ... _amount ...` 후 `Modifiers.Modify`). 지금은 배율을 실을 곳이 없다.

제안(intent-016의 `SEffectContext` 확장):
- `SEffectContext`에 `float DamageMultiplier`(기본 1, `default` 값은 0이 되므로 생성자 인자 기본값을 1로 두고 읽을 때 `<= 0`이면 1로 취급)를 추가한다.
- `SOAttributeEffect.Apply`에서 **피해로 쓰이는 효과에만** `amount`에 곱한다. 회복/비용 효과까지 곱하지 않도록 구분 기준이 필요하다. (예: `Modifier == Subtract` 이고 대상이 HP일 때만, 또는 효과 에셋에 `IsDamage` 플래그)
- intent-016의 `SEffectContext(cursor, origin)` 시그니처 변경과 겹치므로 그 작업 순서에 맞춰 반영한다.

## 요청 2 — Projectile / Hit 담당: 맞은 콜라이더에서 배율 조회
- `BulletController.ApplyHitEffect(Collider other)` : `new SEffectContext(_cursor)` 만들 때 `WeakPoint.GetMultiplier(other)` 를 배율로 전달.
- `MeleeHitController` : 같은 방식(플레이어 근접 공격이 있다면).
- **주의 — 판정 순서:** 총알은 `OnTriggerEnter`에서 첫 번째로 겹친 콜라이더로 처리하고 즉시 소멸한다. 적 몸통 콜라이더와 약점 콜라이더가 같이 겹치면 어느 쪽이 먼저 호출될지 보장되지 않아 약점이 무시될 수 있다.
  - 권장 1: 약점 콜라이더를 몸통 밖으로 살짝 튀어나오게 배치(총알이 먼저 닿도록).
  - 권장 2: 총알이 맞은 순간 같은 적의 `WeakPoint`들을 `GetComponentsInChildren`으로 모아 총알 위치가 약점 콜라이더 안(`Collider.bounds.Contains` 또는 `ClosestPoint` 거리)인지 직접 확인.
  - 어느 쪽을 택할지는 Projectile 담당과 협의 필요.

## 요청 3 — 레벨/적 프리팹 작업자: 약점 배치
- 적 프리팹에 약점 위치의 자식 오브젝트를 만들고, **그 한 오브젝트에** `Collider`(레이어 `Enemy`, 총알 `_hitLayers`에 포함) + `WeakPoint` + 눈에 보이는 렌더러(메시 등)를 붙인다. 평소에 켜져 있어도 된다. 스캔 반응이 평소에는 꺼 두고 스캔 중에만 켠다.
- 같은 적 루트에 `ScanTarget` + `WeakPointReaction`을 붙인다. **연결할 필드는 없다.** `WeakPointReaction`이 적 아래의 `WeakPoint`를 자동으로 찾는다.
- 약점 오브젝트의 `localScale`은 스캔 반응이 바꾸지 않는다(콜라이더 크기가 변하지 않도록).
- 표시용 메시/머티리얼이 벽 뒤에서도 보이게 하려면 깊이 테스트가 꺼진 머티리얼(`M_ScanRevealXRay` 계열)을 쓴다.
- 주의: 약점은 `SetActive`로 꺼지므로, 약점 오브젝트 자체에 다른 기능(AI 스크립트 등)을 붙이지 않는다. 필요하면 약점 오브젝트의 자식이 아닌 별도 오브젝트에 둔다.
