---
id: intent-005
title: Combat 컴포넌트 재설계 — 의존성 0 + HP 옵서버
part: Combat
status: open
created: 2026-09-21
resolved: null
---

## 문제 (Problem)
intent-004에서 구현한 `CharacterCombat`은 "기존 Attribute 코드를 건드리지 않는다"를 우선해 설계되어, 전투 컴포넌트가 속성 시스템의 값 변경까지 대신 실행하는 구조가 됐다.

- `CharacterCombat.ReceiveHit`이 `SOAttributeEffect.Apply`를 호출한다 → 그 Apply가 옵서버 배선을 통해 `NotifyAttributeChanged`로 되돌아와 `OnDeath`가 `OnHit`보다 먼저 터지는 순서 역전이 생겼고, 이를 "OnHit을 Apply보다 먼저 쏜다"는 수동 순서 보정으로 막고 있었다.
- `SHitInfo`가 `SOAttributeEffect` + `IEffectTarget`을 들고 있어 Combat 모듈만 다른 프로젝트로 떼어낼 수 없었다.
- 입구가 2개(`ReceiveHit`, `NotifyAttributeChanged`), 속성 키 문자열 비교, `IsValidTarget` 방어 코드까지 들어가 이동 컴포넌트(`CharacterMotor`)보다 훨씬 무거웠다.

팀(사용자) 확정 기준:
1. 피격·사망 체크 컴포넌트이므로 이동 컴포넌트만큼 단순할 것
2. Attribute를 직접 참조하지 않을 것 (AttributeSet의 함수를 쓰는 쪽은 바깥이면 됨)
3. 전투 컴포넌트만 다른 프로젝트로 가져가도 동작할 것 — 의존성 ≈ 0

## 기대 결과 (Proposed outcome)
`Assets/_Project/Scripts/Combat/`은 `UnityEngine`/`System` 외 아무것도 참조하지 않는다. 속성 시스템과의 연결은 별도 어댑터 컴포넌트(`AttributeToCombatAdapter`) 하나가 전담하며, 이 구조에서는 "OnHit → OnDeath" 순서가 코드 순서 보정 없이 구조적으로 보장된다(체력이 어댑터가 밀어넣는 값이라 Apply 재진입 경로 자체가 없다).

확정된 Combat의 통로는 다음 셋뿐이다:

| 방향 | 통로 | 누가 쓰는가 |
|---|---|---|
| 들어옴 | `Health` setter | 어댑터 (형제 컴포넌트 배선, 중재자 우회) |
| 나감 | `OnHit` / `OnDeath` 이벤트 | 자기 중재자가 구독 |

(2026-09-21 갱신 — 원래 여기 있던 `ReceiveHit(SHitInfo)` 행은 아래 "ReceiveHit/SHitInfo 폐기" 절대로 삭제됐다. 들어오는 통로는 `Health` setter 하나뿐이다.)

### intent-004 해석 파기 (2026-09-21)
intent-004는 `ReceiveHit`을 "공격자가 대상의 중재자를 거치지 않고 직접 호출한다"고 결정하며, `character-architecture.md:32`의 직접 참조 금지 규칙이 "캐릭터 **내부** 컴포넌트 간 규칙"이라 캐릭터 간 호출엔 적용되지 않는다고 해석했다. 이 해석을 파기한다.

**파기 근거:** intent-004 당시엔 캐릭터당 중재자가 1개뿐이라, 모든 캐릭터 간 상호작용까지 그 하나가 떠안으면 과부하가 될 거라는 우려로 예외를 뒀다. 지금은 메인 중재자 + 기능 단위 서브 중재자 구조로 바뀌어 이 우려가 해소됐으므로, §3 규칙 2를 예외 없이 적용한다 — 공격자 쪽 판정 지점이 상대의 전투 서브 중재자를 통해서만 `ReceiveHit`을 호출한다.

**Combat 코드는 이 전환으로 바뀌지 않는다.** `ReceiveHit`은 이미 `public`이라 피격자 자신의 전투 서브 중재자가 캐릭터 내부 호출로 그대로 쓸 수 있다. intent-004의 문장은 코드 제약이 아니라 사용 의도 서술이었을 뿐이다.

