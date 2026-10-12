# 담당별 오브젝트 풀 연결 가이드 작성

## 작업 요약 및 방법
- 사용자의 요청에 따라 지금까지 설명한 참조 전달, Get, 초기화, 반납을 Spawner·Player·Enemy 담당별 Markdown 가이드로 정리했다.
- 현재 풀·스포너·발사·탄환 코드를 재확인하고, 기존 기능과 앞으로 추가할 SetObjectPoolManager 및 재사용 초기화 진입점을 구분했다.
- 런타임 코드, 프리팹, 씬은 변경하지 않았다.

## 결정 사항
- 문서의 권장 예시는 탄환의 시작 상태를 Initialize에 모으고 OnRelease에서 필요한 정리를 수행한다. 타이머를 OnRelease에서 리셋하는 대안도 가능하다고 명시했다.
- 프리팹 에셋과 씬 인스턴스의 참조 차이, Spawner에서 발사 컴포넌트로 매니저 전달, Bullet에는 매니저 전달 불필요를 명시했다.
- Release와 OnRelease 및 ReleaseOrDestroy를 구분하고, Get의 활성화 순서와 실패 객체 반납을 설명했다.

## 변경된 파일
- `docs/HANDOVER/SungJun/2026-10-13/object-pool-team-integration-guide.md` 신규 작성.
- 이 작업 기록 신규 작성 및 `docs/HANDOVER/SungJun/HANDOFF_POINTER.md` 갱신.

## 현재 상태 및 이슈
- 코드 예시는 기존 클래스에 적용할 부분 예시이며 구현 완료가 아니다. Unity 컴파일 및 Play 검증은 수행하지 않았다.
- 기존 `docs/HANDOVER/SungJun/2026-10-12/object-pool-guide.md`의 PrefabPool 설명은 현재 코드와 다르다. 새 담당별 가이드는 현재 키별 풀 구조를 기준으로 작성했고 기존 파일은 수정하지 않았다.

## 다음 할 일
- 각 파트가 참조 전달 함수와 키 이름을 맞추고, Enemy 재사용 초기화 진입점을 정해 실제 연결한다.
- 공통 BulletController의 수정 담당을 정하고 문서의 완료 확인 항목으로 최초 생성과 재사용을 검증한다.
