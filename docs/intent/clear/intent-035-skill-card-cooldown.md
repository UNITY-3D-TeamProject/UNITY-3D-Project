---
id: intent-035
title: 스킬 카드 3장의 원형 쿨타임 바·남은 시간을 PlayerFacade 스킬 사용 이벤트에 연결
part: UI / HUD
status: resolved
created: 2026-10-17
resolved: 2026-10-17
---

## 문제 (Problem)
UITestScene `Canvas/Skills`의 스킬 카드 3장(SCAN·ROLL·BLUETOOTH)에 있는 원형 쿨타임 바(`IconArea/CooldownBar`, 0.7/0.75/0.8)와 남은 시간 글자(`CooldownText`, 2.8s/1.5s/4.0s)가 고정값이다. `PlayerHudPresenter`는 이미 `PlayerFacade.OnSkillUsed(이름, 쿨타임)`로 남은 비율을 계산하지만, View의 `_rollCooldownSlider` 등 슬라이더 3개는 비어 있어 화면에 아무것도 보이지 않는다.

## 기대 결과 (Proposed outcome)
- 데이터 경로는 기존 그대로: `PlayerFacade.OnSkillUsed` → `PlayerHudPresenter`(종료 시각 기록, 매 프레임 갱신) → `PlayerHudView`.
- 원형 바: 스킬을 쓰면 빈 채로 시작해 쿨타임이 끝나면 꽉 찬다 (fill = 1 − 남은 비율). 평소(사용 가능)에는 꽉 차 있다. (사용자 확정)
- 숫자: 쿨 중에만 남은 초를 소수 한 자리로 표시(예: 2.8s), 사용 가능하면 빈 글자. (사용자 확정)
- 스킬 이름 매칭: Scan = `Scan`, Roll = `Roll`, Bluetooth = `SpawnVehicle` (Presenter 기존 설정).

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 플레이어, UI 파트
- 어떤 시스템/모듈이 건드려지는가: `Assets/_Project/Scripts/UI/PlayerHudView.cs`, `PlayerHudPresenter.cs`(남은 초 전달), Test `Prefabs/UI/MainScene/Canvas.prefab`(View 참조 연결)

## 제약 (Constraints)
- 지켜야 할 정책: HUD MVP 구조 유지. Facade에 쿨 조회 API는 추가하지 않는다(intent-018 제약).
- 쓰지 말아야 할 것: 기존 쿨타임 슬라이더 필드 제거 금지(`_Project/Prefabs/UI/Canvas.prefab`이 사용할 수 있음).

## 열린 질문 (Open questions)
- 없음.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: `PlayerHudPresenter.SSkillCooldown`에 `RemainingTime`(남은 초) 추가, `SetRollCooldown/SetBluetoothCooldown/SetScanCooldown(남은 비율, 남은 초)`로 확장. `PlayerHudView`에 `[Header("Skill Cards")]` 원형 바 Image 3개·남은 시간 TMP 3개 추가, `SetCooldownGauge`가 fill = 1 − 남은 비율, 글자 = 0.1초 올림 `N.Ns`(사용 가능하면 빈 글자). 기존 쿨타임 슬라이더 3개도 계속 갱신. Test `Canvas.prefab`의 View에 `Skills/SkillCard_*/IconArea/CooldownBar`·`CooldownText` 연결. Play에서 실제 `Player.prefab` 쿨타임(Scan 6·SpawnVehicle 3·Roll 0.3)으로 `OnSkillUsed`를 발생시켜 2초 뒤 Scan 0.34·4.0s, Bluetooth 0.67·1.0s, Roll 가득·빈 글자, 7초 뒤 Scan 가득·빈 글자 확인.
- 관련 커밋/PR: Test 저장소 3ac8c3a(Canvas.prefab), 팀 저장소 PlayerHudView·PlayerHudPresenter 커밋
