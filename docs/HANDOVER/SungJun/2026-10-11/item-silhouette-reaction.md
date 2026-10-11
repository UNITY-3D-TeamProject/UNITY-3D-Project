# 아이템 벽 너머 실루엣 반응 추가 (2026-10-11)

관련: [intent-019](../intent/intent-019-scan-system.md)

## 작업 요약 및 방법
- 스캔 4번 기능("평소 보이는 아이템이 스캔 중 벽 뒤로도 보임")이 사실상 누락되어 있었다.
  - 기존 가이드는 아이템에 `MeshRevealReaction` + `M_ScanRevealXRay`를 쓰라고 했는데, 이렇게 하면 평소에 아이템이 숨겨지거나(`_shouldHideWhenIdle`), 끄면 원래 외형을 잃고 스캔과 무관하게 항상 벽 너머로 보인다(스캔 종료 후 `_ScanRadius`가 머티리얼 기본값 100으로 복원 + ZTest Always).
- A안(자식 실루엣 오버레이)으로 새로 구현했다.
  - `Assets/_Project/Art/Scan/SG_ScanSilhouette.shadergraph` (`Shader Graphs/SG_ScanSilhouette`): URP Unlit, Transparent, Depth Write Off(강제), **Depth Test Greater**, 그림자 Off → 가려진 부분만 `_Color` 단색 반투명으로 그린다. 노드: Color 프로퍼티(`_Color`, HDR) → Base Color, Split(A) → Alpha.
    - 처음엔 HLSL(`ScanSilhouette.shader`)로 만들었으나, 다른 스캔 셰이더와 맞추고 아티스트가 노드로 수정할 수 있도록 Shader Graph로 교체하고 HLSL은 삭제했다(사용자 결정).
    - 그래프는 `TerrainScanner_tut_unlit.shadergraph`를 템플릿으로 필요한 노드만 남겨 생성했다. 프로퍼티 Reference 이름 `_Color`는 `SilhouetteReaction`이 쓰므로 바꾸면 안 된다.
  - `Assets/_Project/Art/Scan/M_ScanSilhouette.mat`: 위 셰이더 그래프, 기본색 (1, 0.8, 0.2, 0.6).
  - `Core.Scanning.SilhouetteReaction`: Inspector에 연결한 실루엣 렌더러를 스캔 중에만 켜고, `OnFade`에 맞춰 `MaterialPropertyBlock`으로 `_Color` 알파를 줄인다. 원래 렌더러는 건드리지 않는다. 자동 탐색은 원래 렌더러까지 잡히므로 하지 않는다.
- `MeshRevealReaction` 요약 주석에서 "아이템"을 빼고 `SilhouetteReaction`을 안내하도록 1줄 수정.

## 아이템 배선 방법
1. 아이템(콜라이더 있음, 레이어는 `ScanWave._scannableLayers`에 포함)에 `ScanTarget` + `SilhouetteReaction`을 붙인다.
2. 아이템 메시 오브젝트를 복제해 자식으로 두고(같은 Transform), `MeshRenderer` 머티리얼을 `M_ScanSilhouette`로, 그림자 캐스팅은 Off로 한다. 복제본의 콜라이더는 지운다.
3. `SilhouetteReaction._silhouetteRenderers`에 그 렌더러를 연결한다.
- 색을 바꾸려면 `M_ScanSilhouette`를 복제해 `_Color`만 바꾼다.

## 결정 사항
- 아이템은 `SilhouetteReaction`, 숨은 플랫폼은 `MeshRevealReaction`으로 역할을 나눈다.
- B안(런타임 머티리얼 추가), C안(URP Render Objects + 레이어 전환)은 머티리얼 인스턴스·서브메시 처리, 줍기/물리 레이어 영향 때문에 채택하지 않음.
- 실루엣 색은 머티리얼 `_Color`로 정한다(별도 Inspector 색 필드 없음).
- 상호작용 마커(`MarkerReaction`)는 벽 뒤로 보이지 않아도 된다(사용자 결정). 깊이 테스트를 끈 스프라이트 머티리얼은 만들지 않는다.

