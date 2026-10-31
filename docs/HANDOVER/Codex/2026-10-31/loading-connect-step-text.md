# 로딩 접속 단계 문구 전환

## 작업 요약
- 로딩 화면 마지막 준비 단계에서 진행 중에는 `접속 준비중...`, 완료 시에는 `접속 완료`를 표시한다.
- 피드 앱 화면을 90%, 100% 상태로 각각 캡처했다.

## 방법/접근
- `LoadingStageView.SetProgress`가 마지막 단계의 체크 상태와 문구를 함께 갱신하도록 했다.
- 스테이지 씬과 로비 데이터의 마지막 단계 초기 문구를 `접속 준비중...`로 맞췄다.
- 로딩씬 단독 Play에서 UI를 활성화하고 두 진행률을 설정한 뒤 렌더링했다.

## 변경된 파일
- `Assets/_Project/Scripts/SceneTransition/LoadingStageView.cs`
- `Assets/_Project/Scenes/Loading.unity`
- `Assets/_Project/ScriptableObjects/SceneTransition/SOLoadingStageData.asset`
- `docs/screenshots/loading-connect-preparing.png`
- `docs/screenshots/loading-connect-complete.png`

## 결정 사항
- 마지막 단계의 완료 문구는 스테이지와 로비 모두 `접속 완료`로 통일한다.

## 현재 상태 및 이슈
- Unity 재컴파일 오류 없음. 두 캡처에서 문구와 체크 상태 확인.
- 캡처는 로딩씬 단독 Play에서 상태를 직접 설정한 화면이다. 실제 씬 이동 경로는 이번에 재생하지 않았다.

## 다음 할 일
- 필요 시 로비에서 스테이지, 스테이지에서 로비로 이동하며 실제 전환 시점을 확인한다.
