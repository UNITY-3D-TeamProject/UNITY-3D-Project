# Canvas HUD 위치 커밋 확인

## 작업 요약
- 개인 Test 저장소의 Canvas Slider/Text 배치 커밋과 현재 파일을 비교했다.

## 방법/접근
- 중첩 Git 저장소 `Assets/Test/UNITY-3D-Project-for-KSJ-Test`의 Canvas 프리팹 이력을 조회했다.
- `82fee94`에서 Canvas 프리팹이 처음 추가됐고, 현재 `main`의 해당 파일과 차이가 없음을 확인했다.
- Home/Lobby 씬의 Canvas 인스턴스에는 Slider 및 Text의 `m_AnchoredPosition` 덮어쓰기가 없음을 확인했다.

## 변경된 파일
- 이 작업 기록과 `HANDOFF_POINTER.md`만 변경했다. Unity 에셋은 변경하지 않았다.

## 결정 사항
- 배치 커밋을 복원하지 않는다. 현재 프리팹이 이미 그 커밋과 동일하다.

## 현재 상태 및 이슈
- Canvas Scaler는 Constant Pixel Size이고, Slider/Text는 중앙 앵커에서 x 약 -766/-792 픽셀로 배치되어 있다. Game 뷰 해상도 변화 시 위치가 크게 달라질 수 있다.
- 실제 Unity Game 뷰 해상도는 확인하지 못했으므로 이번 변화의 직접 계기는 확정할 수 없다.

## 다음 할 일
- Unity Game 뷰 해상도를 확인하고, 필요하면 좌하단 기준 앵커와 Canvas Scaler 설정을 함께 조정한다.
