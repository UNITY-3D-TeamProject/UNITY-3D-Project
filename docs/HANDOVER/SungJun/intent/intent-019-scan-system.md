---
id: intent-019
title: 스캔 스킬 구현 — 지속시간 동안 파동이 닿은 대상이 IScannable 반응으로 드러났다가 서서히 사라짐
part: Skill / Scan / Core
status: open   # open | resolved
created: 2026-10-10
resolved: null # 해결 시 YYYY-MM-DD 로 변경
---

## 문제 (Problem)
- `Scan` 스킬(`Skill/Skills/Scan.cs`)은 `Debug.Log("Scan")`뿐인 스텁이다. 입력(`OnScan`)과 쿨타임 HUD 연결만 되어 있다.
- 스캔 파동 프로토타입(`TerrainScanner`, `TerrainScanWave`, `ScanOrderRevealTarget`, `SG_ScanReveal`)이 Git에 올라가지 않는 `Assets/Test/`에만 있어 팀이 쓸 수 없다. R키 입력·월드 Instantiate 방식이라 플레이어 스킬 구조와도 맞지 않는다.
- 스캔에 닿은 대상이 어떻게 반응할지(적 약점, 아이템 위치, 플랫폼, 상호작용 오브젝트)를 정하는 구조가 없다.

## 기대 결과 (Proposed outcome)
- (정정 2026-10-10) Player/Skill 연결(`Scan.cs`, `Scan.prefab`)은 이세훈 님 파트이고 후속 작업이다. 지금은 기존 R키 `TerrainScanner`(`Assets/Test`)가 쏜 스캔에 대상이 반응하는 부분만 만든다. 파동 생성/재생은 사용자의 기존 `TerrainScanner`·`TerrainScanWave`를 그대로 쓴다.
- 스캔을 쓰면 `duration` 동안 파동이 확산한다. (~~파동은 플레이어 프리팹 하위 `Scan` 아래에 상주~~ — 철회)
- 파동이 닿은 대상은 `IScannable.OnScanned(SScanHit)`를 통해 자기 반응을 수행한다. **닿은 시점부터 `duration`만큼 활성화**되고, 이후 `fade`(1→0)로 서서히 사라진다.
  1. 적: 약점 부위 노출 (+ 약점 데미지 배율, Phase B)
  2. 아이템: 벽에 가려져도 실루엣/위치가 보임. (정정 2026-10-11) 아이템은 **평소에도 보이는** 오브젝트라 `MeshRevealReaction`+X-Ray 머티리얼로는 맞지 않는다(평소 숨겨지거나, 원래 외형을 잃고 스캔과 무관하게 벽 너머로 보임). 원래 렌더러는 그대로 두고, 스캔 중에만 자식 실루엣 렌더러(`M_ScanSilhouette`, ZTest Greater = 가려진 부분만)를 켜는 `SilhouetteReaction`으로 처리한다. 미로맵용.
  3. 플랫폼: 메시가 투명 상태에서 파동처럼 점점 보임 (충돌은 항상 유지). (정정 2026-10-11) 파동이 닿은 범위만 보이지 않고, **닿으면 닿은 지점부터 메시 전체가 `_revealSpeed`(m/s)로 끝까지 드러난다.** 파동이 더 빠르면 파동을 따라간다.
  4. 상호작용 오브젝트: 위치 마커 표시
- 구조: `IScannable` + `ScanTarget`(타이머·상태) + `ScanReactionBase`(Strategy) 조합. 상세는 설계 계획(`C:\Users\inha\.claude\plans\declarative-snuggling-haven.md`)을 따른다.

