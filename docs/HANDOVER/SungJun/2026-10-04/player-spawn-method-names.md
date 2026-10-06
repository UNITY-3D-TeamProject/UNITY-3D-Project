# 스폰 처리 함수 이름 구분

## 작업 요약 및 방법
- 사용자 요청에 따라 GameManager.RegisterPlayer를 CompletePlayerSpawn으로, PlayerState.RegisterPlayer를 InitializePlayerState로 변경했다.
- PlayerSpawner 및 GameManager의 호출부와 함수 설명 주석을 함께 갱신했다. 동작 변경은 없다.
- 스크립트 변경은 Core 세 파일로 제한했고 PlayerSpawner의 CP949 인코딩을 보존했다.

## 결정 사항
- 스폰 완료 및 알림과 플레이어 참조 연결/조건부 복원을 이름으로 구분한다.

## 현재 상태 및 이슈
- git diff --check 통과. Assets/_Project의 코드 및 씬/프리팹/에셋 검색에서 이전 RegisterPlayer 참조가 남지 않았음을 확인했다.
- Unity 컴파일/실행 검증은 수행하지 않았다.

## 다음 할 일
- 기존 스폰 분기의 Inspector 배선 및 Unity 실행 검증을 이어간다.
