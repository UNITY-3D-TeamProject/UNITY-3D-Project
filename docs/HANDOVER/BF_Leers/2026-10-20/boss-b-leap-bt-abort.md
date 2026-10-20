# 보스 B 도약이 공중에서 멈추는 문제 (BT 조건이 도약을 끊음)

## 작업 요약 및 방법
- 증상: 가운데 칸이 무너지면 B가 점프해서 목표(정중앙)까지 못 가고 공중에서 멈췄다 떨어진다.
- 조사: Play에서 가운데 칸(12번)을 무너뜨린 채 B가 그 칸 위를 건너 뛰게 하고, 도약 중 위치를 0.1초마다 기록했다. 매번 B가 12번 칸 범위(x<270)에 들어서는 순간 도약이 끝났다. `BossBSkillBase.EndRun`에 임시 스택 로그를 넣어 호출 경로를 확인했다(확인 후 제거).
  - 호출 경로: BT `CheckAllActiveObservers` → 공격 가지 종료 → `AIUseSkillAndWaitAction.OnEnd` → `StopSkill` → 도약 `Stop`.
- 원인: BT 조건 `BossBIsOnTelegraphedTileCondition`("B 발밑 칸이 예고 중이다")이 `Controller.Position` 기준이라, 도약 중 B가 지나는 칸이 구멍·예고면 참이 된다. 그러면 공격 가지가 중단되고 `OnEnd`가 아직 붙잡혀 있는 도약을 끊어 B가 그 자리에서 떨어진다. 같은 문제를 `BossBController.OnTileCollapsedCallback`은 이미 `IsBodyInFlight`로 막고 있었지만 BT 조건에는 없었다.
- 수정:
  - `BossBController`: private 메서드 `IsBodyInFlight()`를 public 프로퍼티 `IsBodyInFlight`로 바꿈(호출부 1곳 갱신).
  - `BossBIsOnTelegraphedTileCondition.IsTrue`: `_boss.IsBodyInFlight`면 거짓을 돌려준다.
- 검증: 재컴파일 에러 0. 같은 조건(12번 구멍, B 2번 칸 → 22번 칸)에서 수정 전에는 x≈270에서 끊겼고, 수정 후에는 x 270→258 구멍 위를 지나 22번 칸 중심 (240, 10)에 착지했다.

## 결정 사항
- 도약 체공 중에는 지나는 칸의 상태를 B의 "발밑 칸"으로 보지 않는다(BT 조건, 안전망 모두). 착지 칸의 안전은 `BossBLeap`이 스스로 보장한다.
- 이번에는 착지 칸 고정/보호 방식은 채택하지 않았다. 예고 중에는 지금처럼 원이 안전한 칸으로 옮겨 가고, 그 칸이 곧 착지 칸이다.

## 현재 상태 및 이슈
- 게이트 루프(`gate.cjs loop`)와 Codex 리뷰 미실행, 커밋 전. 실제 Game 뷰 육안 확인 미완.
- 검증은 BT 밖에서 `TryExecute`로 도약을 시작해 한 것이라, BT 안에서 첫 조우 도약이 구멍 위를 지날 때 끝까지 가는지는 육안으로 확인이 필요하다.
- 착지 칸에 플레이어가 서 있으면 B가 플레이어 머리 위에 올라서 미끄러져 내려오며 잠시 멈추는 현상을 봤다(낙사 복귀 위치와 B의 재선택 칸이 같은 칸이 되는 경우). 이번 증상과는 별개이며 수정하지 않았다.
- A의 손(`Enemy` 레이어 비트리거 `BoxCollider`)이 도약을 막는지도 확인했으나, 손 높이에 둔 충돌체를 B가 그대로 통과해 영향이 없었다.
- 측정 중 알게 된 것: 이 테스트 씬에서는 게임이 `timeScale` 0으로 멈춰 있을 수 있고, 플레이어가 죽으면 보스가 재생성돼 추적이 끊긴다.

## 다음 할 일 (Next Steps)
- 사용자가 Play에서 가운데 칸이 무너질 때 B 도약이 목표 칸까지 가는지 육안 확인.
- 게이트 루프 통과 후 커밋.
