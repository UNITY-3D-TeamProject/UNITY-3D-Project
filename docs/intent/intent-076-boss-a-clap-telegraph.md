---
id: intent-076
title: 보스 A 박수(A6) 예고를 다른 내려치기 패턴과 구분되게 바꾸기
part: AI / Boss A
status: open   # open | resolved
created: 2026-10-26
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
최종 단계 A6 박수는 1타 바닥 예고가 줄 양 끝 칸 2개뿐이고 예고 색도 기본색이라, 같은 단계의 A1Double(양손 내려찍기)과 겉보기가 똑같다.
손도 예고가 끝난 뒤에야 떠오르고, 박수만의 신호인 주먹 자세와 사이 칸 위 상자 예고(`_wallPrefab`)는 각각 손이 뜬 뒤, 1타 뒤에야 보인다.
플레이어는 2타(쓸어 모으기) 범위를 맞기 직전 `_strikeInterval`(1.4초) 동안만 알 수 있다.

## 기대 결과 (Proposed outcome)
- 예고가 시작되는 순간 두 손이 주먹을 쥐고 양 끝 위로 떠오른다(`IsHoverDuringTelegraph`).
- 사이 칸 위 상자 예고를 1타 예고 시작부터 박수 순간까지 세워 둔다.
- 상자 예고를 주황·검정 경고 줄무늬(흐르는 사선)로 바꿔 바닥 예고(빨강 단색)와 구분한다.
  - 새 셰이더 `Project/BossWarningStripe`(월드 좌표 사선이라 상자 비율이 바뀌어도 줄 폭 유지) + 새 머티리얼 `M_BossClapWarning` 을 `BossLineTelegraph` 에 연결.
  - 처음엔 `M_BossTelegraph` 를 주황으로 바꿨으나 줄무늬로 대체하면서 원래 색으로 되돌렸다(`BossArenaSetupWindow` 기본 타일 예고 머티리얼이라 그대로 둠).

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 보스 A 최종 단계를 상대하는 플레이어
- 어떤 시스템/모듈이 건드려지는가: `BossAClap.cs`, `BossLineTelegraph.prefab`(머티리얼 교체), 신규 `Art/Shaders/Boss/BossWarningStripe.shader`·`Art/Materials/Boss/M_BossClapWarning.mat`

## 제약 (Constraints)
- 바닥 예고 칸·피해 판정·타이밍 수치는 바꾸지 않는다.
- 다른 A 패턴(`BossAPatternBase` 공통 흐름)은 건드리지 않는다.
- 머티리얼은 YAML 직접 수정 대신 Unity Editor MCP로 바꾼다.

## 열린 질문 (Open questions)
- (해결) 머티리얼 공유 여부: `BossCircleTelegraph`는 어느 프리팹·씬에도 연결돼 있지 않아 원본을 직접 바꾼다(사용자 확인).
- (해결) 색: 주황, 알파는 기존 0.4 유지.
- (해결) 상자 예고 표현: 경고 줄무늬 머티리얼(사용자 선택). 기존에 줄무늬 텍스처·셰이더가 없어 절차적 셰이더로 새로 만듦.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
