# 퀘스트 목록(Active Mission) 배경 패널 추가 (2026-10-16)

## 작업 요약 및 방법
- 사용자 요청: 왼쪽 위 퀘스트 글자(`Active Mission`: "Rescue the Allies", "Defeat all the Enemies")가 밝은 배경에서 안 보여서 패널 하나 골라 넣기.
- 작업 당시 열린 씬은 `Assets/_Project/Scenes/Map_2_Feed.unity`였다. 거기 `Canvas (1)`은 Test `UnityAssets/Prefabs/UI/MainScene/Canvas.prefab` 인스턴스(오버라이드 없음)라서 **프리팹 원본**을 고쳤다 → Map_2_Feed와 MainScene에 같이 반영된다. 씬 파일은 건드리지 않았다.
- `Active Mission` 첫 자식으로 `Panel` 추가(LayoutElement ignoreLayout, 사방 스트레치, 좌우 16·위아래 14 여백):
  - 바탕: Shift `Cut Frame Filled`(Sliced), HP 패널과 같은 색 (0.02, 0.10, 0.15, 0.88)
  - 자식 `Glow`: Shift `Cut Frame Glow - 3px`(Sliced), HP 패널 Glow와 같은 색·크기
- `Active Mission` 위치 (22.5, -25) → (48, -54): 패널 바깥 여백을 HP 패널(왼쪽 32, 화면 끝 40)과 맞춤.

## 결정 사항
- 새 패널은 HP 패널과 같은 Shift Cut Frame 스타일로 통일한다.

## 현재 상태 및 이슈
- 보라색 배경에서 임시 카메라 렌더로 가독성 확인. 프리팹 저장됨, 커밋 안 함.
- 팀 씬 Map_2_Feed가 Git 제외 Test 폴더의 프리팹을 참조하고 있다(사용자 구성). 다른 환경에서는 이 Canvas가 빠질 수 있다.

## 다음 할 일
- 실제 퀘스트 내용(문구) 연결은 미정.
