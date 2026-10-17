---
id: intent-027
title: MainScene Canvas 이동 및 프리팹화
part: UI / Test Scene
status: resolved
created: 2026-10-16
resolved: 2026-10-16
---

## 문제 (Problem)
로비에 추가한 참고 HUD가 Test MainScene의 다른 UI 시안들과 분리되어 있고, MainScene의 Canvas들이 개별 프리팹으로 정리되지 않았다.

## 기대 결과 (Proposed outcome)
Canvas_ReferenceHud를 로비에서 Test MainScene으로 옮기고, MainScene의 모든 루트 Canvas를 Test 폴더의 개별 프리팹으로 저장해 씬 인스턴스와 연결한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: HUD 시안을 비교·재사용하는 개발자와 디자이너.
- 어떤 시스템/모듈이 건드려지는가: 로비 씬, Test MainScene, Test UI 프리팹, 작업 기록.

## 제약 (Constraints)
- 기존 UI 모양과 활성 상태, 사용자가 이미 수정한 MainScene 상태를 유지한다.
- 기존 프리팹과 이름이 충돌하지 않는 Test 경로를 사용한다.
- 이번 요청에서는 커밋·Push하지 않는다.

## 열린 질문 (Open questions)
- 없음.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: Canvas_ReferenceHud를 Test MainScene으로 이동했다. MainScene의 9개 루트 Canvas를 UnityAssets/Prefabs/UI/MainScene/의 개별 프리팹으로 만들고 씬 인스턴스를 연결했다. 로비의 이전 HUD 숨김 조치는 원상복구했다.
- 관련 커밋/PR: 없음 (로컬 변경, 미커밋).
