# 적 스캔 구조 변경 — 몬스터 루트 스크립트 없음, 약점 오브젝트가 스캔 단위 (2026-10-14)

관련 intent: [intent-019 스캔 시스템](../intent/intent-019-scan-system.md)

## 1. 작업 요약 및 방법
- 사용자 결정: 몬스터 프리팹 루트에는 스캔 스크립트를 달지 않는다. 약점 오브젝트는 항상 켜 두고 콜라이더만 끈다. 약점 오브젝트가 스캔에 반응해 홀로그램 셸·빨간 약점 색까지 처리한다.
- 문제: 꺼진 콜라이더는 `ScanWave`의 `OverlapSphereNonAlloc`에 잡히지 않는다. 이벤트로 모든 약점에 알려 거리를 재는 방식은 "닿는" 판정이 아니라 채택하지 않았다.
- 변경 (`Assets/_Project/Scripts/Core/Scanning/`)
  - `ScanWave.TryGetScannable`: 부모에서 `IScannable`을 못 찾으면 자식 중 `WeakPoint` 태그 오브젝트의 `IScannable`을 찾는다(`FindWeakPointScannable`). 파동은 몸체 콜라이더(CharacterController)에 닿고 약점의 `ScanTarget`이 알림을 받는다. 결과는 기존 캐시(스캔마다 콜라이더별)에 저장되므로 프레임당 추가 비용 없음.
  - `WeakPointReaction`: 자기 오브젝트의 `Collider`를 켜고 끈다(Awake에서 끔). 자기에게 `WeakPoint` 태그가 없으면 Assert. 쓰이지 않게 된 `SetWeakPoint` 제거. `WEAK_POINT_TAG` 유지.
  - `SilhouetteReaction`: `_shellSourceRoot` 추가. 셸 자동 생성 시 이 오브젝트 아래 SkinnedMeshRenderer를 찾는다(비어 있으면 자신 = 기존 동작).
  - `WeakPointShellMask`: 주석만 수정(코드는 이미 `collider.enabled`를 확인).
- `Assets/Test/.../WeakPointTestBinder.cs`(Git 제외): `SetWeakPoint` 호출 줄을 주석으로 바꿔 컴파일만 되게 함(파일은 남김). 새 구조에서는 필요 없다.

## 2. 결정 사항
- 몬스터 프리팹 구성 (몬스터 파트 적용)
```
Melee (루트: 기존 컴포넌트 그대로, 스캔 스크립트 없음)
└ WeakPoint (태그 WeakPoint, 항상 활성, SphereCollider)
    ScanTarget, SilhouetteReaction(_autoShellMaterial = M_ScanHologram, _shellSourceRoot = Melee),
    WeakPointReaction, WeakPointShellMask
```
- 몬스터 파트는 `OnEnable`에서 약점을 랜덤 후보 위치로 옮기기만 한다. `SetActive(false)` 하지 않는다(콜라이더 끄기는 `WeakPointReaction`).
- 몬스터당 약점 1개 기준.
- `ScanWave`의 자식 탐색은 `WeakPoint` 태그로 제한한다(자식에 아이템을 둔 큰 환경 콜라이더가 닿아도 자식이 반응하지 않게).
- `_shellSourceRoot`는 자동 탐색하지 않고 연결한다(Melee에 Animator 없음, 풀 부모 때문에 `transform.root` 불가).

## 3. 현재 상태 및 이슈
- 컴파일 에러 0, Scanning 관련 경고 0.
- Play 검증(TestScene의 `Melee`에 Play 중에만 WeakPoint 자식 + 4개 컴포넌트 추가, 저장 안 함):
  - 시작: 오브젝트 활성, 콜라이더 꺼짐, 셸 9개 생성(SkinnedMeshRenderer 9→18)·꺼짐
  - 실제 `ScanWave.Begin`(Melee 옆, 2초, 지름 20m): 파동이 CharacterController(Obstacle 레이어)에 닿아 셸 9/9 켜짐, 약점 콜라이더 켜짐, `_WeakPointCount` = 1
  - 유지 시간·페이드 후: 셸 0, 콜라이더 꺼짐, ScanTarget Update 꺼짐
  - 스캔 중 Melee 비활성→활성: 콜라이더 꺼짐, 셸 0, 셸 중복 생성 없음
