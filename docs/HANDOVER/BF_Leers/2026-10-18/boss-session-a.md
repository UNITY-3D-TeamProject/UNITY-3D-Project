# 보스전 Phase 2.5 결정 반영 + 세션 A (2026-10-18)

- 스펙: `spec/boss.md`(로컬 전용) · Intent: [intent-044](../../../intent/intent-044-boss-fight.md)
- 브랜치: `feature/AI/Boss`

## 1. 작업 요약 및 방법

- **Phase 2.5 결정 P1~P6 반영**: 사용자 답변을 `spec/boss.md` 본문(요약 표, D1, LOGIC-5, UNITY-1·6·9·10·11, CODEX-1·5, USER-5, 점검 2 표)에 반영하고 세션 0을 `done`으로 바꿨다.
- **세션 A (LOGIC-0~2)**
  - `Assets/_Project/Scripts/AI/Boss/Logic/`에 `ETileState`, `ArenaGrid`, `EBossStage`, `BossFightModel`을 만들었다(`UnityEngine` 참조 없음, `System`만).
  - 로컬 전용 asmdef `Boss.Logic`(`noEngineReferences`)과 테스트 asmdef `Assets/Test/Boss/Boss.Logic.Tests`를 만들었다.
  - EditMode 테스트 22개(`ArenaGridTests` 16, `BossFightModelTests` 6)를 MCP `run_tests`로 실행했고 22/22 통과했다. 컴파일 에러 0.

- **검증 게이트 수정**(`docs/tooling/codex-review/`, 로컬 전용, 사용자 승인):
  - PlayMode 단계: `list_tests`로 0개면 건너뛴다. 있으면 `run_tests --async_tests`로 시작하고 `test_status`를 3초마다 확인한다(도메인 리로드로 끊긴 연결은 재시도, 최대 330초). 동기 요청은 Unity가 거부한다.
  - EditMode 단계: 재컴파일 직후 연결이 끊기면(`Connection reset`) 5초 간격으로 최대 5번 재시도한다.
  - Codex 실행: Windows에서는 PATH의 `codex.cmd` 옆 `node_modules/@openai/codex/bin/codex.js`를 현재 node로 직접 실행한다. `spawn('codex')`는 `.cmd`를 못 찾아 ENOENT가 났다(이전 기록의 "Codex 미설치"도 이 문제였다).
  - 게이트 node 테스트 23/23 통과. `LOOP.md` 흐름 설명을 갱신했다.
- **로컬 PlayMode 테스트 갱신**: `Assets/Test/Combat/CharacterCombatTests` 시나리오 3을 `HealthDropAfterDeath_RaisesEventsAgain`(OnHit·OnDeath 2회)로 바꿨다. 팀원 커밋 `6cba045`가 사망 중복 방지를 일부러 뺐고, 중복은 중재자가 막는다.

## 2. 결정 사항

- **P1 (a-1)**: 그로기 중 HP 하한은 `BossHitRelay`가 처리한다. 넘기기 전에 값을 하한으로 자르고, `SEffectContext`에서 맞은 지점·방향을 빼서 넘긴다. 약점 판정이 탄도 연장선 기준(`CharacterCombat.IsCriticalHit`)이라 손에 맞아도 크리티컬이 붙을 수 있기 때문이다. 중재자·AttributeSet·클램퍼는 수정하지 않는다.
- **P2 (a)**: A2 몰기는 구멍과 **나란한** 줄을 친다.
- **P3**: 몰기 구멍은 붕괴 칸만 센다. 구멍이 두 방향 이상이면 무작위 하나. 반대쪽 칸이 판 밖·붕괴·예고면 기본 규칙으로 돌아간다. 단계 첫 A4는 왼손.
- **P4 (a)**: B 안전망. `IArenaQuery`로 옮긴 `OnTileCollapsed`를 `BossBController`가 구독하고, 발밑 칸이 무너지면 `AIController.StopSkill`·`ExecuteSkill`로 회피 도약을 강제한다.
- **P5 (a)**: 보스방 입구에 라운드 인덱스 2 체크포인트를 둔다. 테스트 씬은 CODEX-1에서 직접 두고, 본 씬은 맵 담당이 두기로 했다.
- **P6 (a)**: 포격 예고 1초에는 최종 배율 0.8을 곱하지 않는다.
- **asmdef Git 제외**: 루트 `.gitignore`의 `!/Assets/**/*.meta`가 `.git/info/exclude`보다 우선해 meta가 다시 잡힌다. 그래서 `Logic/.gitignore`(자기 자신 + asmdef + meta)를 쓴다.
- **`ArenaGrid` 세부 동작**(스펙에 없던 것):
  - 이미 무너진 칸에 `Collapse`를 부르면 타이머만 다시 시작하고 이벤트는 보내지 않는다.
  - 예고 칸에 `Collapse`를 부르면 `OnTelegraphChanged(false)` → `OnCollapsed` 순으로 보낸다.
  - `ResetAll`은 바뀐 칸마다 이벤트를 보낸다.
  - `GetNeighbors4` 순서는 row−1, row+1, col−1, col+1이다.

## 3. 현재 상태 및 이슈

- 게이트(세션 키 `cb0eb2a8…`): 4번째 실행에서 컴파일 0·EditMode 22/22·PlayMode 5/5 통과. 그러나 복잡한 변경이라 필요한 Codex 리뷰가 ENOENT로 실행되지 못해 **BLOCKED**. 시도 4회를 다 써서 이 세션 키로는 다시 돌릴 수 없다. Codex 실행은 그 뒤에 고쳤다.
- 커밋 안 함. 새 파일은 `Logic/*.cs`·`.meta`, `Boss.meta`, `Logic.meta`가 Git에 보인다. asmdef·테스트는 Git 제외다.
- 콘솔 에러 13건은 작업 전부터 있던 프리팹 임포트 에러(`Feed`·`Canvas_InGame`의 누락된 중첩 프리팹)로, 이번 변경과 무관하다.
- 팀원 PC에는 asmdef가 없어서 `Logic/` 코드가 `Assembly-CSharp`로 컴파일된다(스펙의 의도대로).

## 4. 다음 할 일 (Next Steps)

- 세션 B: LOGIC-3 `PlayerArenaTracker`, LOGIC-4 `BossBAttackRule` + EditMode 테스트.
- 이후 세션 C(LOGIC-5 선택기). 이때 P2·P3 규칙과 `GetNeighbors4` 순서를 쓴다.
