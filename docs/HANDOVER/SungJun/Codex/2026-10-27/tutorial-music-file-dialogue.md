# 음악 파일 획득 대사 수정

## 작업 요약 및 방법
- 튜토리얼 C7 음악 파일 획득 컷씬의 8개 대사를 사용자 제공 문안으로 교체했다.
- 노래 제목이 실제 음악 파일 화면에도 맞게 보이도록 `MusicFile`의 기본값과 `Map_0_Tutorial`의 직렬화 값을 `다시, 처음부터 · 마지막 데모 파일`로 변경했다.
- 씬의 `MusicFile._onAcquired`가 컷씬 테이블 7번을 재생하는 연결을 확인했다.

## 변경된 파일
- `Assets/_Project/ScriptableObjects/UIandCinematic/Cutscene/SOCutscene_Tutorial_MusicFile.asset`
- `Assets/_Project/Scripts/Tutorial/MusicFile.cs`
- `Assets/_Project/Scenes/Map_0_Tutorial.unity`

## 결정 사항
- 사용자 메시지의 문안을 기준으로 했다. 첨부 이미지는 변경 전 C7 문안과 장면 조건을 확인하는 데 사용했다.
- 경보 및 길 개방의 이벤트 연결은 바꾸지 않았다.

## 현재 상태 및 이슈
- 컷씬의 화자·문구 8쌍과 화자 카메라 키를 검증했다.
- Unity 플레이 화면에서 대사창, 제목 표시, 경보 타이밍은 확인하지 않았다.

## 다음 할 일
- 필요하면 튜토리얼 플레이에서 음악 파일 획득 순간의 대사·제목·경보 타이밍을 확인한다.
