# 보스A 에너지 실드가 보이지 않던 문제 (intent-093 후속, AI_TODO2 보스-33)

## 작업 요약 및 방법
- 원인: `BossA.prefab` 의 `EnergyShield` 는 `Assets/ThirdParty/VFX/AI/HoloShield/Effect_09_HoloShield.prefab` 인스턴스이고, 자식 `Effect_09_Shield` 의 `NewMaterialChange`(`m_timeToReduce=4`)가 4초 뒤 마스크를 줄이다 0이 되면 `Destroy(gameObject)` 로 구체를 지운다. 보스전이 시작될 때는 이미 사라져 있다.
- 수정: `BossA.prefab` 에서 `Effect_09_Shield` 의 `NewMaterialChange` 를 비활성(프리팹 오버라이드 `m_Enabled: 0`). 코드 변경 없음.

## 결정 사항
- ThirdParty 스크립트는 수정하지 않고 인스턴스 오버라이드로만 끈다.

## 현재 상태 및 이슈
- 오버라이드 저장 확인. Play 검증은 불완전: 임시 인스턴스로 14초 뒤 구체가 살아 있고 `_MaskCutOut=1` 이었지만, Play 가 중간에 끝나 있었을 수 있어 확정 증거는 아님. 보스전 육안 확인·게이트 루프 미실행, 커밋 전.
- `ThirdParty` 는 Git 에 올라가지 않으므로 다른 PC 에서는 `EnergyShield` 가 깨진다. 공유하려면 `_Project` 로 옮기거나 자체 실드로 교체해야 한다(사용자 결정 필요).
- 보스전에서 실드 표시/해제와 그로기 중 모양 확인 필요(intent-093 열린 질문 그대로).

## 다음 할 일
- 보스전 육안 확인 후 보스-33 체크. ThirdParty 의존 처리 방침 결정.
