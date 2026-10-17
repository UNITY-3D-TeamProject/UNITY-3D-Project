# 차지샷 바를 PlayerFacade 차지 진행도에 연결 (2026-10-17)

## 작업 요약 및 방법
- 사용자 요청: 크로스헤어 오른쪽 아래 칸형 차지 바(`Crosshair/ChargeBar`)를 PlayerFacade의 차지샷 게이지와 맞추기. 차지 중에만 바가 보이고 빨간 칸이 차오르게.
- 이미 있던 경로 사용: `Fire.ChargeRatio` → `SkillMediator.FireChargeRatio` → `CharacterMediator` → `PlayerFacade.FireChargeRatio`(0~1), 차지 시작·끝 알림 `PlayerFacade.OnFireChargeChanged(bool)`. Fire·Mediator·Facade는 변경 없음.
- 코드
  - `PlayerHudPresenter`: `OnFireChargeChanged` 구독/해제 추가. `HandleFireChargeChanged(true)` → `_isFireCharging` 켜고 바 표시 + 즉시 한 번 갱신, `false` → 숨김. `Update`에서 차지 중일 때만 `UpdateFireChargeHud`(FireChargeRatio → View). `ResetFireChargeHud`(상태 끄고 바 숨김)를 OnEnable·OnDisable·SetPlayer에서 호출.
  - `PlayerHudView`: `[Header("Charge Bar")]` `_chargeBarRoot`·`_chargeCellImages`·`_chargeCellFilledColor`(1, 0.42, 0.31, 1)·`_chargeCellEmptyColor`(같은 색, 알파 0.15) 추가. `SetChargeBarVisible(bool)`, `SetChargeRatio(ratio)`(칸 값 = clamp01(진행도 × 칸 수 − i), `Color.Lerp(빈 색, 찬 색, 칸 값)`).
- Test `Prefabs/UI/MainScene/Canvas.prefab`: View에 `Crosshair/ChargeBar`와 `Segments/Seg (0~9)` 연결.
- 검증: 컴파일 에러 없음. Play에서 `Player.prefab` 생성·`SetPlayer` 연결 후 실제 `Fire.TryExecute`/`Stop` 호출, `EditorApplication.Step`·`Time.timeScale`로 프레임을 넘겨 확인.
  - 차지 전 바 숨김 → 차지 시작 시 표시
  - 진행도 0.32 → 앞 3칸 찬 색, 4번째 칸 알파 0.32, 나머지 0.15 (Game 뷰 캡처 확인)
  - 중간에 떼면 일반샷 발사 후 숨김, 끝까지 누르면 차지샷(`IsCharged` true) 자동 발사 후 숨김

## 결정 사항
- `Fire` 판정(차지 시간을 다 채워야 차지샷)은 바꾸지 않고 바만 연결.
- 칸은 HP 칸과 같은 나눔 방식. 칸 안은 이미지 채움이 아니라 빈 색 → 찬 색으로 밝아지는 방식(기존 칸 이미지·색 그대로 사용).

## 현재 상태 및 이슈
- 바 가운데 흰 눈금 `ThresholdMark`(50%)는 사용자 결정으로 그대로 둠(차지샷은 100%에서 나감).
- 차지샷 자동 발사 후 계속 누르고 있을 때 재차지(0.1초 뒤)가 시작되면 바가 다시 나타남(이벤트 그대로 따름).
- 커밋: 팀 저장소 `PlayerHudView.cs`·`PlayerHudPresenter.cs`, Test 저장소 `Canvas.prefab`(29b27e5).

## 다음 할 일
- GameManager가 있는 실제 씬에서 마우스 입력으로 차지 바 확인.
