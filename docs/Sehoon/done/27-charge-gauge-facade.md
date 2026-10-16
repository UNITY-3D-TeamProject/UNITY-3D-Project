# 27. 차지샷 게이지 퍼사드 노출 (0~1)

- **우선순위**: 미정
- **상태**: 완료
- **관련 작업**: [04. Fire 구조 차지샷으로 변경](04-fire-charge-shot.md), [01. SO Effect / Context Apply 함수 추가](01-facade-so-apply.md) — 퍼사드 외부 창구

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
- [x] 노출 방식: 매 프레임 이벤트(`Action<float>`) / 조회 프로퍼티(`ChargeRatio`) / 둘 다
  - 조회 프로퍼티 + 차지 시작/끝 이벤트. UI 는 시작 이벤트를 받으면 차지 중에만 `Update` 에서 프로퍼티를 읽는다. 매 프레임 이벤트는 3단계 중계 비용만 늘어서 쓰지 않는다.
- [x] 차지 끝(발사·취소) 후 값: 0 으로 되돌리는가
  - 0 으로 되돌린다. 재차지 대기(`_rechargeDelay`) 중에도 0.
- [x] 차지샷 자동 발사 순간(1.0) 을 별도 이벤트로 알리는가
  - 알리지 않는다. 끝 이벤트(`false`)로 충분하고, 차지샷 구분이 필요해지면 그때 추가한다.
- [x] 공개 이름 (예: `OnFireChargeRatioChanged`, `FireChargeRatio`)
  - 이벤트 `OnFireChargeChanged(bool)` (기존 `SkillMediator` 이름과 통일), 프로퍼티 `FireChargeRatio` (0~1).
- [x] 사용할 쪽(UI 담당자)과 확인할 요구사항이 있는가
  - 따로 정한 요구사항 없음. 위 방식으로 진행.
