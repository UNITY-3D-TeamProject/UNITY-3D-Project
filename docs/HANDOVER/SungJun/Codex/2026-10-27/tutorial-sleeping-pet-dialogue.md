# 잠든 펫 첫 만남 대사 수정

## 작업 요약 및 방법
- `SOCutscene_Tutorial.asset`의 잠든 펫 상호작용 컷씬 대사를 사용자 제공 C2 문안 9개로 맞췄다.
- 컷씬 테이블의 2번 항목이 해당 에셋을 참조하는 것을 확인했다.
- 각 대사의 `CameraKey`를 비워 화자 카메라를 쓰는 기존 방식을 유지했다.

## 변경된 파일
- `Assets/_Project/ScriptableObjects/UIandCinematic/Cutscene/SOCutscene_Tutorial.asset`

## 결정 사항
- 기존 두 마지막 대사를 요청 문안에 따라 세 대사로 나눴다.
- 대사 흐름 외의 씬, 코드, 컷씬 테이블은 수정하지 않았다.

## 현재 상태 및 이슈
- 에셋의 화자·문구 9쌍과 카메라 키를 검증하고 `git diff --check`를 통과했다.
- Unity 플레이 화면에서의 줄바꿈과 연출은 확인하지 않았다.

## 다음 할 일
- 필요하면 튜토리얼에서 잠든 펫에게 E 상호작용하여 대사창 표시를 확인한다.
