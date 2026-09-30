# Combat asmdef 분리 + 자동 단위 테스트 도입 (2026-09-21)

## 1. 작업 요약 및 방법

"단위 테스트를 돌린 게 맞냐"는 질문에서 시작해, `CombatManualTest.cs`가 실제로는 NUnit이 아닌 수동(키보드) 테스트임을 확인시키고, asmdef 기반 자동 단위 테스트 도입 여부를 논의했다. 대화로 3가지를 확정(테스트 위치는 `Assets/Test/Combat/` 그대로 / 영구 asmdef는 Combat만 / AttributeSet 연동 경로는 검증만 하고 원상복구)한 뒤 진행했다.

- `Assets/_Project/Scripts/Combat/Combat.asmdef` 신규 — `CharacterCombat`/`SHitInfo`/`IHitReceiver`를 별도 어셈블리로 분리(references 없음).
- `Assets/Test/Combat/Combat.Tests.asmdef` 신규 — `Combat`만 참조하는 EditMode 테스트 어셈블리.
- `Assets/Test/Combat/CharacterCombatTests.cs` 신규 — `CombatManualTest.cs`의 시나리오 1~4를 NUnit `[Test]` 4개로 이식.
- `AttributeSet` 연동(시나리오 5) 자동화를 임시 `Attribute.Core.asmdef`/`Observer.asmdef`로 검증 → **실패**(EditMode에서 `Awake`/`OnEnable` 동기 발화 보장 안 됨) → 계획대로 임시 asmdef 2개 + 임시 테스트 파일 삭제.
- `CombatManualTest.cs`/`FakeEffectTarget.cs` 삭제 — asmdef 폴더 편입으로 더 이상 그 자리에서 컴파일될 수 없게 됐고, 자동화도 실패해 대체 검증 수단이 없어짐(사용자 확인 후 삭제).
- `docs/intent/intent-006-combat-unit-testing.md` 작성 후 즉시 `resolved`로 `docs/intent/clear/`에 저장, `INTENT_POINTER.md` 갱신.

## 2. 결정 사항 (채택된 아키텍처 규칙)

- **테스트 asmdef는 `Assembly-CSharp`를 참조할 수 없다.** Unity 컴파일 순서상 `Assembly-CSharp`가 모든 커스텀 asmdef보다 나중에 컴파일되기 때문 — 이번에 직접 확인된 사실이며, 앞으로 다른 모듈에 asmdef를 추가할 때도 동일하게 적용된다.
- **asmdef 스코프는 폴더 전체다.** 같은 폴더에 있는 무관한 파일(`CombatManualTest.cs` 등)도 강제로 같은 어셈블리에 편입된다 — 앞으로 asmdef를 추가할 폴더엔 그 asmdef가 커버할 파일만 남겨야 한다.
- **EditMode 테스트에서 `SetActive(true)` 직후 `Awake`/`OnEnable`이 즉시 동기 발화한다고 보장할 수 없다.** Play 모드와 다르게 동작한다 — 오늘 오전 세션의 "Edit 모드 eval에서 컴포넌트 라이프사이클 미발화" 관찰과 같은 계열의 현상으로 보인다. MonoBehaviour 라이프사이클에 의존하는 자동 테스트는 `[UnityTest]`(PlayMode)로 만들어야 한다.
- **Attribute/Observer 영역엔 영구 구조 변경을 남기지 않는다.** 이세훈 영역이라는 원칙을 이번에도 지켰다 — 임시로 만들었다가 검증 후 완전히 제거.

## 3. 현재 상태 및 이슈

- **컴파일 정상, 자동 테스트 4/4 PASS 최종 확인.** Unity CLI(`unity test ... --mode EditMode --filter Test.Combat`) 배치 실행으로 확인 — NUnit XML 결과 `passed="4" failed="0"`.
- **Combat.asmdef가 새로 생겨서 `Assets/_Project/Scripts/Combat/`가 이제 별도 컴파일 단위다.** `Attribute`/`Scripts`/`Movement` 참조가 없는 것도 재확인됨(기존과 동일).
- **AttributeSet 연동(시나리오 5) 검증 수단이 사라졌다.** 자동화도 실패했고 수동 테스트 파일도 삭제했다 — 다음에 옵서버 경로를 건드릴 사람은 검증 수단을 새로 만들어야 한다(PlayMode `[UnityTest]` 권장).
- **Unity Editor Pipeline 브릿지가 테스트 실행 중 행(hang)됐다.** `run_tests` 호출 이후 `editor_status`까지 전부 60초 타임아웃, `unity close`도 정상 종료 실패. PID 강제 종료 후 `unity open`으로 재기동해 복구(저장 안 한 씬 변경사항 없어 데이터 손실 없음). **오늘 오전 세션에도 같은 증상·같은 해법이 있었다** — 반복되면 패턴으로 보고 원인 조사 가치 있음.
- `Assets/Test/`가 gitignore 대상이라 이번 결과물(asmdef 2개 + `CharacterCombatTests.cs`)은 git에 커밋되지 않는다(결정 사항).

## 4. 다음 할 일 (Next Steps)

1. AttributeSet 연동 경로를 다시 검증하고 싶으면 `[UnityTest]`(PlayMode 코루틴) 방식으로 새로 작성 — `Attribute/`에 asmdef가 필요해지므로 이세훈과 사전 협의.
2. Editor Pipeline 브릿지 행 현상이 또 발생하면, 이번 문서와 오늘 오전 문서를 비교해 공통 트리거(테스트 실행 직후?)가 있는지 확인.
3. intent-005(캐릭터 간 통신, HP 감소 주체 등)는 이 작업과 무관하게 여전히 이세훈과 협의 대기 중.
