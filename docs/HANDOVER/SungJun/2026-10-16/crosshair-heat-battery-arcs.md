# 크로스헤어 열·배터리 원호를 실제 플레이어 값에 연결 (2026-10-16)

## 작업 요약 및 방법
- 사용자 요청: UITestScene 크로스헤어의 배터리(파랑)·열(빨강) 게이지를 실제 값과 연결.
- 코드 `Assets/_Project/Scripts/UI/PlayerHudView.cs`
  - 중첩 구조체 `SArcCell`(`FillImages` = 원호·심, `GlowImage` = 글로우) 추가.
  - `[Header("Crosshair Arcs")]` `_heatArcCells`·`_heatPercentText`·`_batteryArcCells`·`_batteryPercentText` 추가.
  - `SetHeat`·`SetBattery`가 기존 `_heatSlider`·`_batterySlider` 갱신 뒤 `SetArcCells` 호출.
    - 비율 = current / max (0~1), 칸 i 값 = clamp01(비율 × 칸 수 − i) → 2칸이면 0~50% 첫 칸, 50~100% 둘째 칸.
    - 원호·심 fill = 0.25 × 칸 값, 글로우 fill = 원호 fill + 12/360(양 끝 6°씩 늘린 몫), 칸 값 0이면 글로우 0.
    - 퍼센트 글자 = 비율 반올림 `NN%`.
  - Presenter(`PlayerHudPresenter`)는 이미 `CurrentHeat/MaxHeat`·`CurrentBattery/MaxBattery` 변경 시 `SetHeat`·`SetBattery`를 부르고 있어 변경 없음.
- Test `Prefabs/UI/MainScene/Canvas.prefab` 원본 수정(`LoadPrefabContents`, Unity MCP `eval`): `PlayerHudRoot/PlayerHUD`의 View에
  - 열: [0] `HeatArc`·`HeatCore` + `HeatGlow`, [1] `HeatArc2`·`HeatCore2` + `HeatGlow2`
  - 배터리: [0] `BatteryArc`·`BatteryCore` + `BatteryGlow`, [1] `BatteryArc2`·`BatteryCore2` + `BatteryGlow2`
  - 퍼센트: `HeatLabel/Value`, `BatteryLabel/Value`
- 검증: 컴파일 에러 없음. Play 모드에서 `_Project/Prefabs/Player/Player.prefab` 생성 → `PlayerHudPresenter.SetPlayer` 수동 연결 → `SetAttribute`로 열 28%(3시에서 12시 쪽 56%)·배터리 75%(9시→6시 가득 + 6시→3시 절반), 열 100%·배터리 0%(글로우까지 0) Game 뷰·fill 값 확인.

## 결정 사항
- 사용자 요청으로 이름을 쉽게 바꿈(HP의 `Cell` 표현에 맞춤): `SArcSegment` → `SArcCell`, `ArcImages` → `FillImages`, `SetArcSegmentFill` → `FillArcCell`, 상수 `ARC_CELL_MAX_FILL`(한 칸 최대 fill 0.25)·`GLOW_EXTRA_FILL`(글로우 추가 12/360). 이름을 바꾼 뒤 `Canvas.prefab` 참조를 다시 연결하고 Play로 값을 다시 확인함.
- 칸 나눔·글로우 규칙은 크로스헤어 작업 기록(crosshair-half-arcs.md)에서 정한 그대로 코드화. 칸 수는 배열 길이로 정해져 칸을 늘려도 코드 수정 불필요(단 칸 하나 = 90° 상수 0.25 기준).
- 채움 방향은 이미지 설정(원점·회전)이 결정: 열 3시 → 12시 → 9시, 배터리 9시 → 6시 → 3시.

## 현재 상태 및 이슈
- 같은 `Canvas.prefab`을 쓰는 꺼진 `Canvas_CrosshairFullRing`은 반원 하나(fill 0.5) 구조라, 켜서 쓰면 이 규칙(최대 0.25)으로 덮여 깨진다. 지금은 꺼져 있어 영향 없음.
- UITestScene에는 GameManager가 없어 Play 시 UIManager 에러 → Error Pause로 에디터가 멈춤. 위 검증은 일시정지 해제 후 진행.
- `Canvas_Shift` 등 다른 시안 Canvas는 연결하지 않음.
- Test 저장소 d5c1c61 커밋(`Canvas.prefab`, push 안 함). 씬 파일 변경 없음.

## 다음 할 일
- 차지샷 바(`ChargeBar`)도 같은 방식으로 View 연결(intent-029, 진행률 연결 방식 결정 대기).
- 이 HUD Canvas를 GameManager가 있는 실제 씬에 넣어 스폰 플레이어와 자동 연결 확인.
