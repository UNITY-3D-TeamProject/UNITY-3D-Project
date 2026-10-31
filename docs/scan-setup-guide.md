# 스캔 대상 세팅 가이드

스캔 파동(R키 스캔)에 반응하는 대상 4종을 맵·프리팹에 배치하는 방법을 정리한다.
스크립트는 모두 `Assets/_Project/Scripts/Core/Scanning/`(`Core.Scanning`), 머티리얼·셰이더·마커 스프라이트는 `Assets/_Project/Art/Scan/`(지금 쓰지 않는 옛 리빌·실루엣 머티리얼과 셰이더 그래프는 `Art/Scan/NotUse/`), 프리팹은 `Assets/_Project/Prefabs/System/Scan/`에 있다. 4종 완성 예시는 `Prefabs/System/Scan/Example/`(`Example_HiddenPlatform`, `Example_WeakPoint`, `Example_WallItem`, `Example_Interact`)에 있으니 씬에 놓고 스캔해 보거나 구성을 참고한다. `Example_WallItem`은 +Z 쪽에서 봐야 벽 뒤에 아이템이 있다. `Example_WeakPoint`는 ThirdParty의 Gripper Robot 모델(`Assets/ThirdParty/Gripper Robot Cute Series`)을 쓰므로, 이 에셋이 없으면 모델이 빠진 채로 열린다. 약점은 머리 본(`RigHead`) 아래에 있어 애니메이션을 따라 움직인다.

| 프리팹 | 용도 | 쓰는 법 |
| --- | --- | --- |
| `ScanPlatform` | 숨은 플랫폼 | 그대로 놓고 위치·스케일 조절 |
| `ScanWeakPoint` | 적 약점 | 몬스터 루트 자식으로 달고 2개 연결 |
| `ScanSilhouette` | 벽 뒤 아이템 실루엣 | 아이템 자식으로 달고 메시 교체 |
| `ScanOutline` | 상호작용 테두리 | 물체 자식으로 달고 메시 교체 |
| `ScanMarker` | 상호작용 마커 | 물체 자식으로 달고 위치 조절 |
| `ScanWave` | 스캔 파동(스킬이 생성) | 대상 세팅에는 쓰지 않음. 파동 연출 머티리얼은 `TerrainScanner_tut_blue` |

| 대상 | 붙이는 반응 | 머티리얼 | 보이는 모습 |
| --- | --- | --- | --- |
| 숨은 플랫폼 | `MeshRevealReaction` | `M_ScanPlatformHologram` | 평소 아주 희미 → 스캔 시 모서리·격자·면 순서로 채워진 뒤 반투명 홀로그램 |
| 적 약점 | `SilhouetteReaction` + `WeakPointReaction` + `WeakPointShellMask` | `M_ScanHologram` | 몸 전체 파란 홀로그램 셸, 약점 부위만 빨갛게 맥동 |
| 벽 뒤 아이템 | `SilhouetteReaction` | `M_ScanSilhouette` | 벽에 가려진 부분만 초록 홀로그램으로 비쳐 보임 |
| 상호작용 오브젝트 | `MarkerReaction` + `SilhouetteReaction` | `M_ScanOutline` | 마커 + 주황 홀로그램 테두리(벽에 가려지면 안 보임) |

---

## 0. 공통 규칙 (모든 대상)

1. **`ScanTarget`을 붙인 오브젝트에 `Scan` 태그를 단다.** 태그가 없으면 파동이 찾지 못한다(Play 시 Assert 경고).
2. **반응 컴포넌트는 `ScanTarget`과 같은 오브젝트에 붙인다.** `ScanTarget`은 같은 오브젝트의 반응만 자동으로 찾고, 자식에 있는 반응은 찾지 않는다.
3. **콜라이더가 하나 이상 있어야 한다.** 파동이 닿았는지는 콜라이더로 판정한다. `ScanTarget._scanColliders`를 비워 두면 자기 자신과 자식의 콜라이더를 모두 쓴다.
4. 레이어는 따로 정하지 않아도 된다(스캔 대상은 태그로만 구분한다).

`ScanTarget` 인스펙터 값

