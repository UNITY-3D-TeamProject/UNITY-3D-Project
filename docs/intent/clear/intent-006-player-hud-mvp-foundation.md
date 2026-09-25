---
id: intent-006
title: 플레이어 HUD MVP 기반 추가
part: UI
status: resolved
created: 2026-09-25
resolved: 2026-09-25
---

## 문제 (Problem)
플레이어 Attribute 값을 HUD에 표시하고 UI 표시 상태를 관리할 기본 구조가 없다.

## 기대 결과 (Proposed outcome)
UI 폴더에 Manager, View, Presenter 역할이 분리된 최소 스크립트가 존재하며 HP, 배터리, 열 Attribute 변경이 HUD 게이지에 반영된다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: UI 및 플레이어 파트 개발자
- 어떤 시스템/모듈이 건드려지는가: UI, Attribute 시스템 연동부

## 제약 (Constraints)
- 네임스페이스는 `UI`로 통일한다.
- `UIManager`는 씬 단위 일반 `MonoBehaviour`로 작성한다.
- 프리팹과 씬은 변경하지 않는다.
- 기존 `AttributeSet`의 변경 콜백을 사용한다.

## 열린 질문 (Open questions)
- 없음

## 해결 기록 (Resolution)
- 최종 결정: UI 표시를 담당하는 `PlayerHudView`, Attribute 연동을 담당하는 `PlayerHudPresenter`, HUD 표시 상태를 조정하는 비싱글톤 `UIManager`를 추가했다.
- 관련 커밋/PR: 없음
