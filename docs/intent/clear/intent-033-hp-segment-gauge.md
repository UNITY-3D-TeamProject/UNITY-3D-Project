---
id: intent-033
title: HP 패널 10칸을 게이지로 바꿔 실제 플레이어 HP와 연결
part: UI / HUD
status: resolved
created: 2026-10-16
resolved: 2026-10-16
---

## 문제 (Problem)
왼쪽 아래 HP 패널(`HpPanel/Segments`)의 10칸은 색만 칠한 고정 이미지이고, 퍼센트 글자(82%)도 고정값이다. `PlayerHudView`는 `_hpSlider` 하나에만 HP를 넣고 있어 새 HUD에 실제 플레이어 HP가 보이지 않는다.

## 기대 결과 (Proposed outcome)
- 10칸을 Shift 슬라이더(게이지) 에셋 10개로 바꾼다.
- 한 칸 = `MaxHp / 10` (현재 플레이어 MaxHp 100 → 한 칸 HP 10). 칸마다 자기 몫만큼 채워지고, 칸 몫보다 적게 남으면 그 칸만 부분적으로 찬다 (예: HP 85 → 8칸 가득 + 9번째 칸 절반).
- 퍼센트 글자는 `현재 HP / MaxHp`를 반올림한 값으로 갱신한다.
- 데이터 경로는 기존 그대로: `PlayerFacade.OnAttributeChanged` → `PlayerHudPresenter` → `PlayerHudView.SetHp(current, max)`.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 플레이어, UI 파트
- 어떤 시스템/모듈이 건드려지는가: `Assets/_Project/Scripts/UI/PlayerHudView.cs`, Test `Prefabs/UI/MainScene/Canvas.prefab`(UITestScene·Map_2_Feed 공통)

## 제약 (Constraints)
- 지켜야 할 정책: HUD는 기존 MVP 구조를 따른다. 게이지 그래픽은 Shift 에셋을 쓴다.
- 쓰지 말아야 할 것: 기존 `_hpSlider` 제거 금지(`_Project/Prefabs/UI/Canvas.prefab`이 아직 사용).

## 열린 질문 (Open questions)
- 없음 (사용자 지시로 한 칸 = MaxHp/10, 부분 채움으로 진행).

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: `PlayerHudView`에 `_hpCellSliders`(Slider 배열)·`_hpPercentText` 추가, `SetHp`가 기존 `_hpSlider`와 함께 칸 게이지·퍼센트를 갱신. 칸 i의 값 = `(현재 HP - 한 칸 HP × i) / 한 칸 HP`를 0~1로 자른 값. Test `Canvas.prefab`의 `Seg (0~9)`를 Shift `Slider.prefab` 복제본 `HpCell (0~9)`(손잡이·값 글자·사운드·애니메이터·`UIManagerImage` 제거, 조작 불가)로 교체하고 View에 연결. Presenter 구독이 되도록 `PlayerHudRoot`를 켜고 예전 임시 슬라이더·Legacy 글자는 숨김. Play에서 실제 `Player.prefab`의 `PlayerFacade.SetAttribute("CurrentHp")` → 칸·퍼센트 갱신 확인(100 → 10칸, 73 → 7칸 + 0.3, 65 → 6칸 + 0.5).
- 관련 커밋/PR: Test 저장소 4f6e542(Canvas.prefab), 팀 저장소 PlayerHudView 커밋
