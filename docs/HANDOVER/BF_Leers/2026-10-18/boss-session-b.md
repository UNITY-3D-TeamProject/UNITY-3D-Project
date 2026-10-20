# 보스전 세션 B — LOGIC-3·4 (2026-10-18)

- 스펙: `spec/boss.md`(로컬 전용) · 이전 기록: [보스 세션 A](./boss-session-a.md)
- 브랜치: `feature/AI/Boss`

## 1. 작업 요약 및 방법

- **LOGIC-3 `PlayerArenaTracker`** (`AI/Boss/Logic/PlayerArenaTracker.cs`)
  - `Sample(time, x, z, y, groundY, isGrounded)`: x·z(m)를 `tileSize`로 나눠 칸 단위로 바꾸고, 직전 표본과의 차이로 수평 속도(칸/초)·수직 속도(m/s)를 구한다. 첫 표본과 `Reset()` 직후 표본은 속도 0.
  - `TileIndex`·`IsOnArena`·`IsJumping`, `StayRow`/`RowStaySeconds`, `StayCol`/`ColStaySeconds`(줄이 바뀌거나 판 밖이면 0부터).
  - `PredictLandingTile()`: 남은 체공 `t = (vy + √(vy² + 2gh)) / g` 동안 간 지점의 칸. 판 밖이면 row·col을 0~2로 자른 칸(= 중심이 가장 가까운 칸). 접지 중이면 현재 칸.
- **LOGIC-4 `BossBAttackRule`** (`Logic/BossBAttackRule.cs`, `Logic/EBossBAttack.cs`): 스펙 분기 그대로 `static Choose(...)`.
- **테스트**(`Assets/Test/Boss/`, Git 제외): `PlayerArenaTrackerTests` 11개, `BossBAttackRuleTests` 7개 메서드(TestCase 포함 11건). MCP `run_tests`(EditMode, `Boss.Logic.Tests`) **44/44 통과**, 컴파일 에러 0.
- `.cs`마다 `.meta`를 만들었다. 스펙 세션 표의 B 행을 `done`으로 바꾸고 메모를 적었다.

## 2. 결정 사항

- 트래커에 읽기 전용 `VelocityX`·`VelocityY`·`VelocityZ`를 추가했다(스펙에 없음, "첫 표본 속도 0" 테스트를 확인하려고).
- 칸 계산은 `ArenaGrid`의 정적 `GetIndex`·`GetRow`·`GetCol`만 쓴다. `TryGetTile`은 인스턴스 메서드라서 트래커가 그리드 인스턴스 없이 같은 규칙(`floor`)을 따로 적용한다.
- 접지 중 판 밖이면 `PredictLandingTile()`은 -1이다("접지 중이면 현재 칸"을 그대로 따름). 점프 중이면 판 밖이어도 가장 가까운 칸을 낸다.
- 표본 시각이 거꾸로 가면 경과 시간을 0으로 본다.

## 3. 현재 상태 및 이슈

- 커밋 안 함. Git에 보이는 새 파일: `PlayerArenaTracker.cs`, `BossBAttackRule.cs`, `EBossBAttack.cs`와 각 `.meta`. 테스트는 Git 제외.
- 스펙과 세션 A 기록이 가리키는 `docs/intent/intent-044-boss-fight.md`가 **디스크와 Git 이력 어디에도 없다**. 다시 써야 하는지 사용자 확인이 필요하다.
- `unity status`는 실행 중인 Editor를 못 찾는다(Pipeline 패키지 미설치 메시지). MCP 연결은 정상이다.

## 4. 다음 할 일 (Next Steps)

- 세션 C: LOGIC-5 `BossAPatternSelector`. 입력 `SBossSituation`의 `PlayerTile`·`IsPlayerJumping`·`LandingTile`·줄 머묾은 이 트래커 속성을 그대로 쓴다.
- intent-044 문서 처리 결정.
