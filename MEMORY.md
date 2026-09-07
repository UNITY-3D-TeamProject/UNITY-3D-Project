# 프로젝트 작업 일지 (Project Memory)

이 파일은 AI 에이전트가 이전 세션의 작업 맥락을 잃지 않고 이어서 작업하기 위해 관리하는 파일이다.
AI 에이전트는 새로운 대화를 시작할 때 이 파일을 읽고, 대화를 종료하기 전에 이 파일을 업데이트해야 한다.

---

## 1. 최근 완료 작업 및 주요 결정 사항 (Decisions & Completed)
- `_Project` 및 하위 폴더 뼈대 생성 완료
- 외부 에셋(`ThirdParty/`) 및 테스트(`Test/`) 폴더를 Git에서 제외하는 `.gitignore` 설정 완료
- `CLAUDE.md`, `AGENTS.md`를 통한 AI 에이전트 지침 중앙화 구조 설정
- AI 코딩 원칙 (Karpathy Guidelines) 및 메모리 프로토콜 도입 완료

## 2. 현재 상태 및 이슈 (Current State & Known Issues)
- 깃허브 원격 저장소와 연동할 초기 `main` 브랜치 커밋 준비 상태
- 유니티 프로젝트 기본 환경 설정만 되어 있으며, 아직 게임 로직이나 씬 작업은 진행되지 않음

## 3. 다음 할 일 (Next Steps)
- 프로젝트 초기 파일들을 Git에 커밋(`git add .`, `git commit`)하고 푸시하기
- 팀원들과 협업할 실제 게임 씬, 플레이어 컨트롤러 등 본격적인 게임 로직 구현 시작
