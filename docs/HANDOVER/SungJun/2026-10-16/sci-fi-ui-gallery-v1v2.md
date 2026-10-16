# UI 갤러리에 SCI-FI V1(Blue)·V2(Cyan)·Common 추가 (2026-10-16)

## 작업 요약 및 방법
- 10-15에 만든 갤러리 씬(`Assets/Test/UIGallery/SciFiComponentsGallery.unity`)에는 SCI-FI `SCI-FI UI Components` 629개 + Shift 31개만 있었다. 빠져 있던 SCI-FI UI Pack Pro의 나머지 프리팹 325개를 같은 칸 스타일로 추가했다.
  - `SciFi_V1Blue_Gallery` (x = 104800): `SCI-FI UI Pack_V1 Pro(Blue)/Prefabs` 160개, 폴더 5개
  - `SciFi_V2Cyan_Gallery` (x = 109600): `SCI-FI UI Pack_V2 Pro(Cyan)/Prefabs` 161개, 폴더 5개
  - `SciFi_Common_Gallery` (x = 114400): `Common/White Icons/Prefabs` 아이콘 시트 4장
- 이제 갤러리 칸 총 985개 = 두 팩의 프리팹 전부. 왼쪽부터 Shift → Components → V1 → V2 → Common 순.
- Unity MCP `eval`로 생성. 기존 칸 구조(Cell / CellBg / Label / Frame / 프리팹 인스턴스, 400×400 칸, 380px에 맞춰 축소, 10열)와 색·폰트를 기존 오브젝트에서 그대로 복사했다.
- V1·V2는 섹션 순서(Button → Icon → Option → PanelAndPopup → Widget)와 높이가 같아서 좌우로 같은 줄끼리 비교할 수 있다.
- 갤러리 씬만 저장. MainScene은 열려 있는 미저장 상태 그대로 두었다.

## 결정 사항
- Common 아이콘 시트는 1920×1080 크기라 칸을 2000×1160(2열)로 크게 잡았다.
- Common 프리팹은 Screen Space Overlay Canvas가 루트라 저장된 Scale이 0, 앵커가 (0,0)이다. 갤러리 인스턴스만 Scale 1, 앵커·피벗 0.5, 크기 1920×1080으로 오버라이드했다(원본 프리팹은 그대로).

## 현재 상태 및 이슈
- 씬 뷰 캡처로 V1·V2·Common 모두 보이는 것 확인. 새로 생긴 콘솔 에러 없음.
- 갤러리 씬은 `Assets/Test/`에 있어 Git에 안 올라간다(팀원 공유 안 됨).
- 기존 이슈(Shift Modal Window 분홍색 등)는 그대로.

## 다음 할 일
- 필요한 UI를 고르면 원본 프리팹으로 MainScene 등에 새로 배치(갤러리 인스턴스는 옮기지 않는다).
