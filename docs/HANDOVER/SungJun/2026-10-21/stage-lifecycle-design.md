# 스테이지 실행 책임 분리 설계

## 작업 요약 및 방법

- 사용자가 StageManager와 StageBase의 역할을 명확히 나누고 Markdown 설계 문서로 남기도록 요청했다.
- 현재 StageManager·StageBase·StageRound·FileApp·FeedApp, GameManager의 스폰 완료 순서, PlayerState의 최초 라운드 저장, FeedRound2의 Begin과 피드 시작 버튼 프리팹 연결을 읽었다.
- [설계 문서](../../../design/stage-lifecycle-responsibilities.md)에 책임 표, 상태 소유권, 제안 계약, 앱별 전환·복구 흐름, 코드 이동표, 후속 구현 순서와 검증 기준을 작성했다.

## 결정 사항

- 설계 제안: StageManager는 진행 확정·체크포인트·복구, StageBase는 라운드 공통 실행, StageRound는 개별 게임 규칙을 담당한다.
- FileApp·FeedApp은 라운드 구성·앱별 전환 조건·맵 연결을 담당한다.
- 현재 번호는 Manager만 변경한다. 전체 라운드 수는 Base의 앱 구성에서 제공하며, 별도 로직이 없는 FileApp 1라운드 때문에 로직 객체 수로 계산하지 않는다.
- 라운드 클리어·다음 라운드 진입·최종 스테이지 완료를 구분한다.
- 재시작은 플레이어 재생성·능력치 복원 이후에 실행하도록 제안한다.

## 변경된 파일

- `docs/design/stage-lifecycle-responsibilities.md`
- `docs/intent/intent-055-file-app-round-split.md` 및 `docs/intent/INTENT_POINTER.md`
- 이 인수인계 문서 및 `docs/HANDOVER/SungJun/HANDOFF_POINTER.md`

## 현재 상태 및 이슈

- 설계 문서 작성 완료. 런타임 코드·씬·프리팹은 이번 작업에서 수정하지 않았다.
- 작업 시작 시 FileApp·퍼즐 관련 코드와 폰트·RenderTexture·빌드 설정·문서 등에 기존 미커밋 변경이 있었다. 그대로 보존했다.
- FeedRound2.Begin은 기존 맵을 삭제한 뒤 OnStarted를 발생시킨다. 후속 구현에서 버튼 요청을 먼저 Manager로 보내고, 진입 전 참조·복귀 지점을 검증하는 연결이 필요하다.
- FeedApp은 CP949 파일이다. 후속 수정에서 인코딩을 보존한다.
- intent-055의 기존 테스트·맵 배치·검증 잔여 항목을 해결 처리하지 않았다.

## 다음 할 일

- 설계 검토 후 문서 10장의 적용 순서대로 공통 실행 코드를 이동한다.
- 기존 intent-053·055의 Map·AI·맵 씬 변경 제한을 유지한다. 피드 버튼·맵 계약을 실제 수정할 범위는 후속 작업에서 확인한다.
- 실제 구현 시 중복 전환, 스냅샷 실패, 피드 이전 맵 삭제 후 재스폰, 복구 중 타이머 정지를 검증한다.
