# 파일 맵 미로 2라운드 BGM 교체

## 작업 요약 및 방법
- 사용자 제공 `Maze Hopper.wav`를 `Assets/ThirdParty/Audio/Bgm/`에 추가했다.
- `SOAudioData_BgmFileRound2.asset`의 단일 클립을 기존 `The Labyrinth of Forgotten.wav`에서 `Maze Hopper.wav`로 교체했다. 기존 음량·페이드와 `FileApp`의 라운드 전환 로직은 유지했다.
- 새 클립을 기존 2라운드 BGM과 같은 Streaming/Vorbis/quality 1로 임포트했다.

## 결정 사항
- 2라운드 SO의 참조만 바꾸며, 사용자 작업이 있는 `Map_3_File.unity` 씬은 수정하지 않는다.

## 현재 상태 및 이슈
- Unity 에디터에서 SO가 `Assets/ThirdParty/Audio/Bgm/Maze Hopper.wav`를 참조하고 Streaming 임포트인 것을 확인했다. 실제 청취는 하지 않았다.
- `Assets/ThirdParty/`는 기본적으로 Git에서 제외되지만, 이 WAV와 `.meta`는 `ca13720a`에 명시적으로 포함했다.

## 다음 할 일
- 파일 맵 2라운드 진입 시 곡 전환과 음량을 직접 청취한다.
