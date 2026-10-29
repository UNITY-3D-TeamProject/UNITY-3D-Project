# C15 피드 맨 꼭대기 2라운드 진입 대사 수정

## 작업 요약 및 방법
- `SOCutscene_Feed_Round2Arrive.asset`의 기존 5개 대사를 사용자 제공 8개 문안으로 교체했다.
- 3번째와 7번째 PET 대사에 기존 `Feed_Round2Console` 카메라 키를 적용하고 나머지는 화자 카메라로 설정했다.

## 변경된 파일
- `Assets/_Project/ScriptableObjects/UIandCinematic/Cutscene/SOCutscene_Feed_Round2Arrive.asset`

## 결정 사항
- 컷씬 테이블 15번 연결, 기존 초상화와 시간 정지 설정을 유지했다.
- 가운데 기계 카메라는 기존 `FeedRound2.prefab`의 키를 재사용했다.

## 현재 상태 및 이슈
- 화자·대사·카메라 키 8쌍과 컷씬 테이블 연결을 정적 확인했다. `git diff --check`를 통과했다.
- Unity 플레이 화면에서 긴 대사의 표시와 카메라 구도는 확인하지 않았다.

## 다음 할 일
- 필요하면 피드 맨 꼭대기 도착 시 C15 재생 화면을 확인한다.