- 페이드아웃 단계 중간(콜라이더 꺼짐 + 셸 페이드 중)은 따로 보지 않음(기존 OnFade 로직 그대로).
- TestScene의 옛 구성(`Enemy_WeakPoint`, `Gripper Robot` 루트에 `WeakPointReaction`)은 새 Assert가 뜨고, 루트의 콜라이더(그리퍼 CapsuleCollider 등)를 끄게 된다. Assets/Test라 수정하지 않음 — 새 구성으로 옮기거나 컴포넌트를 빼야 한다.
- 화면 캡처로 색을 눈으로 보지는 않음(값으로 확인).

## 추가 작업: 태그를 `Scan` 하나로 통일
- 이유: 레이어는 오브젝트당 하나라 이미 레이어가 지정된 대상에 스캔 전용 레이어를 줄 수 없다. 스캔에 반응하는 대상은 `Scan` 태그로 구분한다.
- TagManager: `Scan` 추가, `WeakPoint` 제거.
- `ScanTarget`: `SCAN_TAG = "Scan"` 상수 + Awake에서 태그 없으면 Assert.
- `ScanWave.TryGetScannable`: 부모의 `IScannable`이 Scan 태그면 사용, 아니면 자식 중 Scan 태그 대상을 찾는다(`HasScanTag`, `FindTaggedChildScannable`). 태그 없는 대상은 무시.
- `WeakPointReaction`: `WEAK_POINT_TAG`와 태그 Assert 제거. 피격 쪽은 맞은 콜라이더의 `WeakPointReaction` 컴포넌트 유무로 약점을 구분해야 한다(Scan 태그는 아이템에도 달리므로).
- `WeakPointShellMask`: `_weakPoints`가 비어 있으면 같은 오브젝트의 SphereCollider를 쓴다(태그 탐색 제거).
- 주의: 태그 검사는 `OverlapSphereNonAlloc` 뒤에 하므로 물리 질의 비용·256개 상한 문제는 줄이지 못한다. `_scannableLayers`는 기본값(Everything) 그대로 두면 되고, 레이어를 따로 만들 필요는 없다.
- 검증: 컴파일 OK. Play에서 `TryGetScannable` 직접 호출 — Melee CharacterController → 자식 WeakPoint(Scan) 찾음, 태그 없는 대상 무시, 자식 콜라이더 → Scan 태그 부모 찾음. (이번 Play는 게임 루프가 멈춰 파동 재생은 다시 보지 못함, 반응 흐름은 위 검증과 동일)
- TestScene 옛 스캔 대상들(`Item_Behind_Wall`, `Scan_Reveal_Object`, `Enemy_WeakPoint`, `Gripper Robot`, `Marker_Interact`, `Reveal_Platform`)은 Scan 태그가 없어 Assert가 뜨고 스캔에 반응하지 않는다. Assets/Test라 수정하지 않음 — 태그를 달아야 한다.
- 몬스터 프리팹 구성의 약점 태그는 `WeakPoint` → **`Scan`** 으로 바뀐다.

## 추가 작업: 물리 탐색 → 등록 목록 판정
- 이유: 물리 탐색(`OverlapSphereNonAlloc`)은 태그를 몰라 대상이 아닌 콜라이더도 결과 배열(256칸)에 들어온다. 레이어는 이미 다른 용도로 쓰여 거를 수 없다.
- `ScanTarget`
  - static 목록 + `Registered`(읽기 전용). `Scan` 태그가 있으면 Awake에서 등록, `OnDestroy`에서 제거. (대기 중 스스로 `enabled = false`를 하므로 OnEnable/OnDisable로 관리하지 않음)
  - `_scanColliders`(판정 콜라이더) 추가. 비어 있으면 자기와 자식 콜라이더 전부. 몬스터 약점은 몸체 CharacterController 연결.
  - `IsTouchedBy(origin, radius)`: 꺼진 콜라이더 제외 → 범위 상자(`bounds.SqrDistance`)로 거름 → Box/Sphere/Capsule/볼록 Mesh는 `ClosestPoint`로 실제 표면 거리 확정, 그 외(CharacterController 등)는 범위 상자 결과.
