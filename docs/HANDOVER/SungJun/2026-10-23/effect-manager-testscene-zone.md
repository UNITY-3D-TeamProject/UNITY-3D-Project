# EffectManager TestScene 시험 구역

## 작업 요약 및 방법
- 사용자 요청에 따라 TestScene에 `EffectManager` 프리팹을 배치하고, 발판 4개로 재생·반납을 시험하는 구역 `EffectManagerTest`를 만들었다.
- `Assets/_Project` 코드·프리팹은 바꾸지 않았다. 새 파일은 모두 Git에 올라가지 않는 개인 Test 폴더에 있다.
  - `Assets/Test/UNITY-3D-Project-for-KSJ-Test/UnityAssets/Script/Test/TestEffectCaller.cs`: TriggerPlate의 UnityEvent에서 `EffectManager.Instance`를 부르는 테스트 컴포넌트 (`PlayOnce`, `PlayBurst`, `ToggleAtPoint`, `ToggleOnPlayer`). 기존 `TestUIManagerCaller`와 같은 방식.
  - `Assets/Test/UNITY-3D-Project-for-KSJ-Test/UnityAssets/EffectManagerTest/`: 시험용 SO 3개
    - `SOEffect_Test_ElectricArc`: VFX_ElectricArc, 일회성, Duration 1, MaxSize 3
    - `SOEffect_Test_Fire`: VFX_Fire, 루프, StopFadeDuration 2.2(Smoke 수명), MaxSize 2
    - `SOEffect_Test_FireTrail`: VFX_FireTrail, 루프, StopFadeDuration 2.2, MaxSize 2
- 배치 (TestScene)
  - 씬 루트 `EffectManager` (프리팹 인스턴스)
  - `EffectManagerTest`: StartFloor 서쪽 끝에 이어 붙인 바닥(x -55~-41.25, z -13~5), 발판 x -44에 남→북 순서로 4개, 효과 위치는 서쪽, 표지판은 x -53.5.
    1. 일회성 Play (밟을 때마다 번개 1초)
    2. 동시 5개 Play (보관 상한 3)
    3. 루프 켜기/끄기 (불꽃, 다시 밟으면 Stop)
    4. 플레이어에 붙이기 (불꽃 꼬리, 다시 밟으면 Stop)

## 확인 결과 (Play, 실제 TriggerPlate 진입)
- 1번: (-7.5, 0.5, -46) 위치(첫 배치 기준)에서 재생, 1초 뒤 비활성으로 매니저 아래 반납.
- 2번: 5개 동시 활성 → 반납 후 3개만 남음(MaxSize 초과분 Destroy). 첫 1번 인스턴스 재사용 확인.
- 3번: 4초 넘게 계속 켜짐 → 다시 밟으면 방출 중단·남은 입자(66개) 유지 → 2.2초 내 반납, 입자 0.
- 4번: 플레이어 자식으로 붙어 이동을 따라감. 낙하 후 재스폰에도 같이 따라감. 다시 밟으면 방출 중단 후 매니저 아래로 반납.
- 구역 이동 후 다시 Play: 새 바닥 위에서 서 있기·3번 화면 캡처 확인. 표지판 글자는 판 안에 들어옴(반전은 아래 결정 사항에서 수정).
- Play 검증 중에만 `Application.runInBackground = true`로 두고 끝나고 false로 되돌렸다(에디터 비포커스 시 프레임 정지).

## 결정 사항
- 처음에는 ScanFloor 남쪽(z -45)에 만들었으나, 복사한 `ScanFloor`가 에디터에서부터 꺼져 있어 바닥이 꺼진 채 저장됐고, 그 구간은 시작 통로에서 걸어서 갈 수 없었다. 바닥을 켜고 구역을 StartFloor 서쪽 끝으로 옮겼다.
- 표지판은 다른 구역 표지판을 복사했으며, 동쪽에서 오는 플레이어가 읽도록 Sign 회전 270, Text 로컬 회전 0으로 맞췄다. (처음 Text를 180으로 돌렸을 때는 플레이어 쪽에서 좌우 반전으로 보여 사용자 지적 후 0으로 되돌림. 플레이어 위치 임시 카메라 캡처로 정상 확인)

## 현재 상태 및 이슈
- 실제 키보드로 걸어가서 밟는 조작은 하지 않았다(플레이어 순간이동으로 트리거 진입).
- VFX 프리팹이 모두 루프 파티클이라 일회성은 Duration으로 잘라 반납한다. 번개처럼 자연 소멸하는 진짜 일회성 프리팹은 아직 없다.
- 커밋하지 않았다. TestScene·Test 폴더는 Git 대상이 아니다.

## 다음 할 일 (Next Steps)
- 직접 Play로 걸어가 연출 느낌(Duration·StopFadeDuration 값)을 눈으로 확인하고 조정.
- 실제 사용처(호출 후보 문서의 폭발·보스 착탄 등)에 붙일 때는 해당 담당 파트와 범위를 정한 뒤 진행.
