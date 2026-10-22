# AudioManager 효과음 반복 재생·끊기 (intent-055)

## 작업 요약 및 방법
- 차지 공격 담당(세훈)의 요청: 차지하는 동안 반복되다 발사 순간 멈추는 효과음.
- 열린 질문 4개를 사용자에게 확인한 뒤 구현했다 → [intent-055](../../../intent/clear/intent-055-audio-loop-sfx.md)
- 처음에는 핸들 구조체 `SSfxHandle`(새 파일)로 만들었다. 사용자 요청으로 **int 번호**로 바꾸고, 원샷도 끊을 수 있게 정지 함수를 `StopSfx` 하나로 합쳤다. `SSfxHandle.cs`는 삭제했다(커밋 전이라 흔적 없음).
- `AudioManager.cs`만 수정:
  - `PlaySfx(...)`: `void` → `int` 번호 반환 (재생 동작 그대로, 생략되면 0)
  - `PlaySfxLoop(SOAudioData, Transform target, volumeScale = 1, pitchScale = 1)` → `int`
  - `StopSfx(int sfxId, fadeDuration = 0)` — 원샷·반복음 공용
  - 필드 `_sfxPlayIds`·`_nextSfxId`·`_sfxFollowTargets`, `LateUpdate`(대상 따라가기), private `ReleaseSfx`, 코루틴 `CoFadeOutSfx`

## 결정 사항
- 번호(int)로 돌려준다. 0 = 재생 안 됨. 같은 효과음도 여러 개 동시에 반복·개별로 끊을 수 있다.
- 끝난 원샷의 옛 번호로 끊어도, 그 스피커를 다시 쓰는 새 소리는 끊기지 않는다 (소스마다 마지막 번호를 적어 두고 비교).
- 반복음은 Transform을 따라간다. 대상이 파괴되면 마지막 위치에서 계속 나므로 정지는 부르는 쪽 책임.
- 기본은 바로 끊고, `fadeDuration`을 주면 그만큼 줄인 뒤 끊는다. 페이드는 `Time.deltaTime`(일시정지 중엔 멈춤).
- 기존 3D 소스 16개를 같이 쓴다. 반복 중·페이드 중에는 끝나는 시각을 `float.MaxValue`로 적어 단발음이 못 가져간다. 모두 사용 중이면 0.
- 같은 클립 50ms 차단: 원샷은 기존대로 적용(차단되면 0), 반복음은 적용하지 않는다.
- `PlayUi`는 스피커 하나에 겹쳐 내는 구조라 끊기 범위에서 뺐다.

## 현재 상태 및 이슈
- 컴파일 0에러. 다른 스크립트의 `PlaySfx`·`PlayUi` 호출부는 0개라 부르는 방식이 바뀌는 곳이 없다.
- TestScene Play에서 AudioManager 프리팹을 임시로 생성해 확인(씬 저장 안 함): 원샷 즉시 끊기·페이드 끊기 후 반납, 반복음 동시 2개·따라가기·끊기, 0·재정지 무시, 옛 번호가 새 소리를 끊지 않음, 대상 파괴 시 에러 없음.
- 실제 소리는 듣지 않았다.
- Unity 창이 포커스를 잃으면 Play 프레임이 멈춘다(백그라운드 실행 꺼짐). 확인 중에 `Application.runInBackground`를 Play 동안만 켰다가 껐다. PlayerSettings는 바뀌지 않았다.
- TestScene에는 AudioManager가 배치되어 있지 않다. 맵 씬도 아직 대부분 매니저가 없어(intent-038) 그 씬에서는 `Instance`가 null → 호출부는 `?.`로 부른다.
- 커밋하지 않았다. `ProjectSettings/EditorBuildSettings.asset`·`ShaderGraphSettings.asset` 변경은 Unity가 만든 것(줄 끝 문자 등)이라 이번 작업과 무관.

## 다음 할 일 (Next Steps)
- 세훈이 차지 공격 코드에 연결: 차지 시작 `PlaySfxLoop`, 발사·취소·OnDisable에서 `StopSfx`.
- 실제 차지 효과음 SO를 만들고 맵 씬에 AudioManager를 배치한 뒤 청취 확인.
