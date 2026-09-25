---
id: intent-006
title: Combat asmdef 분리 + 자동 단위 테스트 도입
part: Combat
status: resolved
created: 2026-09-21
resolved: 2026-09-21
---

## 문제 (Problem)
Combat의 유일한 검증 수단이 `CombatManualTest.cs`(Play 모드 키보드 조작 + 화면 텍스트 육안 확인)뿐이었다. 프로젝트에 `.asmdef`가 하나도 없어 Unity Test Framework(NUnit)로 `CharacterCombat`을 참조할 수 없었고, `docs/HANDOVER/BF_Leers/2026-09-20/combat-component-channel.md`가 이 문제를 "후속 과제, 범위 밖"으로 명시적으로 미뤄뒀다.

## 기대 결과 (Proposed outcome)
`CharacterCombat`/`SHitInfo`/`IHitReceiver`의 순수 로직(피격·사망 판정)을 `Assert`가 자동으로 판정하는 NUnit 테스트로 커버한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: Combat을 이후에 건드릴 사람(BF_Leers, 또는 인수인계 대상) 본인 — 로컬 회귀 확인용
- 어떤 시스템/모듈이 건드려지는가: `Assets/_Project/Scripts/Combat/`(asmdef 추가), `Assets/Test/Combat/`(asmdef + 테스트 신규, 기존 수동 테스트 축소)

## 제약 (Constraints)
- `Attribute/`, `Observer/` 폴더(이세훈 영역 포함)에 영구적인 구조 변경을 남기지 않는다.
- `.unity`/`.prefab`/`.asset` 편집 금지.

## 열린 질문 (Open questions)
없음 — 대화 중 아래 3가지가 전부 확인되며 해소됨.

## 해결 기록 (Resolution)

### 확정된 결정 3가지
1. **테스트 위치는 `Assets/Test/Combat/`.** `CLAUDE.md` §1은 "공유 필요 작업물은 Test에 두지 않는다"고 하지만, 이번 단위 테스트의 목적을 "지금 구현이 맞게 동작하는지 로컬에서 확인"으로 한정하기로 하고 예외를 뒀다. 기존 Movement 테스트도 같은 위치였다.
2. **영구 asmdef는 Combat에만 만든다.** `Attribute/`(이세훈 영역)는 건드리지 않는다.
3. **AttributeSet 연동(옵서버 경로)은 자동화 가능 여부만 검증하고 원상복구한다.**

### 기술적으로 확인된 것 — 계획 당시 틀렸던 가정 정정
최초 계획엔 "테스트 asmdef가 `Assembly-CSharp`를 참조하면 `Attribute.Core`/`Scripts.Observer`에 접근할 수 있어 `Attribute.asmdef`가 필요 없다"고 적었으나 **틀렸다.** `Assembly-CSharp`는 모든 커스텀 asmdef가 컴파일된 *다음*에 컴파일되는 기본 어셈블리라, asmdef 쪽에서 이를 참조하는 것 자체가 Unity 컴파일 순서상 불가능하다. 실제로는 검증을 위해 `Attribute/Core/Attribute.Core.asmdef`, `Observer/Observer.asmdef`를 **임시로** 만들어야 했다.

또한 `CombatManualTest.cs`/`FakeEffectTarget.cs`가 `Assets/Test/Combat/`(= `Combat.Tests.asmdef`의 스코프) 안에 있어서, asmdef를 그 폴더에 두는 순간 이 두 파일도 강제로 같은 어셈블리에 편입됐다. 이 두 파일은 `Attribute.Core`/`Scripts.Observer`/`UnityEngine.InputSystem`을 참조해 `Combat.Tests.asmdef`가 `Combat`만 참조하는 상태로 못 돌아가게 만드는 요인이었다.

### 검증 결과
Unity CLI(`unity test`) 배치 모드로 EditMode 테스트 실행(Editor Pipeline 브릿지가 원인불명으로 응답 없어져 CLI 직접 실행으로 우회, 아래 "이슈" 참고):
- **시나리오 1~4(`CharacterCombatTests`, 순수 Combat 로직): 4/4 PASS.**
- **시나리오 5(`AttributeObserverIntegrationTests`, AttributeSet 연동): FAIL** — "씨앗값 pull이 되지 않았다, Expected 100.0f, But was 0.0f". EditMode 테스트에서는 `GameObject.SetActive(true)` 직후 `Awake`/`OnEnable`이 Play 모드처럼 즉시 동기 발화한다는 보장이 없는 것으로 보인다(`docs/HANDOVER/.../2026-09-21/combat-component-decoupling-redesign.md`에 기록된 "Edit 모드 eval에서 Awake/OnEnable 미발화" 현상과 같은 계열). 이 경로를 자동화하려면 `[Test]`가 아니라 프레임을 기다리는 `[UnityTest]`(PlayMode 코루틴 테스트)가 필요할 가능성이 높다 — 이번 범위(검증만) 밖이라 더 파지 않음.

### 최종 결정
- **영구 유지:** `Assets/_Project/Scripts/Combat/Combat.asmdef`(references: 없음), `Assets/Test/Combat/Combat.Tests.asmdef`(references: `Combat`만), `Assets/Test/Combat/CharacterCombatTests.cs`(시나리오 1~4, NUnit `[Test]` 4개).
- **삭제:** `Attribute.Core.asmdef`, `Observer.asmdef`(둘 다 임시), `AttributeObserverIntegrationTests.cs`(임시), `CombatManualTest.cs`/`FakeEffectTarget.cs`(asmdef 폴더 편입으로 더 이상 컴파일 불가 + 자동화 실패로 대체 검증 수단도 없어짐 — 사용자 확인 후 삭제).
- **결과:** 시나리오 5(AttributeSet 연동)는 이제 자동/수동 어느 쪽으로도 검증 수단이 없다. 다음에 이 경로를 만질 사람은 PlayMode `[UnityTest]`로 새로 만들어야 한다.

### 이슈 — Unity Editor MCP 브릿지 행(hang)
시나리오 5 테스트를 실행하는 도중(정확히는 `run_tests` 호출 시점) Editor Pipeline 브릿지가 응답 불능 상태가 됐다(`editor_status`까지 전부 60초 타임아웃, `unity status` CLI로는 "ready"로 나왔으나 실제 명령엔 무응답, `unity close`도 정상 종료 실패). 프로세스(PID 9460)를 강제 종료 후 `unity open`으로 재기동해 복구했다 — 이전 세션(2026-09-21 오전)에도 같은 증상·같은 해법이 기록돼 있다. 저장 안 한 씬 변경사항은 없어 데이터 손실 없음. 원인은 미확정 — 테스트 실행 중 Pipeline 명령 디스패처가 멈추는 패턴이 반복되고 있어, 다음에 또 발생하면 패턴으로 보고 조사할 가치가 있다.

### 관련 커밋/PR
(구현 커밋에서 채워짐)
