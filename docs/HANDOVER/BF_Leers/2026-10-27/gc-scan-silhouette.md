# GC·도둑 스캔 실루엣 (2026-10-27)

## 1. 작업 요약 및 방법
- 요청: GC 가 스캔됐을 때 숫자 퍼즐 아이템(`MazePuzzleItem`)처럼 보이게만 하고, 다른 Enemy 처럼 약점은 보이지 않게.
- 숫자 퍼즐 아이템 구성: 루트 Scan 태그 + `ScanTarget` + `SilhouetteReaction`(`M_ScanSilhouette` 머티리얼, 벽에 가려진 부분만 그림 `ZTest Greater`).
- `GC.prefab` 루트에 같은 구성을 추가(Unity MCP `PrefabUtility.LoadPrefabContents`):
  - 태그 `Untagged` → `Scan`
  - `ScanTarget`(기본값, 판정 콜라이더는 자동 = CharacterController)
  - `SilhouetteReaction._autoShellMaterial = M_ScanSilhouette`(GC 는 SkinnedMeshRenderer 라 셸 자동 생성 사용, `_shellSourceRoot` 비움 = 루트)
- `WeakPointSelector`·`WeakPointReaction`·`WeakPointShellMask` 는 붙이지 않음. 코드 변경 없음.
- 실행자: Claude 직접(Codex CLI 미설치).

- 후속 요청: 도둑(`Thief.prefab`)도 같은 방식으로 추가. 루트 태그 `Untagged` → `Scan`, `ScanTarget`, `SilhouetteReaction._autoShellMaterial = M_ScanSilhouette`. 렌더러는 `Robot_Enemy_Prefab/Body` SkinnedMeshRenderer 1개, 약점 컴포넌트 없음.

## 2. 결정 사항
- 약점 없는 캐릭터의 스캔 표시는 루트에 `ScanTarget` + `SilhouetteReaction(_autoShellMaterial)` 만 붙인다.

## 3. 현재 상태 및 이슈
- Play(`Map_4_Security`)에서 GC 를 생성해 확인: `ScanTarget.Registered` 등록, 셸 렌더러 1개(`M_ScanSilhouette`) 생성·평소 꺼짐, `OnScanned` 후 켜짐, 약점 컴포넌트 0.
- 도둑도 Play 에서 같은 항목 확인(등록, 셸 1개 `M_ScanSilhouette`, 스캔 시 켜짐, 약점 0). 두 프리팹 모두 씬 직접 배치 없이 각 Spawner 프리팹으로만 생성.
- 셸 자동 생성 경로는 매끄러운 법선 보정(MeshFilter 전용)을 거치지 않음 — 퍼즐 아이템과 외곽 모양이 조금 다를 수 있음.
- **실제 Q 스캔 육안 확인·게이트 루프 미실행, 커밋 전.**

## 4. 다음 할 일
- 사용자 육안 확인: GC·도둑을 벽 너머에서 스캔했을 때 퍼즐 아이템과 같은 실루엣으로 보이는지.
