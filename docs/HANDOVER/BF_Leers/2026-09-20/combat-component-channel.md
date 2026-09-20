# 전투(Combat) 컴포넌트 — 통신 채널 뚫기

## 작업 요약 및 방법

`feature/Combat` 브랜치에 전투 관련 코드가 전무한 상태에서, 손글씨 설계도와 `docs/character-architecture.md`의 중재자 중심 구조를 근거로 **전투 컴포넌트가 다른 컴포넌트와 통신할 수 있는 채널만** 뚫었다. 중재자 본체, 스킬/공격 로직, 값 변경 로직 자체는 범위 밖.

Plan Mode에서 사용자가 설계 근거를 하나하나 확인하는 긴 Q&A(피격이 왜 캐릭터 간 직접 접촉점인지, `SHitInfo`가 왜 struct인지, `Effect.Apply()`와 왜 중복이 아닌지, `IHitReceiver`가 왜 인터페이스인지, `NotifyAttributeChanged` 시그니처를 맞춘 게 왜 `AttributeSet` 참조가 아닌지 등)를 거친 뒤 승인받아 구현했다.

새로 만든 파일 (`Assets/_Project/Scripts/Combat/`, 네임스페이스 `Combat`):
- `IHitReceiver.cs` — 피격 수신 계약. 인터페이스인 이유: `CharacterCombat`이 `MonoBehaviour`를 상속해야 해서 추상 클래스로는 다중 상속 충돌이 난다.
- `SHitInfo.cs` — `readonly struct`. `SOAttributeEffect`(값 변경 레시피) + `IEffectTarget`(공격자)만 담는 값 객체. `SOAttributeEffect.Apply(target, cursor)` 시그니처를 그대로 옮겨 담은 모양.
- `CharacterCombat.cs` — `IHitReceiver` 구현체. 입구 2개(`ReceiveHit`, `NotifyAttributeChanged`) → 내부 `CheckDeath()` → 출구 2개(`event OnHit`, `event OnDeath`).

## 결정 사항

- **`IHitReceiver.ReceiveHit`은 캐릭터 간 접촉점 — 중재자를 거치지 않는다.** `character-architecture.md:32`의 "컴포넌트끼리 직접 참조 금지"는 **캐릭터 내부** 규칙("캐릭터 1개 = 중재자 1개")이라 캐릭터 A↔B 사이의 피격에는 적용되지 않는다. 공격자 쪽 코드가 대상의 `IHitReceiver`를 직접 호출하고, 대상 내부에서는 결과를 자신의 중재자에게만 이벤트로 알린다(캐릭터 내부 규칙은 여기서 지켜짐).
- **사망 판정 입구를 2개로 분리.** `ReceiveHit`(피격 경로)만으로는 독/낙하 대미지 같은 비-피격 HP 변화를 못 잡는다. `NotifyAttributeChanged(string, float, float)`를 별도로 열어 `_isDead` 플래그로 중복 `OnDeath` 발사를 막았다.
- **`NotifyAttributeChanged`는 `AttributeSet.OnAttributeChange`와 시그니처만 맞췄다 — 타입 참조는 아니다.** C# 델리게이트는 구조적 타이핑이라 메서드가 델리게이트 타입을 몰라도 모양만 맞으면 등록 가능하다. `CharacterCombat.cs` 코드 어디에도 `AttributeSet` 타입은 등장하지 않는다(주석 제외). 등록(`attributeSet.AddOnAttributeChangedCallback(combat.NotifyAttributeChanged)`)은 Combat 바깥 조립 코드의 책임.
- **이식성 기준**: 참조 가능 여부는 네임스페이스가 아니라 "그 의존성이 (컴포넌트+속성시스템+중재자) 묶음 밖을 가리키는가"로 판단. `IEffectTarget`/`SOAttributeEffect`는 묶음 안의 공개 계약이라 참조 가능, `AttributeSet`/`SOAttributeData`(구체 저장 타입)와 중재자 구체 타입은 금지.

## 현재 상태 및 이슈

- **Unity Editor 컴파일/PlayMode 미확인 — 이 환경은 Unity 컴파일러를 실행할 수 없다.** 다음 세션(또는 사용자) 최우선 확인 필요.
- `.meta` 파일은 이 세션에서 수동 생성(무작위 GUID, 기존 프로젝트의 최소 2줄 CRLF 포맷 그대로 따름). Unity가 열리면 정상 인식되는지 확인 필요.
- `docs/intent/intent-004-combat-component-channel.md`는 열린 질문 없이 즉시 `resolved`로 작성해 `docs/intent/clear/`에 바로 넣었고 `INTENT_POINTER.md`도 갱신함.
- `.asmdef` 분리는 하지 않았다 — 이 브랜치엔 asmdef가 하나도 없어(전부 `Assembly-CSharp`) 범위 밖으로 명시적으로 미룸.

## 다음 할 일 (Next Steps)

1. Unity Editor에서 컴파일 에러 없는지, `CharacterCombat`이 씬에 정상 부착되는지 확인.
2. `IEffectTarget`을 구현한 컴포넌트(예: `AttributeSet`)와 `SOAttributeEffect` 에셋을 실제로 붙여서 `ReceiveHit` 경로를 수동 테스트 — `.asset`/`.prefab` 편집은 `[USER]` 티켓(`CLAUDE.local.md` 규칙).
3. 중재자 본체 구현 시 `OnHit`/`OnDeath` 구독 및 `NotifyAttributeChanged` 등록(또는 `AttributeSet` 콜백 직결) 방식 결정 — `character-architecture.md` §8 열린 질문 2와 연동.
4. 스킬 매니저/공격 발신 로직 구현 시 `IHitReceiver.ReceiveHit` 호출 지점 연결.
5. (후속 과제, 이번엔 범위 밖) `Attribute`/`Movement`/`Combat`을 `.asmdef`로 분리해 이식성 규칙을 컴파일러가 강제하도록.
