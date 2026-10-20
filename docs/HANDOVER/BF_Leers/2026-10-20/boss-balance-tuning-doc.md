# 보스 밸런스 조절 문서 + 손 분리 적용 방식 확인 (2026-10-20)

## 작업 요약 및 방법
- 사용자 요청 3가지: ① 보스 A 예고 수치 조절 방법 ② 보스 A 손 분리 FBX 적용 ③ 보스 B 도약 범위 조절 방법. ①③은 md로 정리.
- ①③: 코드(`BossAPatternBase`·`BossController`·`BossHand`·`BossShoulderCannon`·`BossBLeap`·`BossArena`)를 읽고 `docs/features/boss-balance-tuning.md`를 만들었다. 코드·에셋 변경 없음.
- ②: `Assets/ThirdParty/보스 추가 애니메이션/큰보스/Models/Boss/BossA_Hands.fbx`를 확인했다. 정적 메쉬 `Hand_L`·`Hand_R` 2개, 머티리얼 `Gripper Robot`. 적용은 아직 안 했다.

## 결정 사항
- B 도약 범위가 큰 이유: 예고 원 지름 = `_tileSize`(20 m) × `_shockwaveTiles`(1) × 2 = 40 m. 원은 충격파 범위이고 착지 피해 반경(`_landingRadius` 4 m)과는 따로다. 줄이려면 `_shockwaveTiles`를 줄인다(0.3 → 지름 12 m).
- A 예고 체감 시간 = `_telegraphSeconds × 배율` + `BossHand._moveSeconds` + `_slamSeconds`(A4는 예고 중 손이 떠서 max 사용).

## 현재 상태 및 이슈
- (정정) 이 문서를 쓸 때는 보스 프리팹을 못 찾았으나, 이후 `develop` 병합으로 `Prefabs/AI/Boss/BossA.prefab`·`BossB.prefab`이 들어왔다. 프리팹의 실제 값을 읽어 보니 문서의 기본값과 같다. 손 분리 적용 결과는 [보스 A 분리 손 메쉬 적용](./boss-a-hand-mesh-apply.md).
- 사용자 질문: "메쉬를 따로 뺄 필요 없이 본만 내려치면 되지 않나?" Gripper Robot 스킨 가중치를 확인한 결과:
  - 거의 모든 정점이 본 하나에 100%(강체 스킨)라 `RigLArmPalm`/`RigRArmPalm`만 옮겨도 손이 통째로 움직인다.
  - 단, 손목 정점 10개씩은 `Palm`+`Arm1` 섞인 가중치라 손이 멀리 가면 늘어난다.
  - Animator가 매 프레임 본을 덮어쓰므로 `BossHand`가 본을 움직이려면 LateUpdate에서 써야 한다(코드 변경).
  - 손이 몸에서 멀어지면 SkinnedMeshRenderer 경계 때문에 컬링될 수 있다(`updateWhenOffscreen`).
- 사용자 답을 기다리는 중. Unity 에디터 메인 스레드가 한동안 응답하지 않다가(60초 타임아웃) 다시 응답했다.

## 다음 할 일 (Next Steps)
- 사용자에게 본 방식 / 분리 메쉬 방식 중 하나를 확정받고 ②를 진행한다.
- 보스 A 프리팹 위치를 사용자에게 다시 확인한다.
- 프리팹이 생기면 문서 기본값을 실제 Inspector 값과 대조한다.
