# 스캔 상호작용 외곽선 (2026-10-14)

관련: [intent-019](../intent/intent-019-scan-system.md)

## 작업 요약 및 방법
- **(정정, 같은 날) 외곽선은 상호작용 오브젝트 전용이다. 벽 뒤 아이템은 원래대로 초록 실루엣(`M_ScanSilhouette`)으로 벽 너머에서 보여야 한다(사용자 정정).** TestScene `Item_Behind_Wall/Silhouette` 머티리얼을 `M_ScanSilhouette`로 되돌렸고, Play에서 벽 앞에서 초록 실루엣이 보이는 것을 확인했다. 아래 본문의 "벽 뒤 아이템에 외곽선 적용" 내용은 이 정정으로 대체된다. `SilhouetteReaction` 요약 주석·툴팁, 셰이더 머리 주석도 두 용도로 고쳤다.
- 요청: 벽 뒤 아이템이 통째로 보이지 말고 외곽선(실루엣)만 보였으면 좋겠다. 상호작용 아이템도 마커와 함께 외곽선이 뜨게 해 달라(위쳐식 주황 테두리 참고 이미지).
- 사용자 결정: **벽에 가려지면 마커·외곽선 모두 안 보이고, 가려지지 않았을 때만 외곽선이 보인다.**
- 신규 `Assets/_Project/Art/Scan/ScanOutline.shader`(`Project/ScanOutline`, HLSL, 패스 2개)
  - `Mask`(SRPDefaultUnlit): 원래 메시를 ColorMask 0 + ZTest LEqual로 그려, 대상이 실제로 보이는 픽셀에만 스텐실(`_StencilRef`, 기본 64)을 찍는다.
  - `Outline`(UniversalForward): 법선 방향으로 **화면 픽셀 단위**(`_OutlineWidth`, 기본 3px) 부풀린 앞면을 스텐실이 없는 곳에만 그린다 → 바깥 테두리만 남는다. ZTest LEqual이라 벽에 가려지면 안 보인다.
  - 바닥에 닿은 아랫변이 바닥 깊이에 묻히지 않게 정점을 카메라 쪽으로 `_DepthPush`(기본 0.1m) 당긴다. (처음엔 뒷면 컬링 방식이었으나 아랫변이 사라져 이 방식으로 바꿨다.)
- 신규 `Assets/_Project/Art/Scan/M_ScanOutline.mat`: 색 (1, 0.6, 0.1, 1), 두께 3px.
- `Core.Scanning.SilhouetteReaction` 수정
  - Awake에서 MeshFilter 실루엣 렌더러의 메시를 **매끄러운 법선 복제본**으로 바꾼다(`GetSmoothNormalMesh`, 같은 위치 정점의 법선 평균). 상자처럼 각진 메시는 모서리 정점 법선이 갈라져 외곽선이 끊기기 때문이다. 원본 메시 → 복제본을 static 사전에 캐시해 같은 아이템끼리 공유한다.
  - Read/Write가 꺼진 메시는 경고를 남기고 원본을 쓴다(빌드에서 모서리가 끊길 수 있음).
  - SkinnedMeshRenderer(적 홀로그램 셸)는 MeshFilter가 없어 영향 없음.
  - 요약 주석·툴팁을 외곽선 기준으로 갱신(`M_ScanSilhouette` → `M_ScanOutline`).

## 배선 방법 (아이템·상호작용 공통)
1. 대상에 `ScanTarget` + `SilhouetteReaction`을 붙인다(상호작용은 기존 `MarkerReaction` 옆에 추가).
2. 자식 오브젝트를 만들어 같은 메시의 `MeshFilter` + `MeshRenderer`(머티리얼 `M_ScanOutline`, 그림자 Off)를 둔다. 콜라이더는 두지 않는다.
3. `SilhouetteReaction._silhouetteRenderers`에 그 렌더러를 연결한다.
- 아이템 모델 메시는 Read/Write Enabled를 켜야 빌드에서도 모서리가 이어진다.

## TestScene 적용 (Git 제외, `Assets/Test/.../TestScene.unity`)
- `ScanTest/Marker_Interact`(큐브): 자식 `Outline`(Cube, `M_ScanOutline`, 그림자 Off) + `SilhouetteReaction` 추가. `ScanTarget._reactions`는 비어 있어 자동 수집된다.
- `ScanTest/Item_Behind_Wall/Silhouette`: 머티리얼 `M_ScanSilhouette` → `M_ScanOutline`.
- **씬은 저장하지 않았다**(작업 전부터 dirty 상태였음). 사용자가 확인 후 저장.

