# 바람 몹 S1 — Hold API + EnemyWindBlast 스킬

- 날짜: 2026-10-16 / 브랜치: `feature/AI/Enemy`
- 스펙: `docs/spec/wind-enemy.md` / intent: `docs/intent/intent-024-wind-enemy.md`

## 1. 작업 요약 및 방법
- Round 5 결정 6건을 받아 스펙에 반영했다(구현 전 검증의 `틀림` 해소).
- T1 `AIController`: `Hold(Vector3, float)` / `ReleaseHold()` / `IsHolding` 추가. `Update`에서 타이머 감소는 NavMesh early return 앞, 이동 0 + 고정 방향 회전은 `nextPosition` 동기화 뒤·피격 돌아보기 앞. `OnDisable`에서 초기화.
- T2 `EnemyWindBlast`(`Skill.Skills`, `[DefaultExecutionOrder(-50)]`): 램프(선형 속도, 부채꼴·차폐 판정, `AddExternalDisplacement`) → 범위 안이면 EaseOut 넉백 → `ReleaseHold`. `Stop()`/`OnDisable`/대상·모터 파괴 시 즉시 종료.

## 2. 결정 사항 (Round 5)
- 램프 속도 0.5 → 2 m/s(총 2.5 m), `_range` 6 m 유지
- 대상 놓침은 Sensor 유예 포함 `HasVisibleTarget` 그대로
- 근접 "공격" 노드 교체 철회(Melee 그대로)
- 풀 재사용 시 `WindReadyTime` 초기화 안 함(허용)
- Special `_sightRange` 8 m, 후퇴 `SpeedRatio` 1.0

## 3. 현재 상태 및 이슈
- 컴파일 에러 0. 기존 `GetInstanceID` 경고만 있음(이번 변경과 무관).
- **T1 Play eval 미실시**(Melee가 배치된 NavMesh 테스트 씬 이름 미확인). S4에서 함께 확인.
- BT 그래프가 풀 재사용 시 처음부터 다시 시작하지 않는 문제는 Melee·Ranged에도 해당하는 별개 문제(미해결).

## 4. 다음 할 일
- S2: T3 `AIUseSkillAndWaitAction`, T4 `AIIsTimeReachedCondition` + `AISetReadyTimeAction`, T5 배선 가이드 `docs/features/wind-enemy-wiring.md`
- S3: 사용자 에디터 작업 U1, U3, U2, U4
- S4: T6 Play 시나리오 1~9
