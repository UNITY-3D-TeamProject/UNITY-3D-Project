---
name: commit
description: 이 프로젝트의 브랜치/커밋 컨벤션(docs/commit-convention.md, Git-flow)에 맞춰 변경 사항을 커밋한다. "커밋해줘", "커밋 컨벤션대로 커밋" 요청 시 사용한다.
---

# 커밋 스킬 (Commit Skill)

`docs/commit-convention.md`에 정의된 형식으로 커밋 메시지를 작성하고 커밋을 생성한다.

## 절차

1. **컨벤션 확인**
   - 세션 중 아직 읽지 않았다면 `docs/commit-convention.md`를 먼저 읽는다.

2. **변경 사항 파악**
   - `git status`, `git diff` (staged + unstaged)로 무엇이 바뀌었는지 확인한다.
   - `.env`, credentials 등 민감 정보가 포함된 파일은 커밋에서 제외하고 사용자에게 알린다.

3. **스테이징**
   - 이번 작업과 관련된 파일만 이름을 지정해 `git add`한다.
   - `git add -A` / `git add .` 금지 — 의도하지 않은 파일이 섞여 들어갈 수 있다.
   - 이미 스테이징된 변경이 있으면 그 범위를 우선 존중한다.

4. **태그 선택** (`docs/commit-convention.md` 2-2 기준)
   - `feat`: 기능/시스템/스크립트/에셋 추가
   - `fix`: 버그/에러/씬/프리팹 문제 수정
   - `refactor`: 기능 변화 없는 구조 개선/최적화
   - `chore`: 폴더/패키지/Git/문서 작업
   - 변경이 여러 성격에 걸치면 가장 비중이 큰 태그 하나만 선택한다 (하나의 커밋 = 하나의 논리적 변경).

5. **작업자 이름 결정**
   - 현재 브랜치가 `feature/{이름}` 또는 `hotfix/{이름}` 형식이면 `{이름}`을 작업자로 사용한다.
   - 브랜치명에서 알 수 없으면 `git config user.name` 값을 사용한다.

6. **커밋 메시지 작성** (`docs/commit-convention.md` 2-1, 2-3 기준)
   ```text
   태그: 요약문
   - 작업자
   - 상세 작업 내용 1 (변경이 여러 개일 때만)
   - 상세 작업 내용 2 (선택)
   ```
   - 요약문은 `~구현`, `~수정`, `~제거`, `~추가` 형태로 끝맺는다 (`~함`, `~했음` 금지).
   - 요약문만으로 충분히 설명되면 상세 항목은 생략한다.
   - 커밋 메시지 맨 아래에 한 줄 띄우고 다음 줄을 추가한다:
     ```
     Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
     ```

7. **커밋 실행**
   - 하나의 논리적 변경마다 하나의 커밋으로 나눈다 (변경이 섞여 있으면 여러 번의 `git add` + `git commit`으로 분리 제안).
   - `--no-verify`, `--amend`, `--force` 등은 사용하지 않는다.
   - 커밋 후 `git status`, `git log -1`로 결과를 확인한다.

8. **Push는 하지 않는다**
   - 사용자가 명시적으로 요청하지 않는 한 push는 수행하지 않는다.
