# 스킬 카드 3장 원형 쿨타임 바·남은 시간 연결 (2026-10-17)

## 작업 요약 및 방법
- 사용자 요청: 스킬 쿨타임을 PlayerFacade에서 받아 UITestScene 스킬 카드 3장(SCAN·ROLL·BLUETOOTH) 바에 연결.
- 경로: `PlayerFacade.OnSkillUsed(이름, 쿨타임)` → `PlayerHudPresenter.HandleSkillUsed`(종료 시각 기록) → `Update`마다 `UpdateSkillCooldownHuds` → `PlayerHudView`. 경로 자체는 기존 그대로.
- 코드
  - `PlayerHudPresenter.SSkillCooldown`에 `RemainingTime`(남은 초, 사용 가능하면 0) 추가, View 호출에 남은 초를 함께 넘김.
  - `PlayerHudView`: `SetRollCooldown/SetBluetoothCooldown/SetScanCooldown(remainingRatio, remainingTime)`로 매개변수 추가. `[Header("Skill Cards")]` `_rollCooldownBar`·`_rollCooldownText`·`_bluetoothCooldownBar`·`_bluetoothCooldownText`·`_scanCooldownBar`·`_scanCooldownText` 추가. `SetCooldownGauge`가 원형 바 fill = 1 − 남은 비율, 글자 = 0.1초 단위 올림 `N.Ns`(사용 가능하면 빈 글자). 기존 쿨타임 슬라이더 3개도 계속 갱신.
- Test `Prefabs/UI/MainScene/Canvas.prefab`: View에 `Skills/SkillCard_{Roll,Bluetooth,Scan}/IconArea/CooldownBar`와 `CooldownText` 연결.
- 검증: 컴파일 에러 없음. Play에서 `_Project/Prefabs/Player/Player.prefab` 생성 → `SetPlayer` 수동 연결 → 실제 스킬 쿨타임(Scan 6, SpawnVehicle 3, Roll 0.3)으로 `OnSkillUsed` 발생(리플렉션) → 프레임을 직접 넘겨 2초 뒤 Scan 0.34·4.0s, Bluetooth 0.67·1.0s, Roll 가득·빈 글자, 7초 뒤 Scan 가득·빈 글자를 fill 값과 Game 뷰로 확인.

## 결정 사항
- 원형 바는 쓰면 빈 채로 시작해 쿨타임이 끝나면 꽉 참. 평소(사용 가능)에는 꽉 차 있음. (사용자 확정)
- 숫자는 쿨 중에만 남은 초를 소수 한 자리로, 사용 가능하면 숨김. 끝나기 직전 0.0s가 보이지 않게 0.1초 단위 올림. (사용자 확정)
- 스킬 이름 매칭은 Presenter 기존 값 그대로: Roll = `Roll`, Bluetooth = `SpawnVehicle`, Scan = `Scan`.

## 현재 상태 및 이슈
- Roll 쿨타임이 0.3초라 바가 거의 바로 다시 참.
- UITestScene에는 GameManager가 없어 Play 시작 시 에러 → Error Pause, 그리고 에디터가 뒤에 있으면 프레임이 안 넘어감. 검증은 `EditorApplication.Step()`과 `Time.timeScale`로 프레임을 넘겨서 함.
- 카드의 소모값(`Cost/Value` 20·25·30)은 고정 글자 그대로(이번 범위 아님).
- Test 저장소 3ac8c3a 커밋(`Canvas.prefab`, push 안 함).

## 다음 할 일
- 필요하면 카드 소모값을 스킬의 실제 Cost와 연결.
- GameManager가 있는 실제 씬에서 키 입력으로 스킬을 써서 쿨타임 표시 확인.
