# 팀원 환경의 메인 씬 BGM 미재생 진단

## 작업 요약 및 방법
- 메인 씬, AudioManager, BGM SO, 오디오 파일 메타, Git 추적 상태와 기존 인수인계 기록을 대조했다.
- 코드와 에셋은 수정하지 않았다.

## 결정 사항
- 메인 씬은 첫 버튼 입력 후 BGM을 시작하는 설정이므로, 검은 스플래시 화면에서는 무음이 정상이다.
- `SOAudioData_BgmMainMenu`가 참조하는 `Whimsical Technology World.wav`와 `.meta`는 `Assets/ThirdParty/Audio/Bgm/`에 있고 Git 무시 대상이다. 팀원 환경에 두 파일이 같은 GUID로 존재하는지 먼저 확인한다.

## 현재 상태 및 이슈
- 로컬 WAV 메타 GUID `e525e777ddf7497bb80bb79ed013c036`는 BGM SO의 클립 GUID와 일치한다.
- 팀원의 SVN 작업 사본과 Unity 실행 상태는 이 환경에서 확인하지 못해 단일 원인으로 확정할 수 없다.

## 다음 할 일
- 팀원 환경에서 메뉴 진입 후 BGM SO의 Clip 참조, WAV와 `.meta` 파일 존재 여부, 콘솔 경고, BGM 및 Master 볼륨을 확인한다.
