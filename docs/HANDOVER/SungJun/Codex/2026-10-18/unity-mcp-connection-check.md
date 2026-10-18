# Unity MCP 연결 상태 점검

## 작업 요약 및 방법
- `codex mcp list`에서 `unity-editor-mcp`, `unityMCP` 등록 상태를 확인했다.
- 현재 세션에 제공된 MCP 도구 목록과 `unity status`, `unity pipeline list` 결과를 확인했다.
- 샌드박스 외부 진단으로 다른 Unity 프로젝트만 응답 중이며 대상 프로젝트 Editor는 실행되지 않았음을 확인했다.
- `unity open`으로 대상 프로젝트를 열고 `unity mcp configure codex --local`로 프로젝트별 MCP 설정을 등록했다.
- `unity status`와 `unity command editor_status` 호출이 모두 `ready`를 반환했다.

## 결정 사항
- Codex 프로젝트별 MCP 설정을 `.codex/config.toml`에 등록했다.

## 현재 상태 및 이슈
- 대상 프로젝트의 Unity 6000.4.8f1 Editor가 Pipeline 포트 7801에서 `ready` 상태로 응답한다.
- 현재 채팅의 MCP 도구 목록은 시작 시점의 상태여서 Unity 도구가 아직 노출되지 않는다. 이 채팅에서는 Unity CLI를 통해 Editor 명령을 호출할 수 있다.

## 변경된 파일
- `.codex/config.toml`, 이 작업 기록, `HANDOFF_POINTER.md`.

## 다음 할 일
- 새 Codex 채팅에서 Unity MCP 도구가 노출되는지 확인한다.
