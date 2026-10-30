# 로비·피드 라운드 BGM 연결

## 작업 요약 및 방법
- `Whimsical City Adventure.wav`를 로비 BGM 플레이리스트의 세 번째 곡으로 추가했다.
- 피드 맵의 기존 `Social Media Jump.wav`는 씬 시작 BGM으로 유지했다.
- `Smartphone City Groove.wav` 전용 BGM SO를 만들고, `FeedRound2.Begin` 성공 시 `AudioManager.ChangeBgm`으로 전환하도록 연결했다.
- 두 WAV와 메타를 `Assets/ThirdParty/Audio/Bgm`에 배치했다.

## 결정 사항
- 2라운드 시작 전에 1라운드 곡이 자동으로 바뀌지 않도록 라운드 시작 시점에서 교체한다.
- 기존 BGM의 페이드아웃 후 새 곡을 페이드인한다.

## 현재 상태 및 이슈
- 사용자 요청에 따라 Unity 재생·컴파일 검증은 수행하지 않았다.
- `Assets/ThirdParty`는 Git에서 제외되므로 두 WAV와 메타는 별도로 공유해야 한다.

## 다음 할 일
- 필요할 때 피드 1·2라운드 전환과 음량을 Unity에서 확인한다.
