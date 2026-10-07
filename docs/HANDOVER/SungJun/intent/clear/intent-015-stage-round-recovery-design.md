---
id: intent-015
title: Core 기준 낙하 복귀와 라운드 사망 복구 설계
part: Stage / Player State
status: resolved
created: 2026-10-08
resolved: 2026-10-08
---

## 문제 (Problem)
현재 StageManager는 사망 시 스테이지를 종료하고, PlayerSpawner는 Respawn 시 기본 Effect를 적용한다. 생존 낙하의 최근 체크포인트 복귀와 사망의 라운드 체크포인트 복구를 구분할 설계가 필요하다.

## 기대 결과 (Proposed outcome)
Core의 현재 코드를 근거로 책임, 데이터, 이벤트 순서, 적용 패턴, 변경 지점과 검증 기준을 문서화한다. 이번 범위는 설계이며 기능 구현은 포함하지 않는다.

## 영향 범위 (Affected users and systems)
- StageManager, PlayerSpawner, PlayerState, GameManager, SpawnerBase, StageBase.
- 실제 변경은 설계 및 인수인계 문서와 목차에 한정한다.

## 제약 (Constraints)
- 설명과 분석은 Core 코드 기준으로 한다.
- 위치 결정과 기존 플레이어 이동은 StageManager, 생성은 PlayerSpawner가 담당한다.
- 라운드 체크포인트 최초 도달 시 체력·배터리를 보존한다.
- 사용자가 확인한 규칙: 사망하면 일반 체크포인트 진행도도 라운드 시작점으로 되돌린다.
- 과도한 추상화를 피하고 기존 이벤트·PlayerState를 활용한다.

## 열린 질문 (Open questions)
- 설계 문서 작성의 차단 사항은 없다.
- 구현 전 확인 사항: 기존 전투/이동 시스템의 부활·속도 초기화 계약, 실제 HP/배터리 Attribute 키, 라운드 재시작 시 적·기믹 초기화 범위. Core 외부 구현은 이번에 검토하지 않는다.

## 해결 기록 (Resolution)
- 최종 결정: 위치 결정/이동은 StageManager, 생성은 PlayerSpawner, 라운드 최초 HP·배터리 스냅샷은 PlayerState에 분리 보관한다. 사망 시 일반 체크포인트 진행도도 라운드 시작점으로 되돌린다.
- 결과물: [설계 및 인수인계](../../2026-10-08/stage-round-recovery-design.md).
- 설계 범위 완료. 런타임 기능 구현·Unity 검증은 수행하지 않았으며 구현 전 확인 항목은 설계 문서에 남겼다.
- 관련 커밋/PR: 없음.
