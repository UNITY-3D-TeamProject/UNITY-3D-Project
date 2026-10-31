# 보안 앱 1라운드 사망 시 씬 재시작 — intent-103 후속

## 작업 요약 및 방법
- 요청: 보안앱 1라운드에서 죽으면 씬 재시작, 재시작 때 처음 보안맵을 카메라로 보여 주는 연출과 대화는 건너뛰기.
- `Core/Stage/StageManager.RecoverFromDeath`: `_restartSceneOnFirstRoundDeath || CurrentStage == EStageType.SecurityApp`이면 첫 라운드 사망 때 `RestartStage()`(씬 다시 불러오기). 툴팁·주석 갱신.
- 입장 연출 건너뛰기는 코드 추가 없음: 맵 카메라 샷(`SOCameraShot_Security_Overview`)과 대화는 모두 컷씬 26번(`SOCutscene_Security_Enter`) 안에 있고, 26번 트리거 `Cutscene_26_SecurityEnter`(TriggerPlate)에 `HideOnStageRestart`가 이미 붙어 있다. `RestartStage`가 `PrepareSpawn(StageRestart)` 후 씬을 불러오고, 트리거의 `Awake`가 그 값을 보고 꺼진다(플레이어 스폰은 `PlayerSpawner.Start`라 순서 문제 없음). `Map_4_Security`는 읽기만 하고 고치지 않음.
- 보스전 체력·배터리 꽉 채우기는 사용자가 "개발 안 해도 됨"으로 취소.

## 결정 사항
- 보안맵 씬 체크박스 대신 코드로 보안 앱만 켠다 (사용자 선택). 다른 스테이지는 기존 옵션 그대로.
- 사망 팝업의 RETRY와 설정창 RETRY 모두 같은 `RestartStage`를 탄다.

## 현재 상태 및 이슈
- 리컴파일 에러 0.
- **Play 미확인**: TestScene은 빌드 목록에 없어 `LoadScene(buildIndex)` 경로를 못 타고, 맵 씬은 열지 않음.
- `RestartStage`는 로딩 화면 없는 동기 로드라 재시작 때 잠깐 멈출 수 있음(기존과 같음).
- 커밋 안 함.

## 다음 할 일
- 보안맵 1라운드 방 2 이후에서 죽고 RETRY → 씬 재시작, 입장 컷씬 26번이 안 나오는지, 문·쫄몹이 처음 상태인지 확인.
- 2라운드에서 죽고 RETRY → 기존처럼 발판 리셋 리스폰인지 확인.
- 문제 없으면 intent-103 resolved 처리.
