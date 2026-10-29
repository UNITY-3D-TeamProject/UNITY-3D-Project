# 파일 앱 미로맵 BGM Algorithm Arpeggio 연결

## 작업 요약 및 방법
- 제공된 `Algorithm Arpeggio.wav`를 `Assets/ThirdParty/Audio/Bgm`에 두고 Unity AudioImporter 메타를 추가했다.
- 단일 클립 `SOAudioData_BgmFile`을 생성해 `Map_3_File` 씬 AudioManager의 BGM 칸에 연결했다.

## 결정 사항
- 기존 씬별 AudioManager 구조와 다른 맵 BGM의 음량 0.6, 페이드 인/아웃 1초 설정을 따랐다.

## 현재 상태 및 이슈
- 사용자 요청에 따라 Unity 실행, 컴파일, 재생 및 파일 검증은 하지 않았다.

## 다음 할 일
- 사용자가 미로맵에서 재생과 음량을 확인한다.
