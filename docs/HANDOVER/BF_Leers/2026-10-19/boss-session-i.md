# 보스전 세션 I — B 컨트롤러·스킬·BT 노드 UNITY-10·11 (2026-10-19)

- 스펙: `spec/boss.md`(로컬 전용) · 이전 기록: [보스 세션 H](./boss-session-h.md)
- 브랜치: `feature/AI/Boss`

## 1. 작업 요약 및 방법

- `BossB/`: `BossBController`(Initialize·OnDowned·Revive·Retire·돌진 중계·낙하 안전망), 공통 기반 `BossBSkillBase`, 스킬 `BossBMeleeCombo`·`BossBCharge`·`BossBLeap`(`_isEvade`).
- `BT/`: `BossBIsOnTelegraphedTileCondition`, `BossBEscapeTileAction`, `BossBChooseAttackAction`, 블랙보드 열거형 `EBossBTAttack`·`EBossBTStage`(`BossBTEnums.cs`).
- 검증: MCP `recompile` 컴파일 실패 없음. `.meta` 생성 확인. Play·Codex 리뷰·게이트 루프 미수행.
- 실행자: Claude 직접 작성(기존 AIController·스킬·모터 문맥을 이미 읽은 상태라 위임 비용이 더 큼).

## 2. 결정 사항

- `BossBSkillBase`(SkillBase 파생, `[DefaultExecutionOrder(-50)]`): `BeginRun`이 Hold, `EndRun`이 ReleaseHold. 피해는 대상 수평 거리 ≤ 반경(높이 무시, A와 같음), 넉백은 `BossKnockback`.
- 돌진 `NotifyChargePrepared`는 스펙 순서(예고 뒤)가 아니라 **예고 시작 때** 부른다(A가 예고 중에 옆 칸을 쳐야 하므로). 방향은 시작 때 고정.
- 급정거: 몸 앞 지점(`_bodyRadius` + 한 걸음)이 판 밖·구멍·예고면 멈춤. 슈퍼아머는 건드리지 않음(Hold가 경직을 막고 기본 false).
- 도약: 모터 `Jump(중력×T/2)` + 수평 변위 선형. 착지는 체공 절반 이후 `IsGrounded`, T+1초 넘으면 강제 착지. 예고 원판은 B 스킬이 `_circlePrefab`을 직접 생성(`IArenaQuery`는 읽기 전용이라 `ShowCircle` 불가). 충격파 반경 = `TileSize × _shockwaveTiles`.
- 안전망: 발밑 칸 붕괴 시 실행 중 스킬 `StopSkill` → `ExecuteSkill(_evadeSkillName)`. 경직으로 무시되면 최대 1초 재시도.
- `BossBEscapeTileAction`은 예고 남은 시간을 모르므로 `IsStuck`(목적지 제한 시간)·경로 없음일 때 회피 도약 요청.
- 블랙보드: `Stage`는 `EBossBTStage`(진행이 `(EBossBTStage)(int)stage`로 씀), 공격 결과는 `EBossBTAttack`(Switch용). Logic asmdef는 Unity 참조가 없어 `[BlackboardEnum]`을 못 붙이기 때문.

## 3. 현재 상태 및 이슈

- Play 미검증, 게이트 루프·Codex 리뷰 미실행, 커밋 전.
- 임시값: 콤보 범위 4, 돌진 속도 18·예고 1.5·몸 반경 2, 도약 체공 1·착지 반경 4·넉백 8 등.
- 도약 중 NavMeshAgent가 공중에서 NavMesh를 벗어나는 동작, 회피 도약 중 붕괴 직후 낙하 높이는 Play 확인 필요(세션 M).
- 스킬 컴포넌트 `_hitEffect`·`_circlePrefab` Inspector 연결 필요(세션 K).

## 4. 다음 할 일 (Next Steps)

- 세션 J: UNITY-12(`BossEncounter`), UNITY-13(`BossFightSpawner`). B 이벤트 `OnDowned`·`OnChargePrepared(origin, direction)`·`OnChargeEnded` 구독, 블랙보드 `Stage` 쓰기.

## 5. 게이트 루프 결과 (세션 종료 시점)

- 자동 검증(컴파일·EditMode 88/88·5/5)은 4회 모두 통과. 시도 한도(최초 + 보정 3회)를 소진해 `review_required`로 멈춤.
- Codex 리뷰 지적 3회 모두 `BossBLeap` 착지 안전성: ① 체공 중 착지 칸 재검사 추가 ② 체공 시간 이후 변경을 따라잡기(0.2초)·도착 반경 1m 이내일 때만 착지 인정 추가 ③ **마지막 지적(미반영)**: 시간 초과(`FLIGHT_OVERTIME_SECONDS`) 강제 착지가 안전한 칸 도착을 보장하지 않음.
- 수동 증빙(USER-1, USER-4)은 대기. 한도 때문에 사용자 허가 없이 재시도하지 않음.
