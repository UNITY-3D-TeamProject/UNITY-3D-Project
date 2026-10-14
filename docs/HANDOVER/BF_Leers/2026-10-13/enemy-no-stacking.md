# 적끼리 올라타기 방지 (충돌 끄기 + 약하게 밀어내기)

관련 intent: [intent-022](../../../intent/intent-022-enemy-separation.md) (`open`, "3차" 섹션), 이전 기록: [enemy-separation-fix.md](./enemy-separation-fix.md)

## 1. 작업 요약 및 방법
- **문제**: CharacterController끼리 밀면 캡슐 윗부분 반구를 타고 서로 올라탄다. 올라간 뒤에는 `isGrounded`가 true라 `CharacterMotor`가 중력을 -2로 고정해 그대로 붙어 있는다. `Melee` CC(높이 1, 반지름 0.4)는 거의 구라서 특히 잘 올라간다.
- **A. 적끼리 충돌 끄기**: `ProjectSettings/DynamicsManager.asset` 레이어 충돌표에서 Enemy(8) ↔ Enemy(8)를 해제했다(MCP eval `Physics.IgnoreLayerCollision(8, 8, true)` + 저장. 8번 행에서 8번 비트만 꺼진 것을 확인했다).
- **B. 약하게 밀어내기**: `Assets/_Project/Scripts/AI/AllySeparationPusher.cs`(신규). `Update`에서 `AIController.TryGetAwayFromAllies(_pushRadius)`가 true면 `CharacterMotor.AddExternalDisplacement(dir * _pushSpeed * dt)`.
  - `Melee.prefab`, `Ranged.prefab` 루트에 추가했다(`_controller` = 루트, `_motor` = 자식 `MovePrefab`). Melee 반경 0.9 / Ranged 3.0, 속도 1.5.
- **버그 수정**: `AIController.TryGetAwayFromAllies`의 가중치 `1 - distance/radius`를 `Mathf.Clamp01`로 감쌌다. 동료 중심이 반경 밖이고 콜라이더 표면만 걸리면 음수가 돼 방향이 동료 쪽으로 뒤집혔다(간격 벌리기 BT에도 같이 적용된다).
- **검증 (Play, Map_4_Security)**: Melee 4마리를 0.3m 간격으로 겹쳐 생성 → 3초 후 최소 간격 1.04m. 행동 트리를 끄고 같은 목적지로 몰았을 때 최소 간격 1.00m. 4마리 모두 y가 0.08(바닥)로 올라탄 개체 없음. 컴파일 에러 0, 새 경고 0.
  - 테스트용 생성 개체는 `MoveSpeed`가 0이라 `CharacterMotor.Speed`를 직접 넣어야 움직였다(이전 기록과 같은 현상).
  - 수정 전과 비교하려고 Play 중에 충돌을 다시 켰지만 이미 생성된 적끼리는 충돌이 다시 걸리지 않아 재현하지 못했다. 설정은 다시 해제 상태로 되돌렸다.

## 2. 결정 사항
- 적끼리는 물리적으로 부딪히지 않는다. 겹침은 `AllySeparationPusher`(약한 밀어내기)와 간격 벌리기 BT가 막는다.
- 밀어내기는 `AIController`(중재자를 통해서만 이동한다는 규칙)와 `CharacterMotor`(플레이어와 같이 씀)에 넣지 않고 별도 컴포넌트로 뒀다.
- 트리거 콜라이더 + Rigidbody 방식 대신 기존 Overlap 질의(`TryGetAwayFromAllies`)를 재사용했다. 이 질의는 ID가 작은 동료만 피하므로 두 적이 서로 밀어 떨지 않는다.

## 3. 현재 상태 및 이슈
- 레이어 충돌표 변경은 **프로젝트 공용 설정**이라 팀원에게 알려야 한다(Enemy 레이어를 쓰는 다른 오브젝트가 있다면 영향).
- `Ranged` CC가 높이 2.88, 반지름 1.44인 구 형태라 모델보다 넓어 보인다. 의도한 값인지 확인 필요(밀어내기 반경 3.0도 이 값에 맞춘 것).
- 실제 전투(플레이어 추격·공격 중 밀집) Play는 아직 안 했다.
- 이전 기록의 [USER] 그래프 수정(Ranged Guard Abort None, Spread 변수 Shared 해제)은 여전히 남아 있다.

## 4. 다음 할 일 (Next Steps)
1. 실제 전투 Play: 근거리 여러 마리가 플레이어를 둘러쌀 때 올라타지 않고, 너무 밀려나 공격 사거리(2m)를 벗어나지 않는지 확인한다. 필요하면 `_pushRadius`/`_pushSpeed`를 조정한다.
2. 팀원에게 Enemy ↔ Enemy 충돌 해제 공지.
3. `Ranged` CC 크기 확인.
4. 위 항목 확인 후 intent-022 resolved 처리.
