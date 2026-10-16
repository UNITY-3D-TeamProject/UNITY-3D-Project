# Special 공격 모션 중 미끄러짐 — 근접 공격 Hold

- 날짜: 2026-10-16 / 브랜치: `feature/AI/Enemy`
- intent: `docs/intent/intent-024-wind-enemy.md` (open 유지, 사소한 수정이라 별도 intent 없음)
- 이어지는 작업: [공격 끝 거리 분리](./special-attack-exit-range.md) (B안). 이 문서는 A안.

## 1. 작업 요약 및 방법
- 증상: 대상이 공격 사거리를 벗어나면 "공격" 노드가 바로 끝나 추격이 시작되는데, `Attack → Move` 전환은 Exit Time이라 Right Claw Attack 모션(0.83초)이 끝까지 재생되는 동안 몸이 미끄러진다.
- 수정: `EnemyMeleeAttack`에 `_holdDuration`(기본 0)을 추가했다. 0보다 크면 `Execute` 순간 대상 쪽을 바라본 채 `AIController.Hold`로 그 시간 동안 붙잡는다(`EnemyWindBlast`와 같은 방식). 붙잡힌 동안 BT가 추격으로 바뀌어도 `AIController.Update`가 이동을 막고, 풀리면 하던 이동을 이어간다.
- `Special.prefab`의 `EnemyMeleeAttack`만 `_holdDuration` = 0.85로 설정했다(eval로 저장 후 재조회 확인). `Melee.prefab`은 0 그대로다. Special은 Variant가 아니라 일반 프리팹이다.
- 컴파일 에러 0 확인. `docs/features/wind-enemy-wiring.md` 문제 해결 표에 항목을 추가했다.

## 2. 결정 사항
- 붙잡기는 BT 노드가 아니라 스킬이 건다. 실제 발동(쿨타임 통과) 때만 걸어야 하므로, 발동 여부를 아는 스킬 쪽이 맞다.
- 기본 0으로 두고 프리팹별로 켠다. 같은 스킬을 쓰는 다른 몹(Melee 등)의 동작은 바뀌지 않는다.

## 3. 현재 상태 및 이슈
- **Play 검증 미완**.
- 부수 효과: `OnHitFromCallback`은 `IsHolding` 중 경직하지 않으므로, Special은 공격 모션 0.85초 동안 경직되지 않는다(intent-026의 "스킬 사용 중 슈퍼아머"와 같은 규칙).
- 붙잡힌 동안 방향은 발동 순간 대상 방향으로 고정된다. 모션 도중 대상이 옆으로 돌아도 따라 돌지 않는다.
- 바람 가지가 근접 공격 Hold 도중에 끼어들면, 바람 `Execute`의 `Hold`가 덮어쓰므로 정상이다. 다만 바람 발동이 실패했는데(대상 모터 없음 등) 근접 Hold가 남아 있으면 `AIUseSkillAndWaitAction`이 그 Hold를 바람으로 보고 기다린 뒤 쿨타임을 소모한다(드문 경우, 미수정).
- B안 [USER] 배선(`AttackExitRange`)은 여전히 미완.

## 4. 다음 할 일
- [USER] B안 배선 후 Play: 휘두르는 도중 플레이어가 멀어져도 모션이 끝날 때까지 제자리에 있고, 그 뒤 추격하는지 확인한다.
- 모션이 짧게 끊기거나 너무 오래 서 있으면 `Hold Duration`을 조정한다.