## 결정 사항
- 아이템·상호작용 표시는 외곽선(`M_ScanOutline`), 적은 홀로그램 셸(`M_ScanHologram`)로 둘 다 `SilhouetteReaction`을 쓴다. 새 반응 스크립트는 만들지 않았다.
- 외곽선은 화면 픽셀 두께로 고정한다(멀리 있어도 같은 두께).
- 마커(`Sprite-Unlit-Default`)는 원래 깊이 테스트를 하므로 수정하지 않았다.
- `M_ScanSilhouette`/`SG_ScanSilhouette`(가려진 부분 채우기)는 지우지 않고 남겨 두었다.

## 현재 상태 및 이슈
- 컴파일 에러 0, 셰이더 에러 0. Play에서 `ScanWave.Begin`을 eval로 호출하고 임시 카메라로 캡처해 확인:
  - 큐브: 4변(아랫변 포함) 모서리 끊김 없이 주황 외곽선, 안쪽은 원래 외형, 마커 함께 표시.
  - 벽 앞에서 본 `Item_Behind_Wall`: 아무것도 안 보임. 벽 반대편(가려지지 않음)에서 보면 구 외곽선만 보임.
  - 임시 가림막 뒤의 마커: 켜져 있고 알파 1이지만 화면에 안 보임.
- R키 실제 입력·플레이어 카메라 시점 확인은 안 함.
- `_DepthPush`(0.1m)보다 얇게 대상에 붙은 벽은 외곽선이 살짝 비칠 수 있다.
- 외곽선끼리 겹치면 앞 대상의 스텐실 영역에서 뒤 대상 외곽선이 잘린다(의도된 동작에 가까움).
- 참고 이미지 같은 부드러운 번짐(글로우)은 없다. 필요하면 블룸 또는 화면 후처리 방식이 필요하다.

## 추가: 외곽선 빛 번짐
- 요청: 더 눈에 잘 보이게 빛이 나게. 사용자 선택: 화면 후처리(Render Feature) 대신 **셰이더 번짐 층**.
- 확인: TestScene·맵 씬에는 전역 블룸이 없다(TestScene은 VFX 미리보기용 국소 블룸 `VFX_LocalBloom`만 있음). HDR 색만으로는 번지지 않는다.
- `ScanOutline.shader`에 3번째 패스 `Glow`(LightMode `UniversalForwardOnly`)를 추가했다. 외곽선보다 넓게(`_GlowWidth`, 기본 9px) 부풀린 메시를 스텐실 바깥에만 가산(`_GlowStrength`, 기본 0.35)으로 덧그린다. 외곽선 위에도 더해져 테두리가 가장 밝고 바깥 띠는 옅다(2단계).
- 부풀리기 계산은 `ExtrudedPositionCS`로 묶어 Outline·Glow가 함께 쓴다.
- `_Color` 기본값과 `M_ScanOutline`을 HDR (1.4, 0.75, 0.12)로 올렸다. 블룸이 없는 씬에서는 1로 잘려 노란빛 주황 선으로 보이고, 블룸이 있는 씬에서는 더 번진다.
- URP 패스 태그 3개(SRPDefaultUnlit, UniversalForward, UniversalForwardOnly)를 모두 써서 같은 머티리얼로는 번짐 단계를 더 늘릴 수 없다. 더 부드러운 번짐이 필요하면 Render Feature 방식으로 가야 한다.
- 셰이더 에러 0. Play 캡처로 확인: 큐브·구 모두 선 + 바깥 옅은 빛 띠, 벽 앞에서 본 벽 뒤 아이템은 번짐까지 포함해 안 보임.

## 다음 할 일 (Next Steps)
1. TestScene 저장 후 R키로 플레이어 시점 확인, 색·두께·`_DepthPush` 튜닝.
2. 미로맵 아이템·상호작용 프리팹에 외곽선 자식 + `SilhouetteReaction` 적용, 모델 메시 Read/Write 확인.
3. 사용하지 않게 된 `M_ScanSilhouette`/`SG_ScanSilhouette` 정리 여부 결정.
