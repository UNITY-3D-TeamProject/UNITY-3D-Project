# 파일 미로 2·3라운드 BGM 곡 교체

## 작업 요약 및 방법
- `SOAudioData_BgmFileRound2`의 클립을 `The Curious Maze`에서 `The Labyrinth of Forgotten`으로 바꿨다.
- `Maze Mayhem.wav`와 AudioImporter 메타를 `Assets/ThirdParty/Audio/Bgm`에 추가하고, `SOAudioData_BgmFileRound3`의 클립으로 연결했다.

## 결정 사항
- 1라운드 BGM과 `FileApp`의 라운드별 SO 참조 및 전환 코드는 유지했다.
- 기존 `The Curious Maze.wav`는 삭제하지 않았다.

## 현재 상태 및 이슈
- 사용자 요청에 따라 Unity 재생·컴파일 검증은 수행하지 않았다.
- `Assets/ThirdParty`는 Git에서 제외되므로 `Maze Mayhem.wav`와 메타는 별도로 공유해야 한다.

## 다음 할 일
- 필요할 때 2·3라운드 진입 시 곡 전환을 확인한다.
