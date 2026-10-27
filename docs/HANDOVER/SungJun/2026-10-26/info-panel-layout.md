# 왼쪽 위 라운드 패널 줄 정리 + 도둑 잡기 타이머 패널 (intent-080)

## 1. 작업 요약 및 방법
- 먼저 왼쪽 위 `Canvas_InGame/Active Mission` 패널 문구 13가지를 정리했다(전부 `InfoPanelPresenter`가 만든다). 플레이 중에 문구를 직접 넣어 캡처했다.
- 사용자 요청에 따라 패널을 고쳤다.
  - 제목 줄(`N라운드`) 앞 동그라미를 없앴다. 모든 앱이 같은 패널을 쓰므로 한 번에 적용된다.
  - 숫자 퍼즐을 세 줄로 나눴다: `목표 : 12` / `숫자 : 3 5 _  연산자 : + _` (셋째 줄도 동그라미 있음).
  - 패널을 세 줄 크기(400×121)로 키웠다. 두 줄 문구는 줄 높이 56.5·글자 32로 키워 같은 크기를 채운다.
  - 도둑 잡기 남은 시간은 화면 위 가운데 새 패널 `Round Timer`(`남은 시간 45초`)로 뺐다. 왼쪽 위 본문은 `남은 도둑 N명`만 남겼다.
- 바꾼 파일
  - `Scripts/UI/InfoPanel/InfoPanelView.cs`: `_extraText`, `SetTexts(title, body, extra)`, 두 줄 크기 값(`_twoLineRowHeight`·`_twoLineFontSize`)
  - `Scripts/UI/InfoPanel/InfoPanelPresenter.cs`: `RefreshFileApp`·`RefreshPuzzle`·`RefreshTimer`, `_timerView`
  - `Scripts/UI/InfoPanel/RoundTimerView.cs` (새 파일)
  - `Prefabs/UI/Canvas_InGame.prefab`: `Active Mission` 폭 400, 제목 `Icon` 끔, 제목 글자 왼쪽 10, 줄 글자를 세로로 늘림, `Mission Example (2)` 추가, `Round Timer` 추가(기본 꺼짐)

## 2. 결정 사항
- 줄 수에 따라 레이아웃을 새로 만들지 않는다. 기존 VerticalLayoutGroup 줄의 높이와 글자 크기만 바꾼다. 세 줄 기준 값(35·25)은 `Awake`에서 프리팹 값을 읽는다.
- 타이머 패널은 `InfoPanelPresenter`가 함께 맡는다. FileApp을 이미 구독하고 있어서 같은 갱신을 쓴다. 도둑 잡기 라운드 진행 중에만 보이고, 다 잡으면 숨긴다.
- Core/Stage 코드와 맵 씬은 수정하지 않았다.

## 3. 현재 상태 및 이슈
- 컴파일 에러 0.
- `Map_3_File` Play에서 StageManager 라운드 번호를 리플렉션으로 바꿔 확인했다.
  - 2라운드: 세 줄(`목표 : 47`)
  - 3라운드: `남은 도둑 1명` + 타이머 `남은 시간 180초`
  - 1라운드로 돌아가면 타이머가 숨는다.
- 나머지 문구(피드·보안·클리어)는 View에 직접 넣어 화면만 확인했다. 직접 걸어서 하는 플레이 확인은 아직 안 했다.
- 빈 칸이 `___`처럼 붙어 보이던 문제는 칸 사이 두 칸(`HELD_SEPARATOR`)·묶음 사이 네 칸으로 고쳤다(Play 확인).
- 후속: 피드(`피드 재료 모으기` / `3/5`, `피드 분류` / `3/8`)와 보안 1·2라운드(`구역 2/4` / `남은 적 5명`)도 세 줄로 나눴다. `Map_2_Feed`·`Map_4_Security` Play에서 확인(피드 2라운드는 View 직접 입력).
- 후속: 제목(`N라운드`)을 가운데 정렬했다(프리팹 제목 글자 `Center`, 왼쪽 여백 0). 본문 줄은 왼쪽 정렬 그대로다.
- 후속: 타이머 문구를 `남은시간 : N초`로 바꿨다(Play 전체 화면 확인).
- 커밋 안 함.

## 4. 다음 할 일 (Next Steps)
- 실제 플레이로 파일 앱 2·3라운드 패널과 타이머 확인
- 확인되면 intent-080을 `resolved`로 바꾸고 `clear/`로 옮긴다.
