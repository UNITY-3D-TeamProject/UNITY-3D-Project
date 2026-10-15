# 30. 전체 사운드 + VFX 작업

- **우선순위**: 미정
- **상태**: 대기
- **관련 작업**: [29. 총알 메시 + FX](29-bullet-mesh-fx.md)

## 목표
플레이어(및 펫·스킬) 행동 전반에 사운드와 VFX 를 붙인다.

## 현재 상황
- 플레이어 쪽 스크립트에 `AudioSource`·`AudioClip` 사용처가 없다. 효과 관련 코드는 `Core/Scanning/ScanWave`, `Tutorial/` 쪽에만 있다.
- 연결 지점 후보 (이미 이벤트가 있음): 점프 시작·착지, 구르기, 사격·차지 시작(`OnFireChargeChanged`), 피격(`OnHit`), 치명타(`OnCriticalHit`), 사망, 스킬 사용(`OnSkillUsed`), 탈것 탑승·요시 점프(`OnHoverChanged`), 스캔.

## 작업 범위 (예상)
- 사운드·VFX 재생 대상 목록 확정 후 연결.

## 열린 질문
- [ ] 대상 목록과 우선순위 (항목별 하위 문서로 나눌지)
- [ ] 사운드 구조: 공용 사운드 매니저가 있는가 / 새로 만드는가 / 다른 파트 담당인가
- [ ] 연결 방식: 새 SubMediator(예: 사운드·FX 중재자)로 이벤트 구독 / 각 컴포넌트에서 직접 재생 / Animation Event
- [ ] AudioMixer 그룹(SFX·BGM) 사용 여부
- [ ] 에셋 출처와 Git 포함 여부
