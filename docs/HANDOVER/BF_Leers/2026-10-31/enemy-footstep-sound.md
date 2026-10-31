# AI_TODO2 사운드-3: Enemy 이동 사운드 미재생 수정

## 작업 요약 및 방법
- 원인: `SoundController.PlayFootstep` 을 부르는 발 디딤 알림(`OnFootstep`)이 `PlayerAnimator` 에만 있었고 `EnemyAnimator` 에는 없어 Enemy 발소리가 한 번도 호출되지 않음(이동 상태 연결·SoundController 는 이미 정상).
- `EnemyAnimator._footstepStride`(m, 기본 0=끔) 추가: 이동 중 실제 수평 이동 거리를 모아 보폭마다 `OnFootstep` 발생. `EnemyAnimationMediator.OnFootstep` 으로 전달, `CharacterMediator` 가 기존 `OnFootstepSoundCallback` 에 구독/해제.
- `SOAudioData_EnemyFootstep`(`ScriptableObjects/Audio/Sfx/AI/Enemy/`, 클립은 ThirdParty `ROBOTIC_Mech_Movement_05_Leg_Footstep_RR1_mono.wav`) 신규. `Melee`·`Ranged`·`Special` 프리팹에 `_footstep` 연결 + `_footstepStride` 1.5.

## 결정 사항
- 타이밍은 이동 거리 기준(애니메이션·모델 비의존). 플레이어 경로는 변경 없음.
- 보행자·도둑·GC·보스는 보폭 0 이라 영향 없음(필요 시 보폭만 지정).

## 현재 상태 및 이슈
- 리컴파일 통과. Play·육안(청취) 확인, 게이트 루프 미실행, 커밋 전.
- 클립이 ThirdParty(Git 미포함) 의존 → 다른 PC 에서는 클립이 비어 무음. 공유하려면 클립을 `_Project` 로 옮겨야 함.
- Melee·Ranged 의 기존 `SOAudioData_Footstem` 연결은 새 SO 로 교체됨.

## 다음 할 일
- Play 에서 Melee/Ranged/Special 이동 시 소리 확인, 보폭(1.5)·음량 조정.
- 사운드-8(도둑 피격·사망)·사운드-1 점검과 함께 확인.
