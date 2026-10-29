# 피드 앱 악성 피드 M2~M6 대사 수정

## 작업 요약 및 방법
- M2 상호작용, M3 풍선, M4 스타포스, M5 카드 짝 맞추기, M6 다른 그림 찾기 독백을 사용자 제공 문안으로 교체했다.
- M4는 PET 대사 2개, 나머지는 각 1개로 구성했다.
- 요청한 미니게임 UI 안내 문구와 UI 구성은 추가하지 않았다.

## 변경된 파일
- `Assets/_Project/ScriptableObjects/UIandCinematic/Monologue/SOMonologue_Feed_Malicious1_Interact.asset`
- `Assets/_Project/ScriptableObjects/UIandCinematic/Monologue/SOMonologue_Feed_Malicious2_Balloon.asset`
- `Assets/_Project/ScriptableObjects/UIandCinematic/Monologue/SOMonologue_Feed_Malicious3_StarForce.asset`
- `Assets/_Project/ScriptableObjects/UIandCinematic/Monologue/SOMonologue_Feed_Malicious4_CardMatch.asset`
- `Assets/_Project/ScriptableObjects/UIandCinematic/Monologue/SOMonologue_Feed_Malicious5_SpotDiff.asset`

## 결정 사항
- 독백 테이블의 M2~M6 번호, 기존 초상화, 줄당 4초 표시 시간을 유지했다.
- 기존 M5·M6의 두 번째 안내 대사는 요청 문안에 없으므로 제거했다.

## 현재 상태 및 이슈
- 독백 테이블의 GUID 연결과 에셋별 화자·문구·줄 수를 정적으로 확인했다. `git diff --check`를 통과했다.
- Unity 플레이 화면에서 긴 대사가 4초 동안 읽기 편한지 확인하지 않았다.

## 다음 할 일
- 필요하면 피드 앱 미니게임 진입 지점에서 독백 표시와 타이밍을 플레이 확인한다.
