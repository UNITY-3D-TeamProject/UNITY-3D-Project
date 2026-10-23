---
id: intent-072
title: 벽 너머 아이템 스캔 실루엣이 플레이어 몸 위에도 그려짐
part: Core / Scanning / Rendering
status: resolved
created: 2026-10-23
resolved: 2026-10-23
---

## 문제 (Problem)
벽 뒤 아이템에 씌우는 초록 홀로그램 실루엣(`Project/ScanSilhouetteHologram`, `M_ScanSilhouette`)이, 카메라와 아이템 사이에 플레이어가 끼면 플레이어 몸 위에도 그려진다.
셰이더가 `ZTest Greater`(무언가에 가려진 픽셀만 그림)라서, 아이템 앞을 가린 것이 벽인지 플레이어인지 구분하지 못한다. 3인칭 카메라라 자주 겹친다.

## 기대 결과 (Proposed outcome)
- 플레이어가 그려진 픽셀에는 실루엣이 그려지지 않는다. 벽에 가려진 부분은 지금처럼 보인다.
- 플레이어 불투명 메시를 그리는 자리에 스텐실 비트를 찍는 렌더러 기능을 만든다. 대상은 렌더링 레이어로 고른다.
- 실루엣 셰이더는 그 비트가 있는 픽셀을 건너뛴다.

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 스캔 스킬을 쓰는 플레이어
- 어떤 시스템/모듈이 건드려지는가:
  - `Assets/_Project/Art/Scan/ScanSilhouetteHologram.shader` (Stencil 검사 추가)
  - 새 렌더러 기능 `Core.Scanning.PlayerStencilRendererFeature` (`Scripts/Core/Scanning/`)
  - `Core.GameManager.CompletePlayerSpawn` (스폰된 플레이어 렌더러에 렌더링 레이어 추가, 한 줄 호출)
  - `ProjectSettings/TagManager.asset` (렌더링 레이어 `PlayerStencil` 추가)
  - `Assets/Settings/PC_Renderer.asset` (렌더러 기능 등록)

## 제약 (Constraints)
- `Player.prefab`은 수정하지 않는다 (사용자 지시). 플레이어 메시 4개는 Default 레이어라 게임오브젝트 레이어로는 걸러지지 않으므로, 런타임에 렌더링 레이어 비트를 더한다.
- 코드는 `Scripts/Core`에만 둔다. 맵 씬은 열거나 고치지 않고 TestScene에서 확인한다.
- 스텐실 비트는 `ScanOutline`이 쓰는 64와 겹치지 않게 128을 쓴다.

## 열린 질문 (Open questions)
- 없음. Mobile_Renderer에는 등록하지 않는다 (현재 품질은 PC). 모바일 품질을 쓰게 되면 같은 기능을 등록해야 한다.

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
  - `Hidden/Project/PlayerStencilMask`(색·깊이 안 씀, 스텐실 128 Replace, WriteMask 128) 셰이더를 덮어쓰기 머티리얼로, `PlayerStencilRendererFeature`가 렌더링 레이어 `PlayerStencil`(인덱스 8, 마스크 256)을 가진 불투명 렌더러를 `AfterRenderingOpaques`에 다시 그린다 (렌더 그래프).
  - `GameManager.CompletePlayerSpawn`이 `PlayerStencilRendererFeature.MarkRenderers(spawnedPlayerFacade.gameObject)`로 플레이어 렌더러에 비트를 더한다 (기존 비트 유지). PlayerFacade는 프리팹 루트에 있다.
  - `ScanSilhouetteHologram`에 `Stencil { Ref 128 ReadMask 128 Comp NotEqual }` 추가.
  - `PC_Renderer`에 기능 등록, `TagManager`에 렌더링 레이어 추가. Player.prefab 수정 없음.
  - 확인: 컴파일 0에러, TestScene Play에서 플레이어 렌더러 마스크 257, 아이템을 플레이어 뒤에 두고 실루엣을 켠 화면에서 기능 끔 → 플레이어 머리·임시 벽 양쪽에 초록, 기능 켬 → 임시 벽에만 초록. 실제 스캔 스킬 조작·맵 씬은 미확인.
- 관련 커밋/PR: (커밋 전)
