# 11. 플레이어 회전 속도 체크

- **우선순위**: 중간
- **상태**: 완료

## 목표
플레이어 몸통 회전 속도가 조작감에 맞는지 점검하고, 필요하면 조정한다.

## 확정 사항
- 문제: **급격하게 방향을 틀 때 몸이 느리게 따라온다.**
- 원인: `RotatorPrefab` 의 `CharacterRotator._rotateSpeed` 가 **180**(도/초)이었다. 스크립트 기본값은 720 이지만 프리팹 값이 우선한다 (180도 회전에 약 1초, 계산값).
- 수정: 코드 변경 없이 **`Player.prefab` 에서 `_rotateSpeed` 를 720 으로 오버라이드**했다 (커밋 `3a5adb2`). `RotatorPrefab` 원본 값(180)은 그대로 둔다 (적 `Ranged.prefab` 도 별도 오버라이드를 가진다).
- 회전 방식(`Mathf.MoveTowardsAngle` 고정 각속도)은 바꾸지 않는다.

## 현재 상황
- `CharacterRotator._rotateSpeed` 스크립트 기본값 720, `Mathf.MoveTowardsAngle` 로 yaw 회전 (`Assets/_Project/Scripts/Rotation/CharacterRotator.cs:20`, `:76`).
- `Assets/_Project/Prefabs/Actions/RotatorPrefab.prefab` : 180 / `Assets/_Project/Prefabs/Player/Player.prefab` : 720 (오버라이드).

## 열린 질문
- [x] 문제가 나는 상황 → 이동 방향을 급격히 틀 때.
- [x] 원하는 느낌 → 빠르지만 부드러운 회전. 속도 값 상향으로 해결.
- [x] 수정 방식 → 속도 값 상향 (Player 프리팹 오버라이드).

## 완료 기준
- [x] 확인된 문제 상황에서 회전이 의도대로 동작 (사용자 플레이 확인).
