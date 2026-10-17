namespace Map.Maze
{
    /// <summary>
    /// 미로 전용 배치물이 놓일 칸을 고르는 규칙. 어느 규칙이든 막다른 칸(통로가 한 방향뿐인 칸)에는 놓지 않는다.
    /// </summary>
    public enum EMazeCellRule
    {
        // 막다른 칸이 아닌 칸. 열린 방향 하나로 +Z 를 돌린다. 예: 프레스, 가시
        Corridor,

        // 양쪽만 뚫린 직선 통로 칸. +Z 를 통로 방향으로 돌린다. 예: 레이저, 화살 함정
        Straight,

        // 입구에서 출구로 가는 길 위의 직선 칸. +Z 가 출구 쪽을 향한다. 예: 일방통행 문, 감압판 문
        SolutionPath,
    }
}
