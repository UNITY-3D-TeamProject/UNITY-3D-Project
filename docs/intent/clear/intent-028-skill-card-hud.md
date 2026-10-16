---
id: intent-028
title: 스킬 카드형 HUD 시안 Canvas
part: UI / Test Scene
status: resolved   # open | resolved
created: 2026-10-16
resolved: 2026-10-16
---

## 문제 (Problem)
지금 MainScene 스킬 HUD는 원형 버튼 + 키 + 이름만 보여 준다. 스킬 사용에 드는 소모값과 남은 쿨타임(숫자·바)을 한눈에 볼 수 없다.

## 기대 결과 (Proposed outcome)
사용자 참고 이미지처럼 스킬마다 카드 한 장에 아래 6가지가 보이는 시안 Canvas를 MainScene에 하나 만든다.
1. 스킬 아이콘(임시 아이콘)
2. 스킬 키(Q / SHIFT / F)
3. 스킬 이름
4. 스킬 소모값(번개 아이콘 + 숫자)
5. 쿨타임 숫자(예: 2.8s)
6. 쿨타임 바(아이콘을 둘러싼 원형 게이지)

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: UI 시안 확인(김성준)
- 어떤 시스템/모듈이 건드려지는가: Test MainScene, Test `UnityAssets/Prefabs/UI/MainScene/`에 새 Canvas 프리팹

## 제약 (Constraints)
- Test 폴더(Git 제외 팀 저장소 / 별도 Test 저장소)에서만 작업한다. 기존 Canvas는 건드리지 않는다.
- ThirdParty Shift·SCI-FI UI 스프라이트와 Rajdhani 폰트를 쓴다(로컬 에셋 의존).
- 이번 범위는 정적 UI만이다. 값을 바꾸는 스크립트는 넣지 않는다.

## 열린 질문 (Open questions)
- 실제 스킬 데이터·쿨타임과 연결하는 스크립트가 필요한지(필요하면 별도 작업).

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: Test MainScene에 `Canvas_SkillCards`(카드 3장, 6개 요소 모두 표시) 추가, Test 프리팹 `UnityAssets/Prefabs/UI/MainScene/Canvas_SkillCards.prefab`으로 저장. 정적 UI만 만들었고 데이터 연결 스크립트는 열린 질문으로 남김(필요하면 새 intent).
- 관련 커밋/PR: 없음(미커밋). 작업 기록: docs/HANDOVER/SungJun/2026-10-16/skill-card-hud.md
