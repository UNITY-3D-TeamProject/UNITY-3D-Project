# Special 후퇴만 하고 공격·추격 안 함 — 바람 Guard Any → All

- 날짜: 2026-10-16 / 브랜치: `feature/AI/Enemy`
- intent: `docs/intent/intent-024-wind-enemy.md` (open 유지)

## 1. 작업 요약 및 방법
- 증상: Special이 플레이어를 보면 후퇴만 하고, 근접 사거리 안에서도 공격하지 않으며, 멀리 있어도 다가오지 않는다.
- 원인: `EnemyAI_Special.asset`에서 바람 가지 Conditional Guard(조건 3개: 대상이 보인다 / WindRange 안 / WindReadyTime 지남)가 **Any**였다(`m_RequiresAllConditions: 0`, `RequiresAllConditionsTrue: 0`).
  - "대상이 보인다"만으로 바람 가지에 들어가 쿨타임이 무시되고, 근접 가지(공격·추격)로 내려가지 못한다.
  - 2m 안에서는 후퇴→바람→준비 시각 설정을 반복하고, 6m 밖에서는 바람 노드 실패 → 준비 시각 설정을 반복하며 제자리에 서 있다.
- 수정: 사용자가 그래프 편집기에서 **All**로 변경했다. 에셋에서 런타임 162행 `m_RequiresAllConditions: 1`, 작성 모델 1207행 `RequiresAllConditionsTrue: 1`을 확인했다. 코드 변경은 없다.
- `docs/features/wind-enemy-wiring.md` 문제 해결 표에 이 증상을 추가했다.

## 2. 결정 사항
- 새 결정 없음 (가이드 U3의 "모두 만족" 그대로).

## 3. 현재 상태 및 이슈
- **Play 검증 미완**: MCP로 Play에 들어갔지만 프레임이 2에서 진행되지 않아(포커스·autotick 시도) 동작을 관찰하지 못했다. 스폰 직후 Console 에러는 0이었다.
- **`Special.prefab`의 `WindOrigin` 위치 이상(미수정)**: 로컬 (-0.195, -0.253, 4.59), 루트 스케일 2라서 몸 앞 약 9m, 발보다 아래에 있다. 바람 판정 거리·방향이 이 점 기준이라, 플레이어가 몹과 이 점 사이에 있으면 반대로 당겨지거나 범위 판정이 어긋난다. 가이드 U2-3("몸 중심 가슴 높이, 앞으로 띄우지 않는다")대로라면 대략 (0, 0.6, 0)(HitPoint 높이)이어야 한다.

## 4. 다음 할 일
- [USER] `Special.prefab` → `WindOrigin` 로컬 위치를 몸 중심 가슴 높이로 옮긴다.
- Play 확인: 5m 안 진입 → (2m 안이면 한 번 후퇴) → 바람 약 2초 → 8초 동안 추격·공격 → 다시 바람. 6m 밖에서 발견하면 바로 추격한다.
