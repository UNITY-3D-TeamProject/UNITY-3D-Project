# 스캔 설계 2차 검토 (2026-10-11)

## 작업 요약 및 방법
- 1차 검토 반영 후의 `Core/Scanning` 9개 스크립트(`ScanWave`, `ScanTarget`, `IScannable`, `SScanHit`, `ScanReactionBase`, `MeshRevealReaction`, `SilhouetteReaction`, `MarkerReaction`, `WeakPointReaction`)를 정적으로 다시 검토했다.
- 기준: 과설계, 더 단순한 표현, 성능, 활용할 만한 패턴.
- 시험 씬(`TempScene.unity`) 직렬화 값과 외부 호출부(`ScanTestTrigger`, `WeakPointTestBinder`)를 함께 확인했다. 실행/측정은 하지 않았고 런타임 코드는 수정하지 않았다.

## 결정 사항
- 감지(`ScanWave`) → 상태·시간(`ScanTarget`) → 표현(`ScanReactionBase` 구현) 분리는 유지한다. State 패턴, EventBus, 전역 매니저 추가는 근거 부족.
- 아래 개선점은 권고이며 사용자 확인 전 미적용.

## 현재 상태 및 이슈
1. **[버그성] MeshReveal 사라지는 중 재스캔 시 즉시 전체로 튐**: fade-out 중 표시 반경은 `Lerp(near, visible, fade)`로 줄지만 `_revealRadius`는 그대로라, 재스캔으로 Active 복귀하면 다음 프레임 `ApplyProperties(_revealRadius)`로 한 번에 전체가 보인다. 실루엣·마커는 fade를 그대로 따라가 부드럽게 복귀하므로 메시만 다르다. 66~98행 주석("마지막 드러난 반경에서 다시 이어진다")도 실제 동작과 다르다.
   - 제안: 표시 반경을 항상 `Lerp(near, Min(far, _revealRadius), fade)` 하나로 계산하고, Active일 때만 `_revealRadius`를 키운다. `OnScanUpdate`는 `_revealRadius`만 갱신하고 적용은 `OnFade`에 맡긴다.
2. **[성능] 시험 씬 `_scannableLayers`가 Everything(m_Bits 4294967295)**: 지형 등 모든 콜라이더가 매 프레임 Overlap에 들어오고 256 버퍼 포화 위험. 스캔 대상 레이어 지정 필요.
3. **[성능·소] MeshReveal 매 프레임 중복 작업**: 파동 접촉 중 `OnScanUpdate`와 `OnFade`가 같은 프레임에 PropertyBlock을 두 번 쓴다. `GetDistanceRange`는 중심이 고정인데도 매 프레임 `renderer.bounds`를 다시 계산한다(움직이지 않는 대상이면 `OnScanBegin`에서 1회 계산 가능).
4. **[성능·소] Active 유지 중 값 변화 없는 OnFade 반복**: fade가 1에 도달한 뒤에도 유지 시간 내내 실루엣/마커가 같은 값을 매 프레임 설정한다. 대상 수가 적으면 무시 가능, 측정 후 판단.
5. **[과설계 후보]** `MeshRevealReaction._shouldHideWhenIdle = false` 경로와 `RestoreProperties`는 씬에서 사용처 없음(모두 true). `ScanWave.Stop()`/`IsPlaying` public도 외부 호출 없음. `ScanTarget`의 "같은 스캔이 활성 종료 후 다시 닿음" 분기는 파동 시간 = 유지 시간인 현재 구조에서 발생하지 않는 방어 코드.
6. **[패턴 대안]** 실루엣은 메시마다 자식 렌더러를 복제해야 한다. 원래 렌더러에 실루엣 머티리얼 슬롯을 런타임에 추가하고 `SetPropertyBlock(block, materialIndex)`로 알파를 주는 방식, 또는 URP Render Objects(레이어 기반)로 저작 비용을 줄일 수 있다. 아이템 수가 적으면 현재 방식 유지.

## 다음 할 일
1. 1번 MeshReveal 수정 적용 여부 확인 → 적용 시 Play에서 "fade-out 중 재스캔" 검증.
2. 스캔 대상 레이어 생성·지정(2번).
3. 5번 미사용 옵션 정리 여부를 사용자와 결정(적 몸체 등에 쓸 계획이 있으면 유지).