- `ScanWave`: `Registered`를 뒤에서부터 돌며 `activeInHierarchy && IsTouchedBy`면 `OnScanned`. `OverlapSphereNonAlloc`, 256 상한·경고, `_scannableLayers`, 캐시, 태그 탐색 코드 제거. `ScanWave.prefab`의 `_scannableLayers` 직렬화 값은 남지만 무시됨.
- `IScannable`은 유지(`Assets/Test`의 `TerrainScanWave`가 사용).
- 검증(TestScene Play, 저장 안 함): 컴파일 OK.
  - 등록: 태그 있는 대상만 등록(0→2, 태그 없는 대상 제외), 파괴하면 2→1.
  - 경계: CharacterController 범위 상자 끝까지 3.0m에서 r 2.9 false / 3.01 true. 45° 회전 큐브(BoxCollider) — 꼭짓점 방향 실제 0.493m에서 r 0.45 false / 0.52 true, 면 방향은 범위 상자로는 0m지만 실제 0.5m에서 r 0.45 false / 0.51 true(ClosestPoint로 정확).
  - 실제 `ScanWave.Begin`: 약점(셸 9/9, 콜라이더 켜짐, `_WeakPointCount` 1)·태그 아이템 반응, 태그 없는 대상 무반응.
  - Melee 비활성 → 콜라이더 꺼짐, 파동이 건너뜀, 재활성 시 셸 0·중복 생성 없음.
- 몬스터 프리팹 구성: 약점 `ScanTarget._scanColliders` = 몬스터 루트 CharacterController 추가.

## 추가 작업: TestScene 스캔 대상 정리 (Assets/Test, Git 제외)
- 원인: TestScene 스캔 대상 7개가 전부 Untagged라 등록 목록에 안 들어가 스캔이 처리되지 않았다.
- 7개(`Marker_Interact`, `Item_Behind_Wall`, `Scan_Reveal_Object`, `Hidden_Behind_Wall`, `Enemy_WeakPoint`, `Reveal_Platform`, `Gripper Robot`)에 `Scan` 태그.
- 적 2개는 루트의 옛 `WeakPointReaction`이 몸체 CapsuleCollider를 끄는 상태였다 → 루트에서 `WeakPointReaction`·`WeakPointTestBinder` 제거(루트 ScanTarget·홀로그램·그리퍼 WeakPointShellMask는 유지). 약점 구 3개(`WeakPoint_Head`, `WeakPoint_Chest`, `WeakPoint_Visor`)에 `Scan` 태그 + `ScanTarget(_scanColliders = 몸체 Capsule)` + `WeakPointReaction`.
- 씬 저장(작업 전 미저장 변경 없음 확인).
- Play 검증: 등록 9개(꺼진 `Hidden_Behind_Wall` 제외), 플레이어 위치에서 파동 → 9개 모두 반응, 그리퍼 셸 `_WeakPointCount` 1, 스캔 전·후 약점 콜라이더 꺼짐 / 몸체 콜라이더 항상 켜짐. 스캔 관련 에러 0.

## 4. 다음 할 일 (Next Steps)
- 몬스터 파트에 위 프리팹 구성 전달(약점 오브젝트, 후보 5곳, OnEnable에서 이동만)
- TestScene 옛 약점 시험 오브젝트 정리 또는 새 구성으로 교체
- TestScene 옛 스캔 대상에 `Scan` 태그 달기
- 약점 데미지 배율(Phase B): 피격 쪽은 맞은 콜라이더의 `WeakPointReaction` 유무로 판정. 몸체 CharacterController와 약점 구가 겹칠 때 우선순위 정리
