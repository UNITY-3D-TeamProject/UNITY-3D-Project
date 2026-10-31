# 과열 중 히트 원호 진한 색 + 빠른 점멸 (intent-104)

## 1. 작업 요약 및 방법
- 히트 게이지가 100%에 닿아 사격이 잠긴 뒤 열이 0까지 내려가는 동안, 크로스헤어 빨간 히트 원호(2칸 반원)를 진한 빨강으로 바꾸고 빠르게 점멸시킴.
- `PlayerHudPresenter.UpdateHeatHud`: 열 ≥ 최대면 `_isHeatOverheated = true`, 열 ≤ 0이면 `false` (`HeatCost`와 같은 규칙). 매 갱신마다 `_view.SetHeatOverheated(...)` 호출. 플레이어 교체·해제 시 초기화.
- `PlayerHudView`:
  - `[Header("Overheat")]` `_overheatColor`(0.85, 0.04, 0.04), `_overheatBlinkInterval`(0.1초), `_overheatBlinkMinAlpha`(0.15).
  - `Awake`에서 `_heatArcCells`의 원호·심·빛 번짐 이미지 6장과 원래 색을 저장.
  - `SetHeatOverheated(true)` → `Update`에서 진한 색으로 칠하고 0.1초마다 켜짐/꺼짐(꺼짐은 투명도 15%). 이미지별 원래 투명도 비율 유지.
  - `SetHeatOverheated(false)` → 저장한 원래 색으로 복구.

## 2. 결정 사항
- 과열 상태는 `HeatCost` 내부 필드라 외부에서 읽을 수 없음 → 스킬 파트 코드를 건드리지 않고 HUD가 같은 규칙으로 판단.
- 프리팹은 수정하지 않음. 기존 원호 이미지 색만 바꿈 (새 필드는 코드 기본값으로 들어감).
- 경고음은 플레이어 파트 담당이라 제외.

## 3. 현재 상태 및 이슈
- 리컴파일 에러 0.
- `Canvas_InGame.prefab` 격리 로드로 과열 켬 → 6장 진한 색, 끔 → 원래 색 복구, 배터리 원호 영향 없음 확인.
- Play에서 실제 과열·점멸 모습은 미확인. 커밋 안 함.
- `HeatCost`의 과열 규칙이 바뀌면 Presenter 판단도 같이 바꿔야 함.

## 4. 다음 할 일 (Next Steps)
- TestScene Play에서 연사로 100% 채워 점멸·복구 확인, 색·속도 값 조정.
- 확인되면 intent-104를 resolved로 바꿔 `docs/intent/clear/`로 이동.
