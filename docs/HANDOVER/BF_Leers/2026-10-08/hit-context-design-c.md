# 범용 피격 설계: 컨텍스트 관통(C안) 논의 기록

> 코드 변경 없음. 설계 논의만 진행했고 **최종 확정 전**이다. Attribute/Mediator 시그니처 변경이 포함되어 이세훈(담당자)과 협의가 필요하다.

## 1. 작업 요약 및 방법
- 출발점: 플레이어 총알에 맞은 **순찰/수색 상태** 적이 그 위치로 돌아보게 하고 싶음 → AI 한정이 아니라 범용 "피격"으로 확장해 논의.
- 현재 구조 확인:
  - 데미지 경로: `BulletController`/`MeleeHitController` → `SOAttributeEffect.Apply` → `AttributeSet` → `CharacterMediator` → `CombatMediator` 콜백 → `CharacterCombat.Health` setter → `OnHit()`(페이로드 없음).
  - 이 경로 어디에도 **출처(위치·가해자) 정보가 없다**. 2026-09-21에 `IHitReceiver`/`SHitInfo`를 제거했기 때문(폐기 사유는 "HP를 깎는 입구가 2개"였지 출처 정보 자체가 아님).
- 범용 쓰임새 정의(사용자): 플레이어→적(AI 돌아보기·수색), 적→플레이어(피격 반응), 플레이어가 **맞았을 때** UI(방향 표시 등), 플레이어가 **맞혔을 때** UI(히트마커·데미지 숫자).
- 비교한 안:
  - **A. 타격/피해 분리**: `OnHit`(HP 감소) 유지 + 별도 `OnStruck(SHitInfo)`. 0데미지에도 반응. 단 UI는 양(OnHit)과 방향(OnStruck)을 두 이벤트에서 짝지어야 함.
  - **B. 맡겨두기**: 총알이 Apply 직전 Combat에 정보 저장 → Health 감소 시 함께 방출. 0데미지/면역이면 정보가 남아 **다음 HP 감소(독 등)에 오염**, Clear 규칙·동기 전제 필요.
  - **C. 컨텍스트 관통(GAS식)**: Apply 인자로 컨텍스트를 넘겨 HP 변경 콜백까지 함께 흐름. 오염·Clear 문제 없음, 입구는 `Health` 하나 유지. 0데미지 반응은 없음.
- 언리얼 GAS 대응 정리:
  - `UGameplayEffect`↔`SOAttributeEffect`, `FGameplayEffectContext`↔`SEffectContext`(지금의 `cursor`를 넓힌 것)
  - `PostGameplayEffectExecute`(값 변경 직후 컨텍스트 사용)의 **역할은 구현**(AttributeSet 콜백에 ctx 추가), 형태(상속 오버라이드·Data 구조체)는 따르지 않음
  - `OnGameplayEffectAppliedDelegateToSelf`(값 변화 무관 적용 알림 = A의 OnStruck)는 **이번 범위 아님**
  - Spec/레벨/태그, Damage 메타 어트리뷰트, `MakeEffectContext` 팩토리는 불필요(상속·힙 공유 포인터·네트워크 복제 이유가 우리에겐 없음 → 생성자로 충분)

## 2. 결정 사항 (방향, 미확정)
- **기준 위치 = 공격이 시작된 곳(`Origin`)**
  - 총알: 생성 위치(발사 원점). `BulletController` 한 곳 수정으로 플레이어 총알·`EnemyRangedAttack` 총알 모두 해결.
  - 근접: **공격자 몸 위치**. `MeleeHitController`의 위치(`_hitPoint`)는 적 앞쪽이라 밀착 시 방향이 뒤집힐 수 있음 → `EnemyMeleeAttack`이 `transform.position`을 넘겨줌.
- **AI 반응**: 순찰/수색 중 피격 → 원점으로 돌아본 뒤 그 위치를 수색(기존 수색 가지 재사용 예정).
- **전달 = C안**
  ```csharp
  public readonly struct SEffectContext
  {
      public readonly IEffectTarget Instigator;  // null = 가해자 없음
      public readonly Vector3? Origin;           // null = 위치 없음 (독·회복·기믹)
      public SEffectContext(IEffectTarget instigator, Vector3? origin = null) { ... }
  }
  ```
  - `HasOrigin` 플래그 대신 `Vector3?`: (0,0,0)을 실제 원점으로 오인하는 확인 누락을 컴파일 단계에서 막음. 런타임 전용이라 직렬화 불가 문제 없음. (프로젝트에 값 타입 nullable 선례는 없음)
  - struct 생성자로 생성, 힙 할당 없음. 경로 전체를 `SEffectContext` 타입 그대로 넘겨 박싱 없음.
  - `Instigator`와 `Origin`은 독립(재생기는 Instigator만, 함정 기믹은 Origin만 있을 수 있음).
