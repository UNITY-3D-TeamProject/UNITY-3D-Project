# Special 공격↔추격 경계 떨림 — 공격 끝 거리 분리

- 날짜: 2026-10-16 / 브랜치: `feature/AI/Enemy`
- intent: `docs/intent/intent-024-wind-enemy.md` (open 유지, 사소한 수정이라 별도 intent 없음)

## 1. 작업 요약 및 방법
- 증상: Special이 AttackRange 안에서 공격하다가 추격과 맞물려 번갈아 바뀐다.
- 원인: 근접 가지(Melee 복제)는 공격 Guard 조건과 "공격" 노드 종료 조건이 둘 다 `AttackRange`(2)라서, 대상이 2m 근처에 있으면 `StopMove`↔`MoveTo`가 짧게 번갈아 불리고 `IsMoving`이 켜졌다 꺼진다. BT 흐름(안이면 공격, 밖이면 추격)은 설계대로다.
- 수정: `AIAttackTargetAction`에 선택 필드 `ExitRange`(기본 0)를 추가했다. 시작은 `Range`, 끝은 `max(Range, ExitRange)`로 판정한다. 링크하지 않은 그래프(Melee)는 기존 동작 그대로다. story는 바꾸지 않았다(`AIRangedAttackAction.KeepMoving`과 같이 Inspector에서 링크).
- 컴파일 에러 0 확인. `docs/features/wind-enemy-wiring.md` 문제 해결 표에 항목을 추가했다.

## 2. 결정 사항
- 사용자 선택: 공격 중 Hold(A안)·Exit Time 끄기(C안)는 하지 않고 거리 분리(B안)만 한다.
- Special에만 적용한다. Melee는 그래프에 링크하지 않아 변화 없다.

## 3. 현재 상태 및 이슈
- **[USER] 그래프 배선 미완**: `EnemyAI_Special` 블랙보드에 `AttackExitRange`(float, 2.5, Shared 끔) 추가 → "공격" 노드 Inspector `Exit Range`에 링크 → 저장.
- **Play 검증 미완**.
- 남은 알려진 문제(이번에 고치지 않음): 근접 공격은 몸을 붙잡지 않고, `Attack → Move` 전환이 Exit Time이라, 대상이 ExitRange 밖으로 나가면 공격 모션이 재생되는 동안 몸이 미끄러지듯 추격할 수 있다.

## 4. 다음 할 일
- [USER] 위 배선 후 Play: 플레이어가 2~2.5m 사이에서 움직여도 Special이 제자리에서 공격을 유지하고, 2.5m를 넘으면 추격하는지 확인한다.
- 미끄러짐이 거슬리면 A안(공격 모션 동안 Hold)을 검토한다.
