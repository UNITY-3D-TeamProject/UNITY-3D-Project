# 01. SO Apply 함수 퍼사드에 추가

- **우선순위**: 최우선
- **상태**: 대기

## 목표
외부(Stage, 기믹, UI 등)가 `PlayerFacade` 를 통해 `SOAttributeEffect` 를 플레이어에 적용할 수 있게 한다.

## 현재 상황
- `SOAttributeEffect.Apply(IEffectTarget target, IEffectTarget cursor = null)` / `Apply(GameObject target, ...)` 가 있다 (`Assets/_Project/Scripts/Attribute/Effect/SOAttributeEffect.cs:60`, `:87`).
- `PlayerFacade` 는 `EffectTarget` 프로퍼티만 노출한다. 외부는 `effect.Apply(facade.EffectTarget)` 처럼 직접 호출해야 한다.

## 작업 범위 (예상)
- `PlayerFacade` 에 효과 적용 공개 메서드 추가 (`CharacterMediator` 경유 여부는 질문 참고).

## 열린 질문
- [ ] 메서드 시그니처: `ApplyEffect(SOAttributeEffect effect)` 하나만 둘지, 배열(`SOAttributeEffect[]`) 오버로드도 둘지?
- [ ] `cursor`(효과 주체) 인자를 받을지, 플레이어 자신으로 고정할지?
- [ ] `CharacterMediator` 에도 같은 메서드를 추가해 경유할지, 퍼사드에서 `EffectTarget` 으로 바로 적용할지?
- [ ] 이 함수를 실제로 쓸 곳(예: `GimmickUtility.ApplyEffects`, `SpawnerBase`)을 이번에 함께 전환할지?

## 완료 기준
- 컴파일 에러 없음. 외부에서 퍼사드 메서드만으로 효과가 적용되는지 확인.
