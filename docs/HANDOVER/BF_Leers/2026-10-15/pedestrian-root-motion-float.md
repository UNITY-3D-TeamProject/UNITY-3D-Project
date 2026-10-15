# 보행자가 점프 후 공중에 뜬 채 걷는 문제 (2026-10-15)

관련 intent: [intent-025 AI 애니메이션](../../../intent/intent-025-enemy-animation.md) (제약: "Animator의 Apply Root Motion은 끈다")

## 1. 작업 요약 및 방법
- 증상: 보행자가 추월 점프를 한 뒤 땅에 내려오지 않고 떠 있는 채로 걷는 경우가 자주 생겼다.
- 원인: `Pedstrian.prefab`의 모델(`LilRobot`) Animator가 `Apply Root Motion = 1`이었다. FBX의 `Jump` 클립은 Bake Into Pose(Y)가 꺼져 있어, 점프 클립의 `Center` 뼈 상승분이 모델 트랜스폼(`LilRobot`의 localPosition.y)을 올렸다. 착지하면 `IsJumping`이 꺼져 클립이 내려오기 전에 끊기므로 올라간 만큼이 되돌아오지 않았다. 몸(CharacterController)은 땅에 있고 모델만 떠 있던 것이다.
- 수정: `Assets/_Project/Prefabs/AI/NPC/Pedstrian.prefab`의 Animator `m_ApplyRootMotion` 1 → 0 (코드 무변경). 이동은 전부 `CharacterMotor`가 하므로 루트 모션이 필요 없다.
- 검증 (Map_1_Lobby Play)
  - 수정 후: 점프 27프레임 동안 모델 localY 최대 0.000
  - 대조(런타임에 Root Motion을 다시 켠 상태): 점프 40프레임 동안 모델 localY 최대 1.311 → 원인 확인
  - 콘솔 에러 0

## 2. 결정 사항
- 모델 Animator의 Root Motion은 프리팹에서 끈다. FBX 임포트 설정(Bake Into Pose)은 `Assets/ThirdParty`라 Git에 안 올라가므로 쓰지 않는다.

## 3. 현재 상태 및 이슈
- `Ranged.prefab`의 Animator도 `Apply Root Motion = 1`이다(미수정). intent-025 제약과 어긋나며 같은 식으로 모델이 밀릴 수 있다.
- 앞 보행자 머리 위에 착지하는 경우(두 번째 후보)는 이번 Play에서 확인하지 못했다. 수정 후에도 떠 있는 개체가 보이면 그쪽을 조사한다.

## 4. 다음 할 일 (Next Steps)
- `Ranged.prefab` Root Motion 끌지 결정하고 적용한다.
- 장시간 Play에서 점프 후 떠 있는 보행자가 더 없는지 눈으로 확인한다.