- (추가 2026-10-13) 적 스캔 연출: 몸체는 파란 홀로그램 셸(`Project/ScanHologram` 셰이더 = 바깥 외곽선 + 가장자리 빛 + 흐르는 격자)을 같은 메시 자식 렌더러로 두고 `SilhouetteReaction`으로 켜고 끈다(새 스크립트 없음, `_Color` 알파로 페이드). 약점은 약점 오브젝트 자식에 빨간 레티클 스프라이트 + "WEAK POINT" TextMeshPro + `Billboard`를 두어 `WeakPointReaction`이 약점을 켤 때 함께 보인다. TempScene `ScanTest/Enemy_WeakPoint`에 머리·가슴 약점 2개로 시험 배치. (정정 같은 날) 약점 마커는 월드 Billboard 대신 화면 UI로 전환: 약점 오브젝트에 `UI.ScreenMarkerFollower`를 붙여 HUD 캔버스의 마커를 화면 위치로 옮긴다. (추가 같은 날) 약점 부위만 셸 색을 다르게: 셸 셰이더의 구 마스크 + `WeakPointShellMask`가 약점 SphereCollider의 월드 중심·반경을 넘겨, 구 안의 외곽선·격자를 `_WeakColor`로 칠한다(보이는 범위 = 맞는 범위). 그리퍼 로봇(SkinnedMesh)에 시험 적용. (추가 같은 날) 실제 적 프리팹용: `SilhouetteReaction._autoShellMaterial`로 셸을 런타임 자동 생성, 약점 후보 5곳 중 하나로 콜라이더를 옮기고 끄는 것은 몬스터 파트가 몬스터 `OnEnable`에서 한다(스포너는 소환만). 스캔 쪽은 `WeakPoint` 태그로 약점 콜라이더를 자동으로 찾으므로(`WeakPointReaction`, `WeakPointShellMask` Awake) 몬스터 코드는 스캔 코드를 모른다.
- (변경 2026-10-14) 적 스캔 구성 확정: **몬스터 루트에는 스캔 스크립트를 달지 않는다.** 약점 오브젝트(`WeakPoint` 태그, 항상 활성)에 `ScanTarget`, `SilhouetteReaction`(`_autoShellMaterial`, `_shellSourceRoot` = 몬스터 루트), `WeakPointReaction`, `WeakPointShellMask`를 모두 붙인다. 약점은 오브젝트를 끄지 않고 **콜라이더만** 끄고 켠다. 파동은 몸체 콜라이더(CharacterController)에 닿고, `ScanWave`가 부모에서 `IScannable`을 못 찾으면 자식 중 `WeakPoint` 태그 오브젝트에서 찾는다(접촉 판정 유지, 결과 캐시). 몬스터 파트는 `OnEnable`에서 약점 위치만 옮기고 `SetActive(false)`는 하지 않는다. 몬스터당 약점 1개 기준. (같은 날 변경) 태그는 **`Scan` 하나로 통일**: 스캔에 반응하는 모든 대상(`ScanTarget` 오브젝트, 약점 포함)은 `Scan` 태그를 달고, `ScanWave`는 이 태그의 대상에만 알린다. `WeakPoint` 태그는 제거. 피격 쪽 약점 판정은 `WeakPointReaction` 컴포넌트 유무로 한다. (같은 날 변경) 판정은 물리 탐색 대신 **등록 목록**: Scan 태그 `ScanTarget`이 Awake에서 등록하고, `ScanWave`는 목록만 돌며 판정 콜라이더(`_scanColliders`, 비어 있으면 자기·자식 콜라이더)에 파동 구가 닿았는지 범위 상자 + `ClosestPoint`로 계산한다. 레이어 마스크·256개 상한 없음. 약점은 몸체 CharacterController를 판정 콜라이더로 연결한다.
- (추가 2026-10-14) **상호작용 오브젝트**에 외곽선을 더한다. 스캔 중 **벽에 가려지지 않았을 때만** 바깥 외곽선 + 빛 번짐이 보인다(`Project/ScanOutline`, `M_ScanOutline`). 벽에 가려지면 외곽선·마커 모두 보이지 않는다. **벽 뒤 아이템은 그대로** 초록 실루엣(`M_ScanSilhouette`, 가려진 부분만)으로 벽 너머에서 보인다(사용자 정정). 상호작용 오브젝트는 `MarkerReaction` 옆에 `SilhouetteReaction` + 외곽선 자식 렌더러를 더 붙여 마커와 외곽선을 함께 보인다. 각진 메시(상자 등) 모서리에서 외곽선이 끊기지 않도록 `SilhouetteReaction`이 MeshFilter 실루엣 렌더러의 메시를 매끄러운 법선 복제본으로 바꾼다(메시 Read/Write 필요).

