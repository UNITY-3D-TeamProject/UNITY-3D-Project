# 로비 BGM Digital City Morning 연결

## 작업 요약 및 방법
- 제공 WAV를 `Assets/ThirdParty/Audio/Digital City Morning.wav`로 복사하고 Streaming AudioImporter 메타를 추가했다.
- `SOAudioData_BgmLobby`의 기존 3곡을 새 단일 클립으로 교체하고 `Map_1_Lobby`의 AudioManager `_bgm`에 이 SO를 연결했다.

## 결정 사항
- 로비 입장 시 AudioManager가 이 곡을 자동 재생하고 단일 클립 반복 설정을 사용한다.
- `Assets/ThirdParty`는 사용자 요청 위치이며 Git에서 제외된다. WAV와 메타는 별도로 공유해야 한다.

## 현재 상태 및 이슈
- 원본 WAV와 복사본 SHA-256 동일. Unity가 AudioClip 길이 126.8초로 임포트했고 에디터에서 로비 매니저가 `SOAudioData_BgmLobby`의 `Digital City Morning` 1곡을 참조하는 것 확인.
- 로비 Play 프레임 진행 확인 후 BGM 소스 `playing=True`, `clip=Digital City Morning`, `loop=True`, `volume=0.60` 확인. Play 종료 후 기존 MainScene과 `runInBackground=false` 복원.
- Play 중 발생한 전광판 RenderTexture 4개의 자동 변경은 복원했다. 작업 시작 전의 다른 변경 사항은 그대로 두었다.
- 직접 귀로 듣는 음량 평가는 하지 않았다.

## 다음 할 일
- 실제 청취로 음량과 반복 경계를 확인한다.
