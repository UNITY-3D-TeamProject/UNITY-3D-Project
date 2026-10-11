# 13. 총기 과열에 따른 색상 변경

- **우선순위**: 낮음
- **상태**: 완료

## 목표
총기 과열 수치에 따라 총기 색을 바꿔 과열 상태를 보여준다.

## 현재 상황
- 과열 어트리뷰트 `CurrentHeat` / `MaxHeat` 가 있고, HUD 에 이미 표시된다 (`Assets/_Project/Scripts/UI/PlayerHudPresenter.cs:27-28`).
- `AttributeMediator._firePauseKey = "CurrentHeat"` 로 사격 중지와 연동된다.
- [06](06-heat-gauge-fix.md) 완료: Heat 는 0에서 사격마다 증가(+5, 차지샷 +10)하고, 사격 후 3초 뒤 0.1초마다 -2 로 냉각된다. `MaxHeat`(100) 도달 시 0까지 사격이 잠긴다.
- 과열 잠금 상태는 `HeatCost` 내부(`_isOverheated`)에만 있고 외부로 알리지 않는다. `CurrentHeat / MaxHeat` 비율만으로는 "잠금 중"을 구분할 수 없으므로, 잠금 색을 쓰려면 알림 경로를 새로 만들어야 한다 (`Assets/_Project/Scripts/Skill/Costs/HeatCost.cs`).

## 확정 사항
- 대상: 양팔 `RadicalBuster` 메시 2개(`RadicalBuster (3)`, `RadicalBuster (4)`)의 발광부.
- 방식: 연속 그라데이션. 0% = 현재 파랑, 100% = 빨강. 중간 색은 Gradient 를 인스펙터에 노출해 조정한다.
- 머티리얼: 발광(`_Emissive_*`) 속성을 바꾼다. 머티리얼은 `Assets/ThirdParty/`(Git 미포함)에 있으므로 에셋을 고치지 않고 런타임에 `MaterialPropertyBlock` 으로 바꾼다.
- 과열 최대치 깜빡임 등 추가 연출은 하지 않는다.
- 색 기준은 `CurrentHeat / MaxHeat` 비율이다.
- 과열 잠금 중 별도 색은 쓰지 않는다. 비율만 따른다 (잠금 중 냉각되면 색도 파랑 쪽으로 돌아간다).

## 조사 결과 (셰이더)
- 총기 머티리얼 `M_RadicalBuster_Blue.mat` 의 셰이더는 커스텀 셰이더 그래프 `ThirdParty/Tacko3D/RadicalRobotsMike/Art/Materials/M_Material_Parent.shadergraph` 다. `_BaseColor` / `_EmissionColor` 는 쓰지 않는다.
- Emission = `_Emissive_Power` × Saturation(`_Emissive_Color` × Hue(`_Emissive_Texture`, `_Emissive_HueShift`), `_Emissive_Saturation`).
- 발광 텍스처 `T_RadicalBuster_Emissive.png` 의 원래 색은 주황빛 빨강(약 RGB 204,56,18)이고, Blue 머티리얼은 `_Emissive_HueShift` 233.24 로 파랑을 만든다. `_Emissive_Color` 는 곱해지므로 빨강을 넣으면 파란 발광과 곱해져 어두워진다.

## 구현 결과
- `Visual.EmissionHueChanger` (`Scripts/Visual/`): 비율(0~1)의 Gradient 색에서 Hue 만 뽑아 `_Emissive_HueShift = Hue − _sourceHue(12°)` 를 `MaterialPropertyBlock` 으로 적용한다. 열·어트리뷰트는 모른다.
- `Mediator.SubMediators.WeaponVisualMediator`: `CurrentHeat` / `MaxHeat` 변경 시 비율을 계산해 `EmissionHueChanger.SetRatio` 로 전달한다. `CharacterMediator` 가 자식 `MediatorBase` 를 자동 수집하므로 `CharacterMediator` 수정은 없다.
- `Prefabs/Actions/WeaponVisualPrefab.prefab` 을 `Player.prefab` 에 중첩하고 `_renderers` 에 `RadicalBuster (3)`, `(4)` 를 지정했다.
- Gradient 기본값: 0% 파랑(Hue 약 245.5°, 원래 머티리얼과 동일) → 100% 빨강. 색감 조정은 인스펙터의 Gradient / `_sourceHue` 로 한다.

## 열린 질문
- [x] 색이 바뀌는 대상 → 확정 사항 참고
- [x] 색 변화 방식 → 확정 사항 참고
- [x] 머티리얼 방식 → 확정 사항 참고
- [x] 과열 최대치 추가 연출 → 안 함
- [x] 색 기준 `CurrentHeat / MaxHeat` 비율 → 예
- [x] 과열 잠금 중 별도 색 → 안 씀

## 완료 기준
- 플레이 모드에서 과열 수치에 따라 총기 색이 바뀌는지 확인.
