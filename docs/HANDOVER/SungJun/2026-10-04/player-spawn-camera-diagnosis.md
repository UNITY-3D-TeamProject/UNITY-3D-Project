# 스폰 카메라 진단

## 작업 요약 및 방법
- 플레이어 프리팹의 BaseCamera/AimCamera 컴포넌트와 씬별 실제 Camera/CinemachineBrain 구성을 대조했다.
- 이번 진단에서는 코드나 씬 에셋을 수정하지 않았다.

## 변경된 파일
- `docs/HANDOVER/SungJun/HANDOFF_POINTER.md`
- 이 작업 기록

## 결정 사항
- 플레이어 프리팹의 두 CinemachineCamera는 가상 카메라다. Game 뷰 출력을 위해서는 씬에 활성화된 실제 Camera와 CinemachineBrain이 필요하다.

## 현재 상태 및 이슈
- 저장된 `Assets/_Project/Scenes/Lobby.unity`와 `Tutorial.unity`에는 Camera/CinemachineBrain이 없다.
- `Assets/_Project/Scenes/Test/MapTest_*.unity` 여섯 씬에는 Main Camera, 실제 Camera, CinemachineBrain이 있다.
- 사용자가 실행한 씬은 확인되지 않아 실제 발생 원인은 조건부다. 저장되지 않은 에디터 씬 변경도 확인할 수 없다.

## 다음 할 일
- 사용 중인 씬을 확인한다. Lobby/Tutorial이라면 활성화된 Main Camera에 Camera, AudioListener, CinemachineBrain을 구성한다. 테스트 씬이라면 다른 런타임 원인을 조사한다.
