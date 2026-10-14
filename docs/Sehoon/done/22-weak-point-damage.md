# 22. 약점 공격 시 대미지 처리

- **우선순위**: 높음
- **상태**: 완료
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
- [x] 작업 분담: 2단계(대미지 반영)를 이 문서(이세훈)가 모두 맡는다.
- [x] 방식: 대미지 곱. 기본 ×2.
- [x] 적용 위치: HP 반영 전 곱하기. `AttributeSet.OnAttributeChangeWithRef` 시그니처에 `SEffectContext context` 를 추가한다 (오버로드를 두면 Pre 콜백 두 개가 함께 호출되어 헷갈리므로 교체. 호출부는 모든 브랜치에서 `AttributeMediator` 한 곳).
- [x] 배율 값 위치: 맞는 쪽. `CharacterCombat._criticalMultiplier` (`_weakPoint` 바로 아래, Inspector 노출).
- [x] 치명타 알림: `OnCriticalHit(Vector3? hitPoint)` 를 `CombatMediator` → `CharacterMediator` 까지 올려 발행한다.
- [x] 플레이어 약점: 없음 (적 전용).
- [x] 배율 겹침 순서: 무적 차단 → 최종 감소량(`oldValue - newValue`)에 배율 곱 → Clamp. 차지샷은 효과 에셋 피해량이 달라 자연스럽게 곱해진다.

## 확정 사항
- 흐름: `AttributeSet` Pre(context 포함) → `AttributeMediator.ClampAttribute` → 주입된 `CombatMediator.ModifyDamage`(체력 키 필터) → `CharacterCombat.ModifyDamage`: `IsCriticalHit` 이면 `OnCriticalHit` 발행 후 `damage × _criticalMultiplier`.
- 이벤트 순서: `OnCriticalHit` → `OnHit` → (`OnDeath`). 무적으로 막힌 공격은 치명타가 아니다.
- `CombatMediator` Health 콜백의 1단계 판정 로그는 제거했다.

## 작업 시작 시 확인 (2026-10-14)
- 선행 브랜치는 develop 에 머지됨 (`3e0c4f7`, PR #71). 문서 작성 이후 바뀐 점:
  - 판정이 `IsCriticalHit(Vector3? hitPoint, Vector3? direction)` 탄도선 거리 방식으로 바뀌고 `SEffectContext.Direction` 이 추가됐다.
  - `Bullet.prefab._hitLayers` 는 384(Obstacle + Enemy)로 이미 적을 맞힌다.
  - `_weakPoint` 는 Melee·Ranged 에만 연결돼 있고 Player 에는 없다.

## 결과 구조
- `AttributeSet.OnAttributeChangeWithRef`: `(string, ref float, float)` → `(string, ref float, float, SEffectContext)`. `NativePreAttributeChanged` 가 `_currentContext` 를 넘긴다.
- `AttributeMediator`: `SetDamageModifier(Func<string, float, SEffectContext, float>)` / `ClearDamageModifier()`. `ClampAttribute` 는 무적 차단 → 감소량 보정 → Clamp 순.
- `CharacterCombat`: `_criticalMultiplier`(기본 2, Inspector), `ModifyDamage(float, Vector3?, Vector3?)`(약점 명중이면 `OnCriticalHit` 발행 후 배율 곱), `event Action<Vector3?> OnCriticalHit`. Attribute 를 참조하지 않는 이식성 원칙을 지킨다.
- `CombatMediator`: `ModifyDamage(string, float, SEffectContext)` 는 체력 키만 걸러 context 를 풀어 `CharacterCombat.ModifyDamage` 로 넘긴다. `OnCriticalHit` 중계.
- `CharacterMediator`: `event Action<Vector3?> OnCriticalHit` 중계(사망 후 미발행), `SetDamageModifier(_combatMediator.ModifyDamage)` 연결·해제.
- 프리팹 변경 없음 (기존 적 프리팹은 기본값 2 적용).

## 검증
- 컴파일 에러 없음.
- 플레이 모드: 사용자 확인 (약점 명중 2배, 일반 피격 그대로).

## 완료 기준
- 플레이 모드에서 약점 명중 시 피해가 정한 규칙대로 늘고, 일반 피격은 그대로인지 확인.
- 컴파일 에러 없음.