| 항목 | 기본값 | 설명 |
| --- | --- | --- |
| `_fadeInTime` | 0.2 | 닿은 뒤 다 보이기까지 걸리는 시간(초) |
| `_fadeOutTime` | 1.0 | 유지 시간이 끝난 뒤 사라지는 데 걸리는 시간(초) |
| `_reactions` | 비움 | 비워 두면 같은 오브젝트의 반응을 자동으로 찾는다 |
| `_scanColliders` | 비움 | 비워 두면 자신·자식 콜라이더를 쓴다. **적 약점만 따로 연결**한다(2장) |

---

## 1. 숨은 플랫폼

평소에는 거의 보이지 않다가(가까이 2m에서 2.5%, 10m 이상에서 0.5%) 스캔이 닿으면 홀로그램 발판으로 드러난다. **충돌은 항상 켜져 있으므로** 안 보일 때도 밟을 수 있다.

### 구성
```
Platform  (태그 Scan)
  MeshFilter (Cube)
  MeshRenderer   머티리얼 M_ScanPlatformHologram 1개
  BoxCollider
  ScanTarget
  MeshRevealReaction   _shouldHideWhenIdle = 끔
```

### 순서
**새로 만드는 경우:** 프리팹 `Assets/_Project/Prefabs/System/Scan/ScanPlatform.prefab`(기본 Cube, 아래 구성이 모두 들어 있음)을 놓고 위치·크기만 조절하면 끝이다. 크기는 스케일로 바꾼다.

**기존 플랫폼 오브젝트에 붙이는 경우:**
1. 플랫폼에 `Scan` 태그를 단다.
2. 플랫폼에 콜라이더가 있는지 확인한다(BoxCollider 등). 이 콜라이더로 밟히고, 스캔에 닿았는지도 판정한다.
3. 플랫폼에 `ScanTarget`을 붙인다.
4. 플랫폼에 `MeshRevealReaction`을 붙인다.
5. 플랫폼 Mesh Renderer의 머티리얼을 `M_ScanPlatformHologram` 하나만 지정한다(원래 머티리얼은 빼고, 다른 머티리얼과 같이 넣지 않는다).
6. `MeshRevealReaction`의 Should Hide When Idle을 **끈다.** 켜 두면 평소 희미한 형태까지 사라진다.
7. Reveal Speed(기본 10m/s)는 닿은 지점부터 끝까지 채워지는 속도다. 파동이 더 빠르면 파동 속도를 따라간다.
8. Renderers는 비워 두면 자기와 자식의 렌더러를 자동으로 찾는다. 자식에 숨기면 안 되는 장식 렌더러가 있으면 플랫폼 렌더러만 직접 넣는다.

### 머티리얼 설정 (`M_ScanPlatformHologram`, 셰이더 `Project/ScanPlatformHologram`)
- **메시 모양은 자유다.** 희미한 평소 상태, 채워짐, 격자, 가장자리 빛, 완료 발광은 어떤 메시에서도 동작한다.
- 단 **모서리 빛만 상자 기준**이다. 셰이더가 메시의 실제 모서리를 모르기 때문에 오브젝트 공간 상자(`_BoxHalfSize`)의 모서리 근처를 밝힌다.
  - 유니티 기본 Cube(스케일로 늘려도 됨): 그대로 맞는다(기본 0.5, 0.5, 0.5).
  - 다른 상자 메시: `_BoxHalfSize`를 메시의 오브젝트 공간 절반 크기로 맞춘다.
  - 상자가 아닌 메시(원기둥·경사로 등): 실제 모서리에 빛이 생기지 않고, 상자 모서리와 겹치는 곳에만 생긴다. `_BoxHalfSize`를 메시 범위(bounds) 절반 크기로 맞추면 무난하고, 모서리 빛이 어색하면 `_EdgeStrength`를 낮춘다.
  - 피벗이 메시 중심이 아니면 상자가 어긋나 모서리 빛도 어긋난다.
- 주요 조절 값