## 영향 범위 (Affected users and systems)
- 누가 사용하는가: 플레이어(스캔 스킬), 레벨 디자이너(대상 프리팹에 `ScanTarget` 부착)
- 어떤 시스템/모듈이 건드려지는가: `Skill/Skills/Scan.cs`, `Scan.prefab`, `Player.prefab`(중첩 프리팹 변경), 신규 `Scripts/Core/Scanning/`, 에셋 이전(`Assets/Test` → `Assets/_Project`), (Phase B) Attribute/Projectile/Hit 경로

## 제약 (Constraints)
- 스캔 파동 방식은 프로토타입(파티클 구 + `OverlapSphere`)을 그대로 이식한다. 새로 만들지 않는다.
- `Assets/Test`는 Git 제외 경로이므로 건드리지 않고 `Assets/_Project/`로 복사해 쓴다.
- 약점 데미지 배율은 우선 Core 안에서 작성하고, 타 파트(Attribute/Projectile/Hit) 변경은 요청 문서로 전달한다. intent-016(`SEffectContext`)과 경로가 겹친다.
- 플랫폼은 충돌 유지, 외형만 숨긴다.

## 열린 질문 (Open questions)
- ~~약점 데미지 배율을 스캔과 무관하게 항상 적용할지, 스캔으로 드러났을 때만 적용할지~~ → **해결(2026-10-11): 스캔 중에만 적용.** 스캔 중에만 약점 오브젝트가 활성화되어 보이고 맞는다. 평소에는 비활성이라 맞춰도 배율 1이다. (2026-10-14) 오브젝트 대신 약점 콜라이더만 끄고 켠다.
- ~~재스캔(이미 보이는 대상에 새 스캔이 닿음) 시 유지 시간 연장 vs 다시 드러내기~~ → **해결(2026-10-11): 유지 시간만 연장.** 스킬 쿨타임으로 스캔이 겹치지 않게 막을 예정이라, 재스캔은 연출을 건드리지 않고 종료 시각만 늘린다(처음 스캔의 정보만 반응에 전달).
- ~~약점이 켜져 있는 시간~~ → **해결(2026-10-11): 스캔 유지 시간이 끝나면 즉시 끔.** 페이드아웃 구간에는 꺼져 있고, 재스캔으로 다시 활성이 되면 켠다.
- 범위: `Scan.cs`/프리팹 연결(6번)과 피격 코드의 약점 배율 조회(7번)는 타 파트 작업이다. 스캔 발사는 `Assets/Test`의 `ScanTestTrigger`(2026-10-11부터 R키, 옛 `TerrainScanner`는 `Test_Player`에서 비활성)로 시험한다.
- ~~플랫폼이 파동이 닿은 범위만큼만 보여 반만 드러난 채 남는 문제~~ → **해결(2026-10-11): 닿았는지만 판단하고, 닿으면 닿은 지점부터 전체를 드러낸다.** 속도는 대상별 Inspector 값(`MeshRevealReaction._revealSpeed`).
- ~~아이템 벽 너머 표시 방식~~ → **해결(2026-10-11): A안(자식 실루엣 오버레이).** 머티리얼 런타임 추가(B), URP Render Objects + 레이어 전환(C)은 머티리얼 인스턴스·서브메시 처리와 줍기/물리 레이어 영향 때문에 채택하지 않음. 실루엣 색은 머티리얼 `_Color`로 정한다.
- "Core 스크립트 안에서" 작성한다는 범위를 `Scripts/Core/Scanning/`(`Core.Scanning`)으로 이해함. 다른 의미면 폴더만 조정
- `fadeOutTime`(제안 1.0초), fade-in 평활 시간(제안 0.2초) 튜닝값
- 숨은 플랫폼이 충돌은 있는데 안 보이는 상태가 레벨 디자인상 의도에 맞는지

## 해결 기록 (Resolution) — 해결 후 작성
- 최종 결정:
- 관련 커밋/PR:
