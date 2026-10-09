# 유니티 ↔ Claude Code MCP 연결 가이드

## 개요
유니티 에디터를 Claude Code가 직접 조작하도록(씬·컴포넌트 수정, 컴파일 확인, 플레이 테스트 등) 연결하는 방법이다. 사용 패키지는 Coplay의 `unity-mcp`(MCP for Unity v10.3.0)이다. 에디터 개발 도구일 뿐이라 게임 코드는 이 패키지에 의존하지 않는다.

## 준비물
- Unity 2021.3 LTS ~ 6.x
- Git (패키지를 Git 주소로 받기 때문)
- `uv` (MCP 서버를 `uvx`로 실행하므로 필수)
- Claude Code (데스크톱 앱 포함)

## 연결 순서

### 1. `uv` 설치 (가장 먼저)
PowerShell에서 아래 중 하나를 실행한다.

```
winget install --id=astral-sh.uv -e
```

```
powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
```

설치 후 **유니티를 완전히 종료했다가 다시 연다.** 이미 켜져 있던 유니티는 새 PATH를 읽지 못한다. (새 터미널에서 `uvx --version`이 나오면 설치 성공)

### 2. 유니티에 패키지 설치
Window → Package Manager → `+` → **Add package from git URL**

```
https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main
```

버전 고정은 끝의 `#main`을 `#v10.0.0`처럼 바꾼다.

### 3. MCP 창 열기
Window → MCP for Unity → **Toggle MCP Window** (`Ctrl+Shift+M`)

### 4. 서버 시작
- `Connect` 탭의 Transport는 `HTTP Local`(`http://127.0.0.1:8080`)로 둔다.
- `Start Server`를 누른다. 처음에는 의존성 설치 때문에 1분쯤 걸린다.
- `Session Active (프로젝트명)`에 초록 점이 뜨면 성공이다. 이때 콘솔에 `Server ready on http://127.0.0.1:8080`이 나온다.

### 5. Claude Code 등록
- `Client Configuration`에서 Client를 `Claude Code`로 선택한다.
- `Claude CLI Path`가 `Not found`이면 `Browse`로 `claude.exe`를 직접 지정한다. (아래 문제 해결 참고)
- `Configure`를 누른다. 빨간 `Not Configured`가 초록 `Configured`로 바뀌면 성공이다.
- 등록 결과는 `~/.claude.json`의 해당 프로젝트 항목에 `UnityMCP`로 저장된다. 프로젝트 범위(local)로만 등록되어 Git에 올라가지 않는다.

### 6. 새 Claude Code 세션 열기
등록 **이후에** 이 프로젝트에서 새 세션을 열어야 도구 목록에 유니티 항목이 나타난다. 이미 열려 있던 세션에는 추가되지 않는다. 유니티는 켜 둔 상태, `Stop Server`는 누르지 않은 상태여야 한다.

확인: 새 세션에서 "유니티 연결됐는지 확인해 줘"라고 요청한다.

## 문제 해결 (실제로 겪은 것)

| 증상 | 원인 | 해결 |
|---|---|---|
| 콘솔에 `'uvx'은(는) 내부 또는 외부 명령...이 아닙니다`, `WebSocket Connection failed`, `Local HTTP server did not become reachable` | `uv` 미설치 (서버가 뜨지 못함) | 1번대로 `uv` 설치 후 유니티 재시작, `Start Server` 다시 |
| `Configuration failed: Claude CLI not found` | Claude CLI 경로를 못 찾음 (데스크톱 앱은 PATH에 없음) | `Claude CLI Path`의 `Browse`로 `claude.exe` 지정 |
| `Browse`에서 `claude.exe`를 찾을 수 없다는 오류 | 스토어(MSIX)로 설치된 Claude 데스크톱 앱은 실제 파일이 `AppData\Roaming`이 아닌 `Packages` 폴더 안에 있음 | 아래 경로 형식을 파일 이름 칸에 붙여 넣기 |
| 서버는 켜졌는데 도구가 안 보임 | 등록 전에 열린 세션 | 등록 후 새 세션을 연다 |

`claude.exe` 경로 형식 (스토어 설치 기준, 버전·해시 폴더는 사람마다 다르다):

```
C:\Users\<사용자>\AppData\Local\Packages\Claude_<패키지ID>\LocalCache\Roaming\Claude\claude-code\<버전>\<해시>\claude.exe
```

앱이 업데이트되면 버전 폴더가 바뀌어 경로를 다시 지정해야 한다. 일반 설치(`claude`가 PATH에 있는 경우)는 자동 감지된다.

## 팀 공유 시 주의
- 패키지를 설치하면 `Packages/manifest.json`과 `packages-lock.json`이 바뀐다. 이 두 파일은 Git에 올라가서 팀 전원의 프로젝트에 패키지가 설치되므로, **개인 사용이면 커밋에서 제외**하고 팀 전체가 쓰기로 합의했을 때만 별도 커밋으로 올린다.
- MCP는 에디터를 직접 조작해(씬 수정, 에셋 삭제 등) 권한 확인이 뜰 수 있다. 내용을 보고 허용한다.
- 연결은 PC마다 개별 설정이다. 다른 팀원이 쓰려면 위 순서를 각자 진행해야 한다.

## 작업 기록
- 이 PC 기준 결과: 서버 `Session Active`, Claude Code `Configured`까지 확인했고, 유니티 도구는 새 세션에서 사용할 수 있다.
- 다음 할 일: 새 세션에서 `FallZoneTrigger` 연결, `StageManager`의 `_fallZones`·`_checkpoints`·`_roundCheckpointIndices` 연결, 컴파일 확인 ([낙하·사망 복구 구현 기록](./fall-death-recovery-implementation.md) 참고).
