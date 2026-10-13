# 적 스캔 연출 — 파란 홀로그램 몸체 + 빨간 약점 마커 (2026-10-13)

관련 intent: [intent-019 스캔 시스템](../intent/intent-019-scan-system.md)

## 1. 작업 요약 및 방법
참고 이미지(스캔된 몬스터 = 파란 외곽선·격자, 가슴·머리 = 빨간 WEAK POINT 레티클)를 TempScene 캡슐 적(`ScanTest/Enemy_WeakPoint`)에 구현했다. 새 C# 스크립트는 없고, 기존 스캔 반응을 재사용했다.

- **신규 에셋** (`Assets/_Project/Art/Scan/`)
  - `ScanHologram.shader` (`Project/ScanHologram`): 2패스 URP 셰이더
    - `Outline`(SRPDefaultUnlit): 앞면 컬링 + 월드 법선 방향 확장(`_OutlineWidth`, 기본 0.03m) → 바깥 테두리
    - `Shell`(UniversalForward): 가장자리 빛(Fresnel) + 위로 흐르는 가로줄 + 둘레 세로줄 격자, 가산 블렌드
    - 색은 `_Color` → `SilhouetteReaction`이 MaterialPropertyBlock으로 알파 페이드
  - `M_ScanHologram.mat`: 위 셰이더 기본값
  - `T_WeakPointMarker.png`: 흰색 레티클 스프라이트(256px, PPU 256). SpriteRenderer 색으로 빨갛게 칠함
- **TempScene `ScanTest/Enemy_WeakPoint`** (Assets/Test라 Git 제외)
  - 자식 `ScanHologram`: 캡슐 메시 + `M_ScanHologram`, 그림자 끔, 콜라이더 없음
  - `SilhouetteReaction` 추가, `_silhouetteRenderers = [ScanHologram]`
  - `WeakPoint_Head`: 테스트용 빨간 구 MeshRenderer **끔**(콜라이더는 그대로). 자식 `WeakPointMarker` 추가
  - `WeakPoint_Chest` 신규(SphereCollider r=0.22) + 자식 `WeakPointMarker`. 두 번째 `WeakPointReaction` + 두 번째 `WeakPointTestBinder`(_reaction 명시 연결)
  - `WeakPointMarker` = `Billboard`(pitch 잠금 끔, 반전 켬) + `Reticle`(SpriteRenderer, 카메라 쪽으로 0.4m) + `Label`(TextMeshPro "WEAK POINT")

## 2. 결정 사항
- 몹 자체 = 셰이더(파란 셸), 약점 = 월드 공간 표시(스프라이트 + TMP + Billboard). 약점 머티리얼을 바꾸지 않는다.
- 마커는 약점 오브젝트의 자식이라 `WeakPointReaction`이 약점을 켜고 끌 때 함께 켜지고 꺼진다(별도 반응 불필요).
- 마커는 Billboard 방향 기준 카메라 쪽으로 띄워 몸체에 가려지지 않게 했다(ZTest Always 머티리얼은 만들지 않음).
- 몸체 셸은 `SilhouetteReaction` 재사용. 이름은 "실루엣"이지만 "스캔 중에만 렌더러를 켜고 `_Color` 알파로 페이드"라는 동작이 그대로 맞는다.

## 3. 현재 상태 및 이슈
- 셰이더 컴파일 메시지 0건. Play 모드에서 `ScanTarget.OnScanned` 직접 호출로 스캔 전 숨김 → 스캔 후 셸·머리·가슴 마커 켜짐 확인. 씬 뷰 캡처로 모양 확인.
- 에디터가 포커스를 잃어 Time이 흐르지 않아 **유지 시간 종료 후 페이드아웃은 이번에 직접 보지 못함**(기존 반응 코드 경로 그대로).
- 플레이어 카메라 시점에서 R키 스캔으로 실제 보는 확인은 아직 안 함.
- 실제 적 프리팹(Melee 등 SkinnedMesh)에 붙이려면 SkinnedMeshRenderer용 셸 렌더러가 필요(같은 본을 쓰는 복제 렌더러). 이번 범위 밖.
- TempScene 저장 시 세션 시작 전부터 있던 미저장 변경도 함께 저장됨.

## 4. 다음 할 일 (Next Steps)
- R키 스캔으로 플레이 화면에서 레티클 크기(0.45)·글자 크기(1.4)·외곽선 두께 튜닝
- 실제 적 프리팹 적용 방식 결정(SkinnedMesh 셸, 약점 위치 본 부착)
- 약점 데미지 배율(intent-019 Phase B)과 연결

---

## 추가 작업: 약점 마커를 화면 UI로 전환 (A안)

