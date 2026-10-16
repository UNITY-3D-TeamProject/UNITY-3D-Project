# 바람 몹 S2 — BT 노드 3종 + 배선 가이드

- 날짜: 2026-10-16 / 브랜치: `feature/AI/Enemy`
- 스펙: `docs/spec/wind-enemy.md` / 가이드: `docs/features/wind-enemy-wiring.md`

## 1. 작업 요약 및 방법
- T3 `AIUseSkillAndWaitAction`: 거리 > Range면 Failure → `ExecuteSkill` → `IsHolding` 아니면 Failure, 맞으면 Running. `OnUpdate`에서 Hold가 풀리면 Success. `OnEnd`에서 아직 Hold 중이면 `StopSkill`.
- T4 `AIIsTimeReachedCondition`(`Time.time >= ReadyTime`), `AISetReadyTimeAction`(`ReadyTime = Time.time + Delay`, 즉시 Success).
- T5 배선 가이드 `docs/features/wind-enemy-wiring.md` — U1~U4 절차, 블랙보드 표, 트리 구조, 문제 해결 표.
- 노드 id는 새 GUID, 기존 노드(`AIAttackTargetAction` 등)의 구조·카테고리(`Action/적 AI`, `Conditions/적 AI`)를 따랐다.

## 2. 결정 사항
- 새 결정 없음. 스펙 2-3 그대로 구현.

## 3. 현재 상태 및 이슈
- 컴파일 에러 0(`recompile_status` up_to_date, failed false). 새 `.cs.meta` 생성 확인.
- 그래프 편집기 노드 검색 확인은 S3에서 사용자가 한다(가이드 0장).
- T1 Play eval 미실시(S1 이월), BT 풀 재사용 미초기화 문제(Melee·Ranged 공통)는 그대로.

## 4. 다음 할 일
- S3: 사용자 에디터 작업 U1 → U3 → U2 → U4 (`docs/features/wind-enemy-wiring.md` 참고)
- S4: T6 Play 시나리오 1~9, T1 eval 포함
