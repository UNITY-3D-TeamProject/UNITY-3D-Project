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
- `AttributeSet` 내부에서는 context를 지역 보관한다. `SetValue`가 context를 잠깐 들고 있다가 콜백이 발행되면 곧바로 비운다. GAS의 `CurrentModcallbackData` 방식과 같고, `AttributeData`는 바꾸지 않는다.
- 구독자에게는 델리게이트 매개변수로 넘긴다. `CurrentContext` 프로퍼티를 공개하는 방식은 "콜백 중에만 유효하다"는 숨은 규칙이 생겨서 채택하지 않았다.
- `SetValue`와 `Apply`의 context에는 `= default`(출처 없음) 기본값을 둔다. 코스트 지불, 값 복원, 기믹 같은 호출부는 수정하지 않는다.

### 1차 (전달 경로) — 이번 작업
- 신규 `Attribute.Core.SEffectContext`
- `IEffectTarget.SetValue(string, float, SEffectContext context = default)`
- `AttributeSet.OnAttributeChange(string, float, float, SEffectContext)` (On/Post 공용, Pre는 무변경)
- `SOAttributeEffect.Apply(IEffectTarget | GameObject, SEffectContext context = default)`
- `MediatorBase.AttributeCallback`을 `Action<float, float, SEffectContext>`로, `NotifyAttributeChanged(..., context)`
- `AttributeMediator.OnAttributeChanged`를 `Action<string, float, float, SEffectContext>`로
- `CharacterMediator`, `MoveMediator`, `CombatMediator`, `PlayerHudPresenter`는 매개변수만 맞춘다.
- 호출부(`BulletController`, `MeleeHitController`, `AttributeRegenerator`)는 `new SEffectContext(cursor)`로 바꾼다. 원점은 아직 넣지 않는다.

### 2차 — 다음 작업
- `CombatMediator`: `Health`를 설정하고 `OnHit`가 발생하는 구간에서만 context를 보관한다. 같은 파일 상단에 선언한 `IHitController.ReceiveHit(SEffectContext)`로 조종부에 연결한다(`IMoveController`와 같은 배치).
- 원점 기록: `BulletController`는 생성 위치를, `EnemyMeleeAttack`은 공격자 몸 위치를 넣는다. `_hitPoint`는 적 앞쪽이라 밀착하면 방향이 뒤집힐 수 있어서 쓰지 않는다.
- AI: `AIController : IHitController`가 원점을 바라본 뒤 `Sensor`에 위치를 알리고, 기존 수색 BT 가지가 이어받는다.

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
