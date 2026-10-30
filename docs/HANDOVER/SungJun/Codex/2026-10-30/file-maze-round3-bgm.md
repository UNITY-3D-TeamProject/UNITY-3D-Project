# 파일 미로 3라운드 BGM 연결

## 작업 요약 및 방법
- `The Labyrinth of Forgotten.wav`와 AudioImporter 메타를 `Assets/ThirdParty/Audio/Bgm`에 추가했다.
- 단일 클립 BGM SO `SOAudioData_BgmFileRound3`를 만들었다.
- `Map_3_File`의 `FileApp._round3Bgm`에 SO를 연결하고, `FileApp.OnRoundChanged`에서 3라운드 진입 시 BGM을 교체한다.

## 결정 사항
- 1라운드는 씬 `AudioManager`의 `SOAudioData_BgmFileRound1`, 2라운드는 `FileApp._round2Bgm`, 3라운드는 `FileApp._round3Bgm`을 사용한다.

## 현재 상태 및 이슈
- 사용자 요청에 따라 Unity 재생·컴파일 검증은 수행하지 않았다.
- `Assets/ThirdParty`는 Git에서 제외되므로 WAV와 메타는 별도로 공유해야 한다.

## 다음 할 일
- 필요할 때 3라운드 진입 시 곡 전환과 음량을 확인한다.
