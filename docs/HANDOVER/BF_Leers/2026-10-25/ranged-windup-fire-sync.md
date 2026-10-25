# 원거리 몹 조준선 → 사격 모션 연결

## 작업 요약 및 방법
- 원인: `EnemyAI_Ranged` 의 `WindupDuration` 0.3초가 끝나면 조준선이 꺼지고 그제서야 사격 모션이 시작됐다. 총알은 `Ranged.prefab` 의 `EnemyRangedAttack._fireDelay` 0.33초 뒤에 나가므로, 조준선이 사라진 뒤 약 0.33초 동안 선 없이 모션만 재생되다 발사됐다.
- 수정(`AIRangedAttackAction.cs` 만): 사격 모션(`Attack` 스킬)을 조준선이 꺼지기 `FireDelay` 전에 시작해, 조준선이 꺼지는 순간 총알이 나가게 했다. 새 블랙보드 값 `FireDelay`(기본 0.33, 그래프에 없으면 기본값 사용, `AttackMotionDuration` 과 같은 방식).
- `WindupDuration` < `FireDelay` 면 예비 동작이 `FireDelay` 까지 늘어난다(현재 그래프: 0.3 → 0.33초).

## 2차: 팔이 와리가리 하는 문제 (사용자 지적)
- 원인: `AttackWindup` 이 `EnemyAnimator` 매핑으로 `IsWindup` 를 켜서 Windup 상태(Left Rapid Attack, 팔을 든 정지 포즈 0.3초 루프)로 들어갔다가 Attack 상태(Left Blast Attack)로 넘어갔다. Blast Attack 은 t=0 에 팔이 내려 있고 0.1초에 걸쳐 들어 올리므로 팔이 "들기(Windup) → 내림(Attack 시작) → 다시 듦"을 반복했다. 전환 중에는 끊을 수 없어(InterruptionSource 0) 1차 수정 후에도 Idle→Windup→Attack 으로 거쳐 갔다.
- 수정: `Ranged.prefab` 의 `EnemyAnimator._skillAnimations` 에서 `AttackWindup → IsWindup` 항목을 제거(코드·컨트롤러 변경 없음). 이제 사격 모션 하나가 팔 들기→조준 유지→발사→내리기를 전부 맡고, 조준선은 스킬로 계속 표시된다. 저장 시 `_soundMediator`·`_proximityRange` 기본값 줄이 프리팹에 같이 직렬화됨(값 변화 없음).
- Windup 상태·`IsWindup` 파라미터는 컨트롤러에 남아 있으나 Ranged 에서는 더 이상 쓰이지 않는다.

## 3차: 대상을 보고도 1~3초 쉬고 쏘는 문제 (사용자 지적)
- 원인: 노드 시작 시 `BeginWait()` 가 `MinInterval`~`MaxInterval`(그래프 값 1~3초) 랜덤 대기를 먼저 넣었다.
- 수정(`AIRangedAttackAction.cs`): 시작 직후엔 `BeginFirstWait()` 로 기다리지 않고 바로 예비 동작에 들어간다. 노드가 다시 시작되는 경우의 연사를 막으려고 직전 사격(`_lastShotTime`) 후 `MinInterval` 은 지킨다. 사격 후 간격(1~3초)은 그대로.

## 결정 사항
- 조준선 길이와 모션을 맞추고, 예비 동작 전용 애니메이션은 쓰지 않는다. `WindupDuration` 을 `FireDelay` 보다 크게 하면 그 차이만큼 팔을 내린 채 조준선만 보인다.
- `FireDelay` 는 `RangedAttack` 스킬의 Fire Delay 와 같은 값을 유지해야 한다(중복 값).

## 현재 상태 및 이슈
- 리컴파일 통과. Play 육안 확인·게이트 루프 미실행, 커밋 전.
- 사격 모션 시작 후 노드가 중단되면(사거리 이탈 등) 조준선은 꺼지지만 이미 발동한 총알은 나간다.

## 다음 할 일
- Play 에서 원거리 몹 조준선 → 사격이 자연스러운지 육안 확인.
