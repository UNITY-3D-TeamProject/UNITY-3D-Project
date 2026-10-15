---
id: intent-016
title: 효과 적용 출처 정보(SEffectContext)를 어트리뷰트 변경 콜백까지 전달
part: Attribute / Mediator / Combat
status: open   # open | resolved
created: 2026-10-08
resolved: null
---

## 문제 (Problem)
- 플레이어 총알에 맞은 순찰/수색 중인 적이 공격 원점을 돌아보게 하고 싶다. 하지만 데미지 경로(`SOAttributeEffect.Apply` → `AttributeSet` → `CharacterMediator` → `CombatMediator` → `CharacterCombat.Health` → `OnHit()`)에는 **누가, 어디서** 때렸는지에 대한 정보가 전혀 없다.
- 2026-09-21에 `IHitReceiver`/`SHitInfo`를 제거했다. 폐기한 이유는 "HP를 깎는 입구가 2개"라는 점이었고, 출처 정보 자체가 필요 없어서 뺀 것은 아니다.
- 출처 정보를 쓰려는 곳: 플레이어→적(AI 돌아보기·수색), 적→플레이어(피격 반응), 플레이어가 맞았을 때 UI(방향 표시), 플레이어가 맞혔을 때 UI(히트마커).

## 기대 결과 (Proposed outcome)
GAS의 `FGameplayEffectContext`처럼 `SEffectContext{Cursor, Origin}`를 `Apply` 인자로 받아 어트리뷰트 변경 콜백까지 함께 흘려보낸다.

비교한 안:
- **A. 타격/피해 분리**: `OnHit`와 별도로 `OnStruck(SHitInfo)`를 둔다. 0데미지에도 반응할 수 있지만, UI가 피해량과 방향을 서로 다른 이벤트에서 받아 짝지어야 한다.
- **B. 맡겨두기**: 총알이 `Apply` 직전에 Combat에 정보를 저장해 둔다. 0데미지나 면역으로 HP가 안 깎이면 정보가 남아 다음 HP 감소(독 등)에 섞인다.
- **C. 컨텍스트 관통 (채택)**: 섞임 문제가 없고, HP를 바꾸는 입구도 `Health` 하나로 유지된다.

세부 결정:
- 이름은 `Cursor`로 한다. GAS의 `Instigator`에 해당하지만, 프로젝트가 이미 효과를 일으키는 주체를 cursor로 불러왔으므로 통일한다. `Apply`의 `cursor` 인자는 `SEffectContext.Cursor`로 흡수한다.
- `Origin`은 `Vector3?`로 둔다. (0,0,0)을 실제 원점으로 잘못 읽는 실수를 컴파일 단계에서 막기 위해서다.
- `AttributeSet` 내부에서는 context를 지역 보관한다. `SetValue`가 context를 잠깐 들고 있다가 대입이 끝나면 이전 값으로 되돌린다. 콜백 안에서 같은 세트의 `SetValue`가 다시 호출돼도 바깥 호출의 context가 지워지지 않게 하고, 구독자가 예외를 던져도 context가 남지 않도록 `try/finally`를 쓴다(2026-10-09). GAS의 `CurrentModcallbackData` 방식과 같고, `AttributeData`는 바꾸지 않는다.
- 구독자에게는 델리게이트 매개변수로 넘긴다. `CurrentContext` 프로퍼티를 공개하는 방식은 "콜백 중에만 유효하다"는 숨은 규칙이 생겨서 채택하지 않았다.
- `SetValue`와 `Apply`의 context에는 `= default`(출처 없음) 기본값을 둔다. 코스트 지불, 값 복원, 기믹 같은 호출부는 수정하지 않는다.
- 콜백 인자는 `SAttributeChangeData{AttributeName, NewValue, OldValue, Context}`로 묶는다(GAS `FOnAttributeChangeData`). context를 쓰지 않는 구독자가 4번째 인자를 받지 않아도 되게 하기 위해서다(2026-10-09). `CharacterMediator.OnAttributeChanged`와 `PlayerFacade`는 `Action<string, float, float>`을 유지한다.

### 1차 (전달 경로) — 이번 작업
- 신규 `Attribute.Core.SEffectContext`
- `IEffectTarget.SetValue(string, float, SEffectContext context = default)`
- `AttributeSet.OnAttributeChange(SAttributeChangeData)` (On/Post 공용, Pre는 무변경). 처음에는 `(string, float, float, SEffectContext)`였고 2026-10-09에 구조체로 묶었다.
- `SOAttributeEffect.Apply(IEffectTarget | GameObject, SEffectContext context = default)`
- `MediatorBase.AttributeCallback`을 `Action<SAttributeChangeData>`로, `NotifyAttributeChanged(SAttributeChangeData)`
- `AttributeMediator.OnAttributeChanged`를 `Action<SAttributeChangeData>`로
- `CharacterMediator`, `MoveMediator`, `CombatMediator`, `PlayerHudPresenter`는 매개변수만 맞춘다.
- 호출부(`BulletController`, `MeleeHitController`, `AttributeRegenerator`)는 `new SEffectContext(cursor)`로 바꾼다. 원점은 아직 넣지 않는다.

