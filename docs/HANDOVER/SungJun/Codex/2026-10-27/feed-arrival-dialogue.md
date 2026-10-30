# 피드 앱 도착 컷씬 대사 수정

## 작업 요약 및 방법
- `SOCutscene_Feed_Enter.asset`을 사용자 제공 7단계 문안으로 교체했다.
- 기존 `SOCameraShot_Feed_Overview`의 피드 전경 → 기둥 꼭대기 카메라 연출을 유지했다.
- 악성 미끼 피드 설명 두 대사에 `Feed_DecoyFeed` 카메라 키를 지정했다.

## 변경된 파일
- `Assets/_Project/ScriptableObjects/UIandCinematic/Cutscene/SOCutscene_Feed_Enter.asset`

## 결정 사항
- 기존 컷씬 테이블 13번 및 피드 씬의 시작 트리거 연결은 유지했다.

## 현재 상태 및 이슈
- 7단계의 화자·문구·카메라 키, 샷 순서, 씬의 컷씬 13번 호출을 정적 검증했다. `git diff --check`를 통과했다.
- Unity 플레이 화면에서 카메라 구도와 대사 표시 상태는 확인하지 않았다.

## 다음 할 일
- 필요하면 피드 앱 진입 직후 대사와 카메라 흐름을 플레이 확인한다.
