# 26. 차지샷 대미지 하드코딩 수정

- **우선순위**: 미정
- **상태**: 대기
- **관련 작업**: [04. Fire 구조 차지샷으로 변경](done/04-fire-charge-shot.md), [22. 약점 공격 시 대미지 처리](done/22-weak-point-damage.md)

## 목표
차지샷 피해량을 고정값이 아니라 공격력 기반으로 계산되게 한다.

## 현재 상황
- 일반 총알 `Bullet.prefab` → `ScriptableObjects/PlayerFireEffect.asset`: `_valueSource: Attribute`, `_cursorAttribute: AttackDamage` (발사자 공격력 사용).
- 차지 총알 `ChargedBullet.prefab`(Bullet 프리팹 변형) → `ScriptableObjects/PlayerChargedFireEffect.asset`: `_valueSource: Float`, `_amount: 20` (**고정값**). 공격력 버프·성장이 차지샷에 반영되지 않는다.
- `SOAttributeEffect.Apply` 는 `ValueSource` 가 Float 이면 `_amount`, Attribute 이면 `cursor.GetValue(_cursorAttribute)` 를 쓴다. 배율 항목은 없다.
- 약점 배율(22)은 HP 반영 전에 곱해지므로 차지샷에도 그대로 적용된다.

## 작업 범위 (예상)
- 차지샷 피해량 = 공격력 × 배율 형태로 계산.

## 열린 질문
- [ ] 계산 방식: 공격력 × 배율 / 별도 어트리뷰트(예: `ChargedAttackDamage`) / 기타
- [ ] 배율 위치: `SOAttributeEffect` 에 배율 필드 추가 / `Fire`·`BulletFactory` 에서 곱함 / 차지 총알 쪽 값
- [ ] 배율 기본값 (현재 20 과 공격력 값을 비교해 정할지)
- [ ] `SOAttributeEffect` 수정 시 다른 파트(기믹·AI) 효과 에셋에 영향이 없는지 확인 필요 여부
