# 보스 A 칸 예고 가운데 채움 보정 (Map_4_Security)

관련 intent: [intent-053](../../../intent/intent-053-map4-boss-tiles.md), [intent-047](../../../intent/intent-047-bossa-telegraph-fill-in.md)

## 1. 작업 요약 및 방법
- 문제: `Map_4_Security`의 칸 예고 `WarningSignals`(칸 테두리 신호등 띠 메시, 기준점 = 칸 모서리, XZ 평면)가 `BossTile.ApplyFill`의 localScale x·y 확대 때문에 모서리에서 X 방향으로만 늘어났다.
- 수정: `BossTile.cs`만 변경.
  - `CacheFillPivot()`: 예고 메시 bounds 중심을 채움 기준점으로, bounds가 가장 얇은 축을 뺀 두 축을 채움 축으로 캐시한다(MeshFilter 없으면 기존 x·y 축, 기준점 0).
  - `ApplyFill()`: 채움 축만 progress 배로 줄이고, 메시 중심이 부모 기준 같은 자리에 남도록 localPosition을 보정한다.
- 실행자: Claude 직접 수행. 예외 이유: 이 PC에 Codex CLI 없음(`which codex` 실패).

## 2. 결정 사항
- 메시·씬은 고치지 않고 코드로 기준점을 보정한다(사용자 요청: 가운데서 시작해 가장자리까지 채움).
- 테스트 씬의 쿼드 예고(중심 0, XY 평면)는 기존과 같은 결과가 나온다.

## 3. 현재 상태 및 이슈
- `gate.cjs check recompile` 통과(에러 0).
- 저장되지 않는 임시 오브젝트로 eval 확인: progress 0.01/0.5/1에서 월드 중심 (328.64, 0.13, -14.00) 고정(= 칸 중심), 크기 0.11 → 5.72 → 11.45 m, 끄면 원래 위치·스케일 복구. 쿼드는 위치 그대로·x·y만 확대. 씬 dirty 없음.
- **Play 육안 확인·게이트 루프 미실행, 커밋 전.**
- `WarningSignals`는 테두리 띠라, "채움"은 띠가 가운데에서 커져 칸 가장자리 위치까지 펼쳐지는 모양이다(면 전체가 칠해지지는 않음).

## 4. 다음 할 일 (Next Steps)
- Play에서 A 패턴 예고가 칸 가운데서 커지는지 육안 확인.
- 면 전체를 채우는 연출이 필요하면 별도 예고 메시/쿼드 추가를 맵 담당과 결정.
