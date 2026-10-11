# 09. Hit 시 방향별 모션

- **우선순위**: 높음
- **상태**: 대기
- **관련 작업**: [10. 피격 깜빡임 + 무적](10-hit-blink-invincibility.md)

## 목표
피격 방향에 따라 다른 피격 모션을 재생한다.

## 현재 상황
- `CharacterCombat` 은 체력이 줄었는지만 보고 `OnHit` 을 방출한다. **피격 방향이나 공격자 정보가 없다** (`Assets/_Project/Scripts/Combat/CharacterCombat.cs`).
- `OnHit` 은 인자 없이 `CombatMediator` → `CharacterMediator` → `PlayerFacade` 로 전달된다.
- 공격 쪽은 `BulletController`(투사체), `MeleeHitController`(근접)가 `SOAttributeEffect.Apply` 로 데미지를 준다.

## 작업 범위 (예상)
- 피격 방향 정보를 피격 이벤트까지 전달하는 경로 추가 (공개 이벤트 시그니처 변경 가능성 있음).
- Animator 에 방향별 Hit 스테이트·파라미터 추가.

## 열린 질문
- [x] 방향 구분 → **4방향**(앞/뒤/좌/우).
- [ ] 사용할 애니메이션 클립은?
- [ ] 방향 전달 방식: 기존 `OnHit` 은 두고 방향을 담은 이벤트를 **추가**할지, 시그니처를 **변경**할지? (`CharacterCombat` 은 이식 가능 모듈이라 의존성 주의)
- [ ] 방향 기준: 공격자 위치인가, 투사체 진행 방향인가?
- [ ] 피격 모션 중 이동·사격이 끊기는가(경직)? 상체 레이어만인가, 전신인가?
- [ ] 적에게도 적용하는가, 플레이어만인가?

## 완료 기준
- 플레이 모드에서 방향별로 다른 피격 모션이 재생되는지 확인.