## 현재 상태 및 이슈
- Unity 재컴파일 성공, 콘솔 에러 0, 셰이더 그래프 임포트 에러 없음(`ShaderHasError` false).
- Shader Graph 교체 후 Play에서 재확인: 벽에 가려지면 노란 실루엣, 가려지지 않으면 실루엣이 켜져 있어도 원래 회색 구 그대로(씬 뷰 캡처 2장).
- TempScene `ScanTest` 아래에 시험 오브젝트 추가(씬은 Git 제외): `Wall_Item`(10, 2, -9) + 그 뒤 `Item_Behind_Wall`(구, 10, 1, -11, 기본 Lit) + 자식 `Silhouette`(같은 메시, `M_ScanSilhouette`, 그림자 Off, 콜라이더 없음). `ScanTarget` + `SilhouetteReaction` 연결.
- Play 모드에서 eval로 `ScanWave.Begin` 호출해 확인: 평소 실루엣 꺼짐 → 스캔 시 켜지고 알파 0.6 → 유지 후 꺼짐/알파 0, 아이템 원래 렌더러는 계속 켜짐. 씬 뷰 캡처로 벽에 가려진 구가 노란 실루엣으로 보이는 것 확인.
- **씬 저장은 하지 않았다.** 작업 전부터 TempScene이 저장 안 된 상태(dirty)라 사용자 확인 후 저장한다.
- R키 실제 입력과 플레이어 카메라 시점 확인은 미실시.
- 오목한 메시는 자기 자신에 가려진 부분에도 실루엣이 보일 수 있다(ZTest Greater 특성).
- `2026-10-10/scan-system-implementation.md`의 "아이템 = MeshRevealReaction + XRay" 가이드는 과거 기록이라 그대로 두었다. 이 문서가 대체한다.

## 추가: 약점 시험 연결
- TempScene의 `Enemy_WeakPoint`는 `SetWeakPoint`를 호출하는 쪽(enemy 파트)이 없어 `WeakPoint_Head`가 처음부터 켜져 있었다.
- 시험용 `Assets/Test/UnityAssets/Script/Scan/WeakPointTestBinder.cs`(Git 제외)를 만들어 `Start`에서 `SetWeakPoint(WeakPoint_Head)`를 대신 호출하게 하고 `Enemy_WeakPoint`에 붙였다.
- Play(eval)로 확인: 시작 시 꺼짐 → 스캔 중 켜짐 → 유지 시간 뒤 꺼짐.
- Play 확인 중 게임 루프가 멈춰 `runInBackground`를 잠시 켰다가 `PlayerSettings`까지 원복했다(ProjectSettings 변경 없음).
- `WeakPoint_Head`에는 삭제된 `WeakPoint` 스크립트의 Missing 컴포넌트가 남아 있다("referenced script is missing" 경고 원인). 손대지 않았다.

## 추가: 상호작용 마커 스프라이트
- 시험 마커가 Unity 내장 `Knob`(원) + `Sprite-Unlit-Default`라서, 마름모 마커 스프라이트 `Assets/_Project/Art/Scan/T_ScanMarker.png`(256px, PPU 400, 주황 테두리 + 노란 안쪽 마름모 + 글로우, 색을 이미지에 직접 넣음)를 Unity eval로 생성했다.
- TempScene `Marker_Interact/Marker`에 적용하고 SpriteRenderer 색을 흰색으로 바꿨다(이미지 색 그대로 보이게). 머티리얼은 기본 `Sprite-Unlit-Default` 유지.
- 글로우는 알파 블렌딩이라 밝은 배경에서는 옅게 보인다.

## 추가: 시험 씬 표지판 재배치 (TempScene, Git 제외)
- `Sign_스캔`(0, -12)은 그대로 두고, 그 뒤 z=-18에 반응별 표지판 4개를 `Sign_스캔`을 복제해 만들었다(`Signs` 아래, 글자가 길면 Board 폭 확장).
- 플레이어 쪽(+z)에서 보면 왼쪽부터 순서대로, 각 표지판 뒤(z=-23~-25)에 시험 오브젝트를 옮겼다.

| 표지판 | 위치 | 시험 오브젝트 |
|---|---|---|
| `Sign_약점` | (15, -18) | `Enemy_WeakPoint` (15, -23) |
| `Sign_숨은 플랫폼` | (5, -18) | `Reveal_Platform` (7, -23), X-Ray 변형 `Wall` (3, -23) + `Hidden_Behind_Wall` (3, -25) |
| `Sign_벽 뒤 아이템` | (-5, -18) | `Wall_Item` (-5, -23) + `Item_Behind_Wall` (-5, -25) |
| `Sign_상호작용` | (-15, -18) | `Marker_Interact` (-15, -23) |

- 시험 스캔 반경은 15m(`ScanTestTrigger._size` 30)라서, 각 반응 표지판 근처에서 R키를 누르면 된다.
- 옛 프로토타입 `Scan_Reveal_Object`(3.82, -15, 루트)는 손대지 않았다. 스캔하면 `Sign_숨은 플랫폼` 앞쪽에 드러난다.

## 다음 할 일 (Next Steps)
1. TempScene 저장 후 R키로 플레이어 시점에서 확인(뒤로 돌아 x=10 쪽 벽).
2. 실루엣 색/알파 튜닝.
3. 미로맵 아이템 프리팹에 적용.
