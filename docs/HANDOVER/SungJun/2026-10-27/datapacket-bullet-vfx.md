# 데이터 패킷 총알용 VFX 2개 분리 (트레일·충돌)

## 1. 작업 요약 및 방법
- 사용자가 보여 준 "데이터 패킷" 이펙트(하늘색·보라 사각 패킷 흐름 + 벽에 닿으면 팡 터짐)는 Git에 안 올라가는 `Assets/Test/UnityAssets/Vfx/DataPacket/Prefabs/DataPacket_Basic.prefab` 안의 파티클이었다.
  - 트레일: `SquarePackets`(사각 패킷, Loop, World, 거리당 14개) + `DataStreaks`(가는 선, Stretch)
  - 충돌: `ImpactPackets`(Loop 끔, 0.8초, 버스트 1회, Sphere)
- 나중에 총알 VFX에 넣어 쓸 수 있게 `_Project`로 떼어 냈다. 파티클 설정은 원본 그대로 복사했고, 바꾼 것은 `Play On Awake` 켬과 머티리얼 교체뿐.
- 새 에셋 (모두 `_Project` 안, Test 폴더 참조 0 확인)
  - 셰이더 `Art/Shaders/VFX/DataPacketUnlit.shader` — Test 셰이더 복사, 이름만 `Test/DataPacketUnlit` → `Project/DataPacketUnlit`
  - 머티리얼 `Art/VFX/DataPacket/M_VFX_DataPacket.mat`(원본 ParticleWhite), `M_VFX_DataStreak.mat`(원본 SpeedLines)
  - 프리팹 `Prefabs/Projectile/VFX/VFX_DataPacketTrail.prefab`, `VFX_DataPacketImpact.prefab`
- 원본의 `DataPacketTrail.cs`(Test, 따라가기·도착 시 충돌 재생·Destroy)는 가져오지 않았다. 연결은 총알 쪽에서 할 예정.

## 2. 결정 사항
- 색은 스크린샷과 같은 Basic(하늘색 → 보라 TwoColors). Charged(분홍)·Critical(주황)은 같은 프리팹에서 Start Color만 바꾸면 된다.
- 트레일은 루트 정면(+Z)이 총알 진행 방향. 자식 파티클은 원본처럼 Y 180° 돌아 있어 뒤로 흩날린다. Simulation Space가 World라 총알 자식으로 붙이면 지나간 자리에 남는다.
- 충돌은 Stop Action 없음(EffectManager 풀이 시간으로 반납하므로 Destroy를 넣지 않음). EffectManager로 쓸 때 SOEffectData Duration은 1.5초 정도(방출 0.8초 + 수명 최대 0.7초).

## 3. 현재 상태 및 이슈
- 에디터에서 프리팹을 임시로 꺼내 Simulate → 씬 뷰 캡처로 트레일·버스트 모양 확인, 임시 오브젝트 삭제(TestScene 변경 없음). 콘솔 에러 0.
- Play 중 실제 총알에 붙여 본 적은 없음. 커밋 안 함.
- 총알이 풀에서 다시 나올 때 트레일 잔상이 이전 위치에서 이어질 수 있음 → 꺼낼 때 `Clear()` 후 `Play()` 필요할 수 있음(미확인).

## 4. 다음 할 일
- `Bullet.prefab`(또는 ChargedBullet)에 `VFX_DataPacketTrail`을 자식으로 넣고, 충돌 지점에서 `VFX_DataPacketImpact`를 EffectManager(SOEffectData)로 재생.
- 총알이 사라질 때 트레일 잔상이 같이 꺼지는 문제는 연결 시 판단(떼어 내고 방출만 멈추기 등).
