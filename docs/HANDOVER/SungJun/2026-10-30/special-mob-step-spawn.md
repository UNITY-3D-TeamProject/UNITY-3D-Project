# 보안맵 2라운드 특수몹: 발판을 밟을 때 소환 · 뒤 발판 가라앉힘 · 사망 전체 리셋 · 낙사 지금 발판 복귀 (intent-095)

## 1. 작업 요약 및 방법
- 문제: 다음 발판이 나타나는 순간 그 발판의 특수몹을 미리 만들어 둬서(`PrepareCurrentStep`) 건너가기 전에 원거리로 잡을 수 있었고, 지나온 발판으로 되돌아갈 수 있었다.
- `SpecialMobRound`
  - 미리 만들기 제거. 플레이어가 발판 `CombatZone` 트리거에 들어오면 그때 만든다 (`CombatZone` 기존 동작, 코드 변경 없음).
  - `SPlatformStep` 에 `Platform`(StateMover)·`FallReturnPoint` 추가.
  - 구역 시작(`CombatZone.OnStarted`) 때 바로 뒤 발판을 `StateMover.Activate`(-40 m, 3초)로 가라앉히고, `HideLoweredPlatforms`(2라운드 동안 `SecurityApp.Update` 가 호출)가 다 내려가면 끈다.
  - 같은 때 `StageManager.SetFallReturnPoint` 로 낙사 복귀 자리를 그 발판으로 바꾼다. 다른 라운드가 되면 `ClearFallReturnPoint`.
  - 사망: `ResetUnclearedSteps` → `ResetSteps`. 깬 구역까지 `CombatZone.ResetAll`(신규), 가라앉힌 발판 `ResetImmediately`, `Platform_1` 만 켜고 나머지·`Platform_6` 끔.
- `StageManager.SetFallReturnPoint(Transform)` 신규: 지정돼 있으면 낙사 때 체크포인트 대신 그곳으로 보낸다(데미지는 그대로).
- 씬 `Map_4_Security`: `Platform_1~4` 자식 `FallReturnPoint`(발판 중심 x −8 m, y 0.2, +X 방향) 추가, `SecurityApp._specialMobRound._steps` 연결.
- 소환 연출(스케일 0.01→1 + 바닥 DTT 원 + 자라는 동안 Hold)은 `AI/EnemySpawnAppear` 로 만들어 Play 확인까지 했으나, 사용자 요청으로 되돌림(스크립트 삭제, `Special.prefab` 원복). 지금은 연출 없이 바로 나온다.

## 2. 결정 사항
- 사망 시 2라운드 전체를 처음부터(4마리 다시, 아이템 드롭도 다시). 사용자 결정.
- 낙사는 지금 발판(마지막으로 싸움이 시작된 발판)으로 복귀 + 낙하 데미지, 특수몹 유지. 사용자 결정. (뒤 발판을 없애면 기존 방식은 가라앉은 Platform_1 자리 허공으로 보내 무한 낙하하므로 필요)
- 뒤 발판은 즉시 끄지 않고 보스 라운드와 같은 `StateMover` 가라앉힘. 사용자 결정.
- 소환 연출은 보류. 새 코드 없이 되는 방법은 없다. 다시 넣는다면 적 컴포넌트 대신 `CombatZone` 에 넣는 방법도 있다.
- `ResetSteps` 는 `IsActivated` 인 발판만 `ResetImmediately` 한다. 한 번도 켜진 적 없는 `StateMover` 는 `Start` 전이라 원위치가 (0,0,0)이어서 리셋하면 원점으로 날아간다.

## 3. 현재 상태 및 이슈
- 리컴파일 통과. Play(텔레포트·HP 0 으로 강제 진행)에서 확인: P1 진입 때만 몹 생성, P1 클리어 후 P2 비어 있음, P2 진입 때 몹 생성·P1 가라앉아 꺼짐, P2 중 낙사 → P2 복귀·데미지 10, 사망 → P1 만 켜지고 진행도 초기화, 1~4 클리어 후 3라운드 진입 → 낙사 복귀 해제·1~4 내려감. 에러 0.
- 리스폰 자리가 `Platform_1` 구역 안이라 다시 태어나자마자 약 7 m 앞에서 P1 몹이 생성돼 바로 움직인다 (연출을 뺐으므로 묶는 시간 없음).
- 에디터가 백그라운드면 Play 프레임이 멈춘다. eval 로 `Application.runInBackground = true` 를 주면 돈다.
- 씬 저장 때 `Canvas_InGame` 레이아웃(RectTransform) 값이 함께 바뀌었다 (이전에도 있던 현상).
- 이번 작업과 무관한 변경이 작업 트리에 있음: `Pretendard-Bold SDF.asset`, VFX 머티리얼 meta, NavMesh meta, ProjectSettings, `UNITY-3D-Project.slnx` 삭제.
- 커밋 안 함. 직접 조작(점프로 건너가기·실제 전투) 미확인.

## 4. 다음 할 일 (Next Steps)
- 직접 플레이로 2라운드 전체(점프 이동·전투·낙사·사망) 손맛 확인 후 intent-095 해결 처리.
- 리스폰 직후 몹이 너무 가깝다면 `Platform_1` 구역 시작을 앞으로 당기거나, 소환 연출(묶는 시간)을 다시 검토.
