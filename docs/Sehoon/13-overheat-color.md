# 13. 총기 과열에 따른 색상 변경

- **우선순위**: 낮음
- **상태**: 대기

## 목표
총기 과열 수치에 따라 총기 색을 바꿔 과열 상태를 보여준다.

## 현재 상황
- 과열 어트리뷰트 `CurrentHeat` / `MaxHeat` 가 있고, HUD 에 이미 표시된다 (`Assets/_Project/Scripts/UI/PlayerHudPresenter.cs:27-28`).
- `AttributeMediator._firePauseKey = "CurrentHeat"` 로 사격 중지와 연동된다.
- [06](done/06-heat-gauge-fix.md) 완료: Heat 는 0에서 사격마다 증가(+5, 차지샷 +10)하고, 사격 후 3초 뒤 0.1초마다 -2 로 냉각된다. `MaxHeat`(100) 도달 시 0까지 사격이 잠긴다.
- 과열 잠금 상태는 `HeatCost` 내부(`_isOverheated`)에만 있고 외부로 알리지 않는다. `CurrentHeat / MaxHeat` 비율만으로는 "잠금 중"을 구분할 수 없으므로, 잠금 색을 쓰려면 알림 경로를 새로 만들어야 한다 (`Assets/_Project/Scripts/Skill/Costs/HeatCost.cs`).

## 열린 질문
- [ ] 색이 바뀌는 대상: 총기 메시 전체? 특정 파츠(발광부)? 총구 이펙트?
- [ ] 색 변화 방식: 연속 그라데이션(0~100%)인가, 구간별(정상·경고·과열)인가? 색상 값은?
- [ ] 머티리얼 방식: Base Color 변경인가, Emission 변경인가? (총기 머티리얼 셰이더 확인 필요)
- [ ] 과열 최대치(사격 불가) 상태에서 깜빡임 같은 추가 연출이 필요한가?
- [ ] [06. Heat 게이지 구조 수정](done/06-heat-gauge-fix.md) 이후 진행한다. 색 기준은 `CurrentHeat / MaxHeat` 비율로 잡으면 되는가?
- [ ] 과열 잠금(0까지 사격 불가) 중에는 별도 색을 쓰는가?

## 완료 기준
- 플레이 모드에서 과열 수치에 따라 총기 색이 바뀌는지 확인.
