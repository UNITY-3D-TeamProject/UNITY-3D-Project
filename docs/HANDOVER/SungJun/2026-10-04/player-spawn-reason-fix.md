# 스폰 이유 분기 연결 수정

## 작업 요약 및 방법
- 런타임 스크립트 변경은 Core의 GameManager, PlayerSpawner, PlayerState 세 파일로 제한했다.
- GameManager.RegisterPlayer는 SceneTransition 여부를 shouldRestore 인자로 전달한다.
- PlayerSpawner는 NewGame/Respawn에서만 기존 Set + Float Effect 배열을 적용한다. CP949 인코딩을 보존했다.
- PlayerState의 사용자 구현(복원하지 않을 때 저장값 비우기)을 유지하고 매개변수 들여쓰기만 수정했다.
- GameManager에서 기존 SceneLoader.OnBeforeSceneChange를 구독/해제하고 현재 플레이어가 있을 때 일반 씬 이동 전 저장 및 이유 설정을 수행한다. 중복 GameManager는 구독하지 않는다.

## 결정 사항
- 능력치 증가와 LoadGame은 제외한다.
- SceneLoader 등 Core 외부의 스크립트는 변경하지 않았다.
- 사망 이벤트의 현재 동작은 플레이어 삭제이며 자동 재생성은 이번 작업 범위에 추가하지 않았다. 향후 재생성 경로에서 PrepareSpawn(Respawn)을 호출해야 한다.

## 현재 상태 및 이슈
- git diff --check 통과 및 호출 인자/초기화/알림 순서 정적 확인 완료.
- MSBuild 실행은 SDK 경로 접근 권한 오류로 실패했고, 샌드박스 외 실행 승인은 거절됐다. 컴파일 및 Unity PlayMode 검증은 완료하지 못했다.
- Inspector의 Effect 대상 이름/Amount 설정 및 실제 씬/부활 검증이 남아 intent-012는 open 유지한다.

## 다음 할 일
- Unity에서 컴파일과 새 게임/일반 씬 이동/부활 사유 스폰을 확인한다.
- 실제 부활 재생성 흐름을 구현할 때 PrepareSpawn(Respawn)을 연결한다.
