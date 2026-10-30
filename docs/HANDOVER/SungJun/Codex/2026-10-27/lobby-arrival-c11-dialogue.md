# C11 홈 화면 도착 컷씬 대사 수정

## 작업 요약 및 방법
- `SOCutscene_Lobby_ReturnTutorial.asset`을 사용자 제공 C11 문안의 15단계(대사 14개, 로비 전경 카메라 1개)로 맞췄다.
- 기존 로비 전경 샷의 전환 1.6초·유지 4.5초와 13번째 대사의 `Lobby_FeedPortal` 카메라 키를 유지했다.

## 변경된 파일
- `Assets/_Project/ScriptableObjects/UIandCinematic/Cutscene/SOCutscene_Lobby_ReturnTutorial.asset`

## 결정 사항
- 기존 컷씬 테이블 11번 연결과 카메라 에셋을 재사용하고, 컷씬 단계의 대사와 순서만 수정했다.
- 화자 카메라 단계의 `CameraKey`는 비워 두었다.

## 현재 상태 및 이슈
- 15단계의 화자·문구, 전경 샷 시간, 피드 앱 입구 카메라 키, 로비 씬의 컷씬 11번 호출을 정적 검증했다.
- Unity 플레이 화면에서 카메라 구도와 대사 표시 상태는 확인하지 않았다.

## 다음 할 일
- 필요하면 튜토리얼 탈출 후 로비 도착 지점에서 카메라와 대사 흐름을 플레이 확인한다.