### 작업 요약 및 방법
- 월드 공간 마커(Billboard + SpriteRenderer)는 다른 각도에서 보면 몸체에 가려지거나 어긋나서, 화면 UI 방식으로 바꿨다.
- 신규 `Scripts/UI/ScreenMarkerFollower.cs` (`UI` 네임스페이스): 붙은 오브젝트가 켜져 있는 동안 `Camera.WorldToScreenPoint`로 연결된 UI 마커를 그 화면 위치로 옮긴다(`LateUpdate`). 카메라 뒤(z ≤ 0)면 숨기고, 오브젝트가 꺼지면(`OnDisable`) 마커도 끈다.
- TempScene: 약점 아래의 월드 마커 `WeakPointMarker` 삭제. HUD `Canvas`(Overlay, 800×600 기준) 맨 앞에 `WeakPointMarker_Head`/`_Chest`(64px 레티클 Image + "WEAK POINT" TMP UGUI 20pt) 추가, 평소 꺼짐. `WeakPoint_Head`/`WeakPoint_Chest`에 `ScreenMarkerFollower` 부착·연결.

### 결정 사항
- 기존 `WeakPointReaction`은 건드리지 않는다. 약점 오브젝트가 켜지고 꺼지는 것에 마커가 따라간다.
- 마커는 씬의 UI 오브젝트를 직접 연결한다(테스트 씬 기준 최소 구현).

### 현재 상태 및 이슈
- 컴파일 에러 0. Play에서 스캔 → 게임 뷰에 머리·가슴 마커가 약점 위치에 표시됨(캡처 확인), 카메라 뒤면 꺼짐, 스캔 종료 시 꺼짐 확인.
- **한계**: 마커를 씬 UI에 직접 연결하므로, 스폰되는 적 프리팹에는 그대로 못 쓴다(프리팹이 씬 오브젝트를 참조할 수 없음). 실제 적에 적용할 때는 HUD 쪽에서 마커를 생성·풀링해 내주는 방식이 필요하다.
- 벽 뒤에 있는 약점도 마커가 보인다(가림 판정 없음).

### 다음 할 일
- 실제 적 적용 시 HUD 마커 생성·반납 구조 결정
- 벽 뒤 약점을 숨길지 결정(필요하면 레이캐스트로 가림 확인)

---

## 추가 작업: 그리퍼 로봇 — 약점 구 범위만 셸 색을 빨갛게 (구 마스크)

### 작업 요약 및 방법
- 그리퍼 로봇은 SkinnedMeshRenderer 1개·서브메시 1개라 머티리얼로 부위를 나눌 수 없어, 셸 셰이더에 약점 구 마스크를 넣었다.
- `ScanHologram.shader`: `_WeakColor`(기본 빨강), `_WeakEdge`(경계 부드러움, 기본 0.03m) 추가. 렌더러별 값 `_WeakPoints[4]`(xyz 월드 중심, w 반경)·`_WeakPointCount`로 구 안의 외곽선(부풀리기 전 표면 위치 기준)·격자 색을 `_WeakColor`로 바꾼다. 알파는 `_Color.a`를 그대로 써서 페이드가 같이 적용된다. 값이 안 넘어오는 렌더러(캡슐)는 count 0이라 전부 파란색.
- 신규 `Scripts/Core/Scanning/WeakPointShellMask.cs`: 연결된 SphereCollider의 월드 중심·반경(가장 큰 축 스케일 곱)을 LateUpdate마다 MaterialPropertyBlock으로 셸에 넘긴다. 켜진 약점만 넘기므로 `WeakPointReaction`에 따라 색도 켜지고 꺼진다. 셸이 꺼져 있으면 아무것도 안 한다. 기존 블록 값(SilhouetteReaction 색)은 Get→Set으로 유지.
- TempScene 기존 루트 `Gripper Robot`(FBX 인스턴스):
  - 자식 `ScanHologram`: 원본과 같은 mesh·bones·rootBone·localBounds를 쓰는 SkinnedMeshRenderer + `M_ScanHologram`(그림자 끔) → 애니메이션을 따라감
  - 루트에 CapsuleCollider(center (0,1.35,0), r 0.6, h 1.5) — 스캔 파동 판정용
  - `RigHead` 본 아래 `WeakPoint_Visor`(SphereCollider r 0.3, 루트 기준 (0,1.6,0.45) = 얼굴 앞면)
  - 루트에 `ScanTarget`, `SilhouetteReaction`(셸), `WeakPointReaction`, `WeakPointTestBinder`(_reaction·_weakPoint 명시), `WeakPointShellMask`(셸, [WeakPoint_Visor])

