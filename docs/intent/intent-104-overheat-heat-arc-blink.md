---
id: intent-104
title: 과열로 열이 내려가는 동안 히트 원호를 진한 색으로 빠르게 점멸
part: UI / PlayerHud
status: open
created: 2026-10-31
resolved: null
---

## 문제 (Problem)
히트 게이지가 100%에 닿으면 `HeatCost`가 사격을 잠그고, 열이 0까지 내려가야 풀린다.
그런데 내려가는 동안 화면에는 평소와 같은 히트 원호만 보여서, 지금 총을 못 쏘는 상태라는 걸 알기 어렵다.

## 기대 결과 (Proposed outcome)
- 과열 잠금 중에는 크로스헤어의 빨간 히트 원호(2칸 반원)가 더 진한 빨강으로 바뀐다.
- 같은 동안 원호가 빠르게 점멸한다.
- 열이 0이 되어 잠금이 풀리면 원래 색으로 돌아간다.
- 과열 경고음은 플레이어 파트 담당이라 이 작업에 포함하지 않는다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 플레이어(사격 HUD)
- 어떤 시스템/모듈이 건드려지는가: `UI/PlayerHud/PlayerHudPresenter.cs`, `UI/PlayerHud/PlayerHudView.cs`

## 제약 (Constraints)
- 지켜야 할 정책: 코드 범위는 Core/UI만. `HeatCost`(Skill)·`PlayerFacade`는 수정하지 않는다.
- 쓰지 말아야 할 것: 프리팹 구조 변경(새 이미지·CanvasGroup 추가) 없이 기존 원호 이미지 색만 바꾼다.

## 열린 질문 (Open questions)
- 진한 색·점멸 속도 값이 원하는 느낌인지 Play에서 확인 필요 (인스펙터에서 조정 가능).

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: 과열 상태가 외부로 공개돼 있지 않아 Presenter가 `HeatCost`와 같은 규칙(열 ≥ 최대 → 과열, 열 ≤ 0 → 해제)으로 판단해 `View.SetHeatOverheated`에 전달. View는 원호 6장(원호·심·빛 번짐)의 원래 색을 `Awake`에 저장, 과열 중 `Update`에서 진한 색 + 켜짐/꺼짐 사각파 점멸, 해제 시 원래 색 복구.
- 관련 커밋/PR: (미커밋)
