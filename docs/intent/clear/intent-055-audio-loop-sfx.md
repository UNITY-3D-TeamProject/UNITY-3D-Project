---
id: intent-055
title: AudioManager 효과음 반복 재생·정지 (차지 공격용)
part: Audio / Core
status: resolved   # open | resolved
created: 2026-10-21
resolved: 2026-10-21
---

## 문제 (Problem)
`AudioManager`의 효과음 재생은 한 번 재생(`PlaySfx`, `PlayUi`)만 있다. 재생한 소리를 가리킬 수단이 없어서, 차지 공격처럼 "차지하는 동안 반복되다가 쏘는 순간 멈춰야 하는" 효과음을 낼 수 없다.
차지 공격 담당(세훈)이 이 기능을 요청했다.

## 기대 결과 (Proposed outcome)
- 효과음을 반복 재생하는 함수와, 그 소리를 멈추는 함수가 생긴다.
- 차지 시작 때 반복 재생을 시작하고, 발사·취소 때 정지하면 소리가 끊긴다.
- 기존 `PlaySfx`·`PlayUi`·BGM 동작은 바뀌지 않는다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 차지 공격 담당(세훈). 이후 반복음이 필요한 다른 기능.
- 어떤 시스템/모듈이 건드려지는가: `Assets/_Project/Scripts/Core/Audio/AudioManager.cs` (필요하면 핸들 구조체 파일 1개 추가)
- 건드리지 않는 것: `SOAudioData`, 믹서·프리팹, 다른 사람 스크립트(차지 공격 코드 연결은 세훈이 한다)

## 제약 (Constraints)
- 지켜야 할 정책: `docs/coding-convention.md`, 기존 원샷 재생 동작 유지, intent-038 구조(씬 단위 매니저, 3D 소스 고정 배열, `AudioListener.pause` 일시정지) 유지.
- 쓰지 말아야 할 것: 인터페이스 분리, 이벤트 버스, 소스 우선순위 교체 등 요청 밖 기능.
- 검증: 컴파일 0에러, 개인 TestScene에서만 Play 확인 (다른 씬은 열지 않음).

## 결정 사항 (2026-10-21, 사용자 확인)
1. **반환**: ~~핸들 구조체 `SSfxHandle`~~ → (같은 날 사용자 변경) **int 번호**를 돌려준다. 별도 파일 없이 AudioManager 한 파일로 끝내기 위해서다. 0은 "재생 안 됨". 호출마다 번호가 따로 나오므로 같은 효과음을 여러 개 동시에 반복할 수 있고, 이미 끊은 번호·끝난 소리의 번호로는 그 소스를 다시 쓰는 다른 소리를 끊지 않는다.
   - (같은 날 사용자 추가) 원샷 `PlaySfx`도 번호를 돌려주고, 정지 함수를 `StopSfx` 하나로 합쳐 원샷·반복음 모두 끊는다. `PlayUi`는 스피커 하나에 겹쳐 내는 구조라 범위 밖.
2. **위치**: Transform을 넘기면 매니저가 매 프레임(LateUpdate) 위치를 맞춘다. 대상이 파괴되면 마지막 위치에서 계속 나므로 정지는 부르는 쪽이 책임진다.
3. **정지**: 기본은 바로 끊는다. 정지 함수에 페이드 시간(초) 인자를 주면 그 시간 동안 줄인 뒤 끊는다.
4. **풀·차단**: 기존 3D 소스 16개를 같이 쓴다 (정지할 때까지 그 소스를 점유, 모두 사용 중이면 빈 핸들). 같은 클립 50ms 차단은 반복음에 적용하지 않는다.

## 열린 질문 (Open questions)
- 없음.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정 (모두 `AudioManager.cs` 안, 새 파일 없음):
  - `PlaySfx(...)`: `void` → `int` 번호 반환. 재생 동작은 그대로이고, 생략(클립 없음·50ms 차단·소스 부족)되면 0. 기존 호출부 0개라 깨지는 곳 없음.
  - `PlaySfxLoop(SOAudioData, Transform target, float volumeScale = 1, float pitchScale = 1)` → `int`. 빈 3D 소스를 `loop = true`로 틀고 끝나는 시각을 `float.MaxValue`로 적어 단발음이 가져가지 못하게 한다. `LateUpdate`가 대상 위치를 따라간다.
  - `StopSfx(int sfxId, float fadeDuration = 0)`: 원샷·반복음 공용. 0이면 바로 끊고, 양수면 `Time.deltaTime`으로 줄인 뒤 끊는다(일시정지 중엔 페이드도 멈춤). 번호는 부르는 즉시 끝나고, 소스는 페이드가 끝날 때까지 점유한다. 이미 다 재생된 원샷 번호는 무시.
  - 소스별 번호 `_sfxPlayIds`, 따라갈 대상 `_sfxFollowTargets`, private `ReleaseSfx`, 코루틴 `CoFadeOutSfx`.
  - `PlayUi`·BGM 코드는 바꾸지 않았다.
  - 확인 (컴파일 0에러, TestScene Play에서 AudioManager 프리팹을 Play 중에만 임시 생성, 씬 저장 안 함):
    - 핸들 구조체 방식일 때: 같은 클립 반복 2개 동시 재생, 단발음이 반복 소스를 가져가지 않음, 대상 이동 따라가기, 즉시 정지, 재정지 무시, 페이드 후 반납, 대상 파괴 시 에러 없음, target null 경고.
    - int 번호로 바꾼 뒤: 37초 원샷 즉시 끊기, 원샷 3초 페이드 끊기 후 반납, 반복음 끊기(loop 해제), 0·같은 번호 재정지 무시, 끝난 원샷의 옛 번호로 그 소스를 다시 쓰는 새 소리를 끊지 않음, 50ms 차단 시 0 반환.
    - 실제 소리는 듣지 않았다. Unity 창이 포커스를 잃으면(백그라운드 실행 꺼짐) Play 프레임이 멈춰 확인 중에 `Application.runInBackground`를 Play 동안만 켰다(PlayerSettings 변경 없음).
- 관련 커밋/PR: (커밋 전)
