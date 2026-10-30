# 인게임 설정창 버튼 고정과 슬라이더 클릭 영역 확대

## 작업 요약
- `Canvas_InGame` 설정창의 `MAIN MENU`·`QUIT GAME` 버튼을 ScrollView의 Content에서 Window 하단으로 옮겨 처음 열 때 항상 보이게 했다.
- 마스터·음악·효과음·마우스 감도 슬라이더의 클릭 판정 높이를 18px에서 40px로 넓혔다.

## 방법/접근
- 기존 버튼과 이벤트 연결을 유지한 채 `Row_Exit`의 부모와 앵커만 변경했다. ScrollView 높이를 줄여 하단 버튼과 겹치지 않게 했다.
- 각 슬라이더 루트에 투명한 uGUI Image를 추가해 넓어진 사각형 전체가 클릭을 받도록 했다. 배경·채움·손잡이의 표시 높이는 18px로 유지했다.

## 변경된 파일
- `Assets/_Project/Prefabs/UI/Canvas_InGame.prefab`
- `docs/intent/intent-073-settings-scroll-exit-buttons.md`
- `docs/intent/INTENT_POINTER.md`

## 결정 사항
- 종료 버튼은 스크롤과 독립된 창 하단 고정 영역으로 둔다.
- 슬라이더의 표시 크기와 클릭 판정 크기를 분리한다.

## 현재 상태 및 이슈
- 프리팹 파일ID·부모/자식 관계·슬라이더 4개 클릭 영역의 정적 검증을 마쳤다. Unity Editor Play 화면과 실제 마우스 입력은 확인하지 못했다.
- 관련 `intent-073`은 기존 맵 씬·감도 체감 검증이 남아 있어 open 상태를 유지한다.

## 다음 할 일
- Editor Play에서 ESC 설정창을 열어 버튼이 처음부터 보이는지, 각 슬라이더의 막대 위아래 클릭이 값을 바꾸는지 확인한다.
