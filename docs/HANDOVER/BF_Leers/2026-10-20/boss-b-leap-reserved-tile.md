# 보스 B 도약 착지 칸 예약 (A 가 그 칸을 피함)

## 작업 요약 및 방법
- 증상: 1-2 에서 B 도약 원이 뜬 뒤 약 1초 지나 이웃 칸으로 옮겨 가고, B 가 옮겨 간 칸으로 뜀.
- 원인: B 예고 중 A 가 새 패턴으로 같은 칸(대개 플레이어 칸)에 예고를 띄우면 `BossBLeap.RetargetIfUnsafe` 가 착지 칸을 가장 가까운 멀쩡한 칸으로 바꿈(앞선 [도약 원 위치](./boss-b-leap-circle-retarget.md) 수정의 규칙).
- 수정(intent-050, 사용자 결정 "그 칸만 빼고 공격"):
  - `ArenaGrid`: `ReservedTile`(-1 = 없음)·`SetReservedTile` 추가. `FindNearestSafe` 가 예약 칸을 건너뜀 → A4 끌려올 칸·낙사 복귀 칸도 예약 칸을 피함.
  - `IArenaQuery`/`BossArena`: `SetReservedTile` 창구 추가(칸 상태는 안 바꿈).
  - `BossBLeap`: 착지 칸을 고르면 예약, 칸을 바꾸면(`RetargetIfUnsafe`) 다시 예약, 착지(일반) 또는 `OnRunEnded`(회피 착지·중단·비활성)에서 해제.
  - `BossAPatternSelector.ExcludeReserved`: 고른 결과에서 예약 칸 제외. A1·A1Double 은 가장 가까운 멀쩡한 칸으로 대체(없거나 이미 표적이면 빼기, 다 빠지면 Rest), A2·A5 는 그 칸만 비우고 그 칸을 치던 손은 남은 칸 중 가장 가까운 칸으로. A4(칸은 손 고르기용)·A6(최종 전용, 칸 순서 의존)은 그대로.
- 실행자: Claude 직접(외과적 소규모 C# 변경이라 Codex 위임 비용이 더 큼).

## 결정 사항
- B 의 착지 칸은 도약 시작부터 착지까지 A 의 표적이 되지 않는다. 착지 후 경직 중에는 예약을 풀어 A 가 칠 수 있다.
- `RetargetIfUnsafe` 는 남은 경우(예: 예약 전에 이미 위험했던 칸)를 위한 안전장치로 유지.

## 현재 상태 및 이슈
- `gate.cjs check recompile` 통과. 에디터 eval 로 선택기 확인: 예약 12 → A1 [7], A2 [10,11,13,14] 손 10·14 / 예약 10 → A1 [5], A2 [11..14] 왼손 11, A4 불변, 멀쩡한 칸 없음 → Rest, 해제 후 `FindNearestSafe` 원래대로.
- **Play 육안 확인(원이 움직이지 않는지)·게이트 루프 미실행, 커밋 전.**

## 다음 할 일 (Next Steps)
- 사용자가 1-2 Play 에서 B 도약 원이 처음 칸에 그대로 있고 그 칸에 착지하는지, A 가 그 칸만 비우고 치는지 확인.
- 게이트 루프 통과 후 커밋, intent-050 resolved 처리.