**남은 공백:** `character-architecture.md` §3의 3방향 표(요청/명령/이벤트)는 전부 캐릭터 **하나** 안의 통신이고, 캐릭터 A와 캐릭터 B 사이의 통신 자체가 정의돼 있지 않다. "공격자 쪽 판정 지점이 상대 GameObject에서 어떻게 전투 서브 중재자를 얻는가"는 스킬 매니저 설계 때 정할 문제로 남긴다 — 문서 자체(§3) 개정 여부는 이 문서 소유자가 아닌 `character-architecture.md` 작성자와 협의한다.

> **(2026-09-21 후속) 이 절의 라우팅 논의는 아래 "ReceiveHit/SHitInfo 폐기"로 무효가 됐다.** `ReceiveHit` 자체가 없어졌으므로 "공격자 → 피격자의 전투 서브 중재자 → `ReceiveHit`" 경로를 설계할 필요가 사라졌다. 캐릭터 간 통신 문제는 이제 "공격자가 피격자의 AttributeSet HP를 어떻게 깎는가"(열린 질문 ①) 하나로 환원된다. 위 파기 근거 자체는 여전히 유효하므로 기록으로 남긴다.

### ReceiveHit/SHitInfo 폐기 (2026-09-21)
`IHitReceiver`와 `SHitInfo`를 삭제하고, 피격 판정을 `Health` setter 안의 "이전 값 > 새 값" 비교로 대체한다.

**폐기 근거**
1. **HP 감소 자체가 피격 신호다.** `AttributeToCombatAdapter`가 구독하는 `PostAttributeChanged` 콜백은 `(newValue, oldValue)`를 함께 준다. `Health` setter도 대입 직전의 `_health`를 들고 있으므로, 별도 통보 없이도 "줄었다 = 맞았다"를 판정할 수 있다.
2. **`ReceiveHit`은 아무 일도 하지 않았다.** `hitInfo`를 들여다보지 않고 `OnHit`으로 재방출만 했다. `SHitInfo`는 `Attacker` 제거 후 `Damage` 필드 하나만 남은 상태였다.
3. **이중 경로를 없앤다.** HP는 `Health` setter로, 피격은 `ReceiveHit`으로 따로 들어와서, 누가 `ReceiveHit`을 불러주지 않으면 실제로 HP가 깎여도 피격이 아니게 되는 구조였다. 실제 호출자도 0곳이었다.

**`OnHit`은 `event Action`(페이로드 없음)이 된다.** 피해량이 필요한 구독자는 값의 원천인 AttributeSet을 직접 읽으면 된다. Combat이 "직전 대입분"을 들고 다니면 그 값이 곧 또 다른 진실의 출처가 되므로, 만들지 않는다.

**되돌리는 비용**은 파일 2개 복구 + 메서드 1개로 작다. 다만 되돌려야 할 이유가 생긴다면 그건 "피격 판정에 HP 외의 정보가 필요해졌다"는 뜻이므로, 그때는 아래 State/CoR 항목과 함께 재설계한다.

**동작 변화(의도된 것)**
- HP가 0으로 떨어지면 `OnHit` → `OnDeath`가 둘 다 뜬다(기존엔 `OnDeath`만). 순서는 setter의 코드 순서로 보장된다.
- 회복(증가)과 동일 값 재대입은 피격이 아니다.
- 옵서버의 최초 씨앗 pull(0 → MaxHp)은 증가이므로 `OnHit`이 뜨지 않는다.

### 왜 Adapter가 아니라 Observer인가 (2026-09-21 파기)
당초엔 "이 클래스가 하는 일은 서로 다른 인터페이스를 하나로 변환(Adapter)하는 것이 아니라, `AttributeSet`의 변경 통지를 구독해서 Combat에 전달하는 것"이라 보고 `AttributeToCombatObserver`로 이름 붙였다. 기존 `AttributeToMotorAdapter`와 구조가 같다는 걸 알면서도 "이번 범위 밖"이라며 명명 불일치를 감수했었다.

