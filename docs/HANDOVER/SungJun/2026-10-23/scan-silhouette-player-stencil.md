# 스캔 실루엣이 플레이어 위에 그려지는 문제 수정 (intent-072, 해결)

## 1. 작업 요약 및 방법
- 원인: 벽 뒤 아이템 실루엣 셰이더 `Project/ScanSilhouetteHologram`(`M_ScanSilhouette`, 초록 HDR)이 `ZTest Greater`라서 앞을 가린 것이 벽이든 플레이어든 그린다. 3인칭이라 플레이어가 아이템 앞에 자주 낀다.
- 플레이어 메시 4개(MikeZ, RadicalBuster ×2, PowerPod)는 Default 레이어이고 루트만 Player 레이어다. RenderObjects는 게임오브젝트 레이어로만 거르므로 쓸 수 없다. 사용자 지시로 Player.prefab은 고치지 않고 렌더링 레이어로 고르는 방식을 택했다.
- 추가·변경:
  - `Art/Scan/PlayerStencilMask.shader`(`Hidden/Project/PlayerStencilMask`): ColorMask 0, ZWrite Off, ZTest LEqual, 스텐실 128 Replace (WriteMask 128).
  - `Scripts/Core/Scanning/PlayerStencilRendererFeature.cs`: 렌더 그래프 패스로, 렌더링 레이어 `PlayerStencil`을 가진 불투명 렌더러를 위 셰이더(덮어쓰기 머티리얼)로 `AfterRenderingOpaques`에 다시 그린다. 정적 `MarkRenderers(GameObject)`는 하위 렌더러의 `renderingLayerMask`에 비트를 더한다.
  - `GameManager.CompletePlayerSpawn`: `SetCurrentPlayer` 직후 `PlayerStencilRendererFeature.MarkRenderers(spawnedPlayerFacade.gameObject)` 한 줄 (+ `using Core.Scanning`).
  - `ScanSilhouetteHologram.shader`: `Stencil { Ref 128 ReadMask 128 Comp NotEqual }`.
  - `ProjectSettings/TagManager.asset`: 렌더링 레이어 8번 `PlayerStencil`.
  - `Assets/Settings/PC_Renderer.asset`: 렌더러 기능 `PlayerStencil` 등록 (셰이더 연결, 이벤트 AfterRenderingOpaques).
- 확인 (TestScene Play): 컴파일 0에러. 스폰된 플레이어 렌더러 마스크 257(Default + PlayerStencil). `Item_Behind_Wall`을 플레이어 뒤로 옮기고 실루엣을 켜고 임시 벽(Play 중 생성, 저장 안 함)을 옆에 세워 카메라 렌더 캡처. 기능 끔 → 플레이어 머리와 벽에 초록. 기능 켬 → 벽에만 초록.

## 2. 결정 사항
- 스텐실 비트는 128. `ScanOutline`이 64를 쓰므로 Read/WriteMask로 서로 건드리지 않는다.
- 렌더링 레이어 비트는 런타임에 스폰마다 `GameManager`에서 더한다. 스폰(새 게임·재스폰·씬 이동)이 모두 `CompletePlayerSpawn`을 지나기 때문이다.
- 기능은 불투명 큐만 그린다. 렌즈(투명)·스캔 파동 파티클·VFX도 비트는 받지만 스텐실은 찍히지 않는다.

## 3. 현재 상태 및 이슈
- Mobile_Renderer에는 등록하지 않았다 (현재 품질 PC). 모바일 품질에서는 예전처럼 플레이어 위에 실루엣이 보인다.
- 실제 스캔 스킬(Q)로 조작한 확인과 맵 씬 확인은 하지 않았다. 렌더러 설정은 모든 씬에 적용된다.
- 펫이나 적이 아이템 앞을 가리면 여전히 그 위에 실루엣이 보인다 (요청 범위 밖).
- 커밋하지 않았다.

## 4. 다음 할 일 (Next Steps)
- 실제 플레이에서 Q 스캔으로 벽 뒤 아이템이 벽에는 보이고 플레이어 몸 위에는 안 보이는지 확인한다.
- 필요하면 펫·적도 `MarkRenderers`로 같은 비트를 주는 방안을 검토한다.
