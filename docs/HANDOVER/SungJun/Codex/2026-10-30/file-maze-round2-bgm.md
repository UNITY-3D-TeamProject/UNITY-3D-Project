# 파일 미로 2라운드 BGM 연결

## 작업 요약 및 방법
- 기존 `Algorithm Arpeggio` BGM SO를 GUID 유지한 채 `SOAudioData_BgmFileRound1`로 이름 변경했다.
- `The Curious Maze.wav`와 메타를 `Assets/ThirdParty/Audio/Bgm`에 추가하고, `SOAudioData_BgmFileRound2`를 만들었다.
- 파일 씬 `AudioManager._bgm`은 기존 1라운드 SO GUID를 그대로 참조한다.
- `FileApp.OnRoundChanged`에서 2라운드 진입 시 새 SO로 BGM을 전환하도록 연결했다.

## 결정 사항
- 3라운드 전용 BGM은 지정하지 않았으므로 2라운드에서 바뀐 곡이 계속 재생된다.

## 현재 상태 및 이슈
- 사용자 요청에 따라 Unity 재생·컴파일 검증은 수행하지 않았다.
- `Assets/ThirdParty`는 Git에서 제외되므로 WAV와 메타를 별도로 공유해야 한다.

## 다음 할 일
- 필요할 때 미로 1·2라운드 전환과 음량을 확인한다.
