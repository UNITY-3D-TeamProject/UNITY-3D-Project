# SEffectContext에 Effect 필드 추가

> intent-016 (`open`, 2차 작업 남음). 결정 근거는 intent-016의 "Context에 Effect 추가 (2026-10-10 결정)" 절에 있다.

## 1. 작업 요약 및 방법
- 어트리뷰트 값 변경 콜백 구독자가 `data.Context.Effect`를 보고 값을 바꾼 Effect 에셋을 알 수 있게 했다.
- `Attribute/Core/SEffectContext.cs`
  - `public readonly SOAttributeEffect Effect` 필드를 추가했다.
  - 생성자에 `SOAttributeEffect effect = null` 매개변수를 마지막에 추가했다.
- `Attribute/Effect/SOAttributeEffect.cs`
  - `Apply(IEffectTarget, SEffectContext)` 맨 앞에서 `context = new SEffectContext(context.Cursor, context.Origin, this);`를 실행한다.
  - `Apply(GameObject, ...)`는 이 메서드를 거치므로 수정하지 않았다.
- 호출부(`BulletController`, `MeleeHitController`, `AttributeRegenerator`)와 `SAttributeChangeData`는 수정하지 않았다.

## 2. 결정 사항
- **필드만 추가한다.** Effect를 읽는 구독자는 실제로 필요해질 때 만든다.
- **호출부는 Effect를 넣지 않는다.** `Apply`가 항상 `this`로 덮어쓰므로, Context와 실제로 적용된 Effect가 어긋나지 않는다.
- **필드 타입은 `SOAttributeEffect`로 하고 Core ↔ Effect 양방향 참조를 허용한다.**
  - 프로젝트에 asmdef가 없어 모든 스크립트가 `Assembly-CSharp` 하나로 컴파일된다. C#은 어셈블리 단위로 컴파일하므로 C++의 include 순서 같은 문제가 없다.
  - 앞으로 asmdef를 도입하면 Attribute(Core + Effect)를 asmdef 하나로 묶어야 한다. 둘을 나누면 순환 참조 때문에 막힌다.
  - `ScriptableObject` 타입을 쓰면 순환을 피할 수 있지만, 구독자가 캐스팅해야 해서 채택하지 않았다.
- `SEffectContext`는 이세훈 담당 영역이다. 시그니처는 깨지지 않으며, 공유는 BF_Leers가 직접 한다.

## 3. 현재 상태 및 이슈
- **컴파일 0에러**: unity-editor-mcp의 `recompile` 결과다.
- **Play 모드 eval 검증 통과** (`Map_4_Security_T` 씬, `Ranged(Clone)`의 AttributeSet):
  - `PlayerFireEffect.Apply(GameObject, new SEffectContext(cursor))`를 호출하면 콜백의 `Context.Effect`가 `PlayerFireEffect`, `Cursor`가 non-null로 온다.
  - 호출부가 다른 Effect(`Effect_Item_HealHp20`)를 넣어도 `PlayerFireEffect`로 덮어쓴다.
  - `SetValue`를 직접 호출하면 `Effect == null`이다.
  - 검증용 구독은 eval 안에서 등록하고 해제했다. 코드에 남은 것은 없다.
- 실제 총알 발사와 근접 피격 경로는 Play에서 확인하지 않았다. 다만 두 경로 모두 위와 같은 `Apply` 형태를 호출한다.
- 이세훈님 공유가 아직 안 됐다.

## 4. 다음 할 일 (Next Steps)
1. **[USER]** 이세훈님에게 `SEffectContext.Effect` 추가 사실을 공유한다.
2. intent-016 2차 작업을 진행한다.
   - `CombatMediator`에 `IHitController.ReceiveHit(SEffectContext)`를 추가한다.
   - Origin을 기록한다(`BulletController`는 생성 위치, `EnemyMeleeAttack`은 공격자 몸 위치).
   - `AIController`가 공격 원점을 돌아본 뒤 기존 수색으로 이어지게 한다.