### 2차 — 다음 작업
- `CombatMediator`: `Health`를 설정하고 `OnHit`가 발생하는 구간에서만 context를 보관한다. 같은 파일 상단에 선언한 `IHitController.ReceiveHit(SEffectContext)`로 조종부에 연결한다(`IMoveController`와 같은 배치).
- 원점 기록: `BulletController`는 생성 위치를, `EnemyMeleeAttack`은 공격자 몸 위치를 넣는다. `_hitPoint`는 적 앞쪽이라 밀착하면 방향이 뒤집힐 수 있어서 쓰지 않는다.
- AI: `AIController : IHitController`가 원점을 바라본 뒤 `Sensor`에 위치를 알리고, 기존 수색 BT 가지가 이어받는다.

### AI 피격 돌아보기 — 2차 AI 항목 대체 (2026-10-15 결정)
위 2차의 `IHitController` + Sensor 위치 통보 + 수색 대신, 더 단순한 방식으로 AI 반응을 넣었다.
- `CharacterMediator.OnHitFrom(Vector3)`: 플레이어 피격 애니메이션과 같은 지점(`OnHitCallback`의 생존 블록)에서, 이미 계산한 `GetHitSource()` 결과(HitPoint → 없으면 Cursor 위치)를 알린다. `OnHit`보다 먼저 발생하며 기준 위치가 없거나 사망을 일으킨 피격에는 발생하지 않는다.
- `AIController`가 구독해, 대상이 보이지 않을 때만 `_hitLookDuration`(기본 1.0초) 동안 목적지를 유지한 채 멈추고 맞은 쪽을 바라본다. 끝나면 하던 이동을 이어간다. 돌아보는 중 대상을 발견하면 멈춤을 바로 끝낸다.
- 방향 기준은 `-Direction`이 아니라 HitPoint다. 적에게도 방향별 피격 애니메이션을 넣을 예정이라 애니메이션과 같은 기준을 쓴다.
- 메디에이터가 직접 몸을 돌리지 않는다. 플레이어는 카메라 방향이 회전을 덮어쓰고, 적은 조종부가 매 프레임 회전을 요청하며, `LockLookDirection`은 구르기·BT 조준과 충돌한다. 메디에이터는 위치만 알리고 판단은 조종부가 한다.
- 기본값 1.0초는 적 `CharacterRotator` 회전 속도(180°/s)로 180° 도는 시간이다(0.5초면 90°만 돈다).
- BT·Sensor는 바꾸지 않았다. 피격 후 수색(Sensor 위치 통보)은 필요해지면 따로 한다.

### Context에 Effect 추가 (2026-10-10 결정)
팀 제안: 값 변경 콜백에서 "어떤 Effect 때문에 바뀌었는지"도 알 수 있도록 `SEffectContext`에 Effect를 담는다.

| 항목 | 현재 | 변경 |
|---|---|---|
| `SEffectContext` 필드 | `Cursor`, `Origin` | `Cursor`, `Origin`, `Effect`(`SOAttributeEffect`) |
| Effect를 채우는 곳 | 없음 | `SOAttributeEffect.Apply`가 맨 앞에서 `this`로 덮어쓴다 |
| 호출부(`BulletController`, `MeleeHitController`, `AttributeRegenerator`) | `new SEffectContext(cursor)` | 그대로 |
| 구독자가 Effect를 꺼내는 법 | 불가 | `data.Context.Effect` (`SAttributeChangeData`는 바꾸지 않음) |
| Effect 없이 바뀐 값(코스트 지불, 값 복원 등) | `default` | 그대로 `default`, `Effect == null` |

얻는 것:
- 구독자가 `Cursor == null`이 원래 출처 없는 효과(독, 회복 등)인지, 호출부가 context를 빠뜨린 실수인지 Effect를 보고 구분할 수 있다.
- 구독자가 Effect 종류에 따라 반응을 나눌 수 있다.

GAS와 다른 점: GAS는 Effect(`Def`)를 Context가 아니라 `FGameplayEffectSpec`에 둔다. Context만 따로 쓰는 곳(GameplayCue 등)이 많고, 한 Context를 여러 Effect가 공유하며, 네트워크로 따로 복제하기 때문이다. 이 프로젝트의 Context는 어트리뷰트 변경 콜백을 통해서만 전달되고, 호출마다 새로 만들어 공유하지 않으므로 둘을 나눌 이유가 없다. Spec을 따로 두지 않는다는 제약(아래)과도 맞는다.

