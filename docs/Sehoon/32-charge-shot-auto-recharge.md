# 32. 차지샷 누르고 있으면 자동 재차지

- **우선순위**: 미정
- **상태**: 대기
- **관련 작업**: [04. Fire 구조 차지샷으로 변경](done/04-fire-charge-shot.md), [27. 차지샷 게이지 퍼사드 노출](27-charge-gauge-facade.md) — 재차지 때 게이지가 0 부터 다시 오른다. [26. 차지샷 대미지 하드코딩 수정](done/26-charge-shot-damage.md)

## 목표
차지샷이 자동 발사된 뒤에도 마우스를 누르고 있으면, 잠깐 쉬었다가 다시 차지를 시작한다. 마우스를 떼면 바로 발사하는 동작은 그대로 둔다.

## 동작 (요청)
| 상황 | 현재 | 변경 후 |
|---|---|---|
| 누름 | 차지 시작 | 같음 |
| 차지 시간 전에 뗌 | 비차지샷 발사 | 같음 |
| 차지 시간이 참 | 차지샷 자동 발사 후 대기. 다시 쏘려면 뗐다가 눌러야 함 | 차지샷 자동 발사 → 누르고 있으면 일정 시간(예: 0.1초) 뒤 다시 차지 시작 |
| 재차지 중 뗌 | - | 비차지샷 발사 (일반 차지 중 뗌과 같음) |

## 현재 상황
- 입력: `PlayerInputComponent.OnFire` 는 Press And Release 로 누름 → `RequestExecuteSkill("Attack")`, 뗌 → `RequestStopSkill("Attack")` 을 한 번씩 보낸다. 누르고 있는 상태는 따로 전달하지 않는다.
- `Fire` (`Assets/_Project/Scripts/Skill/Skills/Fire.cs`)
  - `Execute`: 차지 코루틴 `CoCharge` 시작 (`WaitForSeconds(_chargeTime)`, 기본 1초).
  - `CoCharge` 완료 → `Shoot(true)` 로 차지샷 자동 발사. 이후 차지 상태가 아니므로 뗌(`Stop`)이 와도 아무것도 하지 않는다.
  - 누름이 다시 와야 `TryExecute` → `Execute` 로 차지가 시작된다.
- `Fire.prefab` 코스트: `CooldownCost._cooldown 0.1`, `HeatCost._heat 5` (차지샷은 `_chargedCostScale 2` 배). 코스트는 발사 순간 지불한다.
- `SkillMediator.NotifyFirePhase` 가 `ChargeStarted` / `Fired` / `Canceled` 를 `OnFireChargeChanged(bool)`, `OnFireRequested` 로 바꿔 알린다. 재차지 시작도 `ChargeStarted` 로 알리면 애니메이션·전투 쪽은 그대로 동작할 것으로 보인다 (작업 시 확인).

## 작업 범위 (예상)
- `Fire` 가 누르고 있는 상태를 기억하고, 자동 발사 후 대기 시간이 지나면 다시 차지 시작.
- 재차지 대기 시간 Inspector 노출.

## 열린 질문
- [ ] 재차지 대기 시간 기본값 (예시 0.1초). 기존 `CooldownCost._cooldown 0.1` 과 같은 값으로 묶는가, 따로 두는가
- [ ] 재차지 시작 시 조건·코스트 판정(`TryExecute`)을 다시 거치는가. 과열 등으로 실패하면 누르고 있는 동안 계속 재시도하는가
- [ ] 대기 시간 중에 떼면: 아무것도 하지 않는가 (비차지샷을 쏘지 않음)
- [ ] 누르고 있는 중에 Fire 가 비활성화됐다가(구르기·탑승 연출) 다시 켜지면 재차지를 이어가는가, 다시 눌러야 하는가
- [ ] 발사 시점 코스트 지불 실패(`Canceled`) 뒤에도 누르고 있으면 재차지하는가
