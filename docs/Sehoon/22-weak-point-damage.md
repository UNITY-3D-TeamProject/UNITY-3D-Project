# 22. 약점 공격 시 대미지 처리

- **우선순위**: 높음
- **상태**: 대기
- **선행 작업**: `origin/feature/AI/WeakPoint` 브랜치(BFandLee, 약점 판정 1단계)가 develop 에 머지되어야 한다. 머지 전에는 시작하지 않는다.
- **관련 작업**: [01. SO Effect / Context Apply 함수 추가](01-facade-so-apply.md) — 둘 다 `SEffectContext` 를 다룬다.

## 목표
약점에 명중하면 추가 대미지 또는 대미지 곱을 적용한다.

## 현재 상황
- 약점 판정은 이 브랜치(`feature/PlayerSkill`)와 develop 에 아직 없다. `origin/feature/AI/WeakPoint`(2026-10-14, `0505496`)에 있다.
  - `CharacterCombat.IsCriticalHit(Vector3? hitPoint)`: 맞은 지점이 약점 `SphereCollider` 반지름 안이면 true. 약점이 없거나 꺼져 있거나 hitPoint 가 없으면 false.
  - `WeakPointSelector`(신규): 활성화될 때 후보 Transform 5개 중 하나를 골라 약점을 붙인다.
  - `SEffectContext.HitPoint`(맞은 지점) 필드 추가. `SOAttributeEffect.Apply` 가 복사하고, `BulletController` 가 총알 위치를 넘긴다.
  - `CombatMediator` 의 Health 콜백은 피해일 때 판정 결과를 **로그로만** 찍는다. 피해량은 바꾸지 않는다 (1단계).
  - 근거 문서: 그 브랜치의 `docs/intent/intent-023-weak-point-critical.md`.
- 스캔(김성준)이 머지되면 약점은 스캔 중에만 켜진다. 그때만 치명타가 된다 (`WeakPointReaction` 이 약점 오브젝트를 켜고 끈다).
- intent-023 이 2단계로 남긴 열린 질문
  - 피해 곱을 어디서 적용하는가
    - HP 반영 전 곱하기: `AttributeSet` Pre 콜백에 context 가 필요하다. 2026-10-14 "HP 반영 전 곱하기"로 정했다고 기록되어 있다.
    - 반영 후 추가 피해: `OnHit` 중복 처리가 필요하다.
  - 배율 위치(`CombatMediator._criticalMultiplier` 안이 나옴)
  - `OnCriticalHit` 이벤트 추가 여부
  - 정밀한 맞은 지점 계산
- 같은 문서 기록: 플레이어 `Bullet.prefab` 의 `_hitLayers` 가 128(Obstacle)뿐이라 Enemy 레이어 적을 맞히지 못한다 (intent-022, 범위 밖).

## 작업 범위 (예상)
- 약점 명중 시 대미지 증가 처리. 위치는 Attribute(Pre 콜백 + context) 또는 Combat/CombatMediator.
- 배율 또는 추가 대미지 값 Inspector 노출.

## 열린 질문
- [ ] 작업 분담: 2단계(대미지 반영)를 이 문서(이세훈)가 맡는가, AI 파트(BFandLee)가 이어서 하고 Attribute 쪽 변경만 지원하는가?
- [ ] 방식: **대미지 곱**(예: ×2)인가, **추가 대미지**(고정량)인가? 기본값은?
- [ ] 적용 위치
  - HP 반영 전 곱하기(intent-023 결정. `AttributeSet` Pre 콜백에 context 필요, 이세훈 영역 Attribute 변경)
  - 반영 후 추가 피해(`OnHit` 중복 주의)
- [ ] 배율 값 위치: 맞는 쪽(적의 `CombatMediator`/`CharacterCombat`)인가, 때리는 쪽(총알·효과 에셋)인가?
- [ ] 치명타 알림(`OnCriticalHit` 이벤트, 연출·UI 용)이 필요한가?
- [ ] 플레이어도 약점을 가지는가, 적 전용인가?
- [ ] 차지샷 등 다른 공격과 배율이 겹칠 때 순서(곱셈 누적)는?

## 완료 기준
- 플레이 모드에서 약점 명중 시 피해가 정한 규칙대로 늘고, 일반 피격은 그대로인지 확인.
- 컴파일 에러 없음.
