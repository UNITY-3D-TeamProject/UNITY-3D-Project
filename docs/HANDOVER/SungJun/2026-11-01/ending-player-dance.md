# 엔딩 크레딧 무대: 플레이어 믹사모 춤 장면 (intent-111)

## 1. 작업 요약 및 방법
- 사용자가 믹사모에 MikeZ를 올려 받은 `Thriller Part 3.fbx`를 `Assets/ThirdParty/Mixamo/`에 복사.
- 믹사모 FBX는 원래 뼈 이름(root/pelvis/…)을 그대로 쓰지만 `SK_ZMike` 노드가 없어 클립 경로가 `root/...`(맨 위 노드 곡선은 경로 "")이다. 플레이어 애니메이터 기준 경로는 `SK_ZMike/root/...`라서, 경로를 바꾼 새 클립 `ThirdParty/Mixamo/EndingCast_Player_Thriller.anim`을 만들었다 ("" → `SK_ZMike`, 나머지 앞에 `SK_ZMike/`). 변화 없는 곡선·직선으로 되살릴 수 있는 키는 뺌(최대 오차 0.0008).
- `EndingCast_Player.controller`에 상태 `EndingCast_Player_Thriller` 추가 (상태 이름 = 클립 이름).
- 새 장면 `EndingCastPlayerDanceBeat` + 프리팹 `Beats/02_PlayerDance`:
  - 시작 배치는 인사 끝 모습과 같게(플레이어 x -0.4 화면 보기 Idle, 펫 옆 0.45/0.9) 해서 이어질 때 튀지 않음.
  - 춤 클립을 0.5초 섞으며 시작, (길이 - 0.5초) 뒤 같은 상태로 다시 섞어 처음부터 반복. `_repeatCount` 0 = 크레딧 끝까지 계속.
  - 펫은 (0.8, 1.3, 0.4)로 비켜나 화면을 보고 같이 춤춤 (`_petDanceState`, 기본 `R11_Dance0`, 사용자 요청으로 추가).

## 2. 결정 사항
- 사람형(Humanoid) 아바타 없이 Generic 그대로 붙였다 (뼈 이름이 같아서 경로만 맞추면 됨).
- 변환 클립이 약 23MB(매 프레임 움직이는 춤이라 줄여도 크게 안 줄어듦)라 `_Project`가 아닌 `ThirdParty/Mixamo`에 둠. → 프리팹·컨트롤러가 ThirdParty 파일을 참조하므로, 다른 팀원도 ThirdParty를 받아야 춤이 나온다 (플레이어 모델 자체도 ThirdParty라 같은 조건).
- 같은 믹사모 춤을 더 쓰려면: FBX를 `ThirdParty/Mixamo/`에 넣고 같은 방식(경로 앞에 `SK_ZMike/`)으로 클립을 만들어 상태 추가 → 장면의 `_danceClip`에 연결.

## 3. 현재 상태 및 이슈
- 리컴파일 통과, 콘솔 에러 0. 에디터 Play에 프리팹을 임시로 띄워 인사 → 춤 전환, 춤 중 렌더, 같은 상태 크로스페이드로 처음 재시작되는 것 확인. 임시 오브젝트 삭제.
- 춤 끝 → 처음 넘어가는 0.5초 구간을 화면으로 직접 보지는 않음. 실제 엔딩(`Map_5_Live`) 미확인. 커밋 안 함.
- 펫 춤은 인스펙터 `Pet Dance State`에서 `R11_Dance1` 등으로 바꿀 수 있음. 임시 Play에서 펫 `R11_Dance0` 재생 확인.

## 4. 다음 할 일 (Next Steps)
- 실제 엔딩에서 춤 위치·펫 자리 확인.
- 적 등장 등 다음 장면 추가 (춤을 계속 반복하면 뒤 장면은 안 나오므로 `_repeatCount` 지정 필요).
