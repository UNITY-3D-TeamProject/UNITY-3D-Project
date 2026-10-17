# GC 애니메이션 추가 (도둑과 같은 구조)

## 작업 요약 및 방법
- 문제: GC 모델을 ChompBot(`R_Chompbot_01`)으로 바꿨지만 Animator에 Controller가 없고, `EnemyAnimator`·`EnemyAnimationMediator`도 없어 애니메이션이 재생되지 않았다.
- 도둑(`Thief.prefab`)과 같은 구조로 Unity MCP(eval)를 써서 연결했다. 코드 변경은 없다.
  - 신규 `Animations/Enemy/GC_Override.overrideController` (base = `EnemyBase.controller`)
    - Enemy_Idle → `A_Chompbot_Idle` / Idle
    - Enemy_Move → `A_Chompbot_Run` / Run
    - Enemy_Attack → `A_Chompbot_Attack` / Attack
    - Enemy_Hit → `A_Chompbot_Hit` / Hit
    - Enemy_Death → 교체 안 함 (GC는 무적이고, ChompBot에 Death 클립이 없음)
    - 클립은 ThirdParty FBX를 GUID로만 참조한다(사본 .anim 없음).
  - `GC.prefab`
    - `Model/R_Chompbot_01`: Animator Controller = `GC_Override`, Apply Root Motion 끔, `EnemyAnimator` 추가(`_referenceMoveSpeed` 3, `_skillAnimations` 기본값 `Attack`→`Attack` Trigger)
    - 루트 아래 `EnemyAnimationPrefab` 인스턴스 추가: `_enemyAnimator` 연결, `_speedValueKey` = `MoveSpeed`
    - `CharacterMediator._enemyAnimationMediator` 연결
- 검증: 저장한 프리팹에서 참조 4개 확인, Run 클립 커브 바인딩 134개가 모두 모델 본과 일치, 콘솔 에러·경고 0. Play 육안 확인은 하지 않았다.

## 결정 사항
- GC 애니메이션은 도둑과 같은 `EnemyBase` + Override 구조를 쓴다. 전용 상태가 필요 없어 컨트롤러를 복사하지 않았다.
- 공격 모션은 `EnemyMeleeAttack`의 스킬 이름 `Attack`과 `EnemyAnimator` 기본 매핑으로 재생된다. 도둑은 공격하지 않아 매핑을 비웠지만 GC는 기본값을 유지한다.
- intent-025의 "컨트롤러는 MCP로 만들지 않는다" 제약은 이번에 사용자 결정으로 예외 처리했다.

## 현재 상태 및 이슈
- Play에서 Idle/Run 전환, 공격 모션, 이동 속도와 발 움직임(`_referenceMoveSpeed` 3)이 맞는지 확인하지 못했다. GC 속도는 2.5 → 7로 오르므로 Run 재생 배율이 0.8~2.3배가 된다.
- `A_Chompbot_Hit.fbx.meta`의 takeName이 `Armature|Attack`이다. 길이(0.50초)가 Attack(0.57초)과 달라 별개 클립으로 보이지만 내용은 육안 확인이 필요하다.
- ThirdParty FBX는 Git에 없으므로 팀원 PC에도 ChompBot 에셋이 있어야 클립이 연결된다.
- 이 세션의 변경은 커밋하지 않았다.

## 다음 할 일 (Next Steps)
- 사용자가 Play에서 GC 애니메이션을 확인하고, 발이 미끄러지면 `EnemyAnimator._referenceMoveSpeed`를 조정한다.
