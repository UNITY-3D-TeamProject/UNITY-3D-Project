# 피격 컨텍스트 1차: SEffectContext 전달 경로 구현

> intent-016 (`open`). 같은 날 앞선 설계 논의: [피격 컨텍스트 C안 설계](./hit-context-design-c.md)

## 1. 작업 요약 및 방법
- 설계 문서의 C안을 확정하고 **보내는 쪽(전달 경로)만** 구현했다. 이세훈과의 협의는 끝났다(사용자 확인).
- 신규 `Attribute.Core.SEffectContext`(readonly struct): `Cursor`(IEffectTarget), `Origin`(Vector3?)과 생성자로 구성된다.
- 시그니처 변경:
  - `IEffectTarget.SetValue(name, value, SEffectContext context = default)`
  - `AttributeSet.OnAttributeChange(name, new, old, context)`: On/Post 공용, Pre(`WithRef`)는 무변경
  - `SOAttributeEffect.Apply(IEffectTarget | GameObject, SEffectContext context = default)`: 기존 `cursor` 인자는 `context.Cursor`로 흡수
  - `MediatorBase.AttributeCallback`을 `Action<float, float, SEffectContext>`로, `NotifyAttributeChanged(..., context)`
  - `AttributeMediator.OnAttributeChanged`를 `Action<string, float, float, SEffectContext>`로
- 매개변수만 맞춘 곳: `CharacterMediator`, `MoveMediator`(람다 2개), `CombatMediator`(람다 1개), `PlayerHudPresenter`
- 호출부: `BulletController`, `MeleeHitController`, `AttributeRegenerator`를 `new SEffectContext(cursor)`로 바꿨다. **Origin은 아직 넣지 않았다.**
- `AttributeSet.SetValue`는 `_currentContext = context; 대입; _currentContext = default;` 순서로 처리한다. 값 대입 중 동기로 발생하는 콜백에 context를 실어 보낸다.

## 2. 결정 사항
- 이름은 `Cursor`로 한다. GAS의 `Instigator`에 해당하지만, 프로젝트가 이미 효과를 일으키는 주체를 cursor로 불러왔으므로 통일한다.
- 이름은 `ctx`가 아니라 `context`로 쓴다(사용자 선호).
- AttributeSet 내부는 지역 보관 방식으로 처리한다. GAS `CurrentModcallbackData`와 같은 방식이고 `AttributeData`는 바꾸지 않았다.
- 구독자에게는 델리게이트 매개변수로 전달한다. `CurrentContext` 프로퍼티를 공개하는 방식은 "콜백 중에만 유효하다"는 숨은 규칙이 생겨서 채택하지 않았다.
- `SetValue`와 `Apply`에는 `= default` 기본값을 둔다. 코스트 지불(`PayAttribute`), 값 복원(`PlayerState`), `SpawnerBase`, `GimmickUtility`는 수정하지 않았다.
- 직렬화 필드 `_cursorAttribute`의 이름은 유지했다(에셋 호환).

## 3. 현재 상태 및 이슈
- **배치 컴파일 0에러**: `unity test` EditMode 배치 실행 결과 exit 0, Editor.log의 `error CS`는 0건이다. `Assembly-CSharp.dll`이 수정 이후 다시 빌드된 것도 확인했다.
- **Combat 단위 테스트는 실행되지 않음(0건)**:
  - `Assets/Test/Combat/CharacterCombatTests.cs`는 있지만, 프로젝트 전체에 `.asmdef`가 하나도 없다. 2026-09-21에 만든 `Combat.asmdef`와 `Combat.Tests.asmdef`가 사라져서 테스트 어셈블리로 인식되지 않는다.
  - git 이력에도 없어서 커밋된 적이 없는 것으로 보인다. 이번 작업과는 별개이고, 복구 여부는 결정이 필요하다.
  - Combat 코드는 이번에 바꾸지 않았다.
- **Play 모드 회귀 검증 미완**: 열려 있는 Editor가 없었다. 동작 변경은 없어야 한다.
- 배치 실행 때 `ProjectSettings/EditorBuildSettings.asset`이 줄바꿈만 바뀌어 원복했다.

## 4. 다음 할 일 (Next Steps)
1. **[USER]** Play 회귀 확인
   - 플레이어 총알로 적 HP가 감소하고 사망한다.
   - 적 근접·원거리 공격으로 플레이어 HP가 감소한다.
   - HUD가 갱신된다.
   - Heat 회복이 동작한다.
2. Combat asmdef 복구 여부를 결정한다(테스트 5종 재실행용).
3. 2차 작업(intent-016)
   - `CombatMediator`: Health를 설정하는 구간에서 context를 보관하고, `IHitController.ReceiveHit(SEffectContext)`로 조종부에 연결한다.
   - 원점 기록: `BulletController`는 생성 위치를, `EnemyMeleeAttack`은 `transform.position`을 넣는다.
   - AI: `AIController : IHitController`가 원점을 바라보고 `Sensor.ReportPosition(origin)`을 호출한다. 그다음은 기존 수색 BT 가지가 이어받는다. **[USER]** 수색 가지의 우선순위와 Abort 설정을 확인한다.
