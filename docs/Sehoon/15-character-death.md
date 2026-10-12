# 15. 캐릭터 사망 시 처리

- **우선순위**: 낮음
- **상태**: 대기
- **관련 작업**: [09. Hit 방향별 모션](09-directional-hit-motion.md), [10. 피격 시 깜빡임 + 무적](10-hit-blink-invincibility.md), [14. 사격·조준 중 이동 속도 감소](done/14-fire-aim-move-slow.md)

## 목표
캐릭터가 사망하면 사망 애니메이션을 재생하고 사망 이벤트를 발행한 뒤 정리한다.

## 확정 사항
- 사망 시 처리: 사망 애니메이션 재생, 사망 이벤트 발행.
- 사망 시 이동속도 감속(14) 복구는 하지 않는다. 사망 후 새로 스폰되는 구조라 남은 값이 이어지지 않는다.

## 현재 상황
- 사망 판정: `CharacterCombat.Health` 가 0 이하가 되면 `OnDeath` → `CombatMediator.OnDeath` → `CharacterMediator.OnDeathCallback`.
- `CharacterMediator.OnDeathCallback` 은 `OnDeath` 를 발행한 **직후 바로 `Destroy(gameObject)`** 한다. 사망 애니메이션을 재생할 시간이 없다.
- 플레이어 사망 흐름: `CharacterMediator.OnDeath` → `PlayerFacade.OnDeath` → `StageManager.HandlePlayerDeath` → `OnPlayerRespawnRequested` → `PlayerSpawner` 가 다음 프레임에 재스폰.
  - 즉 사망 이벤트 발행 시점에 재스폰이 시작된다. 애니메이션 재생 중에 새 플레이어가 생길 수 있다.
- `CharacterCombat.CheckDeath` 는 `_isDead` 가드가 주석 처리되어 있어, 0 이하에서 체력이 다시 바뀌면 `OnDeath` 가 반복 발생할 수 있다. `StageManager` 는 `_isRecovering` 으로 중복을 막는다.
- `AnimationMediator` / `CharacterAnimator` 에는 사망 관련 메서드·파라미터가 없다. (`CharacterAnimator.Params` 에 Death 해시 없음)
- 사망 애니메이션 후보: `ThirdParty/Tacko3D/RadicalRobotsMike/Art/Animations/Anim_ZMIKE_DeathState.fbx`, `Anim_ZMIKE_HitRegisterFront_Death.fbx`. (`ThirdParty` 는 Git 에 올라가지 않으므로 사용할 클립은 `_Project` 쪽으로 가져와야 하는지 확인 필요)
- 적(Melee/Ranged)도 같은 `CharacterMediator` 사망 경로를 쓴다.

## 열린 질문
- [ ] 적용 대상: 플레이어만인가, 적도 포함하는가? (적은 애니메이터 구성이 다르다)
- [ ] 사망 이벤트 발행 시점: 사망 즉시인가, 애니메이션이 끝난 뒤인가? (즉시면 애니메이션 중 재스폰이 시작된다)
- [ ] 오브젝트 파괴 시점: 애니메이션 종료 후인가, 고정 지연(초)인가? 고정이면 몇 초인가?
- [ ] 사망 중 막을 것: 이동·회전·점프·스킬·카메라 입력, 추가 피격 판정(콜라이더) 중 무엇을 막는가?
- [ ] 사망 애니메이션 클립: 위 후보 중 어느 것을 쓰는가? 상체 레이어(사격 등)는 끄는가?
- [ ] `CharacterCombat` 의 중복 사망 가드(`_isDead`)를 되살리는가? (다른 파트 코드라 담당자 확인 필요)
- [ ] (작업 전 확인) 사망 시 진행 중이던 구르기·차지·조준이 남아 이상 동작을 하는지 플레이 모드로 확인.

## 완료 기준
- 플레이 모드 확인: 사망 시 사망 애니메이션이 재생되고, 사망 이벤트가 정해진 시점에 한 번만 발행되며, 정해진 시점에 정리(파괴·재스폰)된다.
- 사망 중 입력으로 캐릭터가 움직이거나 스킬을 쓰지 않는다.
- 컴파일 에러 없음.
