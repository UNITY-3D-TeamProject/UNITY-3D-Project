# 근접 잡몹 이동 모션·이동속도 불일치 / 어색한 움직임 원인 진단

## 1. 작업 요약 및 방법
- 보안맵 스테이지1 근접 잡몹이 이동 모션과 속도가 안 맞고 어색하게 도는 문제를 진단만 함. **코드·에셋 수정 없음.**
- 정적 분석: `AIController`, `EnemyAnimator`/`EnemyAnimationMediator`, `CharacterMotor`, `CharacterRotator`, `EnemyBase.controller`, `Melee_Override`, `EnemyAI_Melee.asset`(BT), `SOMonsterData_Melee`, `CombatZone`.
- 실측: Unity eval 로 PreviewScene 에 `Melee.prefab` 을 띄워 `Tanker Robot@Run Forward In Place`(1초 루프)를 샘플링, 발이 땅에 닿은 구간의 뒤로 미는 속도 측정 → 1배속에서 약 8 m/s(프리팹 스케일 포함).
- Play 관찰은 못 함: 열린 `Map_1_Lobby` 에 저장 안 된 변경이 있어 씬을 바꾸지 않음.
- 실행자: Claude 직접(읽기 전용 eval). 예외 이유: 진단 판단이 반복적이고 파일 변경이 없음.

## 2. 결정 사항
- 없음(진단만). 수정 방향은 사용자 확인 대기.

## 3. 현재 상태 및 이슈 (판정)
1. **모션 문제(주원인) — 다리 속도 불일치**: `EnemyAnimator._referenceMoveSpeed` 3(기본값 그대로, Ranged·Special 도 3), 실제 MoveSpeed 5(`SOMonsterData_Melee`, 생성 시 SetValue) → Move 재생 배율 1.67 → 다리는 약 13 m/s 로 뛰는데 몸은 5 m/s. 다리가 약 2.7배 빠름. 맞추려면 기준값 ≈ 8.
2. **모션/이동 구조 문제 — 옆·뒤로 미끄러짐**: `CharacterMotor` 는 지상에서 속도를 즉시 새 방향으로 바꾸는데 `CharacterRotator._rotateSpeed` 가 180°/s. 90° 꺾을 때 0.5초 동안 5 m/s 로 옆걸음(약 2.5 m) 하면서 앞으로 뛰는 모션 재생.
3. **BT 문제 — 사거리 경계 깜빡임**: `AIAttackTargetAction` 의 `ExitRange` 미설정(0 → Range 2 로 끝). 2 m 경계에서 공격 ↔ 추격이 매 프레임 가까이 번갈아 바뀌어 Idle/Move·회전이 떨림. 코드 주석이 이미 ExitRange 로 막으라고 안내함.
4. 부차: 순찰이 스폰 반경 5 m 무작위 지점 → 1초 대기 반복이라 제자리 맴돎처럼 보일 수 있음. 감속 버프 배율(`MoveMediator._moveSpeedRatio`)은 모터에만 적용되고 애니메이터 배율에는 안 들어감.
- BT 의 abort 설정(공격·추격 가드 LowerPriority)은 정상.

## 4. 다음 할 일
- 사용자 육안 확인(아래 5절 적용 후).

## 5. 적용 및 검증 (같은 날, 사용자 지시 "3개 모두 실행하고 검증")
- 선택한 방법: 1번은 "기준값 8", 2번은 "회전 속도 상향(720, 플레이어 기본값과 같음)" — 각 질문의 첫 번째 안.
- 적용 (실행자 Claude 직접, Unity MCP. 예외 이유: 값 3개 변경이라 Codex 위임 비용이 더 큼. 환경에 codex CLI 도 없음):
  - `Melee.prefab`: `EnemyAnimator._referenceMoveSpeed` 3 → 8, `RotatorPrefab` 의 `CharacterRotator._rotateSpeed` 180 → 720(중첩 프리팹 오버라이드). `LoadPrefabContents` → `SaveAsPrefabAsset`. 저장하면서 `Sensor._proximityRange: 0`(새 필드 기본값)이 같이 직렬화됨(동작 변화 없음).
  - `EnemyAI_Melee.asset`: 공격 노드 `ExitRange` = 2.5. Behavior 편집 API(`SetField` → `BuildRuntimeGraph` → `SaveAsset`, 리플렉션)로 넣음, YAML 직접 수정 없음. 런타임 그래프 다시 만들면서 rid 가 바뀌어 diff 가 큼.
- 검증: `Assets/Test/Boss/Map_4_Security_Boss_Phase_T.unity`(에디터에 열려 있던 씬, 저장하지 않음) Play, Room_1 근접 3마리. 같은 Play 에서 런타임 인스턴스만 옛 값으로 되돌려 비교.
  - 정지 8초 + 왕복 12초 시나리오:
    | 항목 | 옛 값 | 새 값 |
    |---|---|---|
    | 몸 속도 | 5.0 m/s | 5.0 m/s |
    | 모션이 뜻하는 발 속도(배율 × 8) | 13.33 m/s | 5.00 m/s |
    | 바라보는 방향–이동 방향 평균 각 | 39~58° | 9~12° |
    | 90° 넘게 어긋난 이동 프레임 | 20~29% | 4~6% |
  - 경계 시나리오(z=-6 빈 줄, 플레이어가 0.8 m/s 로 멀어짐, 센서 근접 반경 15 m 임시, ExitRange 2.5/0 번갈아 2회씩): **차이 없음**(10초당 공격 7~8회, 이동 전환 14회, 거리 1.9~3.0 m 두 조건 같음).
  - 원인: `EnemyMeleeAttack._holdDuration` 1.2초가 쿨타임 1초보다 길어 붙잡기가 풀리면 바로 다시 공격 가능 → 진단 3번(경계 깜빡임)은 현재 값에서는 생기지 않음. **진단 3번은 틀렸음.** ExitRange 2.5 는 쿨타임이 붙잡기보다 길어질 때를 위한 보호용으로만 남음.
- Play 중 에디터가 백그라운드라 프레임이 안 돌아 `EditorApplication.QueuePlayerLoopUpdate` 펌프와 `Application.runInBackground = true`(Play 중 런타임 값, ProjectSettings 변화 없음 확인)를 썼다. Play 종료로 모두 해제.
- 남은 것: 90° 넘게 어긋난 이동 4~6% 는 동료 밀어내기(1.5 m/s)·급회전 순간으로 추정(미확인). 다리 0.625배속 달리기가 느려 보이는지 **사용자 육안 확인 전**. 게이트 루프 미실행, 커밋 전. Ranged·Special 도 기준값 3(클립 실측 안 함).
