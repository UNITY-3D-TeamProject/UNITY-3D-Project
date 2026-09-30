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

        public SCell[,] GenerateMaze(int width, int height, Vector2Int startPos)
        {
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
            
            // Shuffle directions
            for (int i = 0; i < dirs.Count; i++)
            {
                int rnd = Random.Range(0, dirs.Count);
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
