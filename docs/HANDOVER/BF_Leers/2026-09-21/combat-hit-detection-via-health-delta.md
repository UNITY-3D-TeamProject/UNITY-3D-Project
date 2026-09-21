# Combat 피격 판정을 HP 변화량으로 단일화 (IHitReceiver / SHitInfo 폐기)

- 날짜: 2026-09-21
- 참여자: BF_Leers + Claude (Opus 5)
- 브랜치: `feature/Combat`
- 관련 문서: [intent-005](../../../intent/intent-005-combat-component-decoupling.md)

## 1. 작업 요약 및 방법

`CharacterCombat`의 입구가 `ReceiveHit(SHitInfo)`와 `Health` setter 둘로 갈라져 있던 것을 `Health` setter 하나로 합쳤다.

- **삭제**: `Assets/_Project/Scripts/Combat/IHitReceiver.cs`, `SHitInfo.cs` (각각 `.meta` 동반 삭제)
- **`CharacterCombat.cs`**
  - `MonoBehaviour, IHitReceiver` → `MonoBehaviour`
  - `event Action<SHitInfo> OnHit` → `event Action OnHit`
  - `ReceiveHit` 및 `#region Public Methods` 제거 (public 메서드가 0개가 됨)
  - `Health` setter가 대입 직전 값과 비교해 `value < _health`면 `OnHit`, 이어서 `CheckDeath()`로 `OnDeath`를 방출
- **`AttributeToCombatObserver.cs`는 수정하지 않았다.** setter가 이전 값을 이미 들고 있어 Combat 안에서 판정이 완결되기 때문이다.
- **테스트**(`Assets/Test/Combat/CharacterCombatTests.cs`): `ReceiveHit` 기반 시나리오를 Health 기반 5종으로 교체 — 감소/0도달(순서 포함)/사망 후 추가 감소/회복/동일 값.

## 2. 결정 사항

1. **HP 감소 = 피격.** 별도의 피격 통보 경로를 두지 않는다. `AttributeSet`의 `PostAttributeChanged`도 `Health` setter도 이미 이전 값을 알고 있으므로 판정에 충분하다.
2. **`OnHit`은 `event Action`(페이로드 없음).** 피해량이 필요한 구독자는 값의 원천인 AttributeSet을 직접 읽는다. Combat이 "직전 대입분"을 들고 있으면 또 하나의 진실의 출처가 되므로 만들지 않는다.
3. **판정 위치는 `Health` setter 안.** 옵서버가 비교해서 Combat의 별도 메서드를 부르는 안은 기각 — 입구가 다시 2개가 되고, 옵서버를 거치지 않은 `Health` 대입에서는 `OnHit`이 안 뜨게 된다.
4. **`OnHit` → `OnDeath` 순서는 setter의 코드 순서로 보장.** HP가 0이 되면 이제 두 이벤트가 모두 뜬다(기존엔 `OnDeath`만).
5. **`Combat.asmdef` 위치 정정** (사용자 승인 후): `Assets/Test/Combat/`에 `Combat.asmdef`와 `Combat.Tests.asmdef`가 같은 폴더에 있어 Unity가 "multiple assembly definition files" 에러를 내고 **프로젝트 전체가 컴파일되지 않는 상태**였다. `Combat.asmdef`(+`.meta`)를 원래 있어야 할 `Assets/_Project/Scripts/Combat/`으로 이동해 해결했다. `Assets/Test/`는 gitignore 대상이라 이 깨진 상태는 커밋된 적이 없다.

## 3. 현재 상태 및 이슈

- **검증 완료**: `unity test --mode EditMode` 배치 실행 — 컴파일 에러 0건, `Test.Combat.CharacterCombatTests` **5/5 PASS**(`ZeroHealth_RaisesOnHit_ThenOnDeath`로 이벤트 순서까지 확인).
- Combat 폴더는 여전히 `System`/`UnityEngine` 외 아무것도 참조하지 않는다(의존성 0 유지). 이제 `Combat.asmdef`(references 비어 있음)가 이를 컴파일 단계에서 강제한다.
- **주의**: `unity test`의 `--output`을 프로젝트의 `Temp/` 아래로 주면 Editor 종료 시 폴더가 정리되어 CLI가 `TEST_RESULTS_MISSING`으로 실패한다. 프로젝트 밖 경로를 쓸 것.
- 커밋은 아직 하지 않았다.
- 미해결로 남은 것은 intent-005의 열린 질문 ①(HP를 깎는 주체)·②(서브 중재자 지시 목록)·③(`Current*` 속성 0값 → 옵서버 프리팹 미배선) 그대로다.

## 4. 다음 할 일 (Next Steps)

1. 변경분 커밋 (`docs/commit-convention.md` 준수).
2. **열린 질문 ① 협의(이세훈)** — `ReceiveHit`이 사라진 지금, "공격자가 피격자의 AttributeSet HP를 어떻게 깎는가"가 캐릭터 간 전투 통신의 유일한 경로다. 이것만 정해지면 `OnHit`/`OnDeath`는 자동으로 따라온다.
3. 열린 질문 ③(`CurrentHp`가 0으로 들어있는 문제)이 풀려야 옵서버를 실제 프리팹에 배선할 수 있다.
4. `docs/character-architecture.md` §3에 캐릭터 간 통신 정의가 없는 공백은 그대로 남아 있다 — 문서 작성자와 협의 필요.
