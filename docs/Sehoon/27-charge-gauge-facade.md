# 27. 차지샷 게이지 퍼사드 노출 (0~1)

- **우선순위**: 미정
- **상태**: 대기
- **관련 작업**: [04. Fire 구조 차지샷으로 변경](done/04-fire-charge-shot.md), [01. SO Effect / Context Apply 함수 추가](done/01-facade-so-apply.md) — 퍼사드 외부 창구

## 목표
차지 진행도를 0~1 값으로 `PlayerFacade` 를 통해 외부(UI 등)에 노출한다.

## 현재 상황
- `Fire` 는 `WaitForSeconds(_chargeTime)` 코루틴으로 차지한다. 경과 시간을 저장하지 않아 진행도를 계산할 수 없다.
- 외부 알림은 `SkillMediator.OnFireChargeChanged(bool)` (차지 시작/종료) 뿐이며 `AnimationMediator`·`CombatMediator` 가 구독한다. 퍼사드까지 올라가지 않는다.
- `PlayerFacade` 공개 이벤트: `OnAttributeChanged`, `OnHit`, `OnDeath`, `OnDeathEnded`, `OnSkillUsed`, `OnInteractableChanged` 등. 차지 관련 항목은 없다.

## 작업 범위 (예상)
- `Fire` 에 차지 진행도(0~1) 계산 추가.
- `SkillMediator` → `CharacterMediator` → `PlayerFacade` 로 노출.

## 열린 질문
- [ ] 노출 방식: 매 프레임 이벤트(`Action<float>`) / 조회 프로퍼티(`ChargeRatio`) / 둘 다
- [ ] 차지 끝(발사·취소) 후 값: 0 으로 되돌리는가
- [ ] 차지샷 자동 발사 순간(1.0) 을 별도 이벤트로 알리는가
- [ ] 공개 이름 (예: `OnFireChargeRatioChanged`, `FireChargeRatio`)
- [ ] 사용할 쪽(UI 담당자)과 확인할 요구사항이 있는가
