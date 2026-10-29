# 메인 메뉴 AnyPress 이후 BGM 재생

## 작업 요약 및 방법
- 사용자 WAV `Whimsical Technology World.wav`가 `Assets/ThirdParty/Audio/`에 있으며 Streaming AudioImporter 메타의 GUID를 확인했다. 최초 `_Project/Audio/Bgm/` 복사본은 정리됐다.
- 기존 `SOAudioData_BgmMainMenu`의 클립을 이 WAV로 교체했다. `MainScene`의 AudioManager는 해당 SO를 그대로 참조한다.
- AudioManager에 `_playBgmOnStart`와 `StartSceneBgm()`을 추가했다. 프리팹 기본값은 true, MainScene만 false로 설정했다.
- 검은 스플래시의 AnyPress 입력을 받는 `MainMenuPresenter`가 메뉴를 표시한 직후 `StartSceneBgm()`을 호출한다. 기존 UI 클릭 효과음 호출은 유지했다.

## 결정 사항
- 메인 메뉴 음악은 BGM 채널로 반복 재생하며, 검은 화면에서 첫 입력이 들어오기 전에는 시작하지 않는다.
- 다른 씬은 기존처럼 Start에서 BGM을 자동 재생한다.

## 현재 상태 및 이슈
- 원본과 현재 ThirdParty 파일의 SHA-256 동일, SO의 WAV GUID 참조·씬 오버라이드·프리팹 기본값·AnyPress 호출·`git diff --check` 확인 완료.
- `Assets/ThirdParty/`는 `.gitignore` 대상이므로 이 WAV와 메타는 Git으로 공유되지 않는다.
- Unity Pipeline 연결 후 컴파일은 `up_to_date`, WAV는 Unity `AudioClip`으로 임포트되어 길이 62.76초로 확인했다. MainScene Play 프레임 진행 확인 후 AnyPress 전 BGM `playing=False, clip=null`, Space 입력 뒤 `playing=True, clip=Whimsical Technology World, volume=0.60` 확인. Play 종료, 기존 TestScene과 `runInBackground=false` 복원. 직접 귀로 듣는 음량 평가는 하지 않았다. CLI 1.0.0-beta.14 업데이트가 있으나 승인 없이 실행하지 않았다.
- 작업 시작 전 존재한 다른 파일 변경은 건드리지 않았다.

## 다음 할 일
- 실제 귀로 음량과 약 62.76초 뒤 반복 경계를 확인한다.
