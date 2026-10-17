---
id: intent-029
title: 차지샷 게이지 바 HUD
part: UI / Skill
status: resolved
created: 2026-10-16
resolved: 2026-10-17
---

## 문제 (Problem)
사격 버튼을 누르고 있으면 차지가 진행되지만, 화면에 얼마나 찼는지 보여주는 UI가 없다. 플레이어는 차지샷이 언제 나가는지 알 수 없다.

## 기대 결과 (Proposed outcome)
크로스헤어 오른쪽 아래에 칸형 차지 바가 있고, 차지 진행률만큼 칸이 켜진다. 차지샷 기준(50%) 위치에 눈금이 있다. 퍼센트 숫자는 표시하지 않는다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 플레이어, UI 파트
- 어떤 시스템/모듈이 건드려지는가: Test MainScene `Canvas/Crosshair/ChargeBar`(1단계 시각물), 연결 시 `Fire`·`SkillMediator`·`PlayerFacade`·`PlayerHudPresenter/View`

## 제약 (Constraints)
- 지켜야 할 정책: 그래픽은 Shift 에셋을 사용한다. HUD는 기존 MVP(View/Presenter) 구조를 따른다.
- 쓰지 말아야 할 것: 퍼센트 텍스트

## 열린 질문 (Open questions)
- "50% 넘으면 무조건 차지샷"은 현재 `Fire` 동작과 다르다. 지금은 `_chargeTime`(1초)을 다 채워야 차지샷이 자동 발사되고, 그 전에 손을 떼면 일반샷이다. `Fire` 판정을 50% 기준으로 바꿀지, 바만 먼저 연결할지 결정 필요(Fire는 다른 파트 코드일 수 있음).
- 진행률 전달 경로: `Fire`에 진행률(0~1) 노출 → `PlayerFacade` 이벤트 → `PlayerHudPresenter`가 매 프레임 갱신하는 방식이 후보.

## 결정 기록 (2026-10-17)
- `Fire` 판정은 바꾸지 않고 바만 연결한다 (차지 시간을 다 채우면 차지샷 자동 발사 그대로).
- 진행률 경로는 이미 있는 것을 쓴다: `PlayerFacade.OnFireChargeChanged(bool)` + `PlayerFacade.FireChargeRatio`(0~1). `PlayerHudPresenter`가 true~false 사이에 매 프레임 비율을 읽어 View에 넘긴다.
- 바는 차지 중에만 보이고(평소 숨김), 빨간 칸이 왼쪽부터 차오른다. 칸 몫보다 적게 찬 칸은 빈 색 → 찬 색으로 점점 밝아진다. (사용자 요청)

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: `PlayerHudPresenter`가 `PlayerFacade.OnFireChargeChanged`를 구독해 true면 `_isFireCharging`을 켜고 차지 바를 보이며, 차지 중에는 `Update`마다 `PlayerFacade.FireChargeRatio`를 읽어 `View.SetChargeRatio`로 넘긴다. false(발사·취소)·플레이어 교체·OnEnable/OnDisable이면 숨긴다. `PlayerHudView`에 `[Header("Charge Bar")]` `_chargeBarRoot`·`_chargeCellImages`(10칸)·찬 색/빈 색 추가, 칸 값 = clamp01(진행도 × 칸 수 − i)로 빈 색 → 찬 색 보간. `Fire`는 변경 없음. Test `Canvas.prefab`의 View에 `Crosshair/ChargeBar`와 `Segments/Seg (0~9)` 연결. Play에서 실제 `Fire.TryExecute`로 차지 전 숨김, 차지 중 표시·진행도 0.32 → 3칸 + 4번째 칸 일부, 손 떼면(일반샷) 숨김, 끝까지 누르면 차지샷 자동 발사 후 숨김 확인.
- 관련 커밋/PR: Test 저장소 29b27e5 (팀 저장소 코드는 같은 날 커밋)
