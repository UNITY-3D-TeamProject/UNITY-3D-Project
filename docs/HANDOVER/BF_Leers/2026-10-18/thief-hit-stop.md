# 도둑이 Hit 모션 중에 이동하던 문제 (피격 경직 = 클립 길이)

## 작업 요약 및 방법
- 증상(사용자 보고): 도둑이 총알에 맞으면 Hit 모션이 나오면서 계속 이동한다. Hit 모션 동안은 멈춰 있어야 한다.
- 확인한 사실
  - `Thief_Override`의 Hit 클립은 `ThirdParty/CuteRobots/Animations/Hit To Body.fbx`(0~80프레임). FBX `TimeMode`가 60fps이고 테이크 `LocalTime`이 1.333초라 클립 길이는 **1.33초**다(Unity Editor가 꺼져 있어 FBX 바이너리에서 직접 읽음).
  - `EnemyBase.controller`의 Hit 상태는 클립이 끝날 때(Exit Time 1) Idle/Move로 돌아간다.
  - `Thief.prefab` `AIController`의 경직은 0.4초라 나머지 약 0.9초 동안 Hit 모션을 재생하면서 이동했다.
  - 경직 뒤 면역(1.5초) 동안 다시 맞으면 `EnemyAnimator.PlayHit`이 Hit 모션을 처음부터 다시 재생하지만 `AIController.OnHitFromCallback`은 경직을 걸지 않아 이때도 이동했다.
- 수정(프리팹 값만, 코드 변경 없음): `Thief.prefab` `AIController`
  - `_hitStunDuration` 0.4 → 1.33
  - `_hitStunImmuneDuration` 1.5 → 0
- 씬·다른 프리팹에 이 값을 덮어쓰는 곳이 없음을 확인했다.

## 결정 사항
- 사용자 선택: **맞을 때마다 정지**. 면역을 0으로 두어 Hit 모션이 다시 시작될 때 경직도 같이 다시 시작된다. 계속 쏘면 도둑이 계속 묶이는 것은 허용한다.
- 도둑의 피격 경직 시간은 Hit 클립 길이와 맞춘다. Hit 클립을 바꾸면 `_hitStunDuration`도 같이 바꾼다.
- 다른 적(Melee·Ranged·Special·GC)은 바꾸지 않았다.

## 현재 상태 및 이슈
- Play 검증 미완: Unity Editor가 열려 있지 않아 실제로 쏴 보지 못했다. 사용자 Play 육안 확인이 필요하다.
- 대상이 보이는 중(도망 중)에는 맞은 쪽 돌아보기(`_hitLookDuration`)가 없으므로, 경직 1.33초가 끝나면 바로 하던 도망을 이어간다.
- 이 세션의 변경은 커밋하지 않았다.

## 다음 할 일 (Next Steps)
- Play에서 도둑을 쏴서 Hit 모션 동안 제자리에 있는지, 모션이 끝나면 다시 도망가는지 확인한다.
- 연사로 계속 묶이는 것이 게임 밸런스상 너무 쉬우면 면역을 짧게(예: 0.3초) 두는 방안을 사용자와 다시 정한다.
