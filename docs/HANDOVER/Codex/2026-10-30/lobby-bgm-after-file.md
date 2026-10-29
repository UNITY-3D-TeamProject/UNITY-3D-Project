# 파일맵 복귀 로비 전용 BGM

## 작업 요약 및 방법
- `Digital Integrity Check.wav`를 `Assets/ThirdParty/Audio/Bgm/`에 추가했다.
- 단일 곡 `SOAudioData_BgmLobbyAfterFile`을 만들고 공용 AudioManager 프리팹에 참조를 연결했다.
- AudioManager가 로비에서 시작할 때 `SceneLoader.PreviousScene == ESceneType.File`이면 전용 BGM을 선택한다.

## 결정 사항
- 기존 로비 플레이리스트 3곡은 그대로 유지한다.
- 씬 파일은 수정하지 않는다.
- 이 WAV와 메타 파일은 경로가 Git에서 기본 제외되므로 사용자 요청에 따른 커밋에서 파일만 명시적으로 추가한다.

## 현재 상태 및 이슈
- Unity 컴파일 오류 0건, 새 SO에서 `Digital Integrity Check` 참조 확인.
- 실제 파일맵에서 로비로 전환해 귀로 듣는 검증은 남아 있다.