| 항목 | 기본값 | 설명 |
| --- | --- | --- |
| `_Color` | 파랑 (0.2, 0.75, 1) | 홀로그램 색 |
| `_IdleAlphaNear` / `_IdleAlphaFar` | 0.025 / 0.005 | 평소 희미한 정도(가까이 / 멀리) |
| `_IdleNearDistance` / `_IdleFarDistance` | 2 / 10 | 위 두 값이 적용되는 거리(m) |
| `_FillBand` | 1.5 | 파동 앞면에서 다 채워지기까지의 폭(m). 늘리면 채워지는 과정이 길게 보인다 |
| `_GridSize` | 0.5 | 격자 간격(m) |
| `_HoloAlpha` | 0.2 | 다 드러난 뒤 면의 진하기 |
| `_FlashIntensity` / `_FlashDuration` | 3 / 0.5 | 다 드러난 순간 번쩍이는 세기 / 시간(초) |

- 여러 플랫폼이 머티리얼 하나를 같이 써도 된다(스캔 값은 렌더러별 PropertyBlock으로 넘어간다). 머티리얼 값을 바꾸면 모든 플랫폼이 같이 바뀌므로, 특정 플랫폼만 색이나 `_BoxHalfSize`가 달라야 하면 머티리얼을 복제해 그 플랫폼에만 지정한다.

---

## 2. 적 약점

스캔하면 몬스터 몸 전체에 파란 홀로그램 셸이 씌워지고, 약점 구 범위만 빨갛게 맥동한다. 스캔 중에만 약점 콜라이더가 켜져 맞힐 수 있다.

### 구성
```
Melee  (몬스터 루트: 기존 컴포넌트 그대로, 스캔 스크립트 없음)
├ (기존 SkinnedMeshRenderer들)
└ WeakPoint  (태그 Scan, 항상 활성)
    SphereCollider          약점 범위 = 맞는 범위 = 빨간 표시 범위
    ScanTarget              _scanColliders = 몬스터 루트의 CharacterController
    SilhouetteReaction      _autoShellMaterial = M_ScanHologram, _shellSourceRoot = Melee(루트)
    WeakPointReaction
    WeakPointShellMask
```

### 순서
프리팹 `Assets/_Project/Prefabs/System/Scan/ScanWeakPoint.prefab`(태그 `Scan`, SphereCollider 반경 0.2, 스크립트 4개, Auto Shell Material = `M_ScanHologram`까지 들어 있음)을 쓴다.

1. 몬스터 프리팹을 열고 루트 아래 자식으로 `ScanWeakPoint` 프리팹을 단다. 몬스터 루트에는 스캔 스크립트를 붙이지 않는다.
2. 위치와 SphereCollider 반경을 약점 부위에 맞춘다. 이 구가 그대로 빨간 표시 범위이자 맞는 범위다. **구가 몸 표면에 걸쳐 있어야** 빨간색이 보인다(셸 표면 중 구 안쪽만 빨갛게 칠하므로, 몸 안 깊숙이 있거나 몸 밖에 떨어져 있으면 안 보인다).
3. `ScanTarget`의 Scan Colliders 배열 크기를 1로 두고 **몬스터 루트의 CharacterController**를 넣는다. 비워 두면 스캔 전에는 꺼져 있는 약점 구로만 판정해 반응하지 않는다.
4. `SilhouetteReaction`의 Shell Source Root에 **몬스터 루트**를 넣는다. 비우면 약점 아래에서만 찾아 셸이 생기지 않는다. Silhouette Renderers는 비워 둔다(셸이 게임 시작 시 몬스터의 모든 SkinnedMeshRenderer마다 자동으로 생긴다).
5. `WeakPointReaction`, `WeakPointShellMask`는 설정할 것이 없다.
6. 약점 오브젝트는 **항상 켜 둔다.** SphereCollider는 켜 두든 꺼 두든 상관없다(시작할 때 스크립트가 끄고, 스캔 중에만 켠다). 에디터에서 위치를 맞출 때는 켜 두면 씬 뷰에 구가 보여 편하다.
- 머티리얼은 약점 오브젝트에 붙이지 않는다(렌더러가 없다). 빨간색은 셸 셰이더가 칠하고, 어디를 칠할지는 `WeakPointShellMask`가 매 프레임 켜진 SphereCollider의 위치·반경을 셸에 넘겨 정한다.

