# 숨은 플랫폼 홀로그램 연출 — 2026-10-14

관련 intent: [intent-019](../intent/intent-019-scan-system.md) (추가 2026-10-14 "숨은 플랫폼 홀로그램 연출")

## 1. 작업 요약 및 방법
- 숨은 플랫폼(`MeshRevealReaction`)이 평소에 렌더러를 꺼서 완전히 사라지던 것을 세 단계 홀로그램 연출로 바꿨다.
  1. **평소**: 형태가 아주 희미하게 보인다(카메라 2m 이내 2.5%, 10m 이상 0.5%, 모서리만 2배). 가까이 가야 윤곽이 겨우 보이고 멀리서는 거의 안 보인다.
     - (같은 날 정정) 처음엔 5~15%(3m~15m) + 위로 흐르는 데이터 노이즈 칸이었으나, 스캔 전에 너무 잘 보인다는 피드백으로 값을 낮추고(5%/1% → 다시 요청으로 2.5%/0.5%) 노이즈 칸을 셰이더·머티리얼에서 완전히 뺐다.
  2. **스캔이 닿음**: 파동 앞면에 밝은 선이 지나간다. 앞면에서 `_FillBand`(1.5m) 안쪽으로 모서리 → 격자(0.5m 간격) → 면 순서로 채워진다. 격자는 생길 때 밝았다가 홀로그램 세기로 내려간다.
  3. **다 드러남**: 반투명 홀로그램(면 20% + 격자 + 빛나는 모서리 + 가장자리 빛)으로 유지된다. 다 드러난 순간부터 0.5초 동안 최대 4배 밝기로 빛났다가 원래대로 돌아온다.
- 신규 셰이더 `Assets/_Project/Art/Scan/ScanPlatformHologram.shader`(`Project/ScanPlatformHologram`, Transparent, 뒷면 → 앞면 2패스, 그림자 없음) + `M_ScanPlatformHologram.mat`.
- `MeshRevealReaction` 수정: 메시 전체가 다 드러난 순간의 `Time.time`을 `_ScanRevealTime`으로 넘긴다. 다 드러나지 않았거나 사라지기 시작하면 -1이다. 바뀔 때만 쓴다(`SetFullyRevealed`). `RestoreProperties`도 이 값을 머티리얼 기본값으로 되돌린다.
- TestScene(`Assets/Test`, Git 제외) `Reveal_Platform`: 머티리얼 `M_ScanPlatformHologram` 1개, `_shouldHideWhenIdle = false`. **씬은 저장하지 않았다.**
- 같은 날 먼저 만든 배경 굴절 은신(`ScanCloak.shader`, `M_ScanCloak`, `M_ScanRevealHidden`)은 플레이어가 올라서면 주변까지 일렁여 폐기·삭제했다(커밋 전).

## 2. 결정 사항
- 단계 판단은 픽셀마다 "파동 앞면이 지나간 거리"(`_ScanRadius - 거리`)로 한다. 완료 여부만 C#이 알려 준다. `MeshRevealReaction`은 반경을 메시의 가장 먼 점에서 멈추므로, 셰이더만으로는 먼 쪽 끝이 "채워짐"에 도달하지 못하기 때문이다.
- 셰이더 `_Time.y`는 URP에서 Play 중 `Time.time`이다(`ScriptableRenderer.cs`). 그래서 C#은 `Time.time`을 넘긴다.
- 모서리는 오브젝트 공간 상자(`_BoxHalfSize`, 기본 (0.5, 0.5, 0.5) = 유니티 기본 Cube)의 모서리까지 거리(m)로 판단한다. 격자는 오브젝트 축 기준 미터 좌표라 플랫폼이 움직여도 따라가고 스케일에 늘어나지 않는다.
- (같은 날 정정) 모서리·격자 두께를 처음엔 화면 픽셀 기준(UV 테두리 + `fwidth`)으로 했으나, 면이 비스듬히 보이는 각도에서 면 전체가 모서리·선으로 판정돼 **화면을 돌릴 때마다 번쩍였다.** 두께를 미터 기준으로 바꾸고, 한 픽셀이 선보다 넓어지면 번지는 만큼 어둡게 하는 `LineMask`로 해결했다. 완료 발광은 원래대로 한 번만 난다.
- 평소 상태는 머티리얼 기본값(`_ScanRadius` -1, `_ScanRevealTime` -1)이다. 그래서 `_shouldHideWhenIdle`을 끈 플랫폼은 Awake·스캔 종료 후 자동으로 평소 상태가 된다.

## 3. 현재 상태 및 이슈
- 컴파일 에러 0, 셰이더 에러 0.
- 단계별 화면은 반응 함수 직접 호출과 PropertyBlock 값으로 만들어 캡처해 확인했다: 평소(약 4m 윤곽만 옅게, 약 12m 거의 안 보임, 플레이어 주변 일렁임 없음), 채워지는 중(왼쪽 채워짐/오른쪽 앞면), 완료 순간 발광, 발광 후 홀로그램.
- **실제 시간 흐름으로는 보지 못했다.** 에디터가 백그라운드일 때 Play 시간이 멈춰 있었다. 실제 R키 스캔으로 채워지는 속도·발광 시간·페이드아웃을 확인해야 한다.
- 파동(약 5m/s)과 `_revealSpeed`(10m/s)가 빨라 3m 플랫폼은 1초 안에 다 채워진다. 채워지는 느낌이 부족하면 `_FillBand`를 늘리거나 `_revealSpeed`를 낮춘다(파동이 더 빠르면 파동을 따라가는 점 주의).
- 상자가 아닌 메시나 피벗이 중심이 아닌 메시는 모서리 빛이 맞지 않는다. 크기가 다른 메시는 `_BoxHalfSize`를 메시 절반 크기로 맞춘다.
- 회전 번쩍임 수정 확인: 카메라를 플랫폼 주위로 15°씩 돌리며(높이 1.2m/3.5m) 플랫폼이 더한 밝기를 측정해 튀는 각도가 없음을 확인했다.
- Play 중 스크립트를 다시 컴파일하면 `_propertyBlock`이 비어 `GetPropertyBlock` 예외가 난다(도메인 리로드 시 Awake 미호출, 기존 구조 그대로). Play를 다시 시작하면 정상이다.

## 4. 다음 할 일 (Next Steps)
- 실제 플레이에서 R키 스캔으로 전체 흐름을 보고 값(`_IdleAlpha*`, `_IdleNear/FarDistance`, `_FillBand`, `_HoloAlpha`, `_Flash*`)을 튜닝한다.
- TestScene `Reveal_Platform` 변경을 저장하거나 실제 맵 플랫폼에 같은 구성(머티리얼 + `_shouldHideWhenIdle` 끔)을 적용한다.
