---
id: intent-004
title: 전투(Combat) 컴포넌트 통신 채널 부재
part: Combat
status: resolved
created: 2026-09-20
resolved: 2026-09-20
---

## 문제 (Problem)
`feature/Combat` 브랜치에 전투 관련 코드가 전혀 없었다. `docs/character-architecture.md`가 정의하는 중재자 중심 구조에서 전투 컴포넌트의 역할은 "사망·피격 체크"인데, 다른 컴포넌트(공격자, 중재자)가 전투 컴포넌트와 통신할 방법 자체가 없었다.

## 기대 결과 (Proposed outcome)
전투 컴포넌트가 (a) 공격자로부터 피격을 전달받는 입구, (b) 피격 이외 경로(중재자 relay 등)로 속성 변경을 통지받는 입구, (c) 사망·피격 판정 결과를 구독자(중재자, BT 등)에게 알리는 출구를 갖춘다. 값 변경 로직이나 중재자 본체, 스킬/공격 시스템은 포함하지 않는다 — 통로만 뚫는다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 향후 구현될 스킬 매니저(공격자 쪽), 중재자, BT Condition
- 어떤 시스템/모듈이 건드려지는가: `Assets/_Project/Scripts/Combat/` 신규 (`IHitReceiver`, `SHitInfo`, `CharacterCombat`). 기존 Attribute 시스템은 무수정.

## 제약 (Constraints)
- `SOAttributeData`/`AttributeSet` 같은 구체 저장 타입을 직접 참조하지 않는다 — 속성 접근은 `IEffectTarget`으로만 한다.
- 중재자 본체, 스킬 매니저, 공격 발신 로직은 범위 밖.
- `.asset`/`.prefab`/`.unity` 편집 금지 — 코드만 작성.

## 열린 질문 (Open questions)
없음 — 이번 범위는 "채널만 뚫기"로 명확했고, 구현 중 발생한 설계 질문(피격이 캐릭터 간 접촉점인지, `ReceiveHit`이 인터페이스인 이유 등)은 전부 대화 중 확인하며 해결했다.

## 해결 기록 (Resolution)
- 최종 결정:
  - `IHitReceiver.ReceiveHit(SHitInfo)` — 캐릭터 간 접촉점. 공격자가 대상의 중재자를 거치지 않고 직접 호출한다 (`character-architecture.md:32`의 직접 참조 금지 규칙은 캐릭터 **내부** 컴포넌트 간 규칙이라 적용 대상이 아님).
  - `SHitInfo` — `SOAttributeEffect`(값 변경 레시피) + `IEffectTarget`(공격자)를 담는 `readonly struct`. `SOAttributeEffect.Apply(target, cursor)` 시그니처를 그대로 옮겨 담은 값 객체.
  - `CharacterCombat.NotifyAttributeChanged(string, float, float)` — 피격 이외 경로(독, 낙하 대미지 등)의 HP 변화를 위한 두 번째 입구. `AttributeSet.OnAttributeChange`와 시그니처만 맞춘 것으로 `AttributeSet` 타입 자체는 참조하지 않는다(C# 델리게이트의 구조적 타이핑 이용).
  - `_isDead` 플래그로 두 입구가 동시에 사망을 감지해도 `OnDeath`가 중복 발사되지 않게 막는다.
  - `OnHit`/`OnDeath`는 순수 C# `event`로 방출 — 중재자 구현 형태(`character-architecture.md` §8 열린 질문 2, 아직 미정)를 몰라도 동작한다.
  - (2026-09-20 추가) `ReceiveHit` 내부 순서를 `OnHit 발사 → Effect.Apply → CheckDeath`로 조정했다. 팀원이 `AttributeSet.AddOnAttributeChangedCallback`으로 `NotifyAttributeChanged`를 옵서버 배선하면, `Apply` 호출 중에 `NotifyAttributeChanged → CheckDeath → OnDeath`가 끼어들 수 있다. `OnHit`을 `Apply`보다 먼저 쏘지 않으면 치명타 시 `OnDeath`가 `OnHit`보다 먼저 발사되는 순서 역전이 생긴다. 이 조정으로 배선 전/후 모두 `OnHit → OnDeath` 순서가 보장된다. 5-b(비-피격 경로의 "피격 체크")는 `SHitInfo`를 만들 수 없어 이번 범위에서 제외.
- 관련 커밋/PR: (구현 커밋에서 채워짐)
- (2026-09-21 추가) 이 문서의 설계는 [intent-005](../intent-005-combat-component-decoupling.md)로 대체됨. "기존 Attribute 코드를 건드리지 않는다"를 우선한 결과 Combat이 Attribute/Effect 타입에 묶여 이식성이 깨진 것이 원인 — `SHitInfo`/`CharacterCombat`을 Unity 기본 타입만으로 재작성하고, AttributeSet 연동은 별도 `AttributeToCombatObserver`(Observer)로 분리했다.
- (2026-09-21 추가) 위 결정 항목 중 "공격자가 대상의 중재자를 거치지 않고 직접 호출한다"는 **파기됨**. 당시엔 캐릭터당 중재자가 1개뿐이라 예외를 뒀지만, 이후 메인+서브 중재자 구조로 바뀌어 예외 명분이 사라졌다. 지금은 피격자의 전투 서브 중재자가 `ReceiveHit`을 호출한다 — 근거와 상세는 [intent-005](../intent-005-combat-component-decoupling.md)의 "intent-004 해석 파기" 절 참고.
