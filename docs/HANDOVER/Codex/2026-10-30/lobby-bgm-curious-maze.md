# 로비 일반 BGM에 The Curious Maze 추가

## 작업 요약 및 방법
- 사용자 제공 `The Curious Maze.wav`를 `Assets/ThirdParty/Audio/Bgm/`에 추가했다.
- `SOAudioData_BgmLobby.asset`의 일반 재생 목록 끝에 새 클립을 연결했다.
- 기존 `Digital Integrity Check`는 파일맵 복귀 로비 전용 SO에만 남겨 두었다.

## 결정 사항
- 일반 로비 BGM은 4곡이며 무작위 재생은 꺼져 있어 등록 순서대로 재생한다.
- 파일맵 복귀 전용 곡을 포함하면 로비에서 쓰는 BGM은 총 5곡이다.
- 새 WAV는 기존 BGM과 같은 Streaming 임포트 설정을 쓴다.

## 현재 상태 및 이슈
- Unity AssetDatabase에서 4곡 이름, 별도 전용 1곡, Streaming 설정을 확인했다.
- `Assets/ThirdParty/`는 기본 Git 제외 경로이므로 WAV와 `.meta`를 이번 로비 BGM 커밋에 명시적으로 포함한다.
- 실제 게임에서 곡 전환을 귀로 듣는 확인은 남아 있다.

## 다음 할 일
- 로비 플레이리스트 재생을 청취한다.