**파기 근거:** 완성 후 다시 보니 두 클래스는 Awake의 `GetComponentInParent` 폴백, OnEnable의 초기값 pull + 콜백 등록, OnDisable의 구독 해제까지 구조가 동일하고, 차이는 구독 콜백 단계(`On` vs `Post`)와 대입 대상 프로퍼티뿐이다. "구독"은 구현 수단이지 이 컴포넌트의 정체성이 아니다 — 존재 이유는 `AttributeSet`의 콜백 인터페이스를 `CharacterCombat`이 이해하는 프로퍼티 대입으로 **변환**하는 것이므로 Adapter가 맞는 이름이다. `AttributeToCombatObserver` → `AttributeToCombatAdapter`로 개명하고 `Scripts/Observer/` → `Scripts/Adapter/`로 이동했다 (동작 변경 없음, `Post` 콜백 유지). `character-architecture.md` §5의 "값이 필요한 쪽이 AttributeSet을 직접 구독한다"를 `CharacterCombat` 본체가 수행하면 AttributeSet 참조가 생겨 기준 2·3이 깨진다는 원래 설계 이유는 그대로 유효하다 — Combat을 대신해 구독하는 별도 컴포넌트를 두는 결정 자체는 바뀌지 않았고, 이름만 실체에 맞춘 것이다.

### 검토한 다른 구조 (GoF 전수 대입)
Observer를 채택하되, 기각 사유를 남겨 같은 논의가 반복되지 않게 한다.

| 패턴 | 대입 | 판정 |
|---|---|---|
| **Observer** | 제3자가 AttributeSet을 구독해 `Combat.Health`에 push | **채택** |
| Bridge (DIP) | Combat이 `IHealthSource`를 소유하고 직접 구독 | 기각 — 아래 |
| Mediator | 서브 중재자가 구독해 Combat에 push | 기각 — `character-architecture.md:49,72` 정면 위반 |
| State | `_isDead` 대신 Alive/Dead 상태 객체 | 보류 — 아래 |
| Chain of Responsibility | 무적 → 실드 → 방어력 → HP 데미지 파이프라인 | 범위 밖 — 아래 |
| Command | 서브 중재자 지시를 객체로 캡슐화 | 기각 — `MoveMediator.CommandMove`처럼 메서드면 충분 |
| Strategy | `IDeathRule` 주입 | 기각 — 사망 규칙이 1개뿐 |
| Proxy / Facade / Decorator | 해당 없음 | 부적합 |
| 생성 패턴 5종 | 생성 문제가 아님 | 부적합 |
| Composite / Iterator / Visitor / Memento / Flyweight / Interpreter / Template Method | 대입할 축 없음 | 부적합 |
| *(비-GoF)* Event Bus | 전역 버스에 HP 이벤트 발행 | 기각 — 이식 시 버스까지 딸려가 기준 3 파괴 |

**Bridge를 기각한 이유(유일한 실질 경쟁자):** 인터페이스를 Combat 폴더 안에 두면 의존성은 똑같이 0이고 파일 수도 같다. 하지만 ① 구독/해제 코드가 Combat 내부로 들어오고, ② Unity가 인터페이스를 인스펙터에 직렬화하지 못해 캐스팅/`[SerializeReference]` 우회가 필요하며, ③ `Health`가 pull이 되어 `CharacterMotor.Speed`와 모양이 달라진다 — 전부 기준 1에 역행한다. 후보 중 Combat 안에 코드를 한 줄도 안 늘리는 것은 Observer뿐이다.

**State — 갈아탈 시점을 미리 정해둔다.** `SOAttributeData_Player.asset`에 `InvincibleTime`이 이미 있어 무적 상태가 들어올 예정이다. 지금은 `_isDead` bool 하나뿐이라 State(클래스 3개)는 오버엔지니어링이지만, `_isDead` 옆에 두 번째 bool을 추가하려는 순간이 State로 갈아탈 시점이다.

**Chain of Responsibility — 자리를 미리 못 박아둔다.** 무적·실드·방어력 같은 데미지 감쇄는 CoR이 제자리이고, 그 위치는 Combat이 아니라 스킬 매니저 쪽이다. Combat은 계산된 결과(줄어든 HP)를 보는 곳이지 계산하는 곳이 아니다 — 감쇄가 전부 끝난 뒤의 값만 `Health`로 들어온다.

