# 엔딩 크레딧 무대: 추격 전 플레이어 Flair(비보잉) 5초 (intent-114)

## 1. 작업 요약 및 방법
- 사용자가 받은 믹사모 `ThirdParty/Mixamo/Flair.fbx`(1초짜리 반복 동작, 처음·끝 자세 차이 최대 0.004)를 Thriller와 같은 방식으로 변환:
  - `ThirdParty/Mixamo/EndingCast_Player_Flair.anim` = 원본 곡선 171개를 그대로 복사하고 경로 앞에 `SK_ZMike/`를 붙임("" → `SK_ZMike`). 키 줄이기 안 함.
  - 춤 원본에 없는 눈·눈썹 곡선 250개는 Thriller 변환 클립에 넣어 둔 고정값(Idle 눈 뜬 모습)을 그대로 가져옴.
  - Loop Time 켬.
- `EndingCast_Player.controller`에 상태 `EndingCast_Player_Flair` 추가 (Write Defaults는 Thriller와 같게).
- `EndingCastPlayerDanceBeat`: 클립이 반복(Loop Time)이면 한 번만 틀고 `길이 × _repeatCount`만큼 기다림(0이면 계속). 반복이 아닌 클립(Thriller)은 기존처럼 "길이 - 섞기 시간"마다 처음부터 다시 섞음.
- 프리팹 `EndingCastStage/Beats`: `02_PlayerDance`를 복사해 `03_Flair` 추가, 기존 `03_Chase` → `04_Chase`로 이름만 변경.
  - `03_Flair`: 클립 Flair, 섞기 0.3초, 반복 5 (= 5초), 시작 플레이어 상태·펫 상태 이름 비움(Thriller 끝 자세에서 바로 섞어 들어가고 펫은 계속 춤), 펫 시작 위치 = 춤 자리 (0.8, 1.3, 0.4).

## 2. 결정 사항
- 짧은 반복 동작은 같은 상태로 다시 섞지 않고 애니메이터 반복에 맡긴다 (1초 클립을 0.5초씩 섞으면 절반이 섞이는 구간이 됨).
- 변환 클립은 Thriller와 같이 Git 제외 폴더(`ThirdParty/Mixamo`)에 둔다 → 다른 팀원도 ThirdParty를 받아야 Flair가 나온다 (클립이 없으면 프리팹 참조가 비어 `03_Flair`는 바로 건너뜀).

## 3. 현재 상태 및 이슈
- 리컴파일 통과, 새 콘솔 에러 없음. 프리팹을 불러와 Flair를 샘플링: 클립 경로 421개 모두 플레이어 뼈에 연결, 손·발·머리 범위 x -0.79~0.01 / y -0.12~0.69, 펫까지 최소 1.27m. 미리보기 렌더(0초·0.33초)에서 화면 안에 잘 보임.
- Play로 Thriller → Flair → 추격 전환은 확인 안 함. 실제 엔딩(`Map_5_Live`) 미확인. 커밋 안 함.
- `CoPetDance`가 매 춤 장면 시작마다 펫을 `R11_Run`으로 0.8초 이동시키므로, Flair 시작 때 펫이 제자리에서 0.8초 날기 동작을 했다가 다시 춤춘다 (이미 그 자리라 움직이지는 않음).
- 작업 중 에디터가 저장한 것으로 보이는 무관한 변경(`Pretendard-Bold SDF.asset`, RenderTexture 여러 개 줄바꿈, `ShaderGraphSettings.asset`, `UNITY-3D-Project.slnx`)이 working tree에 있음 — 커밋할 때 빼야 함.

## 4. 다음 할 일 (Next Steps)
- 실제 엔딩에서 Thriller 끝 → Flair 섞임, Flair 5초 뒤 추격 전환 확인.
- 펫 0.8초 날기 동작이 거슬리면 장면에 "펫 이동 생략" 옵션 추가 검토.
