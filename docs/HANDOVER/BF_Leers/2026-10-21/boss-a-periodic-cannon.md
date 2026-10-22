# 보스 A 어깨포 주기 발사(보스-12)

- 날짜: 2026-10-21
- intent: [intent-060](../../../intent/clear/intent-060-bossa-periodic-cannon.md)
- 실행자: Claude 직접. 예외 이유: 이 PC 의 PATH 에 Codex CLI 없음(`which codex` 실패).

## 1. 작업 요약 및 방법
- 기존: 어깨포는 최종 단계 A1Double·A5 가 내려친 직후 플레이어에게 가까운 손 자리로만 쐈고, 포격이 끝나야 패턴이 끝났다(`BossAPatternBase.UsesCannon`·`WaitingCannon`).
- `BossController`: 최종 단계에서 `_cannonIntervalFinal`(임시값 6초)마다 좌·우 번갈아(왼쪽부터) 그 순간 플레이어 XZ·바닥 높이(`Arena.GetTileCenter(0).y`)로 `BossShoulderCannon.Fire`. 첫 발도 최종 진입 후 6초 뒤.
  - `SetStage`: 포격 취소 후 최종이면 타이머 시작, 아니면 끔(그로기는 1-1 단계로 `SetStage` 되므로 자동으로 꺼짐).
  - `CancelPattern` 은 더 이상 포격을 끊지 않는다(BT 가 패턴마다 부름). `OnDisable`·`OnDeath` 에서 포격 취소, 사망 시 타이머 끔.
  - 쓰이지 않게 된 공개 `FireCannon` 제거.
- `BossAPatternBase`: `UsesCannon`·`TryFireCannon`·`OnCannonDone`·`WaitingCannon`·`Stop` 의 포격 취소 제거. `BossASlam._isDouble`(포격 전용) 제거 — 프리팹에 남은 값은 Unity 가 무시. `BossAChessSlam` 오버라이드·주석 정리.
- `BossShoulderCannon` 자체(예고 1초·반경 2.5·넉백)는 변경 없음.

## 2. 결정 사항 (사용자 확정)
- 패턴 부착 포격 제거, 발사 구간은 최종 단계만, 간격 6초(임시값), 좌·우 번갈아.

## 3. 현재 상태 및 이슈
- 리컴파일 통과(`gate.cjs check recompile`).
- Play(테스트 씬 `Assets/Test/Boss/Map_4_Security_Boss_Phase_T`, 1-1 진행 중 `SetStage(Final)` 강제, 런타임에서만 `runInBackground`):
  - 20.82초 최종 → 26.82 L, 32.82 R, 38.82 L, 44.82 R, 50.82 L. 각 1.0초 뒤 폭발. 플레이어가 맞아 매번 x 6 m 밀림(넉백).
  - 44.82 R 예고 중 45.78 패턴 교체(`CancelPattern`) → 45.82 정상 폭발. 직접 `CancelPattern` 호출에도 발사 유지.
  - 발사 중 `SetStage(Stage1_2)` → 포격 취소, 타이머 꺼짐.
- 사망 경로(`OnDeath`)는 Play 로 따로 확인하지 않음(코드 확인만).
- 씬·프리팹 저장 없음.
- **육안 확인·게이트 루프 미실행, 커밋 전.** 이전 작업(보스-10·13)도 같은 브랜치에 커밋 전 상태로 함께 있음.

## 4. 다음 할 일 (Next Steps)
- 사용자 육안 확인: 최종 단계 포격 빈도(6초)·예고 원 위치·패턴과 겹칠 때 체감 → 필요하면 `BossA` 프리팹 `_cannonIntervalFinal` 조정.
- 확인 후 intent-060 resolved, 게이트 루프 통과 후 커밋.
- AI_TODO 보스 다음 순서: 보스-11 (가로·세로줄 제거를 손이 한 칸씩 없애는 연출).
