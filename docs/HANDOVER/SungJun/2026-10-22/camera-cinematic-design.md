# 카메라 연출 설계 (intent-063 작성, 구현 전)

## 1. 작업 요약 및 방법
- 질문: 대화 중 다른 카메라 컷을 넣을 수 있는지, 대화와 따로 카메라 컷을 보여 줄 수 있는지, 이런 기능을 UIManager에서 다 불러야 하는지.
- 코드 확인:
  - 대화 중 고정 카메라는 이미 `SDialogueLine.CameraIndex` → `DialogueDirector._fixedCameras`로 된다(`Canvas_InGame` 프리팹이라 씬마다 오버라이드 필요).
  - 대화 중에는 `PauseGameTime`이라 움직이는 장면을 보여 줄 수 없다.
  - 대화와 따로 쓰는 카메라 연출은 없다.
- [intent-063](../../../intent/intent-063-camera-cinematic-sequence.md)을 작성하고 INTENT_POINTER에 등록했다. 코드·씬 변경은 없다.

## 2. 결정 사항 (제안, 사용자 확인 대기)
- 씬 `CinematicSequence`(단계 목록: 카메라 샷 / 대화) + `CinematicManager.Play(sequence)`.
- 입력·UI 숨김·시간·CinemachineBrain 처리는 `CinematicManager`가 맡는다. UIManager는 UI만 맡는다(연출 → UIManager 한 방향).
- 첫 단계는 Timeline 없이 Cinemachine 블렌드만 쓴다.

## 3. 현재 상태 및 이슈
- 열린 질문 9건은 사용자 답변으로 결정했다(intent-063 "결정 사항").
  - 연출 동안 무적, `Dialogue` 입력 맵 공유, 건너뛰기 없음
  - 한 번만 재생은 Id + PlayerPrefs
  - 맵 씬 오브젝트, `Scripts/Core/Cinematic`(`Core.Cinematic`)
  - 연출 안의 대화는 끝나도 시간·입력·UI를 돌려주지 않는 옵션
  - 샷마다 블렌드, `CameraIndex` 유지, 펫은 그대로 따라옴, UI는 연출 내내 숨김
- 무적: `PlayerFacade`에 원하는 동안 무적을 켜는 공개 함수가 없다. 그래서 사용자 결정으로 첫 구현에서는 빼고, 시간이 흐르는 샷은 적이 없는 곳에서만 쓰기로 했다. 열린 질문은 모두 정리됐다.

## 4. 다음 할 일 (Next Steps)
- 열린 질문에 답을 받은 뒤 TestScene에서 구현한다(예: 발판 → 먼 곳 샷, 시작 연출 → 대화).
