# AI_TODO2 사운드-10: 가비지컬렉터 이동·공격 사운드 미재생 수정

## 작업 요약 및 방법
- 공격음 원인: `GC.prefab` 의 `SoundController._skillSounds` 에 `Attack` 소리가 `Execute`(0) 단계로 걸려 있었으나, `EnemyMeleeAttack` 은 `Impact`(7) 단계로 재생해 한 번도 나지 않음(Melee 프리팹은 7 사용). 단계를 `Impact` 로 변경.
- 이동음 원인: 사운드-3 과 같은 이유. `EnemyAnimator._footstepStride` 가 0 이라 발 디딤 알림이 없었음. GC 의 `EnemyAnimator._footstepStride` 를 2(m)로 지정. 기존 연결된 `SOAudioData_Footstem` 을 그대로 사용.
- 프리팹은 Unity MCP(eval, SerializedObject)로 수정. 코드 변경 없음, diff 는 `_footstepStride: 2` 와 `_phase 0 → 7` 두 줄.

## 결정 사항
- 새 SO·클립 추가 없이 기존 `SOAudioData_Footstem`, `SOAudioData_GCAttack`(클립 `LOFI_Tick_01_mono`) 재사용.
- 보폭 2m 는 GC 스케일 2 기준 초기값.

## 현재 상태 및 이슈
- Play·청취·게이트 루프 미실행, 커밋 전.
- 두 클립 모두 ThirdParty(Git 미포함) 의존 → 다른 PC 에서는 무음.
- `LOFI_Tick` 은 공격음으로 약할 수 있음(청취 후 교체 판단).

## 다음 할 일
- Map_3_File 라운드 2 에서 GC 이동 중 발소리·공격 시 소리 확인, 보폭·음량 조정.
