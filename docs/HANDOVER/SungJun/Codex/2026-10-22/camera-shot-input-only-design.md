# 카메라 샷 중 입력만 막는 설계 검토

## 작업 요약 및 방법
- 사용자 요청에 따라 SOCameraShot, CinematicManager, InputMapController, GameManager, UIManager와 관련 intent·사용 가이드를 정적으로 확인했다.
- 코드·씬·에셋은 변경하지 않았다. Unity Play 검증은 수행하지 않았다.

## 확인 사항
- SCameraShot.ShouldPauseTime이 이미 샷별 시간 정지 여부를 정한다. false인 샷은 CinematicManager.CoPlayShot에서 자기 시간 정지 요청만 해제한다.
- CinematicManager.TryStart는 시간 정지 여부와 독립적으로 Cutscene 입력 맵을 요청한다. ReleaseControl까지 이 요청이 유지되므로 기존 입력 잠금 기능을 재사용할 수 있다.
- CinematicManager.Finish는 마지막 샷 설정과 관계없이 PauseGameTime(this)를 호출해 복귀 블렌드 동안 시간을 멈춘다.
- 컷씬 대사 단계는 별도로 시간 정지를 요청한다. 설정창 정지도 GameManager의 독립된 조건이다.

## 제안 사항
- 로비 전경의 모든 샷은 ShouldPauseTime을 false로 설정한다.
- Finish의 무조건 시간 정지 요청을 제거하고 마지막으로 실행한 단계의 시간 요청 상태를 복귀 블렌드까지 유지하는 최소 변경을 추천한다.
- 복귀 블렌드가 끝나면 기존 ReleaseControl로 연출 자신의 시간·입력 요청만 해제한다. GameManager와 InputMapController의 소유권 구조를 유지한다.
- 이번 요구에는 새 입력 잠금 시스템이나 별도 재생 모드가 필요하지 않다. 제안이며 아직 구현·확정되지 않았다.

## 현재 상태 및 이슈
- 샷 도중에도 시간이 멈춘다는 실제 보고의 원인은 샷 설정·다른 요청자·실행 경로 확인이 더 필요하다. 종료 블렌드의 무조건 정지는 코드로 확인했다.
- 입력 잠금은 물리 고정·무적과 다르다. 이동 중 진입 시 입력 해제, 기존 동작과 중력의 영향은 실제 Play에서 확인해야 한다.

## 다음 할 일
- 구현 시 관련 intent-063·064 및 최신 SO 분리 intent를 확인하고 요구사항을 갱신한다.
- 시간 흐름 샷·시간 정지 샷 각각의 복귀 블렌드, 대사→샷 전환, ESC 열기·닫기, Stop/비활성화 시 자기 요청 해제를 검증한다.
