# 엔딩 크레딧 왼쪽 무대를 장면(Beat) 단위로 나누기 (intent-110)

## 1. 작업 요약 및 방법
- 연출 순서를 코드에서 빼서, 무대 프리팹 `EndingCastStage/Beats` 아래 장면 오브젝트를 위에서부터 차례로 재생하게 바꿨다.
  - `EndingCastBeat`(새, 추상): 모든 장면 공통 "시작 배치" 필드(카메라 위치·각도, 플레이어 위치·방향·애니메이션, 펫을 머리에 태울지, 펫 위치·방향·애니메이션) + `PlaceAtStart` + `CoPlay`(장면마다 작성).
  - `EndingCastDirector`: 장면 실행기 + 공용 기능. 등장인물·카메라 참조(프로퍼티), `IsPetOnHead`(LateUpdate에서 머리 뼈 추적), `Play`·`Face`(static), `CoMove`·`CoTurn`·`CoWait`, 방향 상수 `FACE_CAMERA_YAW`(180)·`FACE_RIGHT_YAW`(90)·`FACE_LEFT_YAW`(270).
  - `EndingCastGreetingBeat`(새): 기존 등장 인사(뛰어 들어옴 → 마주 보기 → 인사)를 그대로 옮김. 시작 배치는 x -2.8, 오른쪽 보기, `Run`, 펫 머리 위.
- 프리팹에 `Beats/01_Greeting` 추가, `_beatsRoot` 연결.

## 2. 결정 사항
- 장면마다 시작 배치를 따로 정하고, 장면이 시작될 때 무대가 먼저 그 자리로 옮긴다 (사용자 선택). 순서를 바꿔도 앞 장면이 끝난 자리에 영향받지 않는다.
- 하이어라키 순서 = 재생 순서. 꺼진 장면 오브젝트는 건너뛴다. 이름 앞 번호(`01_`)는 보기용이고 순서에는 영향 없음.
- 마지막 장면이 끝나면 그 자리에서 멈춘다 (반복 없음).

### 새 장면 추가 방법
1. `Scripts/UI/Credits/`에 `EndingCastBeat`를 상속한 스크립트를 만들고 `CoPlay(EndingCastDirector stage)`만 작성한다 (예: `EndingCastPlayerDanceBeat`). 이동·회전·대기는 `stage.CoMove/CoTurn/CoWait`, 애니메이션은 `EndingCastDirector.Play(stage.PlayerAnimator, "상태이름")`.
2. 쓸 애니메이션은 `Animations/Ending/EndingCast_Player`·`EndingCast_Pet` 컨트롤러에 상태로 추가한다 (상태 이름 = 클립 이름).
3. 프리팹 `Beats` 아래 빈 오브젝트를 만들어 스크립트를 붙이고, 인스펙터에서 시작 배치를 정한다.
- 적 등장 장면은 적 캐릭터를 무대에 다시 넣어야 한다 (intent-109 커밋 `0587e272`에서 삭제, 그 이전 커밋에 `Cast_*` 오브젝트·컨트롤러 있음). 무대에 넣은 적은 장면 스크립트에서 켜고 끈다.

## 3. 현재 상태 및 이슈
- 리컴파일 통과. 에디터 Play에 프리팹을 임시로 띄워 확인: 첫 장면 결과 화면이 이전과 같음, 장면을 하나 복제해 두면 두 번째 장면이 다시 시작 배치(왼쪽 밖, 펫 머리 위)에서 시작함. 임시 오브젝트 삭제.
- 무대가 켜지는 첫 프레임의 시간 간격이 아주 크면(Play 진입 직후 같은 경우) 달리기가 한 번에 끝나 버린다. 실제 엔딩에서는 크레딧 도중 켜져서 문제 없을 것으로 보이나 미확인.
- 실제 엔딩 흐름(`Map_5_Live`)은 미확인. 커밋 안 함.

## 4. 다음 할 일 (Next Steps)
- 플레이어 춤 장면, 적 등장 장면 추가.
