# 보스-32 그로기 손 마커 크기 수정 (intent-092 후속)

## 작업 요약 및 방법
- 마커가 안 보이던 원인: `SOEffectData_BossHandHint._scale` 이 1 이라 `Marker 6 Arrows Loop`(파티클 startSize 1.2)가 월드 1.2m 로 나옴. 손 콜라이더는 11×20×11m, 손 릴레이 lossyScale 1 이라 약 10배 작았다.
- 코드·참조는 정상(ThirdParty 프리팹이 이 PC 에서는 로드됨). MCP 로 SO `_scale` 을 1 → 10 으로만 변경.

## 결정 사항
- 크기는 SO `_scale` 로 조정한다(코드 수정 없음). 10 은 초기값.

## 현재 상태 및 이슈
- 보스전 육안 확인 안 함(마커 가시성·그로기 종료 시 소멸 미확인). 게이트 루프 미실행, 커밋 전.
- SO 가 `Assets/ThirdParty/VFX/AI/HandHint/Marker 6 Arrows Loop.prefab`(Git 미포함)을 참조한다. 다른 PC 에서는 프리팹이 null 이라 `PlayHandHints` 가 조용히 return → 마커 없음. 공유하려면 `_Project` 로 복사 필요.

## 다음 할 일
- 그로기 구간 플레이로 마커 크기·위치 확인(10 이 과하거나 부족하면 `_scale` 조정). 보스-30 과 함께 확인.
- ThirdParty 의존 해소 여부 결정.
