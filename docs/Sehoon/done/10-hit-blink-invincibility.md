# 10. 피격 시 플레이어 깜빡임 + 무적

- **우선순위**: 중간
- **상태**: 완료
- **관련 작업**: [09. Hit 방향별 모션](09-directional-hit-motion.md), [15. 캐릭터 사망 시 처리](15-character-death.md)

## 목표
피격 후 일정 시간 무적이 되고, 그동안 플레이어가 깜빡인다.

## 확정 사항
- 무적 1초, 깜빡임 0.1초 주기(켜기/끄기 전환 간격). Inspector 에서 조절 가능.
- 깜빡임: `Renderer.enabled` 켜기/끄기 (셰이더 비의존).
- 무적 중 막는 대상: Health 감소만. Heat·이동속도 등 다른 어트리뷰트는 그대로. Health 가 줄지 않으므로 `OnHit`·피격 모션(09)도 자연히 나오지 않는다.
- 무적(깜빡임) 중에는 출처와 관계없이 모든 Health 감소를 막는다. 낙하 데미지(`StageManager`)도 막는다. (처음엔 낙하는 막지 않기로 했으나 구현 후 변경. 출처 구분용으로 넣었던 `AttributeSet.CurrentContext` 는 함께 제거)
- 차단 위치: `AttributeMediator` 의 값 반영 전 콜백(pre-change)에서 Health 감소를 이전 값으로 되돌린다. 공격 측 코드는 건드리지 않는다.
- 구르기는 이동이지 무적이 아니다. 구르기 무적은 만들지 않는다.
- 플레이어만 적용. 무적·깜빡임 컴포넌트가 없는 캐릭터(적)는 기존 동작 유지.
- 사망 시(같은 프레임에 Hit → Death) 깜빡임을 시작하지 않거나 즉시 멈추고 렌더러를 켠 상태로 둔다.
- 깜빡임은 애니메이션 쪽 책임이다. `RendererBlinker` 는 `AnimationMediator` 가 참조하고, `CharacterMediator` 가 피격 시 호출한다. `CombatMediator` 는 무적만 다룬다.
- 무적 종료는 `CombatMediator.OnInvincibilityEnded` 이벤트로 알린다(무적 시간 값은 `HitInvincibility` 한 곳에만 둔다).

## 현재 상황
- 구현 완료 (사용자 확인).
- 흐름
  - 피격: 데미지 → `AttributeSet` pre-change → `AttributeMediator.ClampAttribute` 가 `CombatMediator.IsDamageBlocked`(주입, `CharacterMediator` 연결)로 판정해 무적 중이면 Health 감소를 이전 값으로 되돌림 → 막히지 않으면 `CharacterCombat.OnHit` → `CombatMediator` 가 살아 있으면 `HitInvincibility.Begin` → `CharacterMediator.OnHitCallback` 이 살아 있으면 `AnimationMediator.NotifyHit` → `RendererBlinker.Play`.
  - 무적 종료: `HitInvincibility.OnEnded` → `CombatMediator.OnInvincibilityEnded` → `AnimationMediator.NotifyInvincibilityEnded` → `RendererBlinker.Stop`.
  - 사망: `CombatMediator` 가 무적을 멈추고, `AnimationMediator.NotifyDeath` 가 깜빡임을 멈춘다.
- 추가 컴포넌트: `Combat/HitInvincibility`(무적 타이머), `Visual/RendererBlinker`(렌더러 on/off).
- 프리팹: `Player.prefab` 의 CombatPrefab 인스턴스에 `HitInvincibility`, AnimationPrefab 인스턴스에 `RendererBlinker` 추가. `AnimationPrefab` 하위에 렌더러가 없어 `_renderers` 에 `MikeZ`, `RadicalBuster (3)`, `RadicalBuster (4)`, `PowerPod (6)` 를 직접 연결했다. 적과 공용인 `CombatPrefab` 원본은 건드리지 않았다.
- 알려진 사항
  - 무적 중 낙하 구역에 들어가면 데미지는 없지만 `StageManager` 의 체크포인트 이동은 그대로 일어난다.
  - 플레이어 렌더러가 추가·변경되면 `RendererBlinker._renderers` 를 함께 갱신해야 한다.

## 열린 질문
- [x] 무적 시간(초)과 깜빡임 주기는? → 무적 1초, 0.1초 주기.
- [x] 깜빡임 방식: 렌더러 on/off? 머티리얼 색/알파 변경? (셰이더 의존 여부) → 렌더러 on/off.
- [x] 무적 중 막는 대상: 모든 데미지인가, 특정 효과만인가? 피격 모션(09)도 막는가? → Health 감소만. 피격 모션도 막힌다. 낙하 데미지도 막는다.
- [x] 차단 위치: 공격 측(`BulletController`/`MeleeHitController`)에서 판정 / 어트리뷰트 적용 단계에서 차단 / 기타? → `AttributeMediator` pre-change 콜백에서 차단.
- [x] 구르기(Roll) 중 무적도 같은 시스템으로 처리할 계획이 있는가? → 없음. 구르기는 이동이다.
- [x] 플레이어 전용인가, 적에게도 쓰는가? → 플레이어만.

## 완료 기준
- 플레이 모드에서 피격 후 설정 시간 동안 추가 데미지가 없고 깜빡이는지 확인.
