# 스캔 2차 검토 반영 (2026-10-11)

## 작업 요약 및 방법
[2차 검토](./scan-design-review-2.md) 1·3·4번과 단순화 항목을 반영했다. 컴파일 오류 0, Play 검증은 미실시.

- `MeshRevealReaction`
  - 사라지는 중 재스캔 시 전체로 튀던 문제 수정: 활성/사라짐 분기를 `Lerp(near, Min(far, _revealRadius), fade)` 한 식으로 합쳤다. 활성 중에만 `_revealRadius`가 커진다.
  - `OnScanUpdate`는 `_revealRadius`만 갱신하고 셰이더 적용은 `OnFade`에서 한 번만 한다(같은 프레임 PropertyBlock 2회 쓰기 제거). 불필요한 `_origin` 재대입 제거.
  - `OnScanBegin`은 세기 0과 같은 값(가장 가까운 점)으로 시작한다.
  - `_appliedRadius`로 같은 반경이면 PropertyBlock을 다시 쓰지 않는다. 새 스캔 시작 때 초기화.
- `SilhouetteReaction`, `MarkerReaction`: `_appliedFade`로 같은 세기면 다시 쓰지 않는다. 마커는 `OnScanEnd`에서 색을 직접 되돌리므로 초기화한다.

## 결정 사항
- `GetDistanceRange`는 매 프레임 계산을 유지한다. 움직이는 대상과 정지 대상이 모두 있기 때문. 정지 대상도 최종 반경 값이 같으면 셰이더 쓰기는 건너뛰므로 남는 비용은 bounds 계산뿐이다.
- 건너뛰기는 `ScanTarget`이 아니라 각 반응에서 한다. 메시는 활성 중 반경 성장·움직임 반영 때문에 매 프레임 틱이 필요하다.

## 현재 상태 및 이슈
- Play 검증 필요: 단일 스캔, 유지 중 재스캔, 사라지는 중 재스캔(메시가 접힌 지점부터 다시 펼쳐지는지), 움직이는 플랫폼.
- 반응 시작 순간 파동 접촉 정보가 `OnFade`보다 늦게 오면 반경 반영이 1프레임 늦을 수 있다(실행 순서 의존, 체감 없을 것으로 예상).
- 미반영: 스캔 레이어 지정(시험 씬 Everything), 미사용 옵션(`_shouldHideWhenIdle=false` 경로 등) 정리.

## 다음 할 일
1. TempScene Play로 위 시나리오 검증.
2. `Scannable` 레이어 생성 후 `ScanWave._scannableLayers` 지정.
