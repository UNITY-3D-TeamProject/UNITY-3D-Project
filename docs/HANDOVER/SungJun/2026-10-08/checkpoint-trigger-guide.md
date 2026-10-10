# 체크포인트 감지 컴포넌트 분리 코드 안내

- 사용자 합의: 모든 체크포인트(최초 생성 위치·일반·라운드 시작점)에 같은 스크립트를 붙이고 감지를 분리한다. Core 안에서만 작성한다.
- 안내 코드: Core.Stage.CheckpointTrigger, BoxCollider 필수 및 Trigger 설정, Player LayerMask 필터, RespawnPoint 보관, OnPlayerEntered 이벤트로 자기 자신과 감지한 Collider 전달. 현재 플레이어 여부 판단은 이벤트를 받는 StageManager에서 가능하도록 Collider를 전달한다.
- 책임 분리: 체크포인트에 라운드 판정·스냅샷 저장·플레이어 이동·영구 방문 플래그를 넣지 않는다. 중복 접촉과 진행도 역행 방지는 StageManager/PlayerState가 담당한다.
- 설정: 컴포넌트와 BoxCollider는 같은 오브젝트에 두고, 감지 대상 Collider의 실제 레이어를 마스크에 지정한다. Physics 레이어 충돌 설정도 허용해야 한다. RespawnPoint는 안전한 복귀 위치를 별도 연결한다.
- 변경 방향: 앞서 안내한 StageManager.LateUpdate/ComputePenetration 방식은 채택하지 않는다. StageManager의 SCheckpoint 배열을 CheckpointTrigger 배열로 전환하고 OnPlayerEntered 구독·해제를 연결하는 작업이 후속으로 필요하다.
- 현재 상태: 사용자에게 코드만 안내하고 런타임 파일 생성·수정은 하지 않았다. 이벤트 구독과 진행도 갱신 연결, 컴파일 및 Unity 검증은 아직 미실시다.
- 참고: Unity OnTriggerEnter 공식 문서에서 비활성 MonoBehaviour에도 이벤트가 전달될 수 있음을 확인해 isActiveAndEnabled 가드를 포함한다.

## 컴포넌트 특성 및 임포트 오류 설명

- 사용자 적용 파일에서 DisallowMultipleComponent와 RequireComponent(typeof(BoxCollider))가 정상 형태로 작성된 것을 확인했다.
- DisallowMultipleComponent는 같은 GameObject에 동일 타입 컴포넌트의 중복 추가를 막고, RequireComponent는 스크립트 추가 시 같은 오브젝트에 필요한 BoxCollider가 없으면 자동 추가하는 특성으로 설명한다. Is Trigger 설정은 특성이 아니라 Reset/Awake 코드가 담당한다.
- 보고된 SourceAssetDB modification time 오류는 기록된 08:31:37 UTC와 실제 디스크 08:31:43 UTC의 수정 시각 불일치다. 확인한 파일 수정 시각도 뒤의 값과 일치한다. 임포트 도중 저장 등이 원인일 수 있으나 원인은 확정하지 않는다.
- 안내: 파일 저장 완료 → Unity 임포트 대기 → 해당 스크립트 우클릭 Reimport → 지속 시 작업 저장 후 에디터 재시작. 소스/메타/Library 삭제나 런타임 수정은 하지 않았다. 실제 에디터 복구 여부는 확인하지 않았다.

## 완료까지의 후속 작업 순서

- 현재 코드 확인: CheckpointTrigger는 사용자 적용돼 있으나 StageManager는 아직 SCheckpoint[]이며 OnPlayerEntered 구독이 없다. HandlePlayerDeath는 여전히 EndStage/OnStageFailed를 호출한다.
- 다음 순서: (1) StageManager 배열을 CheckpointTrigger[]로 전환하고 최초 스폰 조회의 null 검사를 조정 (2) 도달 이벤트 구독/해제 및 현재 플레이어·진행 상태 검사 (3) 일반 진행도 최대 인덱스/라운드 최초 스냅샷 갱신 (4) Inspector의 새 컴포넌트 참조 재연결 및 도달 검증 (5) 기존 플레이어 이동·이동 상태 초기화 계약 확보 (6) 공통 사망 판정/일회 통지/부활 상태 계약 확인 (7) EndStage 기반 사망 처리를 기존 플레이어 라운드 복구로 변경 (8) 낙하 감지·일회 데미지·생존 복귀 및 공통 사망 경로 연결 (9) 복구 중 중복 차단과 통합 검증.
- 확인된 의존성: 현재 CharacterMotor는 수직/수평 속도를 private으로 보관하고 외부에서 호출할 이동 상태 초기화 API는 없다. Core만 수정하는 범위에서 임의의 private 필드 접근으로 해결하지 않으며 담당 이동 시스템의 공개 초기화 계약 확보가 필요하다.
- 경계: 이번 완료 범위는 최초 스폰·체크포인트 진행·HP/배터리 스냅샷·낙하·사망 플레이어 복구다. 적/기믹/목표 전체 초기화 및 디스크 저장은 포함하지 않는다.
- 이번 작업은 상태 확인과 순서 안내이며 게임 코드 수정·Unity 검증은 수행하지 않았다.