세부 결정:
- 호출부는 Effect를 넣지 않는다. `Apply`가 항상 `this`로 덮어쓰므로, 호출부가 다른 Effect를 넣어도 무시되고 Context와 실제로 적용된 Effect가 어긋나지 않는다.
- 생성자는 `SEffectContext(IEffectTarget cursor, Vector3? origin = null, SOAttributeEffect effect = null)`로 한다. 마지막 매개변수가 기본값을 가지므로 기존 호출부는 컴파일이 깨지지 않는다.
- 한 번 피격으로 어트리뷰트가 여러 개 바뀌는 경우(알려진 한계 2)에는 Effect별로 콜백이 오고, 각 콜백의 `Context.Effect`가 그 값을 바꾼 Effect를 가리킨다.
- 이번에는 필드만 추가하고, Effect를 읽는 구독자는 실제로 필요해질 때 만든다(2026-10-11).
- 필드 타입은 `SOAttributeEffect`로 하고 `Attribute.Core ↔ Attribute.Effect` 양방향 참조를 허용한다(2026-10-11). 지금은 `Effect → Core` 한 방향뿐이다. 프로젝트에 asmdef가 없어 모든 스크립트가 `Assembly-CSharp` 하나로 컴파일되므로 순환 참조가 컴파일이나 실행에 영향을 주지 않는다. 나중에 asmdef를 도입하면 Attribute(Core + Effect)를 asmdef 하나로 묶어야 한다. Core와 Effect를 따로 나누면 순환 참조 때문에 막힌다. 순환을 피하려면 필드 타입을 `ScriptableObject`로 바꿀 수 있지만, 그러면 구독자가 캐스팅해야 해서 채택하지 않았다.
- `SEffectContext`는 이세훈 담당 영역이다. 시그니처는 깨지지 않으며, 공유는 BF_Leers가 직접 한다.

변경 파일:
- `Attribute/Core/SEffectContext.cs`: `Effect` 필드와 생성자 매개변수 추가
- `Attribute/Effect/SOAttributeEffect.cs`: `Apply(IEffectTarget, SEffectContext)` 맨 앞에서 context를 `this`로 다시 만든다. `Apply(GameObject, ...)`는 이 메서드를 호출하므로 바꾸지 않는다.

검증:
- 컴파일 에러 없음
- 플레이 모드에서 총알·근접 피격 시 구독자가 받은 `Context.Effect`가 실제 적용한 에셋인지 확인
- 결과(2026-10-11): 컴파일 에러 0건. Play 모드에서 eval로 `BulletController`와 같은 형태의 호출 `Apply(GameObject, new SEffectContext(cursor))`를 실행해 아래 3가지를 확인했다. 실제 총알 발사와 근접 피격은 해 보지 않았다.
  - `PlayerFireEffect`를 적용하면 `Context.Effect`가 `PlayerFireEffect`로 온다.
  - 호출부가 다른 Effect를 넣어도 `PlayerFireEffect`로 덮어쓴다.
  - `SetValue`를 직접 호출하면 `Effect`는 `null`이다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: AI(피격 반응), 플레이어 HUD(맞은 방향, 추후), 다른 서브 중재자(경직 등, 추후)
- 어떤 시스템/모듈이 건드려지는가: Attribute Core/Effect/ProjectSpecific, Mediator(Base, Character, Attribute, Move, Combat), UI(PlayerHudPresenter), Projectile, Hit
- 이세훈 담당 영역(Attribute/Mediator) 시그니처가 바뀐다. 협의 완료(2026-10-08).

## 제약 (Constraints)
- Combat(`CharacterCombat`)은 바꾸지 않고 `SHitInfo`도 되살리지 않는다. Combat 테스트 5개는 그대로 둔다.
- 직렬화 필드 `_cursorAttribute`의 이름은 유지한다(에셋 호환).
- Spec, 레벨, 태그, `MakeEffectContext` 팩토리 같은 GAS 부가 구조는 들이지 않는다. 생성자로 충분하다.

## 열린 질문 (Open questions)
- 맞혔을 때 UI의 되돌림 경로(맞은 쪽 → `context.Cursor` → 때린 쪽)
- 알려진 한계 1: 0데미지, 무적, 실드 피격에는 반응하지 않는다. 필요하면 "효과 적용됨" 알림을 따로 추가한다.
- 알려진 한계 2: 한 번 피격으로 어트리뷰트 여러 개가 바뀌면 콜백도 여러 번 온다. 현재 효과 에셋은 어트리뷰트 1개라 당장은 문제가 없다.
- 알려진 한계 3: 기본값 `default` 때문에, 새 피해 호출부가 context를 빼먹어도 컴파일 에러가 나지 않는다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
