---
id: intent-005
title: 앱별 스테이지 구현체 추가
part: Stage System
status: resolved
created: 2026-09-24
resolved: 2026-09-24
---

## 문제 (Problem)
`StageBase`를 실제 앱별 동작에 연결할 구체 스테이지 클래스가 아직 없다.

## 기대 결과 (Proposed outcome)
`Stage` 폴더에 `Tutorial`, `FeedApp`, `FileApp`, `SecurityApp`, `LiveApp` 구현체가 존재하고, 각 클래스가 자신의 `EStageType`을 반환한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 앱별 게임플레이 개발자
- 어떤 시스템/모듈이 건드려지는가: Core Stage 시스템

## 제약 (Constraints)
- 각 클래스는 `StageBase`를 상속한다.
- 아직 정의되지 않은 앱별 시작·종료 로직은 추가하지 않는다.
- 요청된 클래스명과 스테이지 타입명을 일치시킨다.

## 열린 질문 (Open questions)
- 없음

## 해결 기록 (Resolution)
- 최종 결정: 다섯 구현체에 스테이지 타입 반환과 빈 시작·종료 골격을 추가하고, `EStageType` 멤버를 같은 이름으로 정리했다.
- 관련 커밋/PR: 없음
