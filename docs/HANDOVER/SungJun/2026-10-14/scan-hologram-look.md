# 스캔 벽 뒤 아이템·상호작용 외곽선 홀로그램화 (2026-10-14)

관련: [intent-019](../intent/intent-019-scan-system.md)

## 작업 요약 및 방법
- 요청: 스캔 시스템이니 스캔으로 드러나는 것은 모두 홀로그램 느낌으로 맞춘다. 벽 뒤 아이템은 벽 너머 실루엣을 홀로그램처럼, 상호작용 오브젝트는 **테두리만** 홀로그램처럼.
- 신규 `Assets/_Project/Art/Scan/ScanHologramEffects.hlsl`: 두 셰이더가 함께 쓰는 연출 함수
  - `ScanLines`: 화면에 고정된 가로줄(주기 px, 어두운 줄 배율). 화면 고정이라 얇은 가로 외곽선이 줄 사이를 오가며 통째로 깜빡이지 않는다.
  - `ScanSweep`: 월드 높이를 따라 위로 지나가는 밝은 띠(간격 m, 폭 m, 속도 m/s).
  - `ScanFlicker`: 초당 rate 번 난수를 굴려 20% 확률로 strength 만큼 어두워지는 깜빡임.
- 신규 `ScanSilhouetteHologram.shader`(`Project/ScanSilhouetteHologram`): ZTest Greater(벽에 가려진 부분만) + 가산 블렌드. 옅은 면(`_FillAlpha`) + 가장자리 빛(rim) + 지나가는 띠를 더하고 가로줄·깜빡임을 곱한다.
  - `M_ScanSilhouette`의 셰이더를 SG_ScanSilhouette(셰이더 그래프) → 이 셰이더로 교체. 머티리얼을 그대로 쓰므로 씬·프리팹 참조는 바뀌지 않는다. 색은 가산용 HDR 초록 (0.3, 1.2, 0.6, 1).
- `ScanOutline.shader`: Mask 패스는 그대로, Outline·Glow 패스에만 `HologramColor`(지나가는 띠에서 밝아짐 × 가로줄 × 깜빡임) 적용. Varyings에 positionWS 추가, 공용 정점 함수 `ExtrudedVert`.
  - 3px 외곽선만으로는 가로줄이 거의 안 보여 `M_ScanOutline`의 빛 번짐을 넓힘: `_GlowWidth` 9 → 14px, `_GlowStrength` 0.35 → 0.5, `_LineDim` 0.15(셰이더 기본값도 0.15).
- `SilhouetteReaction` 주석 2줄만 갱신(SG_ScanSilhouette 언급 제거, "단색" → "홀로그램"). 코드 변경 없음.

## 결정 사항
- 색은 기존 용도별 색을 유지한다(벽 뒤 아이템 초록, 상호작용 주황, 적 셸 파랑). 홀로그램 "느낌"은 공통 연출 함수(가로줄·띠·깜빡임)로 통일한다.
- 상호작용은 외곽선 바깥 빛 번짐 띠에 가로줄이 보이도록 번짐을 넓혀 홀로그램 느낌을 낸다. 오브젝트 면은 그대로 둔다.

## 검증
- 두 셰이더 메시지 0, 스크립트 컴파일 에러 0.
- TestScene Play에서 `Item_Behind_Wall`, `Marker_Interact`의 `SilhouetteReaction`을 직접 켜고(OnScanBegin + OnFade(1)) 임시 카메라로 캡처: 벽 너머 초록 홀로그램(가로줄·rim·띠), 큐브 주황 외곽선 + 가로줄 빛 번짐 확인. 띠는 속도 0으로 고정해 위치 확인 후 값 복원.

## 현재 상태 및 이슈
- 실제 R키 스캔 파동으로 페이드 인/아웃까지 본 것은 아님(반응을 직접 호출해 확인).
- `SG_ScanSilhouette.shadergraph`는 이제 어느 머티리얼도 쓰지 않는다. 삭제 지시가 없어 남겨 둠.
- 깜빡임·띠는 `_Time` 기준이라 모든 대상이 동시에 깜빡인다.

## 다음 할 일 (Next Steps)
- 실제 스캔으로 페이드 포함 확인 후 띠 속도·깜빡임 세기 튜닝.
- 사용하지 않는 `SG_ScanSilhouette.shadergraph` 삭제 여부 결정.
