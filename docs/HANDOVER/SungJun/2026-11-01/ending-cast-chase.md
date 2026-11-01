# 엔딩 크레딧 무대: 가비지 컬렉터 추격 장면 (intent-113)

## 1. 작업 요약 및 방법
- 춤(`02_PlayerDance`) 뒤 세 번째 장면 `Beats/03_Chase` 추가 (`EndingCastChaseBeat`). 모두 왼쪽 → 오른쪽으로 일정한 속도로 화면을 가로지르고, 컷이 바뀔 때 카메라가 바로 넘어간다.
  1. 컷 1 (시작 배치 카메라 0/1.2/-6.5): 플레이어가 `Run_Chase`(양손을 앞으로 뻗고 달리기, MikeZ 기본 클립 `Anim_ZMIKE_RunChase`)로 x -2.6 → 2.4, 2.4초. 펫은 플레이어 기준 (0.7, 1.2, 0.3) 앞 위쪽에서 `R11_Run`으로 같이 날아감.
  2. 컷 2 (카메라 0/0.6/-7, 아래서 올려다봄 -5°): GC만 x -2.8 → 2.8, 2.0초. 플레이어·펫은 화면 밖 x 50에 세워 둠.
  3. 컷 3 (넓은 카메라 0/1.3/-10): 플레이어·펫 x -3.4 → 6.4, 4.9초. GC는 2m 뒤(`_chaseGap`)에서 따라옴.
- GC 모습 복사본: `GC.prefab`의 `Model/R_Chompbot_01`을 복사해 무대 루트 `GarbageCollector`(1.4배, 키 약 2.2m, 기본 꺼짐) 아래에 둠. 스크립트는 지우고 Animator만 남김(`UnscaledTime`, `AlwaysAnimate`). 컨트롤러는 intent-109에서 지웠던 `EndingCast_GC.controller`(Idle·Run·Attack)를 git(`0587e272^`)에서 되살림.
- `EndingCast_Player.controller`에 `Run_Chase` 상태 추가.
- `EndingCastDirector.IsPaused` 추가: 설정창이 열려 있으면 GC 애니메이터도 속도 0 (장면이 직접 처리).
- 춤 `_repeatCount` 0 → 1 (0이면 뒤 장면이 안 나옴).

## 2. 결정 사항
- GC는 이 장면만 쓰므로 참조(`_collector`, `_collectorAnimator`)를 무대가 아닌 장면 스크립트에 둠. 장면 시작(컷 1)·끝에서 끔.
- 컷에 안 나오는 플레이어·펫은 끄지 않고 화면 밖으로 옮김 (끄면 애니메이터 상태가 초기화됨).
- 추격이 마지막 장면이라 `_repeatCount` 0 = 크레딧 끝까지 컷 1~3 반복. 반복할 때마다 `PlaceAtStart`로 컷 1 카메라를 다시 잡음.
- 시작·끝 x는 화면 폭(FOV 30, RT 1024×1152)에서 펫·GC가 가장자리에 걸려 갑자기 나타나지 않게 계산한 값.

## 3. 현재 상태 및 이슈
- 리컴파일 통과, 새 콘솔 에러 없음.
- 열린 씬이 저장 안 된 `ZMike_Demo`라 씬을 바꾸지 않고 확인함: 프리팹 미리보기 씬에서 컷별 자세를 잡아 무대 카메라로 렌더(손 뻗고 도망·GC 혼자·셋 한 화면 구도 확인), 장면 코루틴을 직접 돌려 컷 순서·위치·GC 켜짐/꺼짐·반복 확인.
- 실제 엔딩(`Map_5_Live`)과 Play 재생(애니메이션 속도 대비 발 미끄러짐, 컷 전환 느낌)은 미확인.
- `SaveAssets`가 같이 저장한 폰트 아틀라스·RenderTexture 6개·slnx는 되돌림.
- 클립 모두 ThirdParty(MikeZ·Rob11·ChompBot) 의존. 커밋 안 함.

## 4. 다음 할 일 (Next Steps)
- 실제 엔딩에서 달리는 속도·컷 길이·GC 크기 확인 후 인스펙터(`03_Chase`)에서 조정.
- 춤 1번 + 추격 크레딧 끝까지 반복은 사용자 확정. intent-113은 `resolved`로 `docs/intent/clear/`에 이동.
