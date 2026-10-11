# 06. Heat 게이지 구조 수정

- **우선순위**: 높음
- **상태**: 완료
- **관련 작업**: [04. Fire 차지샷](04-fire-charge-shot.md), [13. 과열 색상 변경](../13-overheat-color.md)

## 목표
지금은 사격하면 Heat 가 줄어든다(버그). 이를 아래 구조로 바로잡는다.

## 확정 사항
- Heat 게이지는 **0에서 시작**한다.
- 사격할 때마다 Heat 가 **차오른다**.
- 일정 시간 사격하지 않으면 Heat 가 **줄어든다**.
- Heat 가 **최대치에 도달하면 0이 될 때까지 사격을 막는다**.
- 구현 방식: Heat 를 올리는 코스트(예: `HeatCost`)를 **새로 만든다**. `ResourceCost` 는 건드리지 않는다.
- Heat 증가는 기존 지불 경로를 그대로 쓴다: `HeatCost.Pay` 가 `requestPay(key, -증가량)` 을 호출한다 (음수 차감 = 증가).
- 수치: 1발당 **+5**, `MaxHeat` 는 플레이어 SO 값(**100**), 냉각은 사격 후 **3초** 대기 뒤 **0.1초마다 -2**.
- 차지샷은 Heat 증가량 **2배** (배수는 Inspector 노출).
- 마지막 발이 `MaxHeat` 를 넘으면 **그 발은 쏘고** Max 에서 잠근다 (클램프로 Max 고정).
- 잠금 중 냉각 속도는 **평소와 같다**.
- 잠금 상태는 `HeatCost` 내부에만 둔다. 외부 알림·HUD 표시는 [13](../13-overheat-color.md) 으로 미룬다.
- 알려진 한계(허용): `CurrentHeat` 는 씬 전환 시 저장·복원되지만 잠금 상태는 저장되지 않는다. 잠금 중 씬을 넘어가면 다음 씬에서 바로 사격할 수 있다.

## 현재 상황
- 사격 코스트는 `ResourceCost` 다. 값을 **깎는** 방식이고, `CanPay` 는 `현재값 >= 코스트` 로 판정한다 (`Assets/_Project/Scripts/Skill/Costs/ResourceCost.cs`).
- 실제 값 변경은 `SkillMediator.OnRequestPay` → `OnPayRequested` 이벤트를 구독한 쪽에서 처리한다 (`Assets/_Project/Scripts/Mediator/SubMediators/SkillMediator.cs:149`).
- 냉각(자동 감소)은 `AttributeRegenerator` 의 규칙(주기마다 `SOAttributeEffect` 적용, 정지 후 재개 대기 시간)으로 처리한다. 사격 시 `AttributeMediator.PauseRegenOnFire()` 가 `CurrentHeat` 회복을 멈춘다.
- 어트리뷰트 키: `CurrentHeat`, `MaxHeat`. HUD 에 이미 표시된다.

## 작업 범위 (예상)
1. `HeatCost` 추가 (`ISkillCost` 구현)
   - `CanPay`: 과열 잠금 상태가 아니면 지불 가능.
   - `Pay`: `CurrentHeat` 를 증가시킨다. `MaxHeat` 에 닿으면 잠금.
   - `CurrentHeat` 가 0이 되면 잠금 해제.
2. 플레이어 `Fire` 스킬의 코스트를 `ResourceCost` → `HeatCost` 로 교체 (프리팹 작업).
3. 냉각 규칙이 `CurrentHeat` 를 **감소**시키도록 `AttributeRegenerator` 설정(SO 효과)을 확인·수정.
4. `CurrentHeat` 초기값이 0인지 확인 (스폰 시 적용하는 효과 SO 포함).

## 열린 질문
- [x] 1발당 Heat 증가량과 `MaxHeat` 값은? 현재 프리팹 값을 유지하는가? → +5, `MaxHeat` 는 플레이어 SO(100)
- [x] 냉각: "일정 시간"(사격 후 감소 시작까지 대기)과 감소 속도는? 현재 Regenerator 설정을 유지하는가? → 대기 3초(처음 1초에서 변경), 0.1초마다 -2
- [x] 과열 잠금 중 냉각 속도는 평소와 같은가, 더 빠른가/느린가? → 평소와 같다
- [x] 마지막 1발로 `MaxHeat` 를 넘으면: 그 발은 쏘고 잠금인가, 넘을 발은 아예 못 쏘는가? → 쏘고 잠금
- [x] 잠금 상태 지불 요청: 잠금 여부를 `HeatCost` 내부에 둘지, 어트리뷰트(예: 별도 플래그)나 이벤트로 외부(HUD, 13번 색상)에 알려야 하는가? → `HeatCost` 내부. 외부 알림은 13번으로 미룬다
- [x] 잠금 중임을 HUD 에 표시해야 하는가? (이번 범위 포함 여부) → 13번으로 미룬다
- [x] 차지샷/비차지샷 Heat 증가량 ([04](04-fire-charge-shot.md) 에서 이월) → 차지샷 2배, 배수 Inspector 노출
- [x] Heat 를 올리는 방법 → 기존 지불 경로에 음수 차감
- [x] 씬 전환 시 잠금 상태 유실 → 허용

## 완료 기준
- 플레이 모드 확인: 시작 시 0 → 사격 시 증가 → 사격 중지 후 일정 시간 뒤 감소 → 최대치 도달 시 0까지 사격 불가 → 0 도달 후 다시 사격 가능.
- 컴파일 에러 없음.
