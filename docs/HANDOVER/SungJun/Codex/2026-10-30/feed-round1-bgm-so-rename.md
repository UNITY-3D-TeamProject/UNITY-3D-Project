# 피드 1라운드 BGM SO 이름 정리

## 작업 요약 및 방법
- 기존 `SOAudioData_BgmFeed.asset`와 메타 파일을 `SOAudioData_BgmFeedRound1`로 이름 변경했다.
- 에셋 내부 `m_Name`도 같은 이름으로 바꿨다.

## 결정 사항
- GUID `a96202c27c8a41a097829c034a02f3e2`를 유지해 피드 씬 `AudioManager._bgm` 참조가 그대로 이어지도록 했다.

## 현재 상태 및 이슈
- 사용자의 이전 요청에 따라 Unity 재생·컴파일 검증은 수행하지 않았다.

## 다음 할 일
- 필요할 때 Unity에서 1라운드 SO 참조를 확인한다.
