# 01. SO Effect / Context Apply 함수 추가 (CharacterMediator + 퍼사드)

- **우선순위**: 최우선
- **상태**: 대기

## 목표
외부(Stage, 기믹, UI 등)가 `SOAttributeEffect` 를 출처 정보(`SEffectContext`)와 함께 캐릭터에 적용할 수 있도록 `CharacterMediator` 에 Apply 함수를 추가하고, 플레이어는 `PlayerFacade` 에도 노출한다.

## 현재 상황
- `SOAttributeEffect.Apply(IEffectTarget target, SEffectContext context = default)` / `Apply(GameObject target, SEffectContext context = default)` 가 있다 (`Scripts/Attribute/Effect/SOAttributeEffect.cs:60`, `:91`).
  - `SEffectContext` 는 `Cursor`(효과 주체), `Origin`(시작 위치), `Effect`(Apply 가 채움)를 담는다 (`Scripts/Attribute/Core/SEffectContext.cs`).
  - `origin/feature/AI/WeakPoint` 브랜치가 `HitPoint`(맞은 지점)를 추가했다. 아직 develop 에 머지되지 않았다 ([22번](22-weak-point-damage.md) 참고).
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
- [ ] 메서드 시그니처
  - `ApplyEffect(SOAttributeEffect effect, SEffectContext context = default)` 하나로 두는가?
  - 배열(`SOAttributeEffect[]`) 오버로드도 두는가?
- [ ] "Context Apply" 의 의미: `SOAttributeEffect` 없이 **어트리뷰트 값 + `SEffectContext`** 만으로 적용하는 함수도 필요한가? (예: `SetAttribute(key, value, context)`)
- [ ] `context.Cursor` 가 비어 있을 때 효과 주체를 캐릭터 자신으로 채우는가, 비워 두는가?
- [ ] 반환값: 적용 성공 여부(`bool`, 어트리뷰트가 없을 때 false)를 돌려주는가?
- [ ] 이 함수를 실제로 쓸 곳(`StageManager`, `GimmickUtility`, `SpawnerBase` 등)을 이번에 함께 전환하는가, 다른 파트에 안내만 하는가?

## 완료 기준
- 컴파일 에러 없음.
- 외부에서 퍼사드 메서드만으로 효과가 적용되고, 어트리뷰트 변경 콜백에 context 가 전달되는지 확인.
