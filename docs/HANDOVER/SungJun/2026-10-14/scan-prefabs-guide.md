# 스캔 대상 프리팹·예시·세팅 가이드 (2026-10-14)

관련: [intent-019](../intent/intent-019-scan-system.md), [세팅 가이드](../../../scan-setup-guide.md)

## 작업 요약 및 방법
- 팀원이 스캔 대상 4종(숨은 플랫폼, 적 약점, 벽 뒤 아이템, 상호작용)을 직접 배치할 수 있도록 세팅 가이드 `docs/scan-setup-guide.md` 작성. 대상별 구성도·번호 순서·머티리얼 조절값·자주 하는 실수 표.
- 부품 프리팹 신규 (`Assets/_Project/Prefabs/System/Scan/`, 폴더는 사용자가 `Prefabs/Scan` → `Prefabs/System/Scan`으로 옮김)
  - `ScanMarker`: SpriteRenderer(`T_ScanMarker`) + Billboard, 스케일 0.6 (TestScene 마커 복제)
  - `ScanOutline`: MeshFilter(Cube) + MeshRenderer(`M_ScanOutline`, 그림자 Off)
  - `ScanSilhouette`: MeshFilter(Cube) + MeshRenderer(`M_ScanSilhouette`, 그림자 Off)
  - `ScanPlatform`: Cube + BoxCollider + `M_ScanPlatformHologram`(그림자 Off) + ScanTarget + MeshRevealReaction(`_shouldHideWhenIdle` 끔), 태그 Scan
  - `ScanWeakPoint`: SphereCollider(0.2) + ScanTarget + SilhouetteReaction(`_autoShellMaterial` = `M_ScanHologram`) + WeakPointReaction + WeakPointShellMask, 태그 Scan. `_scanColliders`·`_shellSourceRoot`는 몬스터마다 연결
  - `ScanWave`: `Prefabs/Skills`에서 이동(AssetDatabase.MoveAsset, GUID 유지)
- 예시 프리팹 신규 (`Prefabs/System/Scan/Example/`): 위 부품으로 새로 조립. TestScene 오브젝트를 그대로 빼지 않은 이유는 적 예시가 옛 구성 + Git 제외 머티리얼(`Assets/Test/.../M_TestWeakPoint`)을 썼고, 나머지도 부품 프리팹을 쓰지 않았기 때문.
  - `Example_HiddenPlatform`(3×0.5×3), `Example_WallItem`(벽 + 1.3m 뒤 구, +Z 쪽에서 봄), `Example_Interact`(큐브 + 마커 + 테두리)
  - `Example_WeakPoint`: Gripper Robot(ThirdParty FBX) + 루트 CharacterController + `RigHead` 아래 ScanWeakPoint(TestScene 바이저 위치, 반경 0.3). 처음엔 Melee의 Sci-Fi Dog로 만들었으나 사용자 요청으로 교체. 팀원 모두 Gripper Robot 에셋 보유 확인(사용자).
- `Art/Scan` 정리: 쓰지 않는 `M_ScanReveal`, `M_ScanRevealSciFi`, `M_ScanRevealXRay`, `SG_ScanReveal`, `SG_ScanRevealXRay`, `SG_ScanSilhouette`를 `Art/Scan/NotUse/`로 이동(GUID 유지, TestScene 옛 오브젝트 참조 유지). `TerrainScanner_tut_blue`(+ 셰이더 그래프)는 ScanWave 파동이 써서 남김. `ScanHologramEffects.hlsl`은 GUID 참조는 없지만 `#include` 경로로 쓰이므로 남김(옮기면 셰이더 깨짐).

## 결정 사항
- 상호작용·벽 뒤 아이템은 원래 물체에 반응을 붙이고, 자식 부품 프리팹(ScanOutline/ScanSilhouette)의 메시만 물체와 같게 바꾸는 방식. 원래 물체의 렌더러는 Silhouette Renderers에 넣지 않는다(넣으면 평소 꺼짐 + 법선 매끄럽게 바뀜).
- 약점 오브젝트는 항상 켜 둔다. SphereCollider 초기 상태는 상관없음(WeakPointReaction이 Awake에서 끔). 몬스터 코드는 OnEnable에서 위치만 옮긴다.
- 숨은 플랫폼 메시는 상자가 아니어도 된다. 모서리 빛만 `_BoxHalfSize` 상자 기준(가이드 정정).
- 플레이어 파트 확인 사항(사용자 정리 검토): ScanWave를 Scan.prefab 아래 켜진 상태로 두고 `Begin(위치, 지속시간, 최대지름)`만 호출. 지속시간 = 확산 시간 = 대상 활성 시간, 쿨타임은 지속시간 이상, 진행 중 재호출 시 이전 파동은 지워짐. 현재 `Scan.cs`에는 ScanWave 참조 없음.

## 검증
- 각 부품·예시 프리팹을 Play에서 인스턴스화해 확인. 4종 모두 `ScanTarget.Registered` 등록, `IsInWaveRange`(실제 파동 판정 함수) true, 반응 호출 후 캡처로 표시 확인(격자 홀로그램 플랫폼, 벽 너머 초록 구, 주황 테두리 + 마커, 로봇 파란 셸 + 빨간 바이저).
- 사용자 절차대로 원기둥·캡슐로 조립해 테두리·실루엣 확인.

## 현재 상태 및 이슈
- 에디터 포커스가 없으면 Play 게임 시간이 멈춰, 실제 파동 확산·페이드는 끝까지 보지 못함(판정 함수 + 반응 직접 호출로 확인).
- Melee·Ranged 프리팹에는 아직 약점이 없음(몬스터 파트 작업).
- 약점 SphereCollider Is Trigger는 꺼짐(기존 테스트 구성). 피격 판정 방식에 맞춰 몬스터·피격 파트와 결정 필요.
- 약점 빨간색이 가산 블렌드라 분홍빛으로 보임. 더 진하게 하려면 `M_ScanHologram`의 Weak Color (1,0,0), Weak Fill Alpha 0.7~0.9, Weak Intensity 4 제안(미적용).

## 다음 할 일 (Next Steps)
- 몬스터 파트: Melee·Ranged에 `ScanWeakPoint` 적용(연결 2개), OnEnable 위치 이동.
- 플레이어 파트: `Scan.cs`에 ScanWave 연결, Begin 호출.
- 약점 색 조정 여부 결정.
