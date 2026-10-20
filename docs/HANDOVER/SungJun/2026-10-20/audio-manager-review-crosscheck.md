# AudioManager 검토 결과 교차 확인 (Codex 검토 재확인)

## 작업 요약 및 방법
- Codex 기록 `Codex/2026-10-20/audio-manager-logic-review.md`의 이슈 3개를 `AudioManager.cs`, `SOAudioData.cs`, intent-038, Audio SO 6개, BGM 클립 길이로 다시 확인했다.
- 코드를 읽어 판단했다. Play 재현은 하지 않았고, 코드와 에셋은 수정하지 않았다.

## 결정 사항 (판정)
1. **SFX 실패 요청이 중복 차단 시각에 기록됨 (190~195, 300행)** — 맞다. 단, 3D 소스 16개가 모두 재생 중일 때 요청이 버려지고, 50ms 안에 슬롯이 비고, 같은 클립을 다시 요청해야만 한 번 빠진다. 영향이 작아 P3. 현재 `PlaySfx` 호출처는 0개다.
2. **UI에도 50ms 차단 적용 (173행)** — 버그 아님. intent-038 39행이 채널 구분 없이 "같은 클립 재요청 차단"을 정의했고, 295행 주석도 UI를 일부러 거른다고 적혀 있다. 103행의 "UI 연타 제한"은 별도의 클릭 쿨다운 기능을 1단계에서 만들지 말라는 뜻이다. 지금은 UI와 SFX가 같은 클립을 공유하는 SO도 없다.
3. **짧은 BGM + 긴 페이드인 무음 (409~417행)** — 원인 분석은 맞지만 Codex의 재현 설명은 과장됐다. 다음 곡이 영영 시작되지 않는 것이 아니라 `FadeIn − 클립 길이`만큼 무음이 이어진 뒤 넘어간다. 0.23초 시점 관찰은 0.6초 페이드인이 아직 끝나지 않은 상태다.
   - 같은 계열로 Codex가 놓친 경우: 클립 길이 < FadeIn + FadeOut이면 페이드아웃 도중 곡이 끝나고 남은 페이드아웃 시간만큼 무음이 생긴다.
   - 현재 에셋: `SOAudioData_BgmLobby`(Background·loop_energy 3.06초·loop_lightning 2.1초, 페이드 1/1, 피치 1)는 2.1 > 2.0이라 겨우 걸리지 않는다. 이 SO는 어떤 씬·프리팹에도 연결되어 있지 않다. P3.

## 현재 상태 및 이슈
- 세 이슈 모두 지금 실사용 경로에는 영향이 없다. 수정하지 않았다.

## 다음 할 일
- 수정 요청 시: #1은 `FindFreeWorldSource` 성공 뒤에 시각을 기록. #3은 페이드인·페이드아웃 시간을 남은 재생 시간으로 제한.
- #2는 설계를 바꿀 때만 다룬다 (현재 intent와 일치).

## 추가 작업: XML 문서 주석 정리
- 사용자 요청으로 `AudioManager.cs`의 `<summary>`를 `//` 주석으로 바꾸고 설명은 유지, `<param>`은 삭제했다 (UIManager 35b2ddbc와 같은 방식). 동작 변경 없음.
- 컴파일 상태: `compilationFailed=false`. 커밋하지 않았다.
- `SOAudioData.cs`에도 `<summary>`·`<returns>`가 남아 있다 (요청 범위 밖이라 그대로 둠).
- `CreateSources` → `SetupSources` 이름 변경 (스피커 1개를 만드는 `CreateSource`와 헷갈려서). private이라 다른 파일 영향 없음. 컴파일 0에러.
- 3D 효과음 이름에서 world → sfx: `_sfxSources`·`_sfxEndTimes`·`_sfxSourceCount`(`[FormerlySerializedAs("_worldSourceCount")]`로 프리팹 값 16 유지 확인)·`FindFreeSfxSource`·오브젝트 이름 `SfxSource_00~15`. 컴파일 0에러. 프리팹 파일은 다시 저장하지 않아 YAML 키는 아직 `_worldSourceCount`.
- `_currentBgm`(SO 또는 null) → `bool _isBgmPlaying`. StartBgm에서 true, 로딩씬 Additive 감지 시 false. `SkipBgmTrack`은 `_isBgmPlaying`·`_bgm.ClipCount`로 판단하고 `_bgm`을 넘긴다. 동작 동일, 컴파일 0에러.
- `SkipBgmTrack` → `PlayNextBgm`, `CoSkipBgmTrack` → `CoPlayNextBgm` (맨 위 사용 흐름 주석 포함). 호출처 없음, 컴파일 0에러.
- `SOAudioData data` 매개변수 8곳(PlayUi·PlaySfx·TryPickClip·PlayBgmTrack·PickNextTrackIndex·StartBgm·CoPlayNextBgm·CoPlayTracks)을 `audioData`로 변경. 컴파일 0에러.
