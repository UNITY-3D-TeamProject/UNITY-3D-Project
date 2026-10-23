# AudioManager 효과음 핸들(SAudioHandle) 도입 — 2026-10-23

관련 intent: [intent-070](../../../intent/clear/intent-070-audio-sfx-handle.md)

## 1. 작업 요약 및 방법
- `EffectManager`가 쓰는 `SEffectHandle`과 같은 모양으로 `Assets/_Project/Scripts/Core/Audio/SAudioHandle.cs` 추가 (`Id`, `IsValid`, internal 생성자·`IsOwnedBy`).
- `AudioManager.cs`
  - `_handleOwner` 토큰 필드 추가.
  - `PlaySfx`·`PlaySfxLoop` 반환을 `int` → `SAudioHandle`, 실패 시 `0` → `default`.
  - `StopSfx(int sfxId, ...)` → `StopSfx(SAudioHandle handle, float fadeDuration = 0f)`. `!IsValid`이거나 이 매니저가 발급하지 않은 핸들이면 무시.
  - 상단 사용 흐름 주석에 핸들 보관·Stop 예시 추가.
- 내부 `_sfxPlayIds`(int 배열)·소스 고르기·페이드·중복 차단 로직은 그대로.
- `AudioManager` 밖에서 `PlaySfx`·`StopSfx`를 부르는 코드는 없어 호출부 수정 없음.

## 2. 결정 사항
- 오디오와 이펙트 핸들은 공용 타입으로 합치지 않고 따로 둔다. 서로 바꿔 넘기면 컴파일 에러가 나게 하는 것이 목적.
- 사용 예:
  ```csharp
  _chargeHandle = AudioManager.Instance.PlaySfxLoop(_chargeSound, transform);
  AudioManager.Instance?.StopSfx(_chargeHandle); _chargeHandle = default;
  ```

## 3. 현재 상태 및 이슈
- 컴파일 0에러.
- 에디터 eval로 임시 매니저 2개(A·B)를 만들어 확인: 둘 다 번호 1 발급, A에 B 핸들·`default`를 넘기면 무시, 자기 핸들은 끊음. 확인 후 임시 오브젝트 삭제. (같은 씬에 매니저 2개라 Awake의 "두 개 있음" 에러 로그는 시험 때문에 난 것)
- Play 모드·실제 청취 확인 없음. 커밋 안 함.
- `EffectManager`와 달리 `_nextSfxId`가 int 끝에 닿는 경우는 막지 않음(기존 그대로).

## 4. 다음 할 일 (Next Steps)
- 효과음을 끊는 기능(차지음 등)을 만들 때 `SAudioHandle` 필드로 보관하고 OnDisable에서 `StopSfx` 후 `default`로 비운다.
