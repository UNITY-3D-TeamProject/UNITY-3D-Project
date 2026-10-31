# AI_TODO2 사운드-8: 도둑 피격·사망 사운드 확인

## 작업 요약 및 방법
- `Thief.prefab` 의 SoundPrefab 오버라이드에 `_hit`(SOAudioData_Hit)·`_death`(SOAudioData_Death)가 이미 연결되어 있고, 두 SO 모두 클립이 있음을 확인.
- Map_4_Security Play 에서 Thief 프리팹을 리스너 앞에 생성해 `CurrentHp` 를 깎음(피격 → 0). 재생 중 AudioSource 에서 피격 클립(`IMPACT_Metal_Soft_Plate_01_mono`)과 사망 클립(`ROBOTIC_Short_Burst_42_Shut_Down_Deep_mono`)을 확인. 즉 프리팹 단독으로는 두 소리 모두 정상 재생.
- 코드·에셋 변경 없음.

## 결정 사항
- 재현되지 않는 문제를 추측으로 고치지 않는다.

## 현재 상태 및 이슈
- 실제 라운드(Map_3_File, `ThiefSpawner` 가 같은 프리팹 생성)에서 소리가 안 났다는 원인은 미확인. 후보: 도둑이 멀 때 거리 감쇠(maxDistance 30), SFX 소스 풀 고갈, 사용자가 들은 시점이 연결 이전 빌드.
- 사운드-8 은 체크하지 않음(직접 플레이 확인 필요).

## 다음 할 일
- Map_3_File 에서 실제로 도둑을 가까이서 맞혀 소리가 나는지 확인. 안 나면 그 씬에서 `AudioManager` 존재·SFX 풀 상태를 점검.
