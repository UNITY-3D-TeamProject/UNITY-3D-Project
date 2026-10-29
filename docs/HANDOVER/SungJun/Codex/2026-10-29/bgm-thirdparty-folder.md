# BGM 파일 위치 통일

## 작업 요약 및 방법
- BGM WAV 5개와 각각의 `.meta` 파일을 `Assets/ThirdParty/Audio/Bgm`으로 이동했다.
- 새 `Bgm` 폴더의 Unity 메타를 추가했다.

## 결정 사항
- 기존 AudioClip GUID를 유지해 BGM SO와 씬 참조는 그대로 둔다.
- `Assets/ThirdParty`는 Git 제외 대상이므로 음원과 메타는 별도 공유가 필요하다.

## 현재 상태 및 이슈
- 사용자 요청에 따라 Unity 실행, 임포트 및 재생 검증은 하지 않았다.

## 다음 할 일
- 사용자가 각 씬에서 BGM 재생을 확인한다.