### 결정 사항
- 약점 색 범위 = 약점 SphereCollider 값. 보이는 범위와 맞는 범위를 한 값으로 관리한다.
- 데미지 2배 판정은 이번 범위 밖(intent-019 Phase B). 콜라이더만 둔다.

### 현재 상태 및 이슈
- 셰이더 메시지 0, 컴파일 에러 0. Play(에디터가 백그라운드라 일시정지 + Step으로 프레임 진행)에서 스캔 → 셸 표시, 얼굴 앞 구 범위만 빨간색 확인(게임 뷰 캡처). 약점 끄면 count 0, 켜면 1로 복귀 확인.
- 경계가 구 모양이라 부품 경계와는 맞지 않는다. 위치·반경은 `WeakPoint_Visor`에서 조정.
- 로봇 몸체 CapsuleCollider 안에 약점 구 일부가 들어가 있어, 사격 판정 시 몸체 콜라이더에 먼저 맞을 수 있다. 피격 쪽 구현 때 레이어/우선순위 정리 필요.
- `WeakPointTestBinder`는 Assets/Test 스크립트 → 실제 적 프리팹에서는 enemy 파트가 SetWeakPoint 호출 필요.

### 다음 할 일
- 약점 위치·반경·색 튜닝(플레이 시점)
- 데미지 2배 판정 연결(약점 콜라이더 맞으면 배율), 몸체 콜라이더와의 우선순위
- 실제 적 프리팹으로 옮길 때 셸 SkinnedMeshRenderer 생성 방식(프리팹에 미리 두기 vs 런타임 생성) 결정

---

## 추가 작업: 약점 표시 강조

- 피드백: 약점 빨간색이 몸체 위에서 티가 안 남(가산 블렌드 + 정면이라 외곽선이 거의 안 보임).
- `ScanHologram.shader`에 추가: 약점 범위 채우기(`_WeakFillAlpha` 0.45), 구 경계 링(`_WeakRingWidth` 0.035m, 가장 밝게), 맥동(`_WeakPulseSpeed` 5, 밝기 0.6~1), HDR 세기(`_WeakIntensity` 2.5). `WeakPointMask`가 (안쪽, 경계 링) 2값을 돌려준다.
- 셰이더 메시지 0. Play 캡처에서 얼굴 앞에 밝은 빨간 원과 테두리가 뚜렷하게 보이는 것 확인. 너무 하얗게 날아가면 `_WeakIntensity`를 낮춘다.

---

## 추가 작업: 실제 적 프리팹용 — 셸 자동 생성 + 랜덤 약점 위치 (Core 스크립트만)

### 작업 요약 및 방법
- 사용자 제약: `Scripts/Core/Scanning` 스크립트만 변경, 프리팹·씬·스포너는 건드리지 않음.
- `SilhouetteReaction`: `_autoShellMaterial` 추가. 지정하면 Awake에서 자식의 모든 SkinnedMeshRenderer마다 셸 복제본(원본의 자식 `ScanHologram`, 같은 mesh·bones·rootBone·localBounds, 서브메시 수만큼 같은 머티리얼, 그림자 끔)을 만들어 실루엣 렌더러로 쓴다. Melee 9개, Ranged 20개를 손으로 만들 필요 없음. `SilhouetteRenderers` 프로퍼티 추가. 비워 두면 기존 동작 그대로.
- `WeakPointShellMask`: `Renderer _shellRenderer` → `SilhouetteReaction _silhouette`. LateUpdate에서 반응의 셸 렌더러 중 켜진 것 전부에 약점 구 값을 넘긴다.
- 신규 `WeakPointSelector`: `_candidates`(후보 Transform, 콜라이더 없음, scale = 구 크기, scale 1 = 지름 1m), `_weakPoint`(SphereCollider 하나, Awake에서 radius 0.5·center 0 고정), `_reaction`. OnEnable마다 후보 하나를 무작위로 골라 약점을 그 자식으로 옮기고(local 0/identity/1) `WeakPointReaction.SetWeakPoint` 호출 → 스캔 중이 아니면 꺼진 상태로 놓임. 선택 시 후보 범위를 와이어 구 기즈모로 그림.

### 결정 사항
- 약점 초기화는 몬스터 쪽(`OnEnable`). 후보는 본에 붙은 프리팹 내부 위치라 스포너가 모르고, 풀 규칙(재사용 초기화는 프리팹 OnEnable)과 맞다. MonsterSpawner·GarbageCollectorSpawner·직접 배치 모두 동작.
- 스포너가 약점을 지정하는 기능은 만들지 않음(필요 시 공개 메서드 추가).

