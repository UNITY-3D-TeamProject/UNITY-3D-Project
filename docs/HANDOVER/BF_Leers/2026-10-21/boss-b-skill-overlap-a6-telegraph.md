# 보스 B 스킬 겹침(보스-7) · A6 박수 예고(보스-8)

## 작업 요약 및 방법
- AI_TODO 보스 파트 시작. 보스-4(스캔 약점)는 사용자 지시로 취소선·보류(스캔 담당이 컴포넌트를 넣은 뒤에도 안 되면 재확인).
- **보스-7 (돌진하면서 동시에 도약)** — 사용자 확인: "돌진하면서 동시에 점프".
  - `Map_4_Security` Play 재현: 스포너 `_activeRoundIndex` 0(런타임만), `BossEncounter.HandleBDowned` 리플렉션 + 그로기 시간 0.01초로 1-2 강제, 플레이어 HP 매 프레임 채움, 3.5~6.5초마다 B에서 20 m 이상 떨어진 멀쩡한 칸으로 플레이어 순간이동. B 스킬 상태를 `Temp/bossb_trace.txt`에 프레임 단위로 기록.
  - 원인: ① 회피 도약(Evade) 체공 중에는 `BossBIsOnTelegraphedTileCondition`이 거짓이라 피하기 가지가 끊기고 공격 가지가 도약(Leap)을 시작 ② 회피 착지 `EndRun`의 `ReleaseHold`가 도약의 붙잡힘까지 풀어 BT가 도약이 끝난 줄 알고 돌진 시작 ③ `Charge:Charging + Leap:Flight` 동시 진행. 근본은 `BossBSkillBase.BeginRun`이 자기 `IsRunning`만 본 것(붙잡힘은 AIController에 하나).
  - 수정: `BossBController.IsSkillRunning` 추가, `BeginRun`이 다른 B 스킬 진행 중이면 시작 거부, `BossBChooseAttackAction`이 스킬 진행 중이면 None(시작 못 할 공격의 쿨타임이 헛돌지 않게).
- **보스-8 (최종 예고 후 땅이 안 사라짐)** — 원인은 칸을 무너뜨리지 않는 A6 박수가 줄 5칸 전체를 예고한 것. 사용자 결정: 칸은 그대로, 맞는 칸만 예고.
  - 수정: `BossAPatternBase.ReplaceTelegraph(tiles, fillSeconds)` 추가. `BossAClap.GetTargetTiles`는 양 끝 칸만, `RetargetHands`에서 가운데 칸으로 예고 교체(채움 시간 `_strikeInterval`).
- 실행자: Claude 직접. 예외 이유: 이 PC에 Codex CLI 없음(`which codex` 실패).
- intent: [intent-055](../../../intent/clear/intent-055-bossb-skill-overlap.md), [intent-056](../../../intent/clear/intent-056-bossa-clap-telegraph.md)

## 결정 사항
- B 스킬(콤보·돌진·도약·회피)은 한 번에 하나만 진행한다. 안전망(발밑 칸 붕괴 → 회피)은 진행 중 스킬을 먼저 끊으므로 영향 없음.
- 공용 `AIController`·`AIUseSkillAndWaitAction`, BT 그래프 에셋은 건드리지 않았다.
- A6는 계속 칸을 무너뜨리지 않는 패턴이며, 예고는 1타 양 끝 → 2타 가운데.

## 현재 상태 및 이슈
- `gate.cjs check recompile` 통과(에러 0).
- Play 확인(파일 변경 없음, Play 종료로 원복):
  - 수정 전: 같은 방법으로 1분 안에 겹침 123프레임(`Leap+Evade`, `Charge+Leap` 등). 원본 `Temp/bossb_trace_before.txt`.
  - 수정 후: 150초 겹침 0건. B 발밑 칸 강제 붕괴 → 회피 체공 중 도약·돌진·콤보 시작 87회 시도 전부 거부, 착지 후 돌진→콤보 정상.
  - A6 강제 실행(줄 10~14): 예고 [10,14] → 1타 → [12] → 2타 → 없음. A6로 무너진 칸 없음.
- 2타 가운데 예고는 1.0초에 다 차지만 손이 가운데로 옮겨 가느라 충돌은 약 0.4초 뒤(꽉 찬 채 대기). 필요하면 육안 확인 때 조정.
- 사용자 육안 확인 완료(2026-10-21, 문제없음) → intent-055·056 resolved, `docs/intent/clear/`로 이동.
- **게이트 루프 미실행, 커밋 전.**

## 다음 할 일 (Next Steps)
- AI_TODO 보스 순서 3번: 보스-3(작은 보스 이동 개선) — 어색한 상황부터 구체화.
