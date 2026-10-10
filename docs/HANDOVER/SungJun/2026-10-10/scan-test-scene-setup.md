# TempScene 스캔 시험 구성 (2026-10-10)

## 작업 요약 및 방법
- 스킬(`Scan.cs`) 연결 전에 `Core.Scanning` 코드를 시험하기 위해 `TempScene`의 스캔 표지판(`Signs/Sign_스캔`, z=-12) 앞에 시험 오브젝트를 배치했다.
- `IScannable`을 구현한 실제 클래스는 `ScanTarget` 하나이므로, 오브젝트는 클래스별이 아니라 **반응(`ScanReactionBase`) 종류별**로 만들었다.
- 파동 발사용으로 기존 `effects_terrain_scanner`(비활성, 원본 유지)를 복제해 `ScanWave` + `ScanTestTrigger`를 붙였다. 레거시 `TerrainScanWave`는 복제본에서 제거했다.
- 모두 `Assets/Test/` 및 씬 안의 작업이라 Git에 올라가지 않는다. `_Project` 코드는 수정하지 않았다.

## 배치 (부모: `ScanTest`, 바닥 z=-9 줄)
| 오브젝트 | 위치 | 구성 | 기대 동작 |
|---|---|---|---|
| `Reveal_Platform` | (-6, -9) | `ScanTarget` + `MeshRevealReaction`, `M_ScanReveal` | 평소 안 보이다가 파동이 닿은 만큼 드러남 |
| `Marker_Interact` | (-2, -9) | `ScanTarget` + `MarkerReaction`, 자식 `Marker`(스프라이트+`Billboard`) | 닿으면 위에 노란 마커가 뜨고 서서히 사라짐 |
| `Enemy_WeakPoint` | (2, -9) | `ScanTarget` + `WeakPointReaction`, 자식 `WeakPointMarker`(빨간 구) + `WeakPointHitbox`(`WeakPoint` 배율 2) | 닿으면 약점 표시가 커지며 나타남 |
| `Wall` + `Hidden_Behind_Wall` | (6, -9) / (6, -11) | 숨은 캡슐에 `ScanTarget` + `MeshRevealReaction`, `M_ScanRevealXRay` | 벽 뒤 캡슐이 드러남 |

- 발사: Play 중 **T키** (R키는 기존 `TerrainScanner`가 쓰므로 피했다). 중심은 `Camera.main` 위치, 지속 6초, 지름 30m(`ScanTestTrigger` Inspector에서 조절).
- 약점은 표시 오브젝트가 꺼지므로 피격용 콜라이더와 `WeakPoint`를 표시와 **별도 자식**에 뒀다.

## 결정 사항
- 시험 전용 코드는 `Assets/Test/UnityAssets/Script/Scan/ScanTestTrigger.cs`, 약점 표시용 머티리얼은 `Assets/Test/UnityAssets/Material/M_TestWeakPoint.mat`에 둔다.
- 기존 `effects_terrain_scanner`는 비활성 상태로 유지한다. 켜면 `TerrainScanWave`가 같은 대상에 중복 신호를 보낸다.

## 현재 상태 및 이슈
- Play 모드에서 `ScanWave.Begin` 호출 시 네 대상 모두 `ScanTarget` 활성화, 렌더러/마커/약점 표시 켜짐을 확인했다. 컴파일 오류 0.
- T키 입력 자체는 MCP `simulate_key`로는 반응하지 못했다(에디터가 비활성이라 게임 루프가 멈춰 있었음. `runInBackground`가 꺼져 있음). 사용자가 직접 에디터에서 눌러 확인해야 한다. 시험 중 임시로 켠 `runInBackground`는 원복했다.
- 스폰 위치(z≈-3)에서 플레이어가 +z를 보고 있어 시험 오브젝트(z=-9)는 뒤쪽이다. 뒤로 돌아서 확인한다.
- `WeakPoint.GetMultiplier`를 호출하는 피격 코드는 아직 없다(타 파트 요청 대기).
- `TerrainScanWave`가 스캔 번호로 `GetInstanceID()`를 써서 항상 같은 번호다. 복제본에서는 제거했으므로 `ScanWave`에서는 영향 없다.

## 다음 할 일 (Next Steps)
- 에디터에서 Play → 뒤로 돌아 T키로 네 종류 반응과 페이드 아웃을 눈으로 확인한다.
- 확인 후 스킬(`Scan.cs`)에서 `ScanWave.Begin`을 호출하도록 연결하고, 이 시험 오브젝트는 정리한다.
- 약점 배율 적용은 타 파트 총알/근접 판정에 `WeakPoint.GetMultiplier` 사용을 요청한다.
