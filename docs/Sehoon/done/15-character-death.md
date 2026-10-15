# 15. 캐릭터 사망 시 처리

- **우선순위**: 낮음
- **상태**: 완료
- **관련 작업**: [09. Hit 방향별 모션](09-directional-hit-motion.md), [10. 피격 시 깜빡임 + 무적](10-hit-blink-invincibility.md), [14. 사격·조준 중 이동 속도 감소](14-fire-aim-move-slow.md)

## 목표
캐릭터가 사망하면 사망 애니메이션을 재생하고 사망 이벤트를 발행한 뒤 정리한다.

## 확정 사항
- 사망 시 처리: 사망 애니메이션 재생, 사망 이벤트 발행.
- 사망 시 이동속도 감속(14) 복구는 하지 않는다. 사망 후 새로 스폰되는 구조라 남은 값이 이어지지 않는다.
- 사망 애니메이션: 34번(`HitRegisterFront_Death`) 1회 재생 후 21번(`Death`) 루프. 둘 다 0.9초 제자리 루프 클립이다.
- 이벤트 2개: 사망 즉시 `OnDeath`, 사망 후 처리(21번이 약 2회 재생)가 끝난 뒤 `OnDeathEnded`. `CharacterMediator`·`PlayerFacade` 가 둘 다 외부로 발행한다. 발행 시점은 자연스럽게 맞춘다(사용자 위임).
- 오브젝트 파괴는 `OnDeathEnded` 발행 직후. `AnimationMediator` 가 없는 캐릭터(적)는 두 이벤트를 같은 프레임에 연달아 발행하고 바로 파괴한다(기존 동작 유지).
- `StageManager` 는 건드리지 않는다. 재스폰 구독을 `OnDeathEnded` 로 바꾸는 것은 사용자가 해당 파트에 따로 요청한다. 그 전까지는 사망 즉시 재스폰이 시작된다.
- 사망 중 입력 차단(B안): `CharacterMediator` 가 사망 시 Move/Skill/Rotate 중재자를 끈다.
- 사망 후 들어오는 피격(`OnHit`)은 무시한다. 사망 처리도 한 번만 한다(`CharacterMediator` 가드).
- `CharacterCombat._isDead` 가드는 되살리지 않는다.

## 현재 상황
- 구현 완료 (사용자 확인).
- 흐름: `CombatMediator.OnDeath` → `CharacterMediator.OnDeathCallback`(한 번만 처리) → `OnDeath` 발행, Move/Skill/Rotate 중재자 비활성화 → `AnimationMediator.NotifyDeath` → `CharacterAnimator.PlayDeath`(Death → DeathLoop) → DeathLoop 2회 뒤 `OnDeathAnimationEnded` → `CharacterMediator.OnDeathEnded` 발행 → `Destroy`.
- `PlayerFacade.OnDeath`(즉시), `PlayerFacade.OnDeathEnded`(사망 후 처리 완료) 두 이벤트를 외부로 발행한다.
- 알려진 문제:
  - `StageManager` 는 아직 `OnDeath` 를 구독한다. 사망 즉시 재스폰이 시작되어 사망 애니메이션 동안 플레이어 2명이 존재한다. `OnDeathEnded` 로 바꾸는 것은 해당 파트에 요청 예정(사용자).
  - 차지 중 사망하면 `SkillMediator` 비활성화 때 `Fire.Stop` 이 비차지샷 1발을 쏜다. 그대로 둔다(사용자 결정 A).
  - `CameraMediator` 는 비활성화하지 않아 사망 중에도 시점 회전·조준 카메라 전환이 된다(몸은 돌지 않음).

## 열린 질문
- [x] 적용 대상: 사망 애니메이션은 `AnimationMediator` 가 있는 플레이어만. 적은 기존처럼 즉시 파괴.
- [x] 사망 이벤트 발행 시점: 즉시(`OnDeath`) + 사망 후 처리 완료(`OnDeathEnded`) 두 번.
- [x] 오브젝트 파괴 시점: `OnDeathEnded` 직후 (21번 루프 약 2회 뒤).
- [x] 사망 중 막을 것: B안, Move/Skill/Rotate 중재자 비활성화. 사망 후 피격은 무시.
- [x] 사망 애니메이션 클립: 34번 → 21번 루프. 상체 사격 자세는 끈다.
- [x] `CharacterCombat` 의 중복 사망 가드(`_isDead`)를 되살리는가? → 되살리지 않는다.
- [x] (작업 전 확인) 사망 시 진행 중이던 구르기·차지·조준: 코드 조사 결과 사격 상체·총신 IK가 사망 모션을 덮고, 이동 입력이 남으면 미끄러질 수 있었다. 구현에서 처리한다.

## 완료 기준
- 플레이 모드 확인: 사망 시 사망 애니메이션이 재생되고, 사망 이벤트가 정해진 시점에 한 번만 발행되며, 정해진 시점에 정리(파괴·재스폰)된다.
- 사망 중 입력으로 캐릭터가 움직이거나 스킬을 쓰지 않는다.
- 컴파일 에러 없음.