### 어댑터 설계 메모
- `OnEnable`의 `GetValue` 1회 pull은 구독이 아니라 의도된 예외다 — `AttributeData` 생성자가 콜백 없이 `_value`를 직접 채우므로(`AttributeData.cs:15-19`) 초기값은 통지로 받을 방법이 없다.
- `On`이 아니라 `Post`를 구독한다 — `AttributeData.Value` setter는 `Pre → On 전부 → Post 전부` 순으로 부른다. 나중에 실드·뎀감이 `On`에서 HP를 되돌려쓰면 사망 판정이 보정 전 값을 볼 수 있다. 사망 판정은 값이 확정된 뒤의 종결 반응이므로 `Post`가 맞다.
- 연결 누락은 `Debug.Assert`가 아니라 `Debug.LogError`로 — `Debug.Assert`는 `UNITY_ASSERTIONS` 조건부라 릴리즈 빌드에서 컴파일아웃된다.
- (2026-09-21 추가) `SHitInfo.Attacker` 필드 제거 — 생성자에서 대입만 되고 읽는 코드가 0곳이었다(`CharacterCombat`은 `hitInfo`를 들여다보지 않고 `OnHit`으로 재방출만 한다). 어그로·킬 크레딧·넉백 방향을 쓰는 구독자가 생기면 다시 필요해지지만, 지금은 구독자 자체가 없어 YAGNI로 판단해 뺐다. 되돌리는 비용은 필드 1개 추가로 작다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 향후 구현될 스킬 매니저(공격자 쪽), 서브 중재자, BT Condition
- 어떤 시스템/모듈이 건드려지는가: `Assets/_Project/Scripts/Combat/`(전면 재작성), `Assets/_Project/Scripts/Adapter/AttributeToCombatAdapter.cs`(신규, 2026-09-21 개명·이동 — 원래 `Assets/_Project/Scripts/Observer/AttributeToCombatObserver.cs`), `Assets/Test/Combat/CombatManualTest.cs`(시나리오 교체)

## 제약 (Constraints)
- `.unity`/`.prefab`/`.asset` 편집 금지 — 코드만 작성한다. 어댑터는 실제 프리팹에 배선하지 않는다(아래 열린 질문 ③).
- 기존 `AttributeToMotorAdapter.cs`는 손대지 않는다.
- 서브 중재자 본체는 이번 범위 밖.

## 열린 질문 (Open questions)
- ① **데미지를 AttributeSet에 반영하는 주체/경로** — 스킬 매니저 설계 때 결정. 이번 범위에서 옵서버는 AttributeSet → Combat 읽기 방향만 담당한다. `AttributeSet`/`SOAttributeEffect`는 `Scripts/Mediator/`·`Scripts/Attribute/` 소유자(이세훈) 영역이라 **이세훈과의 협의 항목**이다 — BF_Leers 단독으로 확정하지 않는다. Combat은 이 결정과 무관하게 이미 완성 상태이므로 블로커는 아니다.
  - (2026-09-21 갱신) `ReceiveHit` 폐기로 이 질문이 **캐릭터 간 전투 통신의 유일한 경로**가 됐다. 공격이 성립하려면 누군가 피격자의 AttributeSet HP를 깎아야 하고, 그 순간 Combat의 `OnHit`/`OnDeath`는 자동으로 따라온다. 중요도는 올라갔지만 여전히 Combat 쪽 블로커는 아니다.
- ② **서브 중재자가 Combat에 내릴 지시의 내용** — 입구 모양(`CharacterCombat`의 public 메서드)은 확정, 목록(예: `SetInvulnerable`/`Kill`/`Revive`)은 서브 중재자 설계 때 결정. 지금 만들면 오버엔지니어링(CLAUDE.md §3-2)이라 만들지 않는다.
- ③ **`Current*` 속성을 `Max*`로 채우는 주체** — `SOAttributeData_Player.asset`/`_Enemy.asset`의 `CurrentHp`(및 `CurrentBattery`/`CurrentHeat`)가 전부 0으로 들어있고 채우는 코드가 없다. 이대로 `AttributeToCombatAdapter`를 실제 프리팹에 붙이면 씨앗 pull이 0을 읽어 전 캐릭터가 첫 프레임에 사망한다. 이것이 정해지기 전까지 어댑터는 코드만 존재하고 프리팹에 배선하지 않는다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
