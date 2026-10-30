# 보스A 그랩 시작 높이 수정 (AI_TODO2 보스-26, intent-094)

## 작업 요약 및 방법
- 원인: `BossHand.LateUpdate` 가 날아가는 동안(`Flying`) 손 본(Palm, `GrabPoint` 포함)을 손 오브젝트보다 `_boneLift`(8m) 위에 둠. `BossAGrab` 은 손 오브젝트를 바닥 + `_hookHeight`(1m)로 보내 집게·끌려가는 플레이어가 바닥 +9m 에 떴다.
- 수정: `BossHand._grabLift`(기본 0) 추가, 그랩 자세 가중치만큼 손 본 높이를 `_grabLift` 로 보간. 프리팹 수정 없음.
- 같은 세션에서 보스-17 점검: 충격파는 `BossBLeap._landingVfx`(`SOEffectData_Leap` → `ThirdParty/VFX/AI/ToonBodySlam.prefab`)가 착지 때 재생됨. 프리팹이 Git 밖이고 두 도약 스킬(`_shockwaveTiles` 1·0.5)이 같은 이펙트를 써 범위가 어긋날 수 있음. 사용자가 이펙트를 따로 추가하기로 함(수정 없음).

## 결정 사항
- 콜라이더(손 오브젝트)와 `BossAGrab` 로직은 그대로, 시각(손 본) 높이만 내린다.

## 현재 상태 및 이슈
- 리컴파일 통과(오류 0). 직접 플레이·게이트 루프 미실행, 커밋 전.
- 손 모델이 바닥에 파묻히거나 아직 높아 보이면 `BossA.prefab` 의 `_hookHeight`(BossAGrab)·`_grabLift`(양손 BossHand) 조정.
- `_grabWeight` 를 갱신하기 전 값으로 높이를 계산해 1프레임 지연이 있다(무시 가능).

## 다음 할 일 (Next Steps)
- 보스전에서 그랩 비행·끌려오는 높이를 확인하고 값 조정.
- 보스-27(끌려갔다 돌아오는 느낌)은 표현 방식 결정 후 진행.
