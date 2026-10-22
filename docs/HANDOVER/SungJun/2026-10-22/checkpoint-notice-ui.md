# 체크포인트 도달 알림 UI (intent-060)

## 1. 작업 요약 및 방법
- `StageManager`에 `public event Action<bool> OnCheckpointReached` 추가 (true = 라운드 체크포인트, false = 일반 체크포인트).
  - `HandleCheckpointEntered`에서 진행도를 갱신하기 전 `isNewCheckpoint = checkpointIndex > _currentCheckpointIndex`를 구해 둔다.
  - 다음 라운드 시작점이 아니면: 새 체크포인트일 때만 `Invoke(false)`.
  - 다음 라운드 시작점이고 스냅샷 저장에 성공해 `_currentRoundIndex`가 바뀌면 `Invoke(true)`.
  - 기존 진행·라운드 저장 로직은 그대로다.
- 새 스크립트 `Assets/_Project/Scripts/UI/Checkpoint/`
  - `CheckpointNoticeView : BaseView` — 제목·설명 TMP, `_tintImages`(아이콘·테두리 빛)에 색 입히기.
  - `CheckpointNoticePresenter` — `Start`에서 `FindAnyObjectByType<StageManager>()` 구독(InfoPanelPresenter와 같은 방식), 도달 시 종류별 문구·색으로 표시, `_displayDuration`(2.5초) 뒤 숨김. 표시 중 또 도달하면 시간을 다시 센다.
- `Canvas_InGame.prefab`에 `CheckpointNotice` 추가 (PetGuidePanel 바로 앞 순서).
  - 왼쪽 가운데 고정(anchor 0,0.5), 위치 (32, 60), 크기 420×96. 위쪽 미션 패널·아래쪽 HP 패널과 겹치지 않는 자리.
  - `CheckpointNotice`(View·Presenter, 항상 켜짐) / `Panel`(=_root, 기본 꺼짐) / `Glow`·`Icon`·`Title`·`Body`.
  - 스프라이트·폰트는 미션 패널이 이미 쓰는 `Cut Frame Filled`·`Cut Frame Glow - 3px`·`Glow Mote`·`Pretendard-SemiBold SDF`만 사용 (새 ThirdParty 참조 없음).

## 2. 결정 사항
- 라운드 체크포인트: 주황 `(1, 0.72, 0.3)`, "라운드 체크포인트 / 사망하면 이곳에서 다시 시작합니다."
- 일반 체크포인트: 하늘색 `(0.388, 0.776, 1)`(HUD 기본색), "체크포인트 활성화 / 떨어지면 이곳으로 돌아옵니다."
- 라운드 체크포인트는 낙하 복귀 지점도 되지만 라운드 알림 하나만 띄운다.
- 이미 지난(인덱스가 작거나 같은) 체크포인트를 밟거나 낙하 복귀로 다시 닿으면 띄우지 않는다. 1라운드 시작점(최초 스폰)도 알림 없음.
- 사망하면 진행도가 라운드 시작점으로 돌아가므로, 그 뒤 일반 체크포인트를 다시 밟으면 알림이 다시 뜬다 (복귀 지점이 실제로 바뀌므로 의도대로).
- 페이드 연출 없이 켜고 끄기만. `Canvas_InGame` 루트 CanvasGroup 아래라 대화 중에는 함께 숨겨진다.

## 3. 현재 상태 및 이슈
- 컴파일 0에러. TestScene Play(창 백그라운드라 Play 중에만 `Application.runInBackground` 켬)에서 플레이어를 체크포인트로 순간이동시켜 확인:
  - `Checkpoint_2` → 하늘색 "체크포인트 활성화" 표시 (화면 캡처)
  - `Checkpoint_R3_Thief` → 주황색 "라운드 체크포인트" 표시 (화면 캡처), 2.5초 뒤 자동 숨김
  - 그 뒤 `Checkpoint_2`(지난 지점) → 표시 안 됨
- 이후 TestScene(개인 씬, Git 미포함) StageManager의 `Round Checkpoint Indices`를 [0, 11, 12] → [0, 3, 6, 9]로 변경 (사용자 요청). `Checkpoint_3_Round`에서 주황 라운드 문구, `Checkpoint_4`에서 하늘색 일반 문구 확인. FileApp 라운드가 StageManager 라운드를 따라가므로 이제 3번에서 숫자 퍼즐(2라운드), 6번에서 도둑 잡기(3라운드)가 켜지고, FileRoundTest의 R2_Puzzle·R3_Thief는 일반 체크포인트가 됨.
- 라운드/일반 구분은 오브젝트 이름이 아니라 `Round Checkpoint Indices`로만 정해진다.
- 실제 조작으로 걸어 들어가는 경우, 맵 씬(`Map_*`)은 확인 안 함. 커밋 안 함.
- 아이콘은 `Glow Mote` 점이라 체크 표시 모양은 아니다. 전용 아이콘이 필요하면 `_Project`에 스프라이트를 추가해 `Icon`에 넣으면 된다.

## 4. 다음 할 일
- 맵 씬에서 실제 플레이로 위치·표시 시간이 괜찮은지 확인하고, 문구·색·시간은 `CheckpointNotice`의 Presenter Inspector에서 조정.
- 필요하면 페이드 인/아웃, 체크 아이콘, 효과음 추가.
