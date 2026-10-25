# TestScene: EffectManager 구역 이동 + AudioManager 시험 구역 — 2026-10-23

## 1. 작업 요약 및 방법
- 사용자 요청: 가로 통로(StartFloor) 서쪽 끝에 붙어 있던 EffectManager 시험 구역을 옆으로 옮겨 통로가 늘어날 자리를 비우고, 같은 방식의 AudioManager 시험 구역을 만든다.
- `_Project` 코드·프리팹은 바꾸지 않았다. 새 파일은 Git에 올라가지 않는 개인 Test 폴더에 있다.
  - `Assets/Test/UNITY-3D-Project-for-KSJ-Test/UnityAssets/Script/Test/TestAudioCaller.cs`: TriggerPlate UnityEvent에서 `AudioManager.Instance`를 부르는 테스트 컴포넌트 (`PlayOnce`, `PlayThenStop`, `ToggleAtPoint`, `ToggleOnPlayer`). `SAudioHandle`을 보관하고 OnDisable에서 StopSfx. `TestEffectCaller`와 같은 방식.
  - `Assets/Test/UNITY-3D-Project-for-KSJ-Test/UnityAssets/AudioManagerTest/`: 시험용 SO 3개
    - `SOAudio_Test_LongExplosion`: explosion_rift (4.06초)
    - `SOAudio_Test_LoopEnergy`: loop_energy
    - `SOAudio_Test_LoopFire`: loop_fire
    - (클립은 ThirdParty `Archanor/Sci-Fi Arsenal`. 1번 발판은 공용 `SOAudioData_SfxShootLaser` 사용)
- 배치 (TestScene, 저장함)
  - 씬 루트에 `AudioManager` 프리팹 인스턴스 추가 (BGM 비어 있음).
  - `EffectManagerTest`: 루트를 Y 90° 회전·이동해 통로 북쪽으로 (바닥 x -54~-36, z 9~22.75). 입구가 동쪽 → 남쪽으로 바뀌고 표지판·소리 위치도 같이 돌아감. 통로와 잇는 다리 `Bridge_FromCorridor`(x -41~-37, z 4.75~9.25) 추가.
  - `AudioManagerTest`: EffectManagerTest를 복제해 서쪽에 배치 (바닥 x -76~-58, z 9~22.75). 두 구역 사이 다리 `Bridge_ToAudio`(x -58.5~-53.5, z 13~17). 발판(남쪽 줄 z 11.75)·소리 위치(z 17.25, 작은 구 `SoundMarker`)·표지판(북쪽):
    1. `1_PlayOnce`: 단발음 PlaySfx, 밟을 때마다 재생
    2. `2_PlayThenStop`: 긴 단발음을 내고 1초 뒤 StopSfx로 끊기
    3. `3_LoopToggle`: 반복음 켜기/끄기, 끄면 1초 페이드
    4. `4_FollowPlayer`: 플레이어 따라가는 반복음, 다시 밟으면 바로 끄기
  - `Map/FallZone` BoxCollider를 서쪽으로 늘림 (x -55 → -80, 동쪽 80 유지). 새 구역에서 떨어져도 복귀하도록.
- 통로 서쪽 끝(x -41.25)은 이제 비어 있어, 통로를 서쪽으로 늘려도 두 구역(z 9 이상)과 겹치지 않는다.

## 2. 결정 사항
- 통로 북쪽 서쪽 끝 외에는 빈자리가 없어(북쪽 PetGuide x -34~, 남쪽 VFX 쇼케이스 x -42.5~) 두 구역을 통로 북쪽에 서쪽으로 나란히 두고 다리로 이었다.
- 오디오 구역은 직접 통로와 닿지 않고 EffectManager 구역을 지나서 간다.

## 3. 현재 상태 및 이슈 (Play, 플레이어 순간이동으로 발판 진입)
- 1번: 핸들 1로 레이저 단발음이 소리 위치(-73.5, 17.25)에서 재생·종료.
- 2번: 핸들 2로 재생, 정확히 1초 뒤 StopSfx, 4초 클립이 끝나기 전에 소스 정지.
- 3번: loop_energy가 5초 넘게 반복(소리 위치 고정) → 다시 밟자 볼륨 1.00→0.18을 약 1초 동안 줄인 뒤 정지.
- 4번: loop_fire가 플레이어를 따라 EffectManager 구역(-45, 18)까지 이동 → 다시 밟자 바로 꺼지고 따라가기 해제.
- 옮긴 EffectManager 구역 3번 발판: 불꽃이 새 소리 위치(-42.5, 17.25)에서 재생 확인.
- 게임 카메라 캡처로 표지판 글자가 반전 없이 읽힘 확인. 4번 문구가 판을 넘어 문구를 줄였고 4개 모두 판 안에 들어온 것을 수치로 확인.
- 실제로 소리를 들어 보지 않았고 실제 키보드로 걸어가 밟지 않았다. Play 중에만 `runInBackground = true`, 끝나고 false로 되돌림.
- 커밋 안 함. TestScene·Test 폴더는 Git 대상이 아니다.

## 4. 다음 할 일 (Next Steps)
- 직접 Play로 걸어가 소리 크기·거리감(Min/Max Distance 1/30)을 들어 보고 조정.