### 몬스터 파트와의 약속
- 약점 위치를 랜덤으로 바꿀 때는 `OnEnable`에서 **위치만 옮긴다.** 약점 오브젝트 `SetActive(false)`나 몬스터 코드에서 콜라이더를 직접 켜고 끄는 것은 하지 않는다(콜라이더는 `WeakPointReaction`이 스캔에 맞춰 켜고 끈다).
- 피격 쪽에서 약점 판정은 맞은 콜라이더에 `WeakPointReaction`이 있는지로 한다(`Scan` 태그는 아이템 등에도 달리므로 태그로 판정하지 않는다).
- 몬스터당 약점 1개 기준이다(셰이더는 최대 4개까지 표시 가능).

### 머티리얼 설정 (`M_ScanHologram`, 셰이더 `Project/ScanHologram`)
- 셸은 자동 생성이라 따로 렌더러를 만들 필요가 없다. 머티리얼만 지정한다(프리팹에 이미 지정됨).
- 모든 적이 같이 쓰는 머티리얼이라 바꾸면 전부 같이 바뀐다. 특정 적만 다르게 하려면 복제해 그 적의 Auto Shell Material에만 지정한다.
- 주요 조절 값

| 항목 | 기본값 | 설명 |
| --- | --- | --- |
| `_Color` | 파랑, 알파 0.4 | 셸 색. 알파가 전체 진하기 |
| `_OutlineWidth` | 0.03 | 바깥 외곽선 두께(m) |
| `_RimStrength` | 0.9 | 가장자리 빛 세기 |
| `_GridDensity` / `_GridColumns` / `_GridSpeed` | 8 / 24 / 0.3 | 흐르는 격자의 가로줄 수(줄/m) / 세로줄 수 / 흐르는 속도 |
| `_WeakColor` | 빨강 | 약점 색 |
| `_WeakIntensity` | 2.5 | 약점 밝기 |
| `_WeakFillAlpha` | 0.45 | 약점 범위 채우기 진하기 |
| `_WeakPulseSpeed` | 5 | 약점 맥동 속도 |

---

## 3. 벽 뒤 아이템

아이템은 평소에도 원래 모습으로 보인다. 스캔하면 **벽에 가려진 부분만** 초록 홀로그램으로 비쳐 보인다(벽 앞에 그대로 보이는 부분은 원래 모습 그대로).

### 구성
```
Item  (태그 Scan)
  MeshFilter / MeshRenderer   원래 아이템 모습(그대로)
  Collider                    줍기용 콜라이더를 그대로 판정에 씀
  ScanTarget
  SilhouetteReaction          _silhouetteRenderers = [ScanSilhouette의 MeshRenderer]
  └ ScanSilhouette  (자식, 프리팹, 태그 없음)
      MeshFilter              아이템과 같은 메시
      MeshRenderer            M_ScanSilhouette, Cast Shadows = Off
```

### 순서
프리팹 `Assets/_Project/Prefabs/System/Scan/ScanSilhouette.prefab`을 쓴다.

1. 아이템에 `Scan` 태그를 단다(콜라이더가 있는지도 확인. 줍기용 콜라이더를 그대로 쓰면 된다).
2. 아이템에 `ScanTarget`을 붙인다.
3. 아이템에 `SilhouetteReaction`을 붙인다.
4. 아이템의 자식으로 `ScanSilhouette` 프리팹을 단다. 위치·회전 0, 스케일 1로 둬서 아이템과 정확히 겹치게 한다(어긋나면 벽 앞에서도 초록 실루엣이 비쳐 보인다). 콜라이더는 붙이지 않는다.
5. `ScanSilhouette`의 MeshFilter 메시를 아이템과 같은 메시로 바꾼다(프리팹 기본값은 Cube).
6. 색은 `M_ScanSilhouette`에서 바꾼다. 모든 벽 뒤 아이템이 같이 쓰는 머티리얼이라 바꾸면 전부 같이 바뀐다. 특정 아이템만 다른 색으로 하려면 머티리얼을 복제해 그 `ScanSilhouette`에만 지정한다.
7. `SilhouetteReaction`의 Silhouette Renderers 배열 크기를 1로 두고 `ScanSilhouette`의 MeshRenderer를 넣는다. 아이템이 여러 메시 조각(예: 상자 몸통 + 뚜껑)이면 조각마다 `ScanSilhouette`을 달아 배열에 모두 넣는다. Auto Shell Material·Shell Source Root는 비워 둔다(적 전용).

