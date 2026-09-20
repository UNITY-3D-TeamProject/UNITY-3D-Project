# CharacterCombat — OnHit/OnDeath 이벤트 순서 역전 방지

## 작업 요약 및 방법

사용자가 구상한 전투 흐름(Execute → Effect가 HP 감소 → 속성Set 변경 이벤트 → 전투 컴포넌트 통지 → 사망/피격 체크 → 중재자에 이벤트 방출)을 기존 `CharacterCombat` 구현과 대조 점검했다. 1~3단계(Effect.Apply, AttributeSet 3단 콜백), 5-a(사망 체크), 6(이벤트 방출)은 이미 동작했지만, 4단계(전투 컴포넌트가 속성 변경을 통지받는 옵서버 배선)는 팀원이 별도로 담당하기로 확인됨.

문제는 그 옵서버 배선이 들어오는 순간 `ReceiveHit` 내부에서 이벤트 순서가 역전된다는 점이었다: `ReceiveHit`이 자기 몸통 안에서 `Effect.Apply`를 호출하는데, `Apply`가 `SetValue`를 거쳐 속성 변경 이벤트를 일으키고 이것이 `NotifyAttributeChanged → CheckDeath → OnDeath`로 즉시 이어지면, `ReceiveHit` 마지막 줄의 `OnHit` 발사보다 `OnDeath`가 먼저 터진다. 중재자가 "사망 처리" 다음에 "피격 연출"을 받게 되어 이미 죽은 캐릭터에 피격 모션이 재생될 수 있다.

Plan Mode로 전환해 사용자와 두 차례 확인(피격 판정을 어느 경로로 할지, AttributeSet 구독 배선을 누가 담당할지)을 거친 뒤, `OnHit` 발사를 `Effect.Apply` 호출보다 앞으로 옮기는 최소 수정으로 승인받았다.

## 결정 사항

- **`CharacterCombat.ReceiveHit`의 순서를 `OnHit 발사 → Effect.Apply → CheckDeath`로 고정.** 옵서버 배선 전에는 원래처럼 `OnHit → Apply → CheckDeath → OnDeath` 순서가 유지되고, 배선 후에는 `Apply` 도중 `NotifyAttributeChanged → CheckDeath → OnDeath`가 먼저 끝나도 `OnHit`은 이미 발사된 뒤이므로 `OnHit → OnDeath` 순서가 항상 보장된다. 이후의 `CheckDeath()` 재호출은 `_isDead` 가드로 무해한 no-op.
- **5-b(비-피격 경로에서의 "피격 체크")는 이번 범위에서 추가하지 않음.** `NotifyAttributeChanged`에서 `OnHit`을 쏘면 직접 피격 시 `OnHit`이 두 번(옵서버 경로 + `ReceiveHit`) 뜨고, 애초에 속성 변경 이벤트는 `(속성명, newValue, oldValue)`만 주므로 `OnHit`의 payload인 `SHitInfo`(Effect+공격자)를 만들 수 없다. 독/낙하 대미지에 피격 연출이 실제로 필요해지면 별도 이벤트(예: `OnHealthDecreased(float delta)`)를 그때 추가하기로 함.
- 구독 배선(`AddOnAttributeChangedCallback(combat.NotifyAttributeChanged)`)은 계속 팀원 담당. `CharacterCombat`은 이 배선의 존재 여부와 무관하게 안전하도록만 만들었다.

## 현재 상태 및 이슈

- `Assets/_Project/Scripts/Combat/CharacterCombat.cs`의 `ReceiveHit` 3줄만 재배치 + 주석 1줄 추가. 다른 파일은 무수정.
- `docs/intent/clear/intent-004-combat-component-channel.md` 해결 기록에 이번 순서 보정 항목을 추가.
- **Unity Editor 컴파일/PlayMode 미확인 — 이 환경은 Unity 컴파일러를 실행할 수 없음.** 기존 `Assets/Test/Combat/CombatManualTest.cs`의 시나리오 2(치명타)로 `OnHit → OnDeath` 순서를 직접 확인해야 함(카운터만 보므로 순서 확인엔 임시 `Debug.Log`가 필요).
- 별건으로 발견했으나 이번에 안 고친 것: HP 클램프 부재(`SetPreAttributeChangedCallback` 미사용), `IsAlive` 프로퍼티가 잘못된 속성명에도 Assert 후 `0.0f`를 반환해 "죽은 것"처럼 보이는 문제, 팀원이 `AddOnAttributeChangedCallback`/`AddPostAttributeChangedCallback` 중 무엇을 쓸지에 따라 다른 구독자와의 순서가 달라지는 점(배선 담당자에게 공유 필요).

## 다음 할 일 (Next Steps)

1. Unity Editor에서 컴파일 확인 후 `CombatManualTest` 시나리오 1~5 재실행, 특히 시나리오 2에서 `OnHit`이 `OnDeath`보다 먼저 뜨는지 확인.
2. 팀원이 `AttributeSet` 옵서버 배선을 완료하면 시나리오 2를 다시 돌려 배선 후에도 순서·중복이 유지되는지 재검증.
3. 독/낙하 대미지 등 비-피격 피격 연출이 필요해지는 시점에 `OnHealthDecreased(float delta)` 같은 별도 이벤트 추가 여부를 다시 논의.
