# 파일맵 장애물 동시 소리 요청 수정

## 작업 요약 및 결정
- 사용자가 위치별 제한 수정을 요청한 뒤 0.05초 제한 자체의 필요성을 질문했다. 위치별 정책 추가보다 3D 단발음 제한 제거로 단순화한다고 설명하고 적용했다.
- AudioManager.PlaySfx는 TryPickClip의 shouldThrottle=false를 사용한다. UI 중복 제한과 반복음, 재생 소스 상한은 유지한다.
- 위치별 제한을 위해 시험적으로 추가한 거리 필드·리스트는 최종 코드에 없다.

## 검증 결과
- Unity 재컴파일 성공. 같은 프레임에 같은 클립의 다른 위치·동일 위치 3D 요청 모두 성공. UI 첫 요청 true·중복 false, UI 직후 3D true, 반복음 2개 모두 true.
- 파일맵에서 가시 효과음 SO의 클립이 null이었다. GUID `5cb925e37e628e34ea290f6dcf6d0296`에 대응하는 파일·meta·AssetDatabase 경로가 없다.
- 원본이 없는 상태를 숨기지 않고 Play 중 임시 생성 음원으로만 실제 장애물의 재생 경로를 검사했다. 가시 4개 중 2곳에서 AudioListener를 약 2.8m 거리로 옮겨 각각 7초 관찰했다. 자연 재생 2·3회, source peak 0.02719, listener peak 0.03372·0.04090. 프레임 2138 진행, timeScale=1, listenerPause=false.
- 임시 음원 연결·청취 위치·카메라 Brain·BGM은 복원했고 SO dirty=false 확인. Play 종료, TestScene 및 runInBackground=false 복원. 시험 스크립트 제거.

## 현재 상태 및 다음 할 일
- 사용자가 음원을 추가한 후 원본으로 재검증 완료했다. 추가 코드 변경은 없다.
- 원본은 `Assets/ThirdParty/Map_Sound/Sounds/Universal Sound FX/ROBOTICS/Servos/ROBOTIC_Servo_Small_Short_Fast_Mechanical_mono.wav`(약 0.245초). 같은 프레임 먼 위치·가까운 위치·동일 위치 요청 모두 true.
- 파일맵 실제 가시 두 곳 `(15, -1.10, 20)`, `(40, -0.79, 30)` 근처에 AudioListener를 약 2.8m 거리로 옮겨 각각 7초 관찰. 원본 자연 재생 2회·3회, non-virtual voice 관측 8회·12회. 소스 peak 0.04947, 최종 출력 peak 0.27799·0.10442. 같은 가시 효과음 최대 4개 동시 재생. 임시 음원 없이 실제 원본 출력으로 검증했으며 직접 귀로 들은 평가는 아니다.
- Play 종료 후 TestScene 및 runInBackground=false로 복원했고 카메라·BGM 복원, 임시 검증 스크립트 제거. intent-081 해결 처리.
- 검증 중에는 커밋하지 않았고, 이후 사용자가 각각 별도 커밋을 요청했다. 펫 수정 커밋 `5b4aee84`와 분리해 이번 오디오 수정 및 관련 기록을 커밋한다.