실루엣은 평소 꺼져 있다가 스캔 중에만 켜지고, 사라질 때 서서히 투명해진다(스크립트가 처리). 마커는 쓰지 않는다.

**종류별 색 (미로)**: 숫자 퍼즐 아이템(`MazePuzzleItem`)은 값이 정해질 때 실루엣 머티리얼도 바꾼다. 숫자(피연산자)는 `M_ScanSilhouette`(초록), 연산자는 `M_ScanSilhouette_Operator`(파랑)이며, 프리팹의 Kind Renderers 4번째 칸이 `ScanSilhouette` 렌더러다. 실행 중에 실루엣 머티리얼을 바꾸면 `SilhouetteReaction.RefreshBaseColors()`를 불러야 색이 반영된다. GC·도둑은 셸 머티리얼 `M_ScanSilhouette_Enemy`(빨강)를 쓴다.

### 머티리얼 설정 (`M_ScanSilhouette`, 셰이더 `Project/ScanSilhouetteHologram`)
- 벽에 가려진 부분만 그리는 셰이더(ZTest Greater)라 따로 설정할 것은 없다.
- 주요 조절 값

| 항목 | 기본값 | 설명 |
| --- | --- | --- |
| `_Color` | HDR 초록 (0.3, 1.2, 0.6, 1) | 홀로그램 색. 가산이라 밝을수록 진하다 |
| `_FillAlpha` | 0.2 | 안쪽 면 진하기 |
| `_RimPower` / `_RimStrength` | 2 / 1 | 가장자리 빛 날카로움 / 세기 |
| `_LinePeriod` / `_LineDim` | 4 / 0.4 | 화면 가로줄 간격(px) / 어두운 줄 밝기 |
| `_SweepSpacing` / `_SweepWidth` / `_SweepSpeed` / `_SweepStrength` | 3 / 0.15 / 1.2 / 0.8 | 위로 지나가는 밝은 띠의 간격(m) / 폭(m) / 속도(m/s) / 세기 |
| `_FlickerRate` / `_FlickerStrength` | 15 / 0.35 | 깜빡임 빈도(회/s) / 세기 |

---

## 4. 상호작용 오브젝트

스캔하면 위치 마커와 주황 홀로그램 테두리가 뜬다. **벽에 가려지면 마커·테두리 모두 보이지 않는다.**

### 구성
```
Interactable  (태그 Scan)
  MeshFilter / MeshRenderer   원래 모습(그대로)
  Collider
  ScanTarget
  MarkerReaction              _marker = ScanMarker
  SilhouetteReaction          _silhouetteRenderers = [ScanOutline의 MeshRenderer]
  ├ ScanMarker  (자식, 프리팹)
  │   SpriteRenderer          스프라이트 T_ScanMarker
  │   Billboard               (Map.Common.Billboard) 항상 카메라를 향함
  └ ScanOutline  (자식, 프리팹)
      MeshFilter              오브젝트와 같은 메시
      MeshRenderer            M_ScanOutline, Cast Shadows = Off
```

### 순서
프리팹 `Assets/_Project/Prefabs/System/Scan/ScanMarker.prefab`(마커), `ScanOutline.prefab`(테두리)를 쓴다.

