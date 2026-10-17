---
id: intent-034
title: 크로스헤어 열(빨강)·배터리(파랑) 원호와 퍼센트를 실제 플레이어 값에 연결
part: UI / HUD
status: resolved
created: 2026-10-16
resolved: 2026-10-16
---

## 문제 (Problem)
UITestScene `Canvas/Crosshair`의 빨강(열)·파랑(배터리) 원호 4칸은 항상 꽉 찬 고정 이미지이고, 옆 `HeatLabel/Value`(28%)·`BatteryLabel/Value`(61%)도 고정 글자다. `PlayerHudView.SetHeat`·`SetBattery`는 꺼진 예전 임시 슬라이더에만 값을 넣는다.

## 기대 결과 (Proposed outcome)
- 데이터 경로는 기존 그대로: `PlayerFacade.OnAttributeChanged` → `PlayerHudPresenter` → `PlayerHudView.SetHeat/SetBattery(current, max)`.
- 색마다 90° 칸 2개에 나눠 채운다 (작업 기록 crosshair-half-arcs의 규칙).
  - 첫 칸 = 0~50%, 둘째 칸 = 50~100%. 칸 안은 부분 채움.
  - 원호(`*Arc`)·밝은 심(`*Core`) fill = 0.25 × 칸 값.
  - 글로우(`*Glow`)는 양 끝을 6°씩 늘린 상태라 fill = 0.25 × 칸 값 + 12/360, 칸 값이 0이면 0.
  - 열: 3시 → 12시 → 9시, 배터리: 9시 → 6시 → 3시 방향으로 참 (이미지 회전·원점 설정 그대로).
- 퍼센트 글자 = `현재 / 최대` 반올림.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 플레이어, UI 파트
- 어떤 시스템/모듈이 건드려지는가: `Assets/_Project/Scripts/UI/PlayerHudView.cs`, Test `Prefabs/UI/MainScene/Canvas.prefab`(View 참조 연결)

## 제약 (Constraints)
- 지켜야 할 정책: HUD MVP 구조 유지, Presenter 변경 없음.
- 쓰지 말아야 할 것: 기존 `_batterySlider`·`_heatSlider` 제거 금지(`_Project/Prefabs/UI/Canvas.prefab`이 사용).

## 열린 질문 (Open questions)
- 없음 (칸 나눔·글로우 규칙은 크로스헤어 작업 때 정한 그대로).

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: `PlayerHudView`에 `SArcCell`(원호·심 `FillImages` + `GlowImage`) 배열 `_heatArcCells`·`_batteryArcCells`와 퍼센트 글자 `_heatPercentText`·`_batteryPercentText` 추가. `SetHeat`·`SetBattery`가 기존 슬라이더와 함께 칸마다 비율을 나눠 채움(칸 값 = clamp01(비율 × 칸 수 − i), 원호 fill = 0.25 × 칸 값, 글로우 fill = 원호 fill + 12/360, 빈 칸 글로우 0). Test `Canvas.prefab`의 PlayerHUD View에 `HeatArc/Core/Glow(2)`·`BatteryArc/Core/Glow(2)`·`HeatLabel/Value`·`BatteryLabel/Value` 연결. Play에서 실제 `Player.prefab`의 `SetAttribute`로 열 28%·배터리 75%, 열 100%·배터리 0% 확인.
- 관련 커밋/PR: Test 저장소 d5c1c61(Canvas.prefab), 팀 저장소 PlayerHudView 커밋
