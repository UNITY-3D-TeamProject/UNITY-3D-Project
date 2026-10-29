# 일반 로비 첫 곡 두 곡 중 무작위 선택

## 작업 요약 및 방법
- `SOAudioData`에 `RandomFirstTrackCount`를 추가했다. 0이면 기존 순서 또는 전체 셔플 설정을 그대로 따른다.
- 일반 로비 BGM 에셋에 2를 설정해 `Digital City Morning`과 `Colorful City Morning` 중 한 곡을 첫 곡으로 고른다.
- 다음 곡부터는 SO 등록 순서대로 진행한다. 파일맵 복귀 전용 `Digital Integrity Check`는 별도 SO에서 반복한다.

## 결정 사항
- 전체 셔플은 켜지 않는다.
- 일반 로비 에셋의 현재 순서는 `Digital City Morning` → `Colorful City Morning` → `Whimsical City Adventure` → `The Curious Maze`이다.

## 현재 상태 및 이슈
- Unity 컴파일 오류 0건. 에디터에서 일반 로비 4곡, 첫 곡 후보 2개, 전체 셔플 해제, 파일맵 복귀 전용 후보 0개를 확인했다.
- 실제 게임 재진입을 반복해 두 곡이 모두 첫 곡으로 재생되는지 청취 확인은 남아 있다.

## 다음 할 일
- 로비를 여러 번 재진입해 첫 곡 선택과 이후 순서를 청취한다.