1. 상호작용할 물체에 `Scan` 태그를 단다(콜라이더가 있는지도 확인).
2. 물체에 `ScanTarget`을 붙인다.
3. 물체에 `MarkerReaction`을 붙인다.
4. 물체에 `SilhouetteReaction`을 붙인다.
5. 물체의 자식으로 `ScanMarker` 프리팹을 달고, 마커가 뜰 위치(보통 물체 위쪽)로 옮긴다. 놓은 위치에 마커가 뜬다.
6. `MarkerReaction`의 Marker 칸에 방금 단 `ScanMarker`를 연결한다(**필수**). 스프라이트 칸은 비워 두면 마커 아래에서 찾는다.
7. 물체의 자식으로 `ScanOutline` 프리팹을 단다. 위치·회전 0, 스케일 1로 둬서 물체와 정확히 겹치게 한다(어긋나면 테두리도 어긋난다). 콜라이더는 붙이지 않는다.
8. `ScanOutline`의 MeshFilter 메시를 물체와 같은 메시로 바꾼다(프리팹 기본값은 Cube). 직접 가져온 모델 메시는 임포트 설정에서 **Read/Write**를 켠다. 각진 모서리에서 테두리가 끊기지 않도록 실행 시 법선을 매끄럽게 바꾸는데, 꺼져 있으면 경고가 뜨고 모서리가 끊길 수 있다.
9. 색은 `M_ScanOutline`에서 바꾼다. 모든 상호작용 물체가 같이 쓰는 머티리얼이라 바꾸면 전부 같이 바뀐다. 특정 물체만 다른 색으로 하려면 머티리얼을 복제해 그 `ScanOutline`에만 지정한다.
10. `SilhouetteReaction`의 Silhouette Renderers 배열 크기를 1로 두고 `ScanOutline`의 MeshRenderer를 넣는다. 물체가 여러 메시 조각이면 조각마다 `ScanOutline`을 달아 배열에 모두 넣는다.

마커와 테두리는 평소 꺼져 있다가 스캔 중에만 켜지고, 사라질 때 서서히 투명해진다(스크립트가 처리).

### 머티리얼 설정 (`M_ScanOutline`, 셰이더 `Project/ScanOutline`)
- 테두리 두께는 **화면 픽셀 기준**이라 멀리 있어도 같은 두께로 보인다.
- 주요 조절 값

| 항목 | 기본값 | 설명 |
| --- | --- | --- |
| `_Color` | HDR 주황 (1.4, 0.75, 0.12, 1) | 테두리 색 |
| `_OutlineWidth` | 3 | 테두리 두께(px) |
| `_GlowWidth` / `_GlowStrength` | 14 / 0.5 | 바깥 빛 번짐 폭(px) / 세기. 홀로그램 가로줄은 주로 이 띠에서 보인다 |
| `_DepthPush` | 0.1 | 바닥에 닿은 아랫변이 바닥에 묻히지 않도록 카메라 쪽으로 당기는 거리(m) |
| `_StencilRef` | 64 | 스텐실 값. 다른 스텐실 효과와 겹칠 때만 바꾼다 |
| `_LinePeriod` / `_LineDim` | 3 / 0.15 | 화면 가로줄 간격(px) / 어두운 줄 밝기 |
| `_SweepSpacing` / `_SweepWidth` / `_SweepSpeed` / `_SweepStrength` | 3 / 0.15 / 1.2 / 1 | 위로 지나가는 밝은 띠 |
| `_FlickerRate` / `_FlickerStrength` | 15 / 0.35 | 깜빡임 빈도 / 세기 |

---

## 5. 자주 하는 실수

| 증상 | 원인 |
| --- | --- |
| 스캔해도 아무 반응이 없다 | `Scan` 태그 누락, 콜라이더 없음, 반응이 `ScanTarget`과 다른 오브젝트(자식)에 붙음 |
| 플랫폼이 평소에 완전히 사라진다 | `MeshRevealReaction._shouldHideWhenIdle`이 켜져 있음 |
| 플랫폼 모서리 빛이 엉뚱한 곳에 생긴다 | 기본 Cube가 아닌 메시인데 `_BoxHalfSize`를 맞추지 않음, 또는 피벗이 중심이 아님 |
| 적 셸이 안 생긴다 | `_shellSourceRoot` 미연결, 또는 `_autoShellMaterial` 미지정 |
| 적 몸에 파동이 닿아도 약점이 안 켜진다 | 약점 `ScanTarget._scanColliders`에 몸체 CharacterController 미연결 |
| 약점 빨간 범위와 맞는 범위가 다르다 | 약점 범위는 SphereCollider만 쓴다. 다른 모양의 콜라이더는 표시되지 않는다 |
| 벽 뒤 아이템이 벽 앞에서도 초록으로 덮인다 | 실루엣 자식이 원래 메시와 어긋나 있음(위치·회전·스케일이 0/0/1인지 확인) |
| 상호작용 테두리 모서리가 끊긴다 | 메시 `Read/Write` 꺼짐(콘솔에 경고가 뜬다) |
