# Combat 통로 결정 정정 — 캐릭터 간 직접 호출 → 서브 중재자 경유 (2026-09-21)

## 1. 작업 요약 및 방법

사용자가 "구조를 만든 팀원이 통로를 뚫어놨는지" 확인을 요청하며 대화가 시작됐고, 확인 과정에서 `ReceiveHit`을 공격자가 중재자 없이 직접 호출하는 게 `character-architecture.md` §3 규칙 2("컴포넌트끼리 직접 참조하지 않는다")와 충돌한다는 걸 짚었다. 원문 근거를 찾아보니 `intent-004`가 "캐릭터 간 직접 호출은 §3 규칙 적용 대상이 아니다(캐릭터 **내부** 규칙이라서)"라고 이미 해석해둔 상태였다.

사용자에게 확인한 결과: 당시(intent-004)엔 중재자가 캐릭터당 1개뿐이라 중재자 과부하를 피하려던 예외였는데, 지금은 메인+서브 중재자 구조로 바뀌어 그 우려가 해소됐다며 **이 해석을 파기**하기로 결정. 코드를 뒤져본 결과 이 전환에 **Combat 코드 변경이 전혀 필요 없다**는 게 드러났다 — `ReceiveHit`이 이미 `public`이라 피격자의 전투 서브 중재자가 그대로 호출하면 되고, intent-004의 문장은 코드 제약이 아니라 사용 의도 서술이었을 뿐이다.

별도로, 이 과정에서 `SHitInfo.Attacker` 필드가 생성자에서 대입만 되고 어디서도 읽히지 않는다는 걸 발견해(전 구독자 부재) 제거 여부를 물었고, 사용자가 제거를 확정했다.

### 변경 파일
- `Assets/_Project/Scripts/Combat/SHitInfo.cs` — `Attacker`(GameObject) 필드/생성자 인자 제거, `using UnityEngine;` 제거. `SHitInfo(float damage)`만 남음.
- `Assets/Test/Combat/CombatManualTest.cs` — `new SHitInfo(null, LIGHT_DAMAGE)` 2곳 → `new SHitInfo(LIGHT_DAMAGE)`.
- `docs/intent/intent-005-combat-component-decoupling.md` — 통로 표 정정, "intent-004 해석 파기" 절 신규, `Attacker` 제거 기록, 열린 질문 ①에 "이세훈과 협의 항목" 명시.
- `docs/intent/clear/intent-004-combat-component-channel.md` — 해결 기록 하단에 파기 사실 추가(기존 내용은 이력이라 보존).
- `docs/intent/INTENT_POINTER.md` — intent-005 한 줄 요약 갱신.

## 2. 결정 사항 (채택된 아키텍처 규칙)

- **캐릭터 간 상호작용도 §3 규칙 2 예외 없이 적용한다.** 공격자 쪽 판정 지점은 상대의 전투 서브 중재자를 통해서만 `ReceiveHit`을 호출한다. (intent-004의 "캐릭터 내부 전용 규칙" 해석은 폐기)
- **`character-architecture.md` §3 표(요청/명령/이벤트)는 캐릭터 하나 안의 통신만 정의한다.** 캐릭터 A↔B 통신 자체가 문서에 없다는 공백이 있음 — "공격자가 상대 GameObject에서 어떻게 전투 서브 중재자를 얻는가"는 스킬 매니저 설계 때 정할 열린 문제로 intent-005에 관찰 기록만 남기고, `character-architecture.md` 본문 개정은 하지 않음(문서 소유자 이세훈과 협의 필요).
- **`SHitInfo`에서 공격자 정보 제거.** 어그로/킬 크레딧/넉백 방향이 필요해지는 시점에 다시 추가하기로(YAGNI).
- **역할 경계 재확인:** `Scripts/Mediator/`·`Scripts/Adapter/`·`Scripts/Attribute/`·`Scripts/Input/`은 이세훈, `Scripts/Combat/`·`Scripts/Observer/`는 BF_Leers. "AttributeSet의 CurrentHp를 누가 깎는가"(열린 질문 ①)는 이세훈 영역을 건드리는 결정이라 이번 세션에서 확정하지 않고 협의 항목으로 명시.

## 3. 현재 상태 및 이슈

- **컴파일/런타임 검증 완료.** `recompile` → `failed: false`, 콘솔 에러 0(기존 콘솔에 있던 에러 2건은 오늘 오전 세션 잔존물, 이번 변경과 무관 — 타임스탬프로 확인).
- Play 모드에서 `eval`로 다음 3개 시나리오를 직접 재현해 PASS 확인 (키보드 입력 자동화 대신 로직을 동일하게 재현):
  - 경상 피격: `hitCount=1, deathCount=0`
  - 사망 후 재피격: `hitCount`가 사망 후 불변, `deathCount=1`, `IsDead=true`
  - AttributeSet 옵서버 연동: 씨앗값 pull `Health=100` 확인 후 `CurrentHp=0`으로 내리면 `deathCount=1`
- `Assets/_Project/Scripts/Combat/`에 `Attribute`/`Scripts`/`Movement` 참조 없음 재확인(`grep`) — 의존성 0 유지.
- `.unity`/`.prefab`/`.asset` 미편집.

## 4. 다음 할 일 (Next Steps)

1. **이세훈과 협의:** ① `AttributeSet.CurrentHp` 감소 주체/경로, ② 전투 서브 중재자가 `ReceiveHit`을 호출하는 배선 방식(캐릭터 간 참조를 어디서 얻는지 포함), ③ `Current*` 속성 초기화 주체(0값 이슈, 프리팹 배선 블로커).
2. 위 3개가 정해지면 `AttributeToCombatObserver`를 실제 캐릭터 프리팹에 배선하고 intent-005를 `resolved`로 옮긴다.
3. `character-architecture.md` §3에 "캐릭터 간 통신" 항목이 없는 공백을 문서 소유자와 상의해 반영할지 결정.
