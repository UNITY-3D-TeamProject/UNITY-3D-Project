# 보행자가 공중에 뜬 채로 스폰되는 문제 (2026-10-15)

관련 intent: [intent-017 로비 군중](../../../intent/intent-017-lobby-crowd.md)

## 1. 작업 요약 및 방법
- 증상: 로비 보행자가 가끔 시작점에서 바닥과 떨어진 채로 스폰됐다.
- 원인: 점프 추월 중(속도 ×3)에 레인 끝에 닿으면 그 프레임에 풀로 반납된다. 그런데 `CharacterMotor`의 `_verticalVelocity`·`_isJumping`·`_horizontalVelocity`가 그대로 남아 있어서, 다시 꺼냈을 때 시작점에서 하던 점프를 이어서 했다. `CrowdController.OnDisable`은 `_isJumpingOver`만 지운다.
- 수정(`Assets/_Project/Scripts/Movement/CharacterMotor.cs`만): `OnDisable`을 추가해 수직·수평 속도, 외부 변위, `_isJumping`, `_isHovering`을 초기화한다. `OnJumpEnded`는 발생시키지 않는다.
- 검증
  - 재컴파일 에러 0
  - Play에서 재현되지 않는지는 확인하지 못했다(가끔 생기는 문제라 사용자가 관찰해야 함)

## 2. 결정 사항
- 공용 `CharacterMotor`에서 초기화한다. 비활성화될 때만 실행되므로 플레이어·적이 평소 움직이는 데에는 영향이 없다.

## 3. 현재 상태 및 이슈
- Play 관찰 검증이 남아 있다.

## 4. 다음 할 일 (Next Steps)
- Map_1_Lobby Play에서 보행자 시작점을 지켜보며 뜬 채로 스폰되는 개체가 없는지 확인한다. 플레이어 점프·이동도 이전과 같은지 확인한다.
