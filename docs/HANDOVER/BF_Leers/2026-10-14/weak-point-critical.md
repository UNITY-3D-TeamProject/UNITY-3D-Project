# 약점 치명타 판정 1단계 — 무작위 약점 + 거리 판정 + 로그 (2026-10-14)

관련 intent: [intent-023](../../../intent/intent-023-weak-point-critical.md)
브랜치: `feature/AI/WeakPoint` (develop에서 분기)

## 1. 작업 요약 및 방법
- `SEffectContext`에 `Vector3? HitPoint`를 추가했다. 생성자의 마지막 매개변수이고 기본값은 null이다. `SOAttributeEffect.Apply`가 context를 다시 만들 때 이 값을 복사한다.
- `BulletController.ApplyHitEffect`가 `hitPoint: transform.position`을 넘긴다. TODO 주석을 달았다: 테스트용이고, 최대 약 0.33m 오차가 있다.
- `CharacterCombat`에 `[SerializeField] SphereCollider _weakPoint`와 `IsCriticalHit(Vector3? hitPoint)`를 추가했다.
  - 판정식은 `|HitPoint − 약점 중심| ≤ radius × lossyScale 최대 성분`이다.
  - null, 콜라이더 꺼짐, 비활성, HitPoint 없음은 모두 false다.
- 신규 `Combat.WeakPointSelector`: `OnEnable`에서 `_candidates` 중 하나를 무작위로 골라 `_weakPoint`를 그 자식으로 옮긴다(local 0, 회전 초기화). `SetActive`는 건드리지 않는다.
- `CombatMediator`의 Health 콜백: HP가 감소할 때만 `IsCriticalHit(data.Context.HitPoint)`로 판정하고 `Critical/Normal Hit` 로그를 남긴다. 피해량은 바꾸지 않는다.
- 프리팹(Unity MCP로 작업, `LoadPrefabContents` → `SaveAsPrefabAsset`):
  - Melee: 후보 5개를 `head`, `neck_01`, `spine_03`, `spine_01`, `pelvis` 뼈 아래에 둔 `WeakPointCandidate_1~5`로 만들었다.
  - Ranged: 후보 5개를 `RigHead`, `RigRibcage`, `RigLArm1`, `RigRArm1`, `RigPelvis` 아래에 만들었다.
  - 두 프리팹 공통: `WeakPoint`는 SphereCollider(trigger, 반지름 0.3, 레이어 `Ignore Raycast`)를 단 오브젝트로, 처음에는 후보 1 아래에 둔다.
  - 두 프리팹 공통: 루트에 `WeakPointSelector`를 붙이고, `CombatPrefab`의 `CharacterCombat._weakPoint`에 연결했다.

## 2. 결정 사항
- 치명타 판정은 거리 계산 하나뿐이다. 약점 콜라이더는 판정 데이터(중심, 반지름, 켜짐 여부)로만 쓴다. 물리적으로 맞지 않도록 `Ignore Raycast` 레이어의 trigger로 둔다.
- 1단계는 판정 로그만 남긴다. 피해 곱, `_criticalMultiplier`, `OnCriticalHit`, AttributeSet Pre 콜백 변경은 사용자 결정으로 뒤로 미뤘다.
- AttributeSet, AttributeMediator, CharacterMediator는 수정하지 않았다. 적은 루트 AttributeSet을 직접 쓰고, On 콜백이 이미 context를 전달한다.
- 약점 위치 선택은 `OnEnable`에서 한다. 풀에서 재사용할 때도 새로 고르고, 스캔 파트와 합의한 방식과 같다.

## 3. 현재 상태 및 이슈
- 컴파일 에러 0.
- Play 검증(`Map_4_Security`, eval, 씬 저장 안 함):
  - Melee 20회 생성: 후보 5곳에 모두 분포했다(2/2/7/5/4).
  - `PlayerFireEffect.Apply` + HitPoint 결과:
    - 약점 중심 → Critical
    - 0.29m → Critical
    - 0.31m → Normal
    - null → Normal
    - 콜라이더 `enabled = false` → Normal
    - HP 감소량은 모든 경우 1로 동일했다.
  - **실제 총알**(Bullet.prefab 인스턴스, `_hitLayers`만 런타임에 Enemy 추가): 약점(pelvis) 높이로 쏘아 `Critical Hit (HitPoint: …799.92)`를 확인했다. 약점 중심과 약 0.1m 거리였다. 총알이 약점 콜라이더에 막히지 않고 CC에 맞았다.
  - 실제 총알 Normal 경우는 시험 중 적이 공중에서 낙하해 깔끔하게 확인하지 못했다. Normal 판정 자체는 eval로 확인했다.
- **알려진 문제(범위 밖):** `Bullet.prefab`의 `_hitLayers`가 128(Obstacle)뿐이다. 적이 Enemy 레이어로 옮겨진 뒤(intent-022)라 플레이어 총알이 적을 맞히지 못한다. 실제 게임에서 확인하려면 384(Obstacle + Enemy)로 바꿔야 한다.
- 후보 위치는 뼈 위치 기본값이다. 몸 안쪽 깊은 후보(예: Ranged의 `RigPelvis`, `RigRibcage`)는 CC 표면까지 0.3m보다 멀 수 있어 치명타가 나지 않을 수 있다.
- `ProjectSettings/EditorBuildSettings.asset`는 에디터가 줄바꿈만 바꾼 것이다. 이번 작업과 관계없으므로 커밋하지 않는다.

## 4. 다음 할 일 (Next Steps)
- [USER] Scene 뷰에서 후보 5곳의 위치와 약점 반지름을 조정한다. 약점 구가 CC 캡슐 표면에 닿도록 한다.
- [USER] `Bullet.prefab`의 `_hitLayers`를 Enemy 레이어까지 포함하도록 바꿀지 결정한다(팀 공유).
- 2단계: 치명타 피해 곱을 적용하고 `OnCriticalHit` 이벤트를 추가한다(intent-023 열린 질문).
- 공유:
  - 이세훈: `SEffectContext.HitPoint` 추가
  - 김성준: 약점 오브젝트 구성. 판정은 거리 계산이고, 약점 콜라이더는 `Ignore Raycast` trigger다. 스캔 컴포넌트는 `WeakPoint` 오브젝트에 붙이면 된다.
