# Special 제자리 회전 Turn Left / Turn Right

- 날짜: 2026-10-16 / 브랜치: `feature/AI/Enemy`
- 관련: intent-025(AI 애니메이션), `special-nexus-animation.md`

## 1. 작업 요약 및 방법
- Special은 `CharacterRotator`가 몸 전체를 코드로 돌리는데 애니메이션은 Idle 그대로라 미끄러지듯 돌았다. 멈춘 채 왼쪽으로 돌면 `Turn Left`, 오른쪽으로 돌면 `Turn Right`가 나오게 했다.
- 신규 `Scripts/CharacterAnimation/TurnAnimator.cs`: `LateUpdate`에서 자기 transform의 월드 yaw 변화 속도를 잰다. `_turnSpeedThreshold`(30°/s)보다 빠르게 오른쪽으로 돌면 +1, 왼쪽이면 -1, 아니면 0을 Animator float `TurnDirection`에 넣는다. Mediator 연결 없이 회전 결과만 읽는다(`JumpAnimator`처럼 특정 객체만 붙이는 컴포넌트).
- `Special.controller`(Base Layer): 파라미터 `TurnDirection` 추가, 상태 `TurnLeft`/`TurnRight`(ThirdParty `Nexus Robot@Turn Left/Right.FBX` 원본 클립) 추가.
  - Idle→TurnLeft `TurnDirection < -0.5`, Idle→TurnRight `> 0.5`, 되돌아가기는 각각 `> -0.5` / `< 0.5`
  - TurnLeft/Right→Move(`IsMoving`), →Attack(`Attack`), →Hit(`Hit`). Death·WindCharge는 기존 AnyState 전환이 처리한다.
  - Exit Time 없음, 0.1초(Attack·Hit는 0.05초), Interruption Source는 기존 Idle→Move와 같은 `Source`
- `Special.prefab`: `Nexus Robot`(Animator·`EnemyAnimator`가 있는 오브젝트)에 `TurnAnimator` 추가. `LoadPrefabContents` → `SaveAsPrefabAsset`으로 저장했다.
- **ThirdParty 임포트 설정 변경(로컬 전용)**: Turn 클립은 원래 몸통 뼈 `RigMain`의 yaw를 직접 90° 돌린다(0.5초, 루프 아님). 그대로 쓰면 코드 회전과 겹치고, 클립이 끝날 때 몸이 튄다. 그래서 두 FBX의 Animation 임포트 Mask(Create From This Model)에서 `RigMain`만 껐다. 이제 커브 경로가 19개에서 18개로 줄었다.

## 2. 결정 사항
- 회전은 지금처럼 코드(`CharacterRotator`)가 맡고, Turn 클립은 팔·머리 같은 나머지 뼈 움직임만 준다.
- Turn은 Idle에서만 들어간다(이동 중에는 Fly Forward 유지). 이동 중 제외는 코드가 아니라 컨트롤러 전환으로 처리한다.
- 원본 키프레임을 복사한 `.anim`은 만들지 않는다. 퍼블릭 레포에 올리면 Asset Store EULA상 재배포가 되기 때문이다(intent-025 제약에 보충).

## 3. 현재 상태 및 이슈
- 컴파일 에러 0, 새 경고 없음(콘솔의 `LilRobot` Legacy 경고는 기존 경고).
- 에디트 모드 시뮬레이션(Special 프리팹 인스턴스에서 `Animator.Update`)으로 다음을 확인했다.
  - `TurnDirection` -1/+1이면 Idle→TurnLeft/TurnRight, 0이면 Idle로 돌아온다.
  - `IsMoving`이면 Move로 가고, 이동 중에는 `TurnDirection`이 들어와도 Move를 유지한다.
  - Turn 중 `RigMain` 로컬 회전·위치가 Idle과 같다(몸통이 튀지 않음).
- `TurnAnimator` 판정은 `LateUpdate`를 리플렉션으로 호출해 확인했다. 왼쪽 20° → -1, 오른쪽 20° → +1, 정지 → 0이고, 0°/360° 경계를 넘어도 -1로 나온다.
- **Play 검증은 미완**: MCP로 Play에 들어가면 프레임이 2에서 멈춰서(이전과 같은 증상) 실제 AI 회전으로는 보지 못했다. 육안 확인도 하지 않았다.
- **팀원 PC 설정 필요**: ThirdParty는 gitignore라 임포트 Mask 설정은 이 PC에만 있다. 다른 PC에서는 Turn 중 몸통이 이중 회전한다. 설정 방법:
  1. `Assets/ThirdParty/Nexus Robot Cute Series/FBX/Nexus Robot@Turn Left.FBX` 선택 → Inspector **Animation** 탭
  2. 클립 아래 **Mask** 펼치기 → Definition `Create From This Model` → **Transform**에서 `RigMain`만 체크 해제 → **Apply**
  3. `Nexus Robot@Turn Right.FBX`도 똑같이 한다.
- 클립이 0.5초라 회전이 더 길면 마지막 자세로 멈춰 있다. WindCharge 중 조준 회전은 WindCharge 상태라 Turn이 나오지 않는다. 파라미터는 LateUpdate에서 넣으므로 Animator에는 1프레임 늦게 반영된다.

## 4. 다음 할 일
- Play에서 Special이 멈춘 채 돌 때(피격 후 돌아보기, Hold 조준 등) Turn 모션이 나오는지, 반대 방향으로 나오지 않는지 육안으로 확인한다.
- 너무 자주 끼어들거나 깜빡이면 `_turnSpeedThreshold`를 올린다. 긴 회전에서 마지막 자세 고정이 어색하면 루프 여부를 검토한다.
- 팀원에게 위 임포트 Mask 설정을 공유한다.
