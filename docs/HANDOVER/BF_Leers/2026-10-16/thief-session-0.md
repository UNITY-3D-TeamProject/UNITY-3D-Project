# 도둑(Thief) 세션 0 — 애니메이션 코드 반영 + 남은 결정 확정

## 작업 요약 및 방법
- `/start-feature`로 도둑 기능을 이어서 진행했다. Phase 1(grilling)·Phase 2(티켓)·Phase 2.5(구현 전 검증)는 이미 끝나 있었고, 세션 0("AI 애니메이션 코드 푸시 대기")에서 멈춰 있었다.
- 애니메이션 코드가 develop(PR #83)에 들어온 것을 확인했다. 그래서 develop 52bc2ec 기준으로 스펙 가정을 다시 확인했다.
  - 사망 끝 알림 경로
  - `Melee.prefab` 구성
  - 바뀐 코드 범위
- 코드 변경은 없다. 문서만 바꿨다.
  - `docs/spec/thief.md`(개인, Git 제외): 애니메이션·수명·초기값 행, 세션 0 `done`, UNITY-4, USER-1, USER-3, USER-4를 갱신했다. "세션 0 확인" 절을 새로 추가했다.
  - `docs/intent/intent-027-thief.md`: `origin/feature/AI/Thief`의 `intent-026-thief.md`를 가져와 번호를 바꾸고 열린 질문을 정리했다. `INTENT_POINTER.md`에 항목을 추가했다.
  - `docs/planning/thief.md`(개인): D1 답을 기록했다.

## 결정 사항
- **D1 초기값**: 몬스터 DB(`SOMonsterData_Thief`). `ThiefSpawner`가 생성 직후 `SetValue`로 넣는다.
- **잡힌 도둑 제거**: `ThiefSpawner`가 `OnDeathEnded` 뒤 `Destroy`한다. `Melee.prefab`을 복제하면 `EnemyPoolReturner`가 따라오므로 도둑 프리팹에서는 뺀다.
- **애니메이션**: `EnemyAnimator` + `EnemyAnimationMediator`를 그대로 쓰고 코드는 바꾸지 않는다. 컨트롤러는 `EnemyBase.controller`의 Override(`Thief_Override`)로 만든다. **`Death` 상태 이름을 반드시 유지한다.** 이 상태가 끝나야 사망 끝 알림이 나온다.
- **브랜치**: `feature/AI/Maze`에서 진행한다. `origin/feature/AI/Thief`(intent 커밋 1개)는 쓰지 않는다.
- **intent 번호**: 026 → 027. develop의 `intent-026-enemy-hit-stun`과 겹쳐서 바꿨다.

## 현재 상태 및 이슈
- 세션 0 완료. 세션 A~E는 `todo`/대기 상태다.
- 스펙의 `AIController.cs`·`AIRetreatAction.cs`·`CharacterMediator.cs` 줄 번호는 develop 변경으로 밀렸을 수 있다. 구현 세션에서 다시 확인해야 한다.
- `origin/feature/AI/Thief` 원격 브랜치에 옛 `intent-026-thief.md` 커밋이 남아 있다. 정리할지는 사용자가 정한다.
- 이번 문서 변경(intent-027, INTENT_POINTER, 이 기록)은 아직 커밋하지 않았다.

## 다음 할 일 (Next Steps)
- **새 세션에서 세션 A(UNITY-1)를 진행한다.** `RoundSpawnerBase`(`AI/RoundSpawning/`) 신규 작성 + `GarbageCollectorSpawner` 상속 전환 + 직렬화 값 유지 확인.
- USER-1(`SOMonsterData_Thief`, `SOAttributeData_Thief`)과 USER-5(담당자 요청 전달)는 언제든 할 수 있다.
