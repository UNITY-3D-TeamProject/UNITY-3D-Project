---
id: intent-004
title: 스테이지 코어 타입 골격 구성
part: Stage System
status: resolved
created: 2026-09-24
resolved: 2026-09-24
---

## 문제 (Problem)
스테이지 전체 진행과 앱별 스테이지·내부 Phase를 분리하기 위한 공통 타입이 아직 정리되어 있지 않다.

## 기대 결과 (Proposed outcome)
`Core.Stage` 네임스페이스 아래에 스테이지 매니저, 스테이지 베이스, 타입, 결과, Phase 베이스의 최소 골격이 존재한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 스테이지 및 앱별 게임플레이 개발자
- 어떤 시스템/모듈이 건드려지는가: Core Stage 시스템

## 제약 (Constraints)
- 요청된 다섯 스크립트만 구성한다.
- 기존 스크립트의 불필요한 로직 변경은 하지 않는다.
- 프로젝트 명명 및 네임스페이스 컨벤션을 따른다.

## 열린 질문 (Open questions)
- 없음

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: `Core.Stage` 아래에 `StageManager`, `StageBase`, `EStageType`, `SStageResult`, `StagePhaseBase`를 구성했다. 기존 타입과 Unity GUID는 새 명명에 맞춰 유지했다.
- 관련 커밋/PR: 없음