### 적 프리팹 구성 예시 (적 파트 적용용, 이번에 하지 않음)
```
Melee (루트): ScanTarget, SilhouetteReaction(_autoShellMaterial = M_ScanHologram),
              WeakPointReaction, WeakPointShellMask(_silhouette, [WeakPoint]), WeakPointSelector
├ head/WeakPointCandidate_Head (빈 오브젝트, scale = 구 크기)
├ spine_03/WeakPointCandidate_Chest ... (총 5곳)
└ WeakPoint (SphereCollider)
```

### 현재 상태 및 이슈
- 컴파일 에러 0, Scanning 관련 경고 0. Play 확인은 안 함(프리팹·씬 미수정).
- TempScene 그리퍼의 `WeakPointShellMask`는 필드가 바뀌어 `_silhouette` 재연결 필요(Assets/Test).
- 후보 scale이 비균등이면 기즈모는 타원, 콜라이더는 가장 큰 축 기준 구라 다르게 보인다. 후보 scale은 균등하게 둘 것.

### 다음 할 일
- 적 파트에 프리팹 구성 전달, 후보 5곳 위치·크기 배치
- 플레이 화면에서 셸 자동 생성(9/20개)·약점 이동·스캔 색 확인
- 약점 데미지 배율, 몸체 콜라이더와의 피격 우선순위(Phase B)

---

## 추가 작업: ScreenMarkerFollower 삭제
- 사용자 요청으로 `Scripts/UI/ScreenMarkerFollower.cs`(+meta, 미커밋)를 삭제했다. 저장된 씬·프리팹 참조 없음(GUID 검색 확인). 컴파일 에러 0.
- 약점 화면 UI 마커 방식(A안)은 폐기. 약점 표시는 셸 셰이더의 약점 구 마스크(빨간색)만 사용한다.
- TempScene에 미저장 상태로 붙어 있었다면 Missing Script로 보일 수 있다(Assets/Test).

---

## 추가 작업: 약점 초기화를 몬스터 파트로 넘기고 태그로 자동 연결

### 작업 요약 및 방법
- 합의: 몬스터 파트가 약점 후보 5곳·약점 콜라이더를 만들고, 몬스터 `OnEnable`에서 랜덤 위치 지정 + `SetActive(false)`까지 한다. 시스템 쪽 `MonsterSpawner`는 소환만 한다.
- 이에 따라 `WeakPointSelector.cs`(미커밋)를 삭제했다.
- 몬스터 코드가 스캔 코드를 모르게 하려고 **태그 `WeakPoint`** 로 연결한다(TagManager에 태그 추가).
  - `WeakPointReaction`: `WEAK_POINT_TAG` 상수 추가. Awake에서 자식(비활성 포함) 중 이 태그가 달린 콜라이더를 찾아 `SetWeakPoint`로 지정. 없으면 Assert. `SetWeakPoint`는 그대로 공개(다른 오브젝트로 바꿀 때용).
  - `WeakPointShellMask`: `_silhouette`가 비어 있으면 같은 오브젝트의 `SilhouetteReaction`, `_weakPoints`가 비어 있으면 자식 중 태그 달린 SphereCollider를 Awake에서 찾는다.
- 컴파일 에러 0.

### 결정 사항
- 몬스터 ↔ 스캔 연결 규칙 = 약점 콜라이더 오브젝트에 `WeakPoint` 태그. 프리팹마다 Inspector 연결 불필요. 피격 쪽 약점 판정(Phase B)도 같은 태그를 쓸 수 있다.
- 약점 콜라이더는 프리팹 안에 미리 있어야 한다(Awake에서 찾으므로 런타임에 새로 만들면 못 찾음). 위치 이동은 상관없음.

### 몬스터 프리팹 구성 (몬스터 파트)
- 루트: `ScanTarget`, `SilhouetteReaction`(`_autoShellMaterial` = M_ScanHologram), `WeakPointReaction`, `WeakPointShellMask` — 참조 연결 없이 붙이기만
- 약점 콜라이더(SphereCollider) 오브젝트에 태그 `WeakPoint`

### 현재 상태 및 이슈
- TempScene 캡슐·그리퍼 약점에는 태그가 없어 `WeakPointReaction` Assert가 뜬다(테스트 바인더가 이후 SetWeakPoint로 지정하므로 동작은 함). 필요하면 약점 오브젝트에 태그 추가.
- Play 확인 안 함.
- (같은 날 추가 결정) 약점 식별은 `WeakPoint` 태그로 확정. 마커 컴포넌트(`WeakPoint : MonoBehaviour`) 방식도 검토했으나 사용자 선택으로 태그 유지. `Scan` 태그 하나로 감지·약점을 겸하는 안은 약점 콜라이더가 스캔 전 비활성이라 감지에 못 쓰고, 몸체에 달면 약점과 구분이 안 되어 채택하지 않음(감지는 `ScanTarget` 컴포넌트).
