# 스캔 시스템 구현 (2026-10-10)

> **정정 (같은 날):** 아래 1차 구현 중 `Scan.cs` 본구현과 `Scan.prefab`에 `ScanWave`를 합친 부분은 이세훈 님 코드를 덮어쓴 것이라 **원본으로 복구**했다(`git checkout`). 스킬/Player 연결은 후속 작업이고, 현재 방향은 사용자의 R키 `TerrainScanner`(`Assets/Test`)가 쏜 스캔에 대상 오브젝트가 반응하게 하는 것이다. 아래 `ScanWave`, `Art/Scan/`의 복사 에셋은 현재 쓰이지 않는다. 구조(`IScannable`, `ScanTarget`, 반응 컴포넌트)는 그대로 재사용한다.

관련 intent: [intent-019](../intent/intent-019-scan-system.md) · 요청 목록: [약점 배율 타 파트 요청](./scan-weakpoint-handoff-requests.md)

## 1. 작업 요약 및 방법
- **프로토타입 이전:** `Assets/Test`(Git 제외)의 스캔 에셋을 `Assets/_Project/Art/Scan/`으로 복사하고 GUID를 새로 부여해 참조를 다시 이었다. `TerrainScanner_tut_unlit.shadergraph`(파동 구), `TerrainScanner_tut_blue.mat`, `SG_ScanReveal.shadergraph`, `M_ScanReveal.mat`(원래 이름 `Shader Graphs_SG_ScanReveal.mat`), 그리고 신규 `SG_ScanRevealXRay.shadergraph` + `M_ScanRevealXRay.mat`(ZTest Always 변형: 벽 뒤에서도 보임).
- **구조(`Assets/_Project/Scripts/Core/Scanning/`, 네임스페이스 `Core.Scanning`):**
  - `IScannable.OnScanned(in SScanHit)` / `SScanHit{ScanId, Origin, Radius, Duration}`
  - `ScanWave` : 프로토 `TerrainScanWave` 이식. 파티클 크기로 반경 계산 → `OverlapSphereNonAlloc` → `IScannable`(콜라이더별 캐시)
  - `ScanTarget` : 기본 `IScannable`. 닿은 시점부터 `Duration` 유지 → `fade` 1→0으로 사라짐. 대상이 자기 타이머를 소유
  - `ScanReactionBase` : `OnScanBegin / OnScanUpdate / OnFade(fade, isFadingOut) / OnScanEnd`
  - 반응: `MeshRevealReaction`(플랫폼·아이템), `MarkerReaction`(상호작용 마커), `WeakPointReaction`(적 약점 표시), `WeakPoint`(배율 보유)
- **스킬:** `Skill/Skills/Scan.cs`를 본구현. `_duration`(10), `_size`(500, 지름), `_wave`, `_origin`. 발동 시 `ScanWave.Begin(origin.position, duration, size)`. `Stop()`/`OnDisable`은 파동만 멈춘다.
- **프리팹:** `ScanWave`와 `Sphere`(파티클)를 `Scan.prefab` 직계 자식으로 합쳤다(중첩 프리팹이 아니라 같은 프리팹 안의 자식 — 직접 YAML을 편집해 참조 오류 위험을 줄임). `ScanWave`가 Awake에서 파티클을 World 공간 + Play On Awake 끔으로 설정하고, Begin 때 파티클 위치를 중심으로 옮긴 뒤 재생한다. `_scannableLayers` = Default | Obstacle | Enemy | Interact.

## 2. 결정 사항
- 파동은 프로토 방식(구형 파티클 + OverlapSphere) 그대로. 새로 만들지 않았다.
- 닿은 시점부터 `duration`만큼 활성, 이후 `fadeOutTime`(기본 1초)에 걸쳐 사라짐. 같은 스캔의 반복 호출은 타이머를 건드리지 않고, 새 스캔 첫 접촉은 타이머를 연장한다.
- 플랫폼은 충돌 유지, 렌더러만 숨김/드러냄.
- 메시 사라짐 연출은 셰이더 수정 없이 표시 반경을 `Lerp(최근접거리, min(최원거리, 파동반경), fade)`로 접는다.
- 약점 데미지 배율은 `WeakPoint`까지만 Core에서 만들고 나머지는 요청 문서로 분리.
- 네임스페이스는 `Skill.Skills.Scan` 클래스와 겹치지 않도록 `Core.Scanning`.

## 3. 현재 상태 및 이슈
- 컴파일 오류 0. **Play 스모크 테스트 통과(임시 테스트, 삭제함):** 거리 2/6/12m 대상의 접촉 시각이 반경×시간과 일치, `activeUntil = 접촉 + duration`, 종료는 `+ fadeOutTime`, `OnScanBegin` 1회, 스킬 오브젝트를 200m 옮겨도 파동 중심 고정, 플랫폼 렌더러 숨김→드러남→반경 접힘→숨김과 콜라이더 유지.
- **아직 실제 프리팹에 적용하지 않았다.** 플랫폼/아이템/상호작용/적 프리팹에 `ScanTarget` + 반응을 붙이는 작업은 레벨 작업이라 남겨뒀다(아래 가이드).
- `Scan.prefab`의 `CooldownCost`가 1초라 파동이 10초인데 재발동되면 파동이 재시작된다(대상 타이머는 연장됨). 쿨타임 ≥ duration으로 올릴지 결정 필요.
- `size = 500`(지름)은 프로토 기본값이라 매우 크다. 레벨 규모에 맞춰 조정 필요.
- `ProjectSettings/ShaderGraphSettings.asset`이 Unity 임포트로 줄바꿈만 바뀌어 modified로 보인다. 커밋에 포함하지 않는다.
- `MarkerReaction`의 "벽 뒤에서도 보이는 스프라이트 머티리얼"과 마커 아이콘은 아직 없다.
- Phase B(피격 시 배율 적용)는 타 파트 반영 전이라 동작하지 않는다.

## 4. 다음 할 일 (Next Steps)
1. 에디터에서 대상 프리팹 배선(가이드):
   - 플랫폼: 메시 렌더러에 `M_ScanReveal` 지정 → 오브젝트에 `ScanTarget` + `MeshRevealReaction`. 콜라이더는 그대로.
   - 아이템: 렌더러에 `M_ScanRevealXRay` 지정 → `ScanTarget` + `MeshRevealReaction`. 트리거 콜라이더도 감지된다(레이어는 마스크에 포함).
   - 상호작용 오브젝트: 마커 자식(`SpriteRenderer` + `Map.Common.Billboard`) 생성 → `ScanTarget` + `MarkerReaction`(`_marker` 연결).
   - 적: 약점 자식에 콜라이더 + `WeakPoint`, 표시용 자식 오브젝트 → 적 루트에 `ScanTarget` + `WeakPointReaction`(`_markers` 연결). `MeshRevealReaction`은 적 몸체에 쓰지 않는다.
2. 마커용 "항상 위에 그려지는" 스프라이트 머티리얼과 아이콘 제작.
3. 쿨타임·duration·size 튜닝, `ScanTarget`의 fadeOutTime 튜닝.
4. 약점 배율 요청서 내용을 담당자와 협의(특히 `SEffectContext` 확장과 약점 판정 순서).
5. 실제 프리팹 적용·Play 확인이 끝나면 intent-019를 resolved로 처리하고 `clear/`로 이동.
