# 파일 앱 1라운드 목표 문구 추가

## 작업 요약 및 방법
- 파일 앱 1라운드 왼쪽 위 정보 패널에 `미로를 탈출하세요`를 표시한다. 제목 `1라운드`는 그대로이고 본문이 비어 있던 자리를 채웠다.
- `FileApp`에 `MAZE_ESCAPE_ROUND_INDEX = 0` 상수를 추가하고, `InfoPanelPresenter.RefreshFileApp`에 해당 라운드 case를 추가했다.

## 결정 사항
- 다른 라운드 문구와 같이 `InfoPanelPresenter`에 직접 적는다. 맵 씬과 프리팹은 수정하지 않았다.

## 현재 상태 및 이슈
- 확인함: 리컴파일 에러 0, TestScene Play에서 패널 문구와 콘솔 에러 0, 화면 캡처.
- 미확인: 실제 `Map_3_File`에서의 표시. 패널 코드는 같지만 맵 씬은 열지 않았다.
- 스크립트 반영을 위해 사용자가 켜 둔 MainScene Play를 끄고, 에디터에 TestScene을 연 상태로 끝냈다.
- `Pretendard-Bold SDF.asset`(diff 약 5만 줄)과 `RT_LoadingRunners.renderTexture`가 Play 뒤 변경 상태로 남아 있다. 이 작업과 별개로, 되돌리지도 커밋에 넣지도 않았다.
- 커밋 안 함.

## 다음 할 일 (Next Steps)
- 문구 커밋. 폰트 에셋 변경은 커밋에 넣을지 사용자가 판단.
- 실제 파일 맵에서 패널 표시 확인.
