# 01. SO Effect / Context Apply 함수 추가 (CharacterMediator + 퍼사드)

- **우선순위**: 최우선
- **상태**: 완료

## 목표
외부(Stage, 기믹, UI 등)가 `SOAttributeEffect` 를 출처 정보(`SEffectContext`)와 함께 캐릭터에 적용할 수 있도록 `CharacterMediator` 에 Apply 함수를 추가하고, 플레이어는 `PlayerFacade` 에도 노출한다.

## 현재 상황
- `SOAttributeEffect.Apply(IEffectTarget target, SEffectContext context = default)` / `Apply(GameObject target, SEffectContext context = default)` 가 있다 (`Scripts/Attribute/Effect/SOAttributeEffect.cs:60`, `:91`).
  - `SEffectContext` 는 `Cursor`(효과 주체), `Origin`(시작 위치), `Effect`(Apply 가 채움)를 담는다 (`Scripts/Attribute/Core/SEffectContext.cs`).
  - `HitPoint`(맞은 지점), `Direction`(날아온 방향)도 담는다 (WeakPoint 머지됨, [22번](22-weak-point-damage.md) 참고).
- `CharacterMediator` 는 `EffectTarget` 프로퍼티만 있고 Apply 함수는 없다. `PlayerFacade` 도 `EffectTarget` 만 노출한다.
- 현재 외부 호출부는 대상의 `EffectTarget` 이나 GameObject 로 직접 Apply 한다.
  - `StageManager:424` — `_fallEffect.Apply(_playerFacade.EffectTarget)`
  - `GimmickUtility:39` — `effect.Apply(target)`
  - `SpawnerBase:42` — `effect.Apply(attributes)`
  - `BulletController:108`, `MeleeHitController:70` — `Apply(target, new SEffectContext(cursor))`

## 작업 범위 (예상)
- `CharacterMediator` 에 효과 적용 공개 메서드 추가.
- `PlayerFacade` 에 같은 메서드 노출 (`CharacterMediator` 경유).

## 열린 질문
- [x] 메서드 시그니처 → 배열 오버로드 없이 `ApplyEffect(SOAttributeEffect effect)` + `ApplyEffect(SEffectContext context)` 두 개.
- [x] "Context Apply" 의 의미 → 값 직접 설정 함수는 만들지 않는다. context 만 받는 함수는 `context.Effect` 를 적용한다.
- [x] `context.Cursor` 가 비어 있을 때 → 채우지 않는다. 효과가 cursor 를 필요로 하는데(ValueSource=Attribute) 없으면 적용하지 않고 false.
- [x] 반환값 → `bool`. 적용했으면 true (Float 효과는 cursor 없이도 true), 효과·대상·대상 어트리뷰트·필요한 cursor 가 없으면 false.
- [x] 호출부 전환 → 이번에는 전환하지 않고 다른 파트에 안내만 한다 (`StageManager:424`, `FeedRound2:543`, `HomingHazard:82` 가 `PlayerFacade.EffectTarget` 직접 사용).

## 완료 기준
- 컴파일 에러 없음.
- 외부에서 퍼사드 메서드만으로 효과가 적용되고, 어트리뷰트 변경 콜백에 context 가 전달되는지 확인.
