# 보스 B 등장 연출 (intent-075)

## 1. 작업 요약 및 방법
- 요청: 2라운드에서는 작은 보스(B)가 안 보이다가, 3라운드 진입 시 B 가 큰 보스(A) 머리 위에서 Spell 애니메이션을 재생한 뒤 땅에 내려와 보스전이 진행되게.
- 사용자 확정: 재도전 때도 연출 재생 / 연출 중 무적 / 하강은 쿵 착지 / 연출 중 플레이어 잠금.
- 코드
  - `BossFightSpawner`: 미리 생성(2라운드)은 A 만. 3라운드 `Spawn()` 에서 B 를 `_bossBPoint` 에 만들고 `CoEnterAndBegin` 시작 → 플레이어 `SetMoveLocked(true)` → `BossBController.PlayEntrance(공중, 착지)` → `IsEntrancePlaying` 이 끝날 때까지 대기 → 잠금 해제·A/B BT 켜기 → `BossEncounter.Begin`. 착지 순간(`OnEntranceLanded`) 플레이어 카메라 흔들림(`_landingShakeForce`). 인스펙터 신규 값 `_entranceHeightAboveHead`(6 m), `_landingShakeForce`(1). `Despawn`/재스폰 때 코루틴 중지·잠금 해제, 연출 중 플레이어가 새로 만들어지면 새 플레이어도 잠금.
  - 신규 `BossBEntrance : BossBSkillBase`: 시작하면 B 의 모든 콜라이더(CharacterController 포함)를 꺼서 무적으로 만들고 공중 위치로 옮김 → `_hoverSeconds`(1.2, Spell 클립 길이) 동안 제자리 → 중력 가속도로 낙하하며 수평은 착지 위치에 선형 보간 → 착지 시 콜라이더 복구·`Boss.NotifyEntranceLanded` → `_landingPauseSeconds`(0.5) 후 종료. 끊기면 착지 위치로 내려놓고 콜라이더 복구.
  - `BossBSkillBase.BeginRun(..., bool isArenaRequired = true)`: 보스전 시작 전(아레나 미연결)에도 시작 가능한 인자 추가.
  - `BossBController`: `PlayEntrance`·`IsEntrancePlaying`·`OnEntranceLanded`·`NotifyEntranceLanded`.
  - `BossController`(A): `HeadTransform`(머리 부위 `BossHitRelay` 의 Transform) — B 공중 위치 기준.
- 에셋 (MCP)
  - `Animations/Enemy/Clips/BossB_Spell.anim`: `Tanker Robot@Cast Spell.FBX` 클립 복사(1.17초, 루프 없음).
  - `BossB.controller`: Trigger `Spell`, 상태 `Spell`, AnyState→Spell(Spell 트리거)·Spell→Idle/Move(IsMoving, 종료 시간).
  - `BossB.prefab`: `SkillPrefab/Entrance`(`BossBEntrance`, `_skillName` Entrance, `_hoverSeconds` 1.2) + `EnemyAnimator` 매핑 `Entrance → Spell`(Trigger).

## 2. 결정 사항
- 무적은 B 콜라이더를 꺼서 처리(A 는 `BossHitRelay` 가 처음부터 비허용이라 `Begin` 전엔 이미 안 맞음).
- B 는 NavMesh 위(`_bossBPoint`)에서 생성하고 같은 프레임에 공중으로 옮김. 처음에 공중에서 바로 만들었더니 `Failed to create agent because it is not close enough to the NavMesh` 에러 10건이 나서 바꿈.
- 연출은 코루틴 하나로 순서를 묶고, B 의 몸 이동은 기존 Leap 과 같은 스킬 방식(Hold + `SuspendAgentSync`).

## 3. 현재 상태 및 이슈
- 리컴파일 에러 0. Play(Map_4_Security, 라운드 인덱스를 리플렉션으로 1→2): 2라운드 A 만 존재·B 없음, 3라운드 B 가 A 머리 위(y≈27)에서 Spell 재생·콜라이더 0개, 낙하하며 `(377.89, 0.08, 10)` 착지, 콜라이더 복구, 연출 중 플레이어 `_isMoveLocked` true → 끝나면 false, `IsBegun` true, BT 켜짐, 에이전트 NavMesh 위, 콘솔 에러 0(경고는 기존 Mediator 컴포넌트 미할당 경고).
- 테스트 중 `Time.timeScale` 이 0 이어서(대사 일시정지) 직접 1 로 올려 확인함. 이 프로젝트에 `BossFightSpawner` PlayMode 테스트는 없어서 새로 추가하지 않음.
- 실행자: Claude 직접. Codex CLI 는 설치돼 있으나 설계 맥락 의존이 커서 위임 비용이 더 크다고 판단(예외 사유).
- 상태: `waiting_user` — 육안 확인 전. 공중 높이 6 m(A 머리 y≈20 이라 B y≈27), 낙하 중 모션(Spell 이 끝나면 Idle 포즈로 낙하), 카메라 흔들림 1, 착지 후 멈춤 0.5초는 임시값. 게이트 루프 미실행, 커밋 전.
- `BossB.controller`·`BossB_Override` 에는 이전 도약 애니메이션 수정(미커밋)이 같이 들어 있음.

## 4. 다음 할 일 (Next Steps)
- 사용자가 Play 로 B 등장 연출 확인: 공중 높이·카메라 구도에서 B 가 보이는지, 낙하 속도(중력 기반), 쿵 흔들림 세기.
- 낙하 중 모션이 어색하면 낙하 구간에 `LeapFlight` 같은 모션 알림 스킬 요청 추가 검토.
- 확인되면 intent-075 를 `resolved` 로 바꿔 `docs/intent/clear/` 로 이동.
