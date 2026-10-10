# StageManager 체크포인트 컴포넌트 연결

## 작업 요약 및 방법
- 사용자 요청 1~3번을 StageManager.cs에 구현했다. 사용자가 마지막에 코드를 직접 보고 확인하겠다고 요청해 수정 부분을 대화에 제공한다.
- SCheckpoint 제거, CheckpointTrigger[] 전환, 최초 RespawnPoint 조회 시 컴포넌트 null 검사 추가.
- OnEnable/OnDisable에서 이벤트 구독/해제. 스폰된 AttributeSet의 부모 CharacterController를 캐싱하고 도달 Collider의 부모와 비교한다.
- 실행 중/Playing/첫 스냅샷 준비/현재 플레이어/HP 양수/CombatMediator 생존을 검사한다.
- 일반 진행도는 최대 인덱스 유지. 다음 라운드 시작점과 실제 도달 인덱스가 같으면 PlayerState.SaveRoundSnapshot 호출 후 저장 성공 시 라운드를 갱신한다.

## 결정 사항
- CheckpointTrigger는 감지·알림, StageManager는 목록과 진행 판단, PlayerState는 최초 HP·배터리 보관을 담당한다.
- 일반 진행도보다 낮은 인덱스라도 다음 라운드 시작점이면 라운드 진입을 처리한다.
- 기존 사용자 변경을 보존하고 StageManager의 CP949 인코딩을 유지했다.

## 현재 상태 및 이슈
- 빌드 오류 0, 기존 경고 5. git diff --check 통과.
- Unity Play 검증과 Inspector 재연결은 미수행. 사용자 코드 검토 대기.
- 일반 실행 도구는 Windows sandbox setup refresh 오류로 실패해 승격 실행으로 파일 읽기/수정/빌드를 수행했다.
- 사망 시 EndStage/OnStageFailed는 기존 동작이며 복구는 후속 단계다.

## 다음 할 일
- Checkpoints 배열에 CheckpointTrigger를 경로 순서대로 연결하고 레이어, BoxCollider, RespawnPoint, 중복 없이 증가하는 라운드 인덱스를 설정한다.
- 최초 스폰 → 일반 지점 → 다음 라운드 → 이전 지점/동일 지점 재접촉을 확인한다. 더 뒤의 일반 지점을 먼저 밟고 다음 라운드 시작점을 나중에 밟는 경우도 확인한다.
- 다른 플레이어/일시정지/사망 상태의 접촉과 비활성화/재활성화 후 이벤트 중복 여부를 확인한다.
- 검증 후 기존 플레이어 이동·공개 속도 초기화 API 및 공통 사망/낙하 복구로 진행한다.