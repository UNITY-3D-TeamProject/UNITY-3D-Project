# TestScene 레이아웃 정리 (오브젝트 풀 공간 삭제, 실험 공간을 가로 통로 양옆 한 줄로)

대상: 개인 테스트 씬 `Assets/Test/UNITY-3D-Project-for-KSJ-Test/UnityAssets/Scene/TestScene.unity` (Git 미포함). 다른 씬은 열지 않았다. 코드 변경 없음.

## 1. 작업 요약 및 방법
- 좌표 기준: 가로 통로 = `Map/StartFloor`. 북(+z) = 통로 위, 남(-z) = 통로 아래.
- 오브젝트 풀 공간 삭제 (사용자 확인 후): `PoolTest` 전체(바닥, 꺼진 `ObjectPoolManager` 2개, `PoolTestSpawner`·SpawnPoint 3개), `Map/Signs/Sign_오브젝트 풀`.
  - `MonsterSpawnerTest/ObjectPoolManager`(켜짐, 몬스터·총알·도둑이 쓰는 실제 풀)는 남겼다. 그 안의 `PoolTestMonster` 항목도 그대로다.
- 통로 연장: 빈 폭이 모자라(북 약 37 m·남 약 22 m, 필요 약 88 m) `StartFloor`를 동쪽으로 늘렸다. x -41.25 ~ 66 (scale x -82.5 → -107.25, 위치 x 0 → 12.375). 스폰 (0, 0, 0)은 그대로다.
- 공간은 묶음째 옮겼다(회전 포함 강체 변환, 자식 상대 위치 유지).

| 공간 | 옮긴 오브젝트 | 전 → 후 (바닥 범위) | 회전 |
|---|---|---|---|
| 숫자 퍼즐(2라운드) | `NumberPuzzleTest`, `FileRoundTest/Checkpoint_R2_Puzzle`, `FileRoundTest/Sign_2라운드` | x -58~-34, z 31~57 → 북 x 6~32, z 5~29 | +90° (입구 띠가 통로 쪽, 퍼즐 판은 북쪽 끝) |
| 도둑(3라운드) | `FileRoundTest/R3_Area`, `FileRoundTest/Checkpoint_R3_Thief`, `FileRoundTest/Sign_3라운드`, `ThiefChaseTest`(웨이포인트 9개·Thief_Spawner) | z 57~81 → 북 x 34~58, z 5~29 | 없음 |
| 몬스터 스포너 | `MonsterSpawnerTest` (NavMeshSurface·풀·스포너 2개·표지판) | 통로 동쪽 끝 밖 x 41~57, z 3~19 → 남 x 22~38, z -31~-15 | 180° (표지판이 통로 쪽) |
| 컷씬 | `CutsceneTest` | 펫 안내 뒤 z 31~57 → 남 x 41~65, z -41~-15 | 180° (표지판이 통로 쪽) |

- 그대로 둔 공간: 북 `PetGuideTest`(x -34~-10)·점프맵+체크포인트 0~10(x -3.5~3.5), 남 `VFX_DataPacket_Showcase`(x -42.5~-20.5)·`ScanTest`/`Gripper Robot`(x -17~19).
- 통로 위로 나오게 된 표지판 `Sign_2라운드`·`Sign_3라운드`만 z 6.97(공간 바닥 안쪽 2 m)로 당겼다.
- `Map/FallZone`: 위치 (0, -15, 25) → (12.5, -15, 7.5), 박스 120×120 → 135×125 (x -55~80, z -55~70).
- NavMesh 2개 다시 굽기: `MonsterSpawnerTest`, `FileRoundTest/R3_Area` (`TestScene/NavMesh-*.asset` 갱신). MCP `bake_navmesh_surfaces`는 미구현이라 eval에서 `NavMeshAssetManager.StartBakingSurfaces`를 리플렉션으로 호출했다.
- 씬 저장 완료.

## 2. 결정 사항
- 실험 공간은 통로 북·남 한 줄에만 둔다. 공간이 늘면 통로를 동쪽으로 더 늘리고, `FallZone`도 같이 넓힌다.
- StageManager 체크포인트 13개·`Round Checkpoint Indices` [0, 3, 6, 9]는 바꾸지 않았다(오브젝트 이동만, 참조 유지).
- 초록 박스는 사용자 결정으로 그대로 둔다(아래 3번).

## 3. 현재 상태 및 이슈
- Play 확인(에디터가 백그라운드라 `EditorApplication.Step`으로 프레임 진행, 순간이동):
  - 플레이어 (0, 0.58, 0) 스폰·접지. 몬스터 2마리가 새 몬스터 바닥 NavMesh 위에서 움직임.
  - R2 띠 → 체크포인트 11. 점프맵 3·6 → 라운드 1·2. 라운드 2에서 도둑 5명이 새 R3 바닥에 생성, 모두 `isOnNavMesh`.
  - R3 띠 → 체크포인트 12. 빈 곳 낙하 → FallZone → R3 복귀 지점 (46, 0.58, 8.47) 재스폰.
  - 컷씬 발판 → 대화 재생(timeScale 0, 패널 표시) 화면 확인.
  - 실제 걷기·사격·도둑 잡기는 미확인. TestScene에는 보안 앱·보스 공간이 없어 해당 확인은 없다.
- 초록 박스의 정체: `VFX_DataPacket_Showcase/VFX_LocalBloom`. URP 로컬 `Volume`(Priority 20, 프로필 `Vfx/DataPacket/DataPacketPreview.asset`) + Trigger `BoxCollider` 28×24×64, 중심 (-31.5, 5, -26)이고, 초록 박스는 이 영역을 보여 주는 Gizmo다. 같이 보이는 흰 선은 꺼진 `VFX_PreviewCamera`의 시야 Gizmo다.
  - 2026-10-14 오전에 데이터 패킷 VFX 전시장과 함께 생겼다(에셋 생성 09:12~11:17). 이를 만든 작업 기록은 HANDOVER에 없고, 10-14 `scan-item-outline.md`에 이미 있었다는 언급만 있다.
  - 영역이 z 6까지라 통로 서쪽 끝(x -45.5 ~ -17.5)을 덮는다. 그 안에서는 블룸이 들어간다.
- 통로 위에 원래 있던 것(옮기지 않음): `ScanTest/Cube` (-2, 4.2, -4.7, 공중), 통로 가장자리 표지판 `Sign_체크포인트`(z 4)·`Sign_펫 안내`(z 3.5)·`Sign_스캔`(z -14.4).
- `Sign_숫자 퍼즐 시험`은 원래처럼 퍼즐 바닥 밖으로 0.8 m 걸쳐 있다(점프맵과의 빈 틈 쪽).

## 4. 다음 할 일 (Next Steps)
- 실제 걷기로 각 공간 입구(체크포인트 띠·표지판 방향) 확인.
- 필요하면 `ScanTest/Cube`와 통로 가장자리 표지판 정리 여부 결정.
- 필요하면 `VFX_LocalBloom` 영역을 통로에 걸치지 않게 줄일지 결정.
