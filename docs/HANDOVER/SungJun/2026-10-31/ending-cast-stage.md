# 엔딩 크레딧 왼쪽 캐릭터 무대 (intent-101)

## 1. 작업 요약 및 방법
- 크레딧 왼쪽 절반(원래 사진 자리)에 캐릭터 장면을 넣었다. 사용자 선택: "섞기"(장난 → 커튼콜), 사진 대신 애니메이션.
- 로딩 러너(intent-097)와 같은 방식: 스크립트·충돌체를 뺀 모습 전용 복사본 + RT 카메라를 먼 곳(y=-3000)에 둔 `Prefabs/UI/Credits/EndingCastStage.prefab`, 크레딧 왼쪽 `CastView` RawImage로 표시.
- 연출 `UI.Credits.EndingCastDirector`: 플레이어·펫 장난(머리 위 돌기·웃기 / 손 흔들기 / 콩·깜짝 / 쫓아가기 / 같이 점프 / 펫 춤) → 카메라가 뒤로 빠지며 GC·근접·원거리·특수·도둑·보스B 순서로 걸어 들어와 줄 서기 → 피날레 반복.
- 애니메이터 `Animations/Ending/EndingCast_*.controller` 8개(상태 이름 = 클립 이름).
- `EndingCredits`에 `_castStage` 필드: 크레딧이 시작될 때(어두워지기 시작) 무대를 켬.
- `Map_5_Live`: 무대 인스턴스(꺼 둠), `CreditsGroup/CastView` 추가, 연결. 명단 아래에 ASSETS & SOURCES / SPECIAL THANKS / TOOLS / 제목 / 마지막 문장 추가(사용자 요청).

- 후속: 크레딧 끝 페이드아웃. 명단이 다 올라가고 2초 뒤 `CreditsGroup/FadeOutCover`(맨 위 검은 이미지)가 2초 동안 화면을 덮은 뒤 메인 메뉴로(`EndingCredits._fadeOutCover`·`_fadeOutSeconds`).

## 2. 결정 사항
- 적·NPC는 게임 크기의 0.65배(한 화면에 줄 세우기 위해). 플레이어·펫은 원래 크기.
- 장면은 크레딧 스크롤과 따로 돈다(시간 동기화 없음). 피날레는 크레딧이 끝날 때까지 반복.
- 실제 시간(unscaled)으로 움직이고 애니메이터도 UnscaledTime.
- 명단 추가 문구 제목은 기존 영어 제목에 맞춰 `ASSETS & SOURCES` / `SPECIAL THANKS` / `TOOLS`로 썼고, `THANK YOU FOR PLAYING`은 맨 끝에 그대로 둠.

## 3. 현재 상태 및 이슈
- 리컴파일 통과, Play 캡처로 장면별 구도 확인. 실제 엔딩 흐름에서 크레딧과 겹친 모습은 미확인.
- 도둑(검은 로봇)은 어두워 뒤줄에서 잘 안 보임.
- 에셋 출처·스페셜 땡스 내용은 "..."로 비워 둠.
- 커밋 안 함.

## 4. 다음 할 일 (Next Steps)
- 실제 엔딩에서 확인 후 타이밍·위치·크기 조정(`EndingCastDirector` 인스펙터 값, 프리팹 안 위치).
- 에셋 출처·스페셜 땡스 명단 채우기.
