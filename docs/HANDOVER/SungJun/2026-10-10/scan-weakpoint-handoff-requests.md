# 스캔 약점 데미지 배율 — 타 파트 요청 목록 (2026-10-10)

관련 intent: [intent-019](../intent/intent-019-scan-system.md)

## 현재 준비된 것 (Core 쪽, 완료)
- `Core.Scanning.WeakPoint` : 약점 부위 콜라이더가 있는 오브젝트에 붙이는 컴포넌트. `DamageMultiplier`(기본 2, 최소 1)를 가진다.
- `WeakPoint.GetMultiplier(Collider hitCollider)` : 맞은 콜라이더가 약점이면 그 배율, 아니면 `1`을 돌려준다.
- 배율은 **스캔과 무관하게 항상 적용**된다고 가정했다(스캔은 약점 위치를 보여주는 정보 역할). 스캔으로 드러났을 때만 적용해야 하면 `WeakPointReaction`이 켠 표시 상태를 `WeakPoint`가 알도록 바꿔야 한다. (intent-019 열린 질문)

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
- 적 프리팹에 약점 위치의 자식 오브젝트를 만들고 `Collider`(레이어 `Enemy`, 총알 `_hitLayers`에 포함) + `WeakPoint`를 붙인다.
- 같은 적 루트에 `ScanTarget` + `WeakPointReaction`(`_markers`에 약점 표시 오브젝트 연결)을 붙인다. 표시용 메시/머티리얼이 벽 뒤에서도 보이게 하려면 깊이 테스트가 꺼진 머티리얼(`M_ScanRevealXRay` 계열)을 쓴다.
