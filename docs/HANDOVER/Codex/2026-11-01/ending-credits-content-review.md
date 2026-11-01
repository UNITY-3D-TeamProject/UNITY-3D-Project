# 엔딩 크레딧 표시 내용 확인

## 작업 요약
- `Map_5_Live.unity`의 `CreditsRoll` 텍스트와 엔딩 크레딧 연출 설정을 읽어 사용자에게 현재 표시 내용을 순서대로 전달했다.
- 사용자 요청에 따라 게임 제작 에셋 범위에 `메인메뉴`를 추가하고 `SPECIAL THANKS`와 마지막 문장 한 줄을 삭제했다.
- 35번 공연 이후 컷씬의 비트 #2와 코리 #3 대사를 사용자가 선택한 문구로 바꿨다.

## 방법/접근
- 씬에 저장된 TextMeshPro `m_text`의 이스케이프 문자를 디코딩하고 서식 태그를 제외해 실제 문구를 확인했다.
- `EndingCredits.cs`와 씬의 연결 값을 확인해 안내 문구 및 재생 흐름을 확인했다.

## 변경된 파일
- `Assets/_Project/Scenes/Map_5_Live.unity`의 `CreditsRoll` 텍스트 한 줄을 수정했다.
- `SOCutscene_Live_AfterShow.asset`, `docs/cutscene-dialogue-script.md`, `docs/intent/intent-096-ending-cutscene.md`의 대사 내용을 갱신했다.
- 이 작업 기록과 `HANDOFF_POINTER.md`만 갱신했다.

## 결정 사항
- 없음. 사용자가 내용을 검토한 뒤 수정할 예정이다.

## 현재 상태 및 이슈
- 수정 후 크레딧 텍스트를 다시 디코딩해 세 가지 요청 사항을 확인했다.
- 컷씬 에셋에서 변경한 두 대사를 디코딩해 확인했다.
- Unity 플레이 모드의 시각 검증은 수행하지 않았다.

## 다음 할 일
- 사용자가 추가 크레딧 수정안을 주면 같은 씬의 텍스트를 이어서 수정한다.
