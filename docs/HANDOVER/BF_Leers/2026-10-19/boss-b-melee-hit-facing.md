# 보스 B 근접공격·피격 회전 복구

## 작업 요약 및 방법
- `SmallBoss`의 비어 있던 `Attack.None` 가지에 기존 `TimeOut(0.5초) → 추격` 가지를 옮겼다. 비어 있는 Switch 가지는 Success를 반환하므로 기존 Selector의 추격 fallback에는 도달하지 않았다.
- Unity Editor API로 BT를 재생성했다. 작성 그래프에는 존재하지만 저장된 실행 그래프에서 빠져 있던 돌진 뒤 6초/도약 뒤 8초 쿨타임 후속 노드도 복구됐다.
- `AIController._shouldFaceVisibleTargetOnHit`(기본 false)를 추가하고 BossB 프리팹만 true로 설정했다. 전투 중 피격 시 실제 VisibleTarget으로 회전을 요청하고 경직 중에도 추적한다. 기존 경직 시간을 늘리지 않으며 Hold·슈퍼아머의 방향 고정은 보존한다.
- 테스트 씬 `Assets/Test/Boss/Map_4_Security_Boss_T.unity`의 `05_Boss` NavMeshSurface가 RenderMeshes로 베이크돼 타일 사이 경로가 끊겨 있었다. PhysicsColliders로 다시 베이크하고 결과는 테스트 씬 전용 경로 `Assets/Test/Boss/Map_4_Security_Boss_T/NavMesh-05_Boss-PhysicsColliders.asset`에 저장했다. 기존 테스트 씬에는 geometry 설정과 새 데이터 참조를 저장했다.

## 변경된 파일
- `Assets/_Project/Scripts/AI/AIController.cs`
- `Assets/_Project/Prefabs/AI/Boss/BossB.prefab`
- `Assets/_Project/BehaviorTree/SmallBoss.asset`
- `Assets/Test/Boss/Map_4_Security_Boss_T/NavMesh-05_Boss-PhysicsColliders.asset` 및 meta(신규)
- 위 테스트 씬(기존 Git 제외 경로, 로컬 적용)
- intent-046 및 포인터

## 결정 사항
- 기존 근접 선택 거리 4m, 콤보 3타, 돌진/도약 쿨타임 값은 유지한다. 공격 가중치 변경 대신 추격 차단 원인을 수정했다.
- 다른 적의 전투 중 피격 회전은 기본 설정을 유지한다.
- 사용자 지시로 테스트 씬 전용 NavMesh 데이터는 `Assets/Test/Boss/Map_4_Security_Boss_T/`에 둔다. 기존 파일과의 충돌을 피하고 GUID를 보존하여 이동했다. 잘못 생성했던 `_Project/Scenes/Map_4_Security/NavMesh-05_Boss.asset` 및 meta는 남아 있지 않다.

## 검증
- Unity 재컴파일: completed, failed=false, compilationFailed=false.
- 기존 `BossBAttackRuleTests`: 11/11 Passed.
- Play: 실제 `OnSkillUsed` 이벤트에서 Charge → ChargeRun → ChargeStop → Combo1 → Combo2 → Combo3 확인. 연속 돌진의 종료→다음 시작 간격 6초 이상 확인.
- NavMesh 교체 전: 타일 간 PathPartial. 교체 후: 저장된 데이터로 9개 타일 중심의 모든 조합 81/81 PathComplete 확인.
- Play: 돌진/도약 쿨타임을 테스트용으로 미래 시각에 둔 상태에서 추격이 3초 동안 11.62m 이동, 플레이어와 거리 15.63m → 4.02m. 이후 근접 범위 3.65m까지 접근 확인.
- Play 피격 이벤트: 180도 뒤의 실제 플레이어를 향해 최종 각도 0도. 기본 false일 때 기존 방향 유지, true일 때 플레이어 방향 요청, Hold 중 방향 유지 모두 확인.
- 테스트 후 신규 콘솔 error 항목 없음(기존 Inspector의 SerializedObject/MissingReference 오류는 잔존). 코드 diff 공백 검사 통과. Unity YAML 자동 직렬화의 빈 Name 뒤 공백은 존재.

## 현재 상태 및 이슈
- 이번 버그 수정 완료, Play 종료, 테스트 씬 저장 완료. 커밋하지 않았다.
- 기존 미커밋 변경을 되돌리지 않았다. 작업 중 다른 주체의 Git 변경이 관찰됐으므로 최종 커밋 시 전체 파일 범위를 다시 확인할 것.
- Play 검증 중 보스 A 공격을 비활성화하고 플레이어 HP를 올려 보스 B 동작을 격리했다. 전체 보스전의 체감 밸런스는 사용자 플레이로 확인하면 된다.

## 다음 할 일
- 실제 조작으로 근접 빈도와 회전 체감 확인.
- 다른 PC에서 테스트 씬을 구성한다면 위 Geometry/데이터 참조도 적용.
