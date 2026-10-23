---
id: intent-070
title: AudioManager 효과음 번호를 SAudioHandle로 바꾸기
part: Audio / Core
status: resolved   # open | resolved
created: 2026-10-23
resolved: 2026-10-23 # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
`AudioManager.PlaySfx`·`PlaySfxLoop`가 int 번호를 돌려주고 `StopSfx(int)`가 그 번호로 소리를 끊는다.
- 씬마다 매니저가 새로 생기고 번호가 1부터 다시 시작하므로, 이전 씬에서 받은 번호를 새 씬 매니저에 넘기면 상관없는 소리를 끊을 수 있다.
- 그냥 int라서 다른 숫자(이펙트 번호 등)를 실수로 넘겨도 컴파일 단계에서 막지 못한다.
- `EffectManager`는 이미 `SEffectHandle`(번호 + 매니저 식별표)로 같은 문제를 막고 있어 두 매니저의 사용법이 다르다.

## 기대 결과 (Proposed outcome)
- `Core.Audio.SAudioHandle`(번호 + 매니저 식별표, `SEffectHandle`과 같은 모양)을 추가한다.
- `PlaySfx`·`PlaySfxLoop`는 `SAudioHandle`을 돌려주고(생략되면 `default`), `StopSfx(SAudioHandle, fadeDuration = 0)`는 자기가 발급한 핸들만 처리한다.
- 사용 예: `_chargeHandle = AudioManager.Instance.PlaySfxLoop(_chargeSound, transform);` → `AudioManager.Instance?.StopSfx(_chargeHandle); _chargeHandle = default;`

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 효과음을 중간에 끊어야 하는 기능(차지음 등). 지금은 `AudioManager` 밖에서 `PlaySfx`·`StopSfx`를 부르는 코드가 없다.
- 어떤 시스템/모듈이 건드려지는가: `Scripts/Core/Audio/AudioManager.cs`, 새 파일 `Scripts/Core/Audio/SAudioHandle.cs`

## 제약 (Constraints)
- 지켜야 할 정책: 재생·끊기 동작(소스 고르기, 페이드, 50ms 중복 차단)은 바꾸지 않고 반환·인자 타입과 소유 검사만 바꾼다.
- 쓰지 말아야 할 것: `SEffectHandle`을 오디오에 같이 쓰거나 공용 핸들로 합치지 않는다. (서로 바꿔 넘기는 실수를 막는 것이 목적)

## 열린 질문 (Open questions)
- 없음

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정: `Core.Audio.SAudioHandle`(Id·IsValid, 매니저 토큰 `_owner`) 추가. `AudioManager`에 `_handleOwner` 토큰을 두고 `PlaySfx`·`PlaySfxLoop`는 `SAudioHandle`(생략 시 `default`) 반환, `StopSfx(SAudioHandle, fadeDuration = 0)`는 `default`·다른 매니저 핸들을 무시. 내부 `_sfxPlayIds`(int)·소스 고르기·페이드는 그대로.
- 확인: 컴파일 0에러. 에디터 eval로 임시 매니저 2개를 만들어 둘 다 번호 1을 발급해도 A가 B의 핸들·`default`는 무시하고 자기 핸들로만 끊는 것 확인(청취·Play 모드 확인 없음).
- 관련 커밋/PR: 커밋 안 함
