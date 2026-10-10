# 1단계 — 체크포인트 데이터 구성 코드 안내

- 작업 요약: 사용자 요청에 따라 StageManager의 현재 코드를 다시 읽고, 실제 파일 수정 대신 붙여 넣을 코드와 위치를 안내했다.
- 방법: 기존 SCheckpoint(Area, RespawnPoint)와 _checkpoints를 유지하고 라운드별 체크포인트 인덱스 배열, 현재 라운드 인덱스, 읽기 전용 진행도 프로퍼티를 추가하는 최소 구성을 제안했다.
- 데이터: _roundCheckpointIndices의 배열 인덱스는 0부터 시작하는 라운드 인덱스, 배열 값은 _checkpoints의 인덱스다. 예: [0, 3, 6].
- 초기 상태: 기존 _currentCheckpointIndex = -1을 유지하고 _currentRoundIndex도 -1로 둔다. 실제 첫 라운드 초기화는 스폰/스냅샷 준비 단계에서 연결한다.
- 결정 사항: 별도 체크포인트 스크립트나 중복 IsRoundCheckpoint 플래그를 추가하지 않는다. StageProgress 및 CurrentClearProgress를 라운드 인덱스로 재사용하지 않는다.
- 현재 상태: 설명용 코드만 제공. 런타임 스크립트 수정·Unity 컴파일·Inspector 설정은 수행하지 않았다. 사용자 적용 여부는 후속 작업에서 실제 파일을 확인해야 한다.
- 다음 할 일: 1단계 적용 및 Inspector 매핑 확인 후 2단계 PlayerState의 라운드 스냅샷 저장·복원을 진행한다.
