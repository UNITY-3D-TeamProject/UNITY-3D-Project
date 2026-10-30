# 피드 맵 BGM Social Media Jump 연결

## 작업 요약 및 방법
- 제공 WAV를 `Assets/ThirdParty/Audio/Social Media Jump.wav`로 복사하고 Streaming AudioImporter 메타를 추가했다.
- 새 `SOAudioData_BgmFeed`를 단일 클립·음량 0.6·페이드 인/아웃 1초로 만들어 `Map_2_Feed`의 AudioManager `_bgm`에 연결했다.

## 결정 사항
- 피드 맵 입장 시 AudioManager가 이 곡을 자동 재생하며 단일 클립 반복 설정을 사용한다.
- `Assets/ThirdParty`는 사용자 지정 위치이고 Git 제외 대상이다. WAV와 메타는 별도로 공유해야 한다.

## 현재 상태 및 이슈
- 원본 WAV와 복사본 SHA-256 동일. Unity가 AudioClip 길이 72.2초로 임포트했고 새 SO의 유일한 클립이 `Social Media Jump`임을 확인했다.
- 피드 씬 AudioManager가 새 SO를 참조하는 것을 에디터에서 확인했다. Play 프레임 진행 후 BGM 소스 `playing=True`, `clip=Social Media Jump`, `loop=True`, `volume=0.60` 확인.
- Play 종료 후 기존 로비 씬과 `runInBackground=false` 복원. 검증 중 자동 변경된 전광판 RenderTexture 4개 복원. 시작 전의 다른 변경 사항은 그대로 두었다.
- 직접 귀로 듣는 음량 평가는 하지 않았다.

## 다음 할 일
- 실제 청취로 음량과 약 72.2초 뒤 반복 경계를 확인한다.
