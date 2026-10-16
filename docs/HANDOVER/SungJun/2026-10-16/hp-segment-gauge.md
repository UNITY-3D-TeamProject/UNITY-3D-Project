# HP 패널 10칸 게이지화 + 실제 플레이어 HP 연결 (2026-10-16)

## 작업 요약 및 방법
- 사용자 요청: UITestScene 왼쪽 아래 HP 패널을 실제 플레이어 HP와 연결. 칸은 게이지 에셋 10개로, 전체 체력을 10으로 나눠 한 칸 = HP 10으로 계산.
- 코드 `Assets/_Project/Scripts/UI/PlayerHudView.cs`
  - `[Header("HP Cells")]` `_hpCellSliders`(Slider[])·`_hpPercentText`(TMP_Text) 추가.
  - `SetHp(current, max)`가 기존 `_hpSlider` 갱신 뒤 `SetHpCells` 호출. 한 칸 HP = `max / 칸 수`, 칸 i 값 = `(current - 한 칸 HP × i) / 한 칸 HP`를 0~1로 자름 → 칸 안 부분 채움. 퍼센트 = `current / max` 반올림.
  - Presenter·UIManager 경로(`PlayerFacade.OnAttributeChanged` → `PlayerHudPresenter.UpdateHpHud` → `View.SetHp`)는 변경 없음.
- Test `Prefabs/UI/MainScene/Canvas.prefab` 원본 수정(`LoadPrefabContents`, Unity MCP `eval`)
  - `HpPanel/Segments/Seg (0~9)` 삭제, 같은 자리(26×22, 간격 30)에 Shift `Prefabs/Slider/Slider.prefab` 복제본 `HpCell (0~9)` 배치.
  - 칸에서 제거: Handle Slide Area, Raycast Trigger, Value 글자, `SliderManager`, `UIElementSound`, `Animator`, `UIManagerImage` 전부. Background의 Glow는 끔. Slider `interactable` false, Transition·Navigation None.
  - Background·Fill = Cut Frame Filled(Sliced), 빈 색 (0.10, 0.28, 0.36), 채움 색 (0.30, 0.85, 1). Fill Area·Fill 여백·위치 0.
  - PlayerHUD의 View에 `HpCell` 10개와 `HpPanel/Percent` 연결.
  - `PlayerHudRoot`가 꺼져 있어 Presenter가 구독을 못 하던 문제 → `PlayerHudRoot` 켬, 예전 임시 `HpSlider`·`BatterySlider`·`HeatSlider`와 Legacy 글자 3개는 끔(값 갱신은 계속 됨).
- 검증: Play 모드에서 `_Project/Prefabs/Player/Player.prefab`을 생성해 `PlayerHudPresenter.SetPlayer`로 연결, `SetAttribute("CurrentHp")`로 100 → 10칸 100%, 73 → 7칸 + 0.3, 65 → 6칸 + 반 칸 65% 화면 확인. 컴파일 에러 없음.

## 결정 사항
- 한 칸 HP는 고정 10이 아니라 `MaxHp / 10` (현재 플레이어 MaxHp 100 → 10). MaxHp가 바뀌어도 10칸 유지.
- 칸 몫보다 적게 남은 칸은 부분적으로 참 (5 데미지 = 반 칸).
- `_hpSlider`는 `_Project/Prefabs/UI/Canvas.prefab`이 아직 쓰므로 남김.
- Shift `UIManagerImage`는 Awake에서 스스로 다시 켜지고 테마색으로 덮어쓰므로, 색을 고정할 칸에서는 비활성이 아니라 제거해야 함.

## 현재 상태 및 이슈
- UITestScene에는 GameManager가 없어 Play 시 UIManager가 "씬에 GameManager가 없습니다" 에러를 내고 자동 연결은 안 됨(위 검증은 수동 SetPlayer). 실제 맵 씬(Map_0~4)은 `_Project` Canvas를 쓰고 있어 이 HUD가 아직 없음.
- `View.Hide()`는 `PlayerHUD`만 끄므로 `HpPanel`은 같이 숨지 않음.
- `Canvas_Shift` 등 다른 시안 Canvas는 손대지 않음(Seg 고정 이미지 그대로).
- 씬 파일은 변경 없음(프리팹만 저장). Test 저장소 4f6e542 커밋(push 안 함).

## 다음 할 일
- 이 HUD Canvas를 실제 게임 씬(또는 GameManager가 있는 테스트 씬)에 넣어 스폰된 플레이어와 자동 연결 확인.
- 배터리·열 원호, 차지 바도 같은 방식으로 View 연결.
