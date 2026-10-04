using System.Collections.Generic;
using UnityEngine;

namespace Map.Maze
{
    public class MazeGenerator
    {
        public enum EDirection
        {
            North = 0,
            East = 1,
            South = 2,
            West = 3
        }

        public struct SCell
        {
            public int X;
            public int Y;
            public bool IsVisited;
            
            // True if there is a passage (no wall) in that direction
            public bool OpenNorth;
            public bool OpenEast;
            public bool OpenSouth;
            public bool OpenWest;
        }

        private int _width;
        private int _height;
        private SCell[,] _grid;
        private System.Random _random;

        /// <summary>
        /// 미로를 생성한다. 같은 random(시드)에서는 항상 같은 미로가 나온다.
        /// </summary>
        public SCell[,] GenerateMaze(int width, int height, Vector2Int startPos, System.Random random)
        {
            _random = random;
            _width = width;
            _height = height;
            _grid = new SCell[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _grid[x, y] = new SCell { X = x, Y = y };
                }
            }

            RecursiveBacktracking(startPos.x, startPos.y);
            return _grid;
        }

        private void RecursiveBacktracking(int x, int y)
        {
            _grid[x, y].IsVisited = true;

            List<EDirection> dirs = new List<EDirection>
            {
                EDirection.North, EDirection.East, EDirection.South, EDirection.West
            };
            
            // Shuffle directions (Fisher-Yates)
            for (int i = dirs.Count - 1; i > 0; i--)
            {
                int rnd = _random.Next(i + 1);
                EDirection temp = dirs[i];
                dirs[i] = dirs[rnd];
                dirs[rnd] = temp;
            }

            foreach (var dir in dirs)
            {
                int nx = x;
                int ny = y;

                switch (dir)
                {
                    case EDirection.North: ny += 1; break;
                    case EDirection.East:  nx += 1; break;
                    case EDirection.South: ny -= 1; break;
                    case EDirection.West:  nx -= 1; break;
                }

                if (nx >= 0 && nx < _width && ny >= 0 && ny < _height && !_grid[nx, ny].IsVisited)
                {
                    // Remove walls
                    switch (dir)
                    {
                        case EDirection.North:
                            _grid[x, y].OpenNorth = true;
                            _grid[nx, ny].OpenSouth = true;
                            break;
                        case EDirection.East:
                            _grid[x, y].OpenEast = true;
                            _grid[nx, ny].OpenWest = true;
                            break;
                        case EDirection.South:
                            _grid[x, y].OpenSouth = true;
                            _grid[nx, ny].OpenNorth = true;
                            break;
                        case EDirection.West:
                            _grid[x, y].OpenWest = true;
                            _grid[nx, ny].OpenEast = true;
                            break;
                    }
                    RecursiveBacktracking(nx, ny);
                }
            }
        }

        /// <summary>
        /// start 에서 각 칸까지 가는 데 필요한 칸 수를 구한다. 갈 수 없는 칸은 -1.
        /// 가장 먼 칸을 도착 지점으로 고르는 데 쓴다.
        /// </summary>
        public int[,] ComputeDistances(SCell[,] grid, Vector2Int start)
        {
            int w = grid.GetLength(0);
            int h = grid.GetLength(1);

            int[,] distances = new int[w, h];
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    distances[x, y] = -1;
                }
            }

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            distances[start.x, start.y] = 0;
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();

                foreach (var neighbor in GetNeighbors(grid, current))
                {
                    if (distances[neighbor.x, neighbor.y] >= 0) continue;

                    distances[neighbor.x, neighbor.y] = distances[current.x, current.y] + 1;
                    queue.Enqueue(neighbor);
                }
            }

            return distances;
        }

        public List<Vector2Int> FindPathAStar(SCell[,] grid, Vector2Int start, Vector2Int end)
        {
            int w = grid.GetLength(0);
            int h = grid.GetLength(1);

            List<Vector2Int> openSet = new List<Vector2Int>();
            HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

            Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();
            Dictionary<Vector2Int, float> fScore = new Dictionary<Vector2Int, float>();

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    Vector2Int pos = new Vector2Int(i, j);
                    gScore[pos] = float.MaxValue;
                    fScore[pos] = float.MaxValue;
                }
            }

            gScore[start] = 0;
            fScore[start] = Vector2.Distance(start, end);
            openSet.Add(start);

            while (openSet.Count > 0)
            {
                Vector2Int current = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (fScore[openSet[i]] < fScore[current])
                    {
                        current = openSet[i];
                    }
                }

                if (current == end)
                {
                    return ReconstructPath(cameFrom, current);
                }

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (var neighbor in GetNeighbors(grid, current))
                {
                    if (closedSet.Contains(neighbor)) continue;

                    float tentative_gScore = gScore[current] + 1; // All steps cost 1

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                    else if (tentative_gScore >= gScore[neighbor])
                    {
                        continue;
                    }

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentative_gScore;
                    fScore[neighbor] = gScore[neighbor] + Vector2.Distance(neighbor, end);
                }
            }

            return null; // No path found
        }

        private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            List<Vector2Int> totalPath = new List<Vector2Int> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                totalPath.Insert(0, current);
            }
            return totalPath;
        }

        private List<Vector2Int> GetNeighbors(SCell[,] grid, Vector2Int pos)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();
            SCell cell = grid[pos.x, pos.y];

            if (cell.OpenNorth) neighbors.Add(new Vector2Int(pos.x, pos.y + 1));
            if (cell.OpenEast)  neighbors.Add(new Vector2Int(pos.x + 1, pos.y));
            if (cell.OpenSouth) neighbors.Add(new Vector2Int(pos.x, pos.y - 1));
            if (cell.OpenWest)  neighbors.Add(new Vector2Int(pos.x - 1, pos.y));

            return neighbors;
        }
    }
}
