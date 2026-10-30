---
id: intent-090
title: 특수몹 바람 스킬이 이동하는 플레이어를 따라가도록 변경
part: AI / Enemy / Skill
status: open   # open | resolved
created: 2026-10-29
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
특수몹 바람 스킬(`EnemyWindBlast`)은 발동하는 순간 몹→플레이어 수평 방향을 `_forward` 로 고정한다.
몸 방향(`AIController.Hold`)과 판정 부채꼴(좌우 ±30°)이 모두 이 고정 방향을 쓰므로,
램프(2초) 동안 플레이어가 옆으로 움직이면 바람이 따라가지 않는다 (AI_TODO2 잡몹-9).

## 기대 결과 (Proposed outcome)
- 램프 동안 매 프레임 바람 방향(`_forward`)을 플레이어 쪽으로 다시 맞추고, 몸도 그 방향을 바라보게 `Hold` 방향을 갱신한다.
- 램프 끝 넉백 방향은 지금처럼 넉백 시작 순간에 고정한다.
- VFX 는 몸에 붙은 기준점을 따르므로 몸이 돌면 함께 돈다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 스테이지2(보안) 특수몹과 싸우는 플레이어
- 어떤 시스템/모듈이 건드려지는가: `Skill/Skills/EnemyWindBlast.cs` 만

## 제약 (Constraints)
- 지켜야 할 정책: 캐릭터 구조(스킬 → `AIController.Hold` 요청은 기존 방식 유지), 코딩 컨벤션
- 쓰지 말아야 할 것: 회전 속도 제한(사용자 결정: 즉시 추적), 새 예비 동작 단계

## 열린 질문 (Open questions)
- (해결) 대상 스킬: Wind(바람). 2026-10-29 사용자 결정.
- (해결) 따라가는 방식: 램프 내내 매 프레임 즉시 추적. 2026-10-29 사용자 결정.
- 즉시 추적이라 램프 중 좌우 각도 판정은 사실상 항상 통과한다(거리·위아래 각도·장애물만 남음). 회피가 너무 어려운지는 직접 플레이로 확인.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