- **Combat 무변경, `SHitInfo` 부활 안 함**: 출처 정보는 Combat 판정에 쓰이지 않음. `CombatMediator`가 어트리뷰트 콜백에서 받은 ctx를 `Health` 설정 → `OnHit` 동기 발생 구간 동안만 들고 있다가 조종부로 넘김(같은 메서드 안에서 저장·사용·초기화하므로 B안식 오염 없음). Combat 테스트 5개 무변경.
- **수신 구조**

  | 소비자 | 경로 |
  |---|---|
  | 조종부(AI) | `CombatMediator` → `IHitController.ReceiveHit(ctx)` |
  | 플레이어 맞았을 때 UI | `AttributeSet` 콜백 `(name, new, old, ctx)` 직접 (Combat 경유 안 함) |
  | 다른 서브 중재자 반응(경직·시전 취소, 미래) | center(`CharacterMediator`)가 `_combatMediator.OnHit` 구독 자리에서 연결 |
  | 플레이어 맞혔을 때 UI | 보류 |

- **`IHitController` 배치** — 기존 `IMoveController`와 동일:
  - 선언: 중재자 쪽(`CombatMediator.cs` 상단), 구현: 조종부(`AIController`), 호출: 중재자(`GetComponentInParent`로 찾아 OnEnable/OnDisable에서 Bind/Unbind)
  - 다른 점은 데이터 방향(중재자→조종부)뿐이라 Set/Clear 쌍이 아닌 `ReceiveHit(SEffectContext)` 메서드 하나.
  - center가 아닌 `CombatMediator`가 연결하는 이유: center가 보내는 건 "어트리뷰트 변경"(원재료)이고 "피격" 판정 결과는 CombatMediator가 가짐. 조종부 연결은 서브 중재자 담당이라는 기존 규칙 유지.
  - 플레이어 조종부는 구현 안 하면 연결이 건너뛰어짐(선택적).

## 3. 현재 상태 및 이슈
- 코드 변경 없음. 설계 방향만 좁힌 상태이며 **사용자 최종 확정 전**.
- **협의 필요(이세훈 담당 영역)**: `IEffectTarget.SetValue`, `AttributeSet.SetValue`·`OnAttributeChange` 델리게이트, `AttributeMediator`, `MediatorBase.NotifyAttributeChanged`·AttributeCallback 시그니처, `CharacterMediator`, `PlayerHudPresenter` 콜백.
- 미결:
  - 조종부 연결 주체 최종 확정(`CombatMediator` 연결 추천, 조종부 직접 구독안과 미결정 상태로 남음)
  - 맞혔을 때 UI의 되돌림 경로(맞은 쪽 → `ctx.Instigator` → 때린 쪽)
  - `SOAttributeEffect.Apply`에서 기존 `cursor` 인자를 `SEffectContext.Instigator`로 흡수할지 병행할지
- 알려진 한계: 0데미지·무적·실드 피격에는 반응 없음(필요 시 "효과 적용됨" 알림을 별도 추가). 한 피격에 여러 어트리뷰트가 바뀌면 콜백이 여러 번 옴(현재 효과 에셋은 어트리뷰트 1개라 당장 문제 없음).
- `CombatMediator`는 조종부 1개만 찾음(기존 서브 중재자와 같은 제약).

## 4. 다음 할 일 (Next Steps)
1. C안 확정 여부 결정 후 이세훈과 Attribute/Mediator 시그니처 변경 협의.
2. 확정되면 `docs/intent/`에 intent 문서 작성(A/B/C 비교, 변경 시그니처 목록, 열린 질문) 및 `INTENT_POINTER.md` 갱신.
3. 구현 순서(안): `SEffectContext` → Apply/SetValue/콜백 시그니처 → `CombatMediator`(ctx 보관 + `IHitController` 연결) → `BulletController`(원점 기록)·`EnemyMeleeAttack`/`MeleeHitController`(공격자 위치) → `AIController : IHitController` + BT(돌아보기 → 원점 수색).
4. 검증: 컴파일 0에러, Combat 테스트 5/5, Play에서 순찰/수색 적 피격 시 돌아보기·수색 / 독 등 출처 없는 피해에 반응 없음 확인.
