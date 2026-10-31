# 3D 효과음 한쪽 귀 쏠림 완화 — intent-105

## 작업 요약 및 방법
- 증상: 이어폰에서 튜토리얼 플레이어 총소리, 플레이어에게서 떨어진 오브젝트 소리가 한쪽 귀로만 들리고 반대쪽은 무음.
- 원인 진단
  - AudioListener는 `Player.prefab` 루트에 있음(`Main Camera.prefab`의 리스너는 꺼짐).
  - `AudioManager.PlaySfx`가 모든 3D 효과음을 `spatialBlend = 1`, `spread = 0`으로 재생 → 리스너 옆에서 난 소리는 한쪽 채널로 100% 쏠림.
  - 총소리는 `SkillMediator.NotifyFirePhase`가 위치 `null`을 넘겨 `SoundPrefab`(Player 루트 바로 아래, 위치 0) 위치에서 남 → 리스너와 거리 0이라 방향 계산이 깨져 한쪽으로 쏠림.
  - 클립(`PLASMA_CANNON_Short_Dry_mono.wav`)은 모노라 파일 문제는 아님.
- 수정 (`Core/Audio/AudioManager.cs`만)
  - `[Header("3D SFX")]`에 `_sfxSpread`(Range 0~360, 기본 120) 추가.
  - `PlaySfx`: `source.spread = _sfxSpread`.
  - `PlaySfxLoop`: `Mathf.Max(spread, _sfxSpread)` 적용. `MusicFile`처럼 180을 넘기는 곳은 그대로 180.

## 결정 사항
- Core만 수정하고 호출 쪽(SoundController 등, 팀원 담당) API는 그대로 둠.
- 리스너 위치·회전은 바꾸지 않음. 카메라로 옮기면 3인칭 뒤 카메라라 들리는 거리가 달라짐(사용자 의견).
- 퍼짐 값은 AudioManager 인스펙터에서 조정. 새 필드라 기존 프리팹·씬은 기본값 120을 씀.

## 현재 상태 및 이슈
- 리컴파일 에러 0.
- 실제 청취(이어폰 좌우 출력) 확인 안 함. 120이 방향감과 양쪽 들림 사이에서 적당한지는 들어 보고 조정 필요.
- 거리 0인 플레이어 자기 소리(총소리·점프·발소리)는 양쪽에서 들리겠지만 여전히 한쪽으로 약간 기울 수 있음. 거슬리면 리스너 근처 소리만 더 넓게 퍼뜨리는 처리를 검토.
- 커밋 안 함.

## 후속: 3D 효과음 도플러 끔
- 증상: 튜토리얼 데모파일(`MusicFile`) 반복음이 플레이어가 아래로 떨어지면 피치가 내려가 들림.
- 원인: 3D 소스의 `dopplerLevel`이 기본값 1, 프로젝트 `Doppler Factor`도 1. 리스너가 플레이어 몸(CharacterController 오브젝트)에 있어 낙하 속도만큼 도플러로 피치가 내려감. 리스폰 순간이동 때도 튈 수 있음.
- 수정: `AudioManager.CreateSfxSource`에서 `dopplerLevel = 0`. 프로젝트 설정은 건드리지 않음.
- 리컴파일 에러 0, 실제 청취 확인 안 함, 커밋 안 함.

## 다음 할 일
- 튜토리얼에서 떨어질 때 데모파일 소리 피치가 그대로인지 확인.
- 튜토리얼에서 이어폰으로 총소리·주변 기믹 소리 좌우 확인, 필요하면 `_sfxSpread` 조정.
- 문제 없으면 intent-105 resolved 처리 후 `docs/intent/clear/`로 이동.
