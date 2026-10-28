# 피드 2라운드 내 게시물 외형 확인

## 작업 요약 및 방법
- `FeedRound2.prefab`의 `MyFeedCard`와 `FeedComposeSequence` 연결을 확인했다.
- `T_FeedMine_base/title/song/photo/caption/hashtag.png`를 원본 좌표대로 겹쳐 완성 카드 정면 미리보기를 만들었다.

## 결정 사항
- 프로젝트 에셋은 수정하지 않았다. 미리보기는 Codex 시각화 폴더에만 저장했다.

## 현재 상태 및 이슈
- Unity 에디터 인스턴스가 연결되지 않아 Play 화면 캡처는 하지 못했다. 제공 이미지는 원본 텍스처 합성 미리보기다.

## 다음 할 일
- 게임 내 조명과 카메라가 적용된 실제 화면이 필요하면 에디터 실행 후 게시 연출을 재생해 캡처한다.
