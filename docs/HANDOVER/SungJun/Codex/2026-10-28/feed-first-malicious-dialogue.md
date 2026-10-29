# C14 첫 악성 피드 대사 수정

## 작업 요약 및 방법
- 피드 앱 첫 악성 피드 컷씬의 기존 대사 6개를 사용자 제공 9개 문안으로 교체했다.
- `SOCutscene_Feed_FirstMalicious.asset`의 화자 순서와 문구를 요청 표에 맞췄다.

## 변경된 파일
- `Assets/_Project/ScriptableObjects/UIandCinematic/Cutscene/SOCutscene_Feed_FirstMalicious.asset`

## 결정 사항
- 컷씬 테이블 14번 및 첫 악성 피드 프리팹의 호출 연결을 유지했다.
- 모든 줄의 기존 화자 카메라, 시간 정지, 초상화 설정을 유지했다.

## 현재 상태 및 이슈
- 9개 대사의 화자와 문구를 확인하고 `git diff --check`를 통과했다.
- Unity 플레이 화면에서 긴 대사의 줄바꿈과 표시 상태는 확인하지 않았다.

## 다음 할 일
- 필요하면 피드 앱 첫 악성 피드 앞에서 C14 재생 화면을 확인한다.
