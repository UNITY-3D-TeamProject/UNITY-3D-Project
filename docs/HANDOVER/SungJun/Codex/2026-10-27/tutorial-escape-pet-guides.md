# 탈출 코스 펫 안내 P8~P14 문구 수정

## 작업 요약 및 방법
- `SOPetGuideTable.asset`의 P8~P14 문구를 사용자 제공 문안에 맞췄다.
- P12는 같은 대화 중 추가로 받은 최신 문안(“이번엔 꽤 멀리 뛰어야 할 거야!”)을 적용했다.
- P13 문구는 이미 요청과 같아 유지했다.

## 변경된 파일
- `Assets/_Project/ScriptableObjects/UIandCinematic/PetGuideTable/SOPetGuideTable.asset`

## 결정 사항
- 기존 번호와 `Portrait` 연결은 유지하고 문구 및 줄바꿈만 수정했다.
- P12의 추가 문안에는 강조 표시가 없어 일반 텍스트로 저장했다.

## 현재 상태 및 이슈
- P8~P14의 문구와 줄바꿈이 요청 문안과 일치함을 검증했고 `git diff --check`를 통과했다.
- P12는 기존 안내보다 길다. Unity 플레이 화면에서 패널 안에 모두 표시되는지는 확인하지 않았다.

## 다음 할 일
- 필요하면 튜토리얼 탈출 코스에서 P12 안내 패널의 표시 범위를 확인한다.
