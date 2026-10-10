# StageManager 체크포인트·낙하 설계 검토

- 작업 요약 및 방법: Core의 StageManager, GameManager, PlayerSpawner, PlayerState, SpawnerBase와 프로젝트 지침을 읽고 Unity 공식 물리 API 문서를 확인했다.
- 사용자 제약: Core 코드만 취급. 별도 Checkpoint/FallZone 스크립트 없이 StageManager에서 관리. 이번 요청은 설계이며 기능 구현은 하지 않았다.
- 제안: StageManager 내부 SCheckpoint 구조체에 BoxCollider 감지 영역과 Transform 복귀 지점을 묶고 배열 순서를 진행 인덱스로 사용한다. 낙하 구역은 BoxCollider 배열로 관리하고 플레이어 몸체와의 겹침을 직접 검사한다. 현재 인덱스보다 앞선 지점만 활성화한다.
- 처리 흐름: 낙하 감지 → 진입 1회 데미지 → 생존·진행 여부 재확인 → 최근 체크포인트 또는 최초 스폰 위치로 이동. 낙하 감지 회차에는 체크포인트 갱신을 생략한다.
- 현재 상태 및 이슈: Core에서 데미지 전달 및 이동 내부 속도 초기화 API는 확인되지 않았다. Core 외 코드는 읽거나 수정하지 않았다. 얇은 영역의 고속 통과는 현재 위치 겹침 검사만으로 누락될 수 있다.
- 검증: 코드·API 문서 검토만 수행. Unity 실행·컴파일 미실시.
- 다음 할 일: 구현 요청 시 intent 등록 후 몸체 Collider 식별, 데미지 전달, 이동 상태 초기화 계약을 확정하고 Core 내 최소 변경으로 구현한다.