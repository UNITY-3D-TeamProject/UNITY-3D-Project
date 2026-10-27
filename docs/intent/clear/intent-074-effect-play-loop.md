---
id: intent-074
title: EffectManager 루프 재생을 PlayLoop로 분리
part: Core / Effect / VFX
status: resolved
created: 2026-10-24
resolved: 2026-10-24
---

## 문제 (Problem)
EffectManager는 `Play` 하나로 재생하고, 루프 여부를 SO의 `Is Loop`로 정한다. 그래서 루프 이펙트를 쓰려면 SO의 `Is Loop`와 프리팹 자식 파티클의 `Looping`을 둘 다 미리 맞춰야 한다.
AudioManager는 부르는 쪽이 `PlaySfx`/`PlaySfxLoop`로 정하므로 두 매니저의 사용법이 다르다.

## 기대 결과 (Proposed outcome)
- `EffectManager.PlayLoop(data, position, rotation, parent = null)`를 추가한다. Stop 전까지 반납하지 않는다.
- `PlayLoop`이면 자식 파티클 `Looping`을 모두 켜고, `Play`이면 프리팹의 원래 `Looping` 값으로 되돌린다. 같은 풀 인스턴스를 두 방식으로 번갈아 써도 맞게 재생된다.
- `SOEffectData`에서 `IsLoop`를 뺀다. SO에는 프리팹·`Duration`(Play용)·`StopFadeDuration`(PlayLoop용)·`MaxSize`만 남는다.
- `Stop`은 그대로 쓴다. Play로 낸 효과는 즉시, PlayLoop로 낸 효과는 방출을 멈추고 `StopFadeDuration` 뒤 반납한다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: VFX를 재생하는 기능 개발자. 현재 `_Project` 안 호출부는 없다.
- 어떤 시스템/모듈이 건드려지는가: `Assets/_Project/Scripts/Core/Effect/EffectManager.cs`, `SOEffectData.cs`.
- 개인 시험 코드(`Assets/Test`의 `TestEffectCaller`, EffectManagerValidation)의 루프 호출도 `PlayLoop`로 맞춘다. (Git 미포함)

## 제약 (Constraints)
- 핸들 타입(`SEffectHandle`)·풀 구조·반납 규칙은 바꾸지 않는다.
- SO 에셋의 런타임 상태는 바꾸지 않는다. 원래 `Looping` 값은 인스턴스를 만들 때 매니저가 기억한다.
- 알려진 한계: `PlayLoop`는 자식 파티클을 전부 루프로 만들므로, 루프 이펙트 안에 "시작 때 한 번만 터지는" 자식 파티클이 있으면 그것도 반복된다. (사용자 선택 A, 2026-10-24)

## 열린 질문 (Open questions)
- 없음. 2026-10-24 사용자가 A안(PlayLoop 추가, SO IsLoop 제거, 파티클 Looping 자동 설정)으로 결정.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: `Play`·`PlayLoop`가 private `PlayEffect(..., isLoop)`를 함께 쓴다. 인스턴스를 만들 때 자식 파티클의 프리팹 Loop 값을 `_prefabLoopsByInstance`에 기억하고, 재생할 때마다 `main.loop = isLoop || 원래 값`으로 정한다. 루프 여부는 `SActiveEffect.IsLoop`에 담아 `Stop`이 쓴다. `SOEffectData`의 `_isLoop`·`IsLoop` 삭제, Header를 `Play Only`/`PlayLoop Only`로 변경.
- 검증: Unity 재컴파일 failed=false / errors=[]. 미리보기 씬 eval로 Loop가 섞인 임시 프리팹(부모 off·자식 on)을 Play → `0,1`, Stop → 즉시 반납 / 같은 인스턴스를 PlayLoop → `1,1`, Stop → 페이드 대기·중복 Stop 무시 / 다시 Play → `0,1` 복구, 원본 프리팹 값 변경 없음 확인.
- 시험 코드(Git 미포함): `TestEffectCaller`의 ToggleAtPoint·ToggleOnPlayer, EffectManagerValidation의 루프 호출을 `PlayLoop`로, Builder의 `_isLoop` 설정 삭제.
- 미확인: TestScene Play에서 루프 발판 직접 확인, 시각 확인. 기존 SO 에셋의 `_isLoop` 직렬화 값은 남아 있으나 쓰이지 않는다(다음 저장 때 사라짐).
- 관련 커밋/PR: 없음 (커밋 안 함).
