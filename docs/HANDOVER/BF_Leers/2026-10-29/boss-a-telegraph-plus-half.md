# 보스A 예고 시간 +0.5초 (AI_TODO2 보스-22)

- 날짜: 2026-10-29
- 관련: [intent-091](../../../intent/intent-091-boss-a-telegraph-plus-half.md), [AI_TODO2](../../../Jengbin/TODO/AI_TODO2.md) 보스-22·보스-20

## 1. 작업 요약 및 방법
- 보스 권장 순서 1번(보스-22 → 보스-20) 중 보스-22 만 진행.
- `Prefabs/AI/Boss/BossA.prefab` 의 예고 시간 값 9개를 Unity Editor MCP(`eval`, `LoadPrefabContents` → `SerializedObject` → `SaveAsPrefabAsset`)로 +0.5초 수정. 기존 값이 예상 값일 때만 바꾸도록 가드.
  - A1·A1Double `_telegraphSeconds` 1.2 → 1.7
  - A2·A5·A6 `_telegraphSeconds` 1.5 → 2.0
  - A4 `_aimSeconds` 1.0 → 1.5, `_slamTelegraphSeconds` 1.5 → 2.0
  - ShoulderCannon_L·R `_telegraphSeconds` 1.0 → 1.5
- `git diff` 로 프리팹 변경이 위 9줄뿐임을 확인. 씬에는 이 값들의 오버라이드가 없음(grep 확인).
- 실행자: Claude 직접(MCP). Codex 위임 대신 직접 한 이유: 값 9개 변경이라 위임 비용이 더 큼.

## 2. 결정 사항
- 보스A 예고 시간만 늘린다. 보스B(도약·돌진·콤보·회피)는 제외 (사용자 결정).
- 최종 단계 `TelegraphScale` 0.8 은 그대로 → 최종 단계 실제 증가폭은 +0.4초(어깨포는 배율 미적용이라 +0.5초).
- `GroggyPalm`(예고 0)과 A6 `_strikeInterval`(1타→2타 간격)은 예고 시간이 아니라 제외.
- 보스-20 "내려치기 간격"은 1단계 패턴 사이 텀(`BossController._handStayPhase1`, 현재 2초)으로 확정, 늘릴 양은 보스-22 를 플레이해 본 뒤 결정.

## 3. 현재 상태 및 이슈
- 코드 변경 없음. 직접 플레이 확인·게이트 루프 미실행, 커밋 전.
- `ProjectSettings/ShaderGraphSettings.asset` 은 줄바꿈 경고만 있고 내용 diff 없음(이번 작업과 무관).

## 4. 다음 할 일 (Next Steps)
- 보스전 직접 플레이로 예고가 길어진 체감 확인, 예고 끝과 손이 내려치는 시점이 맞는지 확인.
- 보스-20: `_handStayPhase1` 늘릴 양 결정 후 적용.
- 이후 밸런싱-2 보스 HP 체감 재확인.
