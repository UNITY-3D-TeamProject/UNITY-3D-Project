# 엔딩 크레딧 영어·오른쪽 배치 (intent-096 후속)

## 1. 작업 요약 및 방법
- `Map_5_Live`의 `Canvas_EndingCredits/CreditsGroup/CreditsRoll` 텍스트를 영어로 바꿨다.
  - `FRAGMENT IN DEVICE`(72) / `CREDITS`(36, 회색)
  - PLAYER — Lee Sehun / AI & ENEMY — Lee Jungbin / MAP — Yang Junbeom / SYSTEM — Kim Sungjun
  - 끝 문구 `THANK YOU FOR PLAYING`
- 같은 오브젝트의 앵커 x를 0.5 → 0.75(오른쪽 절반 가운데), 폭을 1200 → 800으로 바꿨다.
- MainScene에 저장 안 된 변경이 있어 `Map_5_Live`는 Additive로 열어 이 씬만 저장하고 닫았다.

## 2. 결정 사항
- 왼쪽 절반은 비워 둔다. 사용자가 나중에 사진(회상 장면)을 직접 넣는다.
- 스크롤 방식(`EndingCredits`)은 바꾸지 않았다. 명단만 오른쪽 칸에서 올라간다.
- 이름 로마자 표기는 임시로 정한 것(성 이름 순서). 본인 표기가 다르면 텍스트만 고치면 된다.

## 3. 현재 상태 및 이슈
- 확인: 씬 diff는 `CreditsRoll` 앵커·폭·텍스트만, 제목 폭 744px(줄바꿈 없음), 임시 카메라 렌더로 오른쪽 배치 확인.
- 미확인: 실제 Play 엔딩 흐름에서 본 모습.
- `Pretendard-Bold SDF.asset`·`ShaderGraphSettings.asset` 변경은 이번 작업과 무관하니 커밋에서 뺀다.
- 커밋 안 함.

## 4. 다음 할 일 (Next Steps)
- 왼쪽 절반에 회상 사진 넣기(사용자).
- 팀원 이름 영문 표기 확인.
