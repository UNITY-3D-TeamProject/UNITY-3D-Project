using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using Map.Common;

namespace Map.Maze
{
    /// <summary>
    /// 같은 크기의 정사각형 미로 여러 개를 한 줄(+Z 방향)로 미리 만들고 다리로 잇는다.
    /// 각 미로의 입구는 -Z 면, 출구는 +Z 면에 있고, 다음 미로의 입구는 앞 미로의 출구와 같은 열이라 다리는 항상 직선이다.
    /// 출구 칸에는 출구 프리팹(MazeExitGate)이 놓여, 플레이어가 도착하면 벽이 열린다.
    /// 마지막 미로의 출구 밖에도 다리 한 구간을 놓고, 그 가운데가 도착 지점(Goal Prefab 자리)이 된다.
    /// 벽은 타일 프리팹이 아니라 칸과 칸 사이에 벽 프리팹을 하나씩 세우는 방식이다.
    /// </summary>
    public class MazeChainGenerator : MapGeneratorBase
    {
        #region Constants
        private const float START_HEIGHT_OFFSET = 0.1f;   // 플레이어가 바닥에 파묻히지 않도록 시작 위치를 띄우는 높이
        private const float FLOOR_THICKNESS     = 0.5f;   // 바닥 두께. 윗면이 y = 0 이 되도록 아래로 내려 놓는다
        private const string BOUNDARY_WALLS_ROOT_NAME = "BoundaryWalls";
        private const string INNER_WALLS_ROOT_NAME    = "InnerWalls";
        #endregion

        #region Serialized Fields
        [Header("Settings")]
        [Tooltip("미로 크기, 미로별 겉모습, 바닥·다리 프리팹을 담은 설정 에셋")]
        [SerializeField] private SOMazeChainSettings _settings;

        [Header("Checkpoints")]
        [Tooltip("씬에 미리 놓아 둔 체크포인트 (선택). 0번은 첫 미로의 입구 칸으로, 그 뒤는 차례대로 각 미로의 출구 밖 다리 가운데로 옮긴다.")]
        [SerializeField] private Transform[] _checkpointAnchors;

        [Header("Waypoints")]
        [Tooltip("미로별 웨이포인트 묶음 (선택). 생성할 때마다 각 미로의 칸 가운데로 무작위로 옮기며, 그 칸에는 배치물을 놓지 않는다.")]
        [SerializeField] private SMazeWaypointGroup[] _waypointGroups;
        #endregion

        #region Protected Methods
        /// <inheritdoc />
        protected override void Build(
            Transform root,
            System.Random random,
            List<Vector3> placementPoints,
            out Vector3 startPosition,
            out Vector3 goalPosition)
        {
            startPosition = transform.position;
            goalPosition = transform.position;

            if (_settings == null)
            {
                Debug.LogError($"[{name}] 설정 에셋(Settings)이 연결되지 않았습니다.", this);
                return;
            }

            if (!HasAllPrefabs())
            {
                Debug.LogError($"[{name}] 미로 테마(벽·출구), 바닥, 다리 프리팹이 모두 연결되어야 합니다.", this);
                return;
            }

            var generator = new MazeGenerator();
            float bridgeLength = _settings.BridgeTileCount * _settings.TileSize;
            float mazePitch = (_settings.Size * _settings.TileSize) + bridgeLength;
            int entranceColumn = random.Next(_settings.Size);

            for (int i = 0; i < _settings.Themes.Count; i++)
            {
                Transform mazeRoot = new GameObject($"Maze_{i}").transform;
                mazeRoot.SetParent(root, false);
                mazeRoot.localPosition = new Vector3(0.0f, 0.0f, i * mazePitch);

                // NavMesh 를 만들지 않는 미로는 통째로 베이크에서 뺀다
                if (!_settings.Themes[i].ShouldBakeNavMesh)
                {
                    mazeRoot.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
                }

                var entranceCell = new Vector2Int(entranceColumn, 0);
                SMazeTheme theme = _settings.Themes[i];
                MazeGenerator.SCell[,] grid = generator.GenerateGrowingTreeMaze(
                    _settings.Size, _settings.Size, entranceCell, theme.NewestCellRatio, theme.BraidRatio, random);
                Vector2Int exitCell = FindFarthestExitCell(generator.ComputeDistances(grid, entranceCell));

                // 벽·기둥 프리팹 고르기는 따로 뽑은 난수를 써서, 프리팹 개수를 바꿔도 미로 모양과 배치가 그대로이게 한다
                var decorRandom = new System.Random(random.Next());

                bool isFirstMaze = i == 0;
                bool isLastMaze = i == _settings.Themes.Count - 1;
                BuildMaze(mazeRoot, grid, _settings.Themes[i], entranceCell, exitCell, isFirstMaze, isLastMaze, decorRandom);
                BuildBridge(mazeRoot, exitCell.x);

                // 웨이포인트 → 이 미로 전용 배치물 순서로 칸을 차지하고, 남은 칸은 맵 공통 배치에 넘긴다
                List<Vector2Int> freeCells = CollectFreeCells(entranceCell, exitCell);
                MoveWaypointAnchors(i, mazeRoot, freeCells, random);
                List<Vector2Int> solutionPath = generator.FindPathAStar(grid, entranceCell, exitCell);
                PlaceMazeObjects(mazeRoot, grid, _settings.Themes[i].Placements, solutionPath, freeCells, random);
                foreach (Vector2Int cell in freeCells)
                {
                    placementPoints.Add(GetCellCenter(mazeRoot, cell));
                }

                if (isFirstMaze)
                {
                    startPosition = GetCellCenter(mazeRoot, entranceCell) + (Vector3.up * START_HEIGHT_OFFSET);
                    MoveCheckpointAnchor(0, GetCellCenter(mazeRoot, entranceCell));
                }

                // 마지막 미로에서는 출구 밖 다리의 가운데가 도착 지점이 된다.
                float bridgeCenterZ = ((_settings.Size - 0.5f) * _settings.TileSize) + (bridgeLength * 0.5f);
                goalPosition = mazeRoot.TransformPoint(new Vector3(exitCell.x * _settings.TileSize, 0.0f, bridgeCenterZ));
                MoveCheckpointAnchor(i + 1, goalPosition);

                // 다음 미로는 이 미로의 출구와 같은 열에서 시작한다.
                entranceColumn = exitCell.x;
            }
        }
        #endregion

        #region Private Methods
        // 생성에 꼭 필요한 프리팹이 모두 연결되어 있는지 확인한다. (입구 표시 프리팹은 선택)
        private bool HasAllPrefabs()
        {
            if ((_settings.Themes == null) || (_settings.Themes.Count == 0)) return false;
            if ((_settings.FloorPrefab == null) || (_settings.BridgeSegmentPrefab == null)) return false;

            foreach (SMazeTheme theme in _settings.Themes)
            {
                if ((theme.WallPrefabs == null) || (theme.WallPrefabs.Length == 0) || (theme.GatePrefab == null)) return false;
                if (HasEmptySlot(theme.WallPrefabs) || HasEmptySlot(theme.PillarPrefabs)) return false;
            }

            return true;
        }

        // 목록에 비어 있는(null) 칸이 있는지
        private static bool HasEmptySlot(GameObject[] prefabs)
        {
            if (prefabs == null) return false;

            foreach (GameObject prefab in prefabs)
            {
                if (prefab == null) return true;
            }

            return false;
        }

        // 출구 면(+Z 쪽 마지막 줄)의 칸 가운데 입구에서 가장 먼 칸을 고른다.
        private Vector2Int FindFarthestExitCell(int[,] distances)
        {
            int exitRow = _settings.Size - 1;
            int farthestColumn = 0;

            for (int x = 1; x < _settings.Size; x++)
            {
                if (distances[x, exitRow] > distances[farthestColumn, exitRow])
                {
                    farthestColumn = x;
                }
            }

            return new Vector2Int(farthestColumn, exitRow);
        }

        // 미로 하나의 바닥·벽·기둥·출구·입구 표시를 mazeRoot 아래에 놓는다. 칸 (x, y) 의 중앙은 (x * 칸 크기, 0, y * 칸 크기) 이다.
        // 꼭짓점 (i, j) 는 칸 (i, j) 의 남서쪽 모서리, 즉 ((i - 0.5) * 칸 크기, 0, (j - 0.5) * 칸 크기) 이다.
        private void BuildMaze(
            Transform mazeRoot,
            MazeGenerator.SCell[,] grid,
            SMazeTheme theme,
            Vector2Int entranceCell,
            Vector2Int exitCell,
            bool isFirstMaze,
            bool isLastMaze,
            System.Random decorRandom)
        {
            BuildFloor(mazeRoot);

            Transform boundaryWallsRoot = CreateContainer(mazeRoot, BOUNDARY_WALLS_ROOT_NAME);
            Transform innerWallsRoot = CreateContainer(mazeRoot, INNER_WALLS_ROOT_NAME);
            mazeRoot.gameObject.AddComponent<MazeInnerWallLowering>().Setup(innerWallsRoot);

            float halfTile = _settings.TileSize * 0.5f;
            Quaternion alongX = Quaternion.identity;
            Quaternion alongZ = Quaternion.Euler(0.0f, 90.0f, 0.0f);
            var wallCorners = new HashSet<Vector2Int>();

            for (int x = 0; x < _settings.Size; x++)
            {
                for (int y = 0; y < _settings.Size; y++)
                {
                    MazeGenerator.SCell cell = grid[x, y];
                    var center = new Vector3(x * _settings.TileSize, 0.0f, y * _settings.TileSize);
                    var cellPosition = new Vector2Int(x, y);

                    // 각 칸은 북쪽·동쪽 벽만 세운다. (남쪽·서쪽은 이웃 칸이 세운다)
                    if (!cell.OpenNorth)
                    {
                        if (cellPosition == exitCell)
                        {
                            GameObject gate = PlaceObject(theme.GatePrefab.gameObject, mazeRoot, center, alongX, $"Gate_{x}_{y}");

                            // 마지막 미로의 문은 라운드를 클리어해야 열린다
                            gate.GetComponent<MazeExitGate>().ShouldRequireRoundClear = isLastMaze;
                        }
                        else
                        {
                            Transform parent = (y == _settings.Size - 1) ? boundaryWallsRoot : innerWallsRoot;
                            PlaceObject(PickPrefab(theme.WallPrefabs, decorRandom), parent, center + (Vector3.forward * halfTile), alongX, $"Wall_N_{x}_{y}");
                        }

                        wallCorners.Add(new Vector2Int(x, y + 1));
                        wallCorners.Add(new Vector2Int(x + 1, y + 1));
                    }

                    if (!cell.OpenEast)
                    {
                        Transform parent = (x == _settings.Size - 1) ? boundaryWallsRoot : innerWallsRoot;
                        PlaceObject(PickPrefab(theme.WallPrefabs, decorRandom), parent, center + (Vector3.right * halfTile), alongZ, $"Wall_E_{x}_{y}");
                        wallCorners.Add(new Vector2Int(x + 1, y));
                        wallCorners.Add(new Vector2Int(x + 1, y + 1));
                    }

                    // 바깥 테두리의 서쪽·남쪽 벽
                    if (x == 0)
                    {
                        PlaceObject(PickPrefab(theme.WallPrefabs, decorRandom), boundaryWallsRoot, center + (Vector3.left * halfTile), alongZ, $"Wall_W_{x}_{y}");
                        wallCorners.Add(new Vector2Int(x, y));
                        wallCorners.Add(new Vector2Int(x, y + 1));
                    }

                    // 첫 미로는 입구도 막혀 있고(안에서 시작), 나머지는 다리와 이어지도록 입구를 비운다.
                    bool isOpenEntrance = !isFirstMaze && (cellPosition == entranceCell);
                    if ((y == 0) && !isOpenEntrance)
                    {
                        PlaceObject(PickPrefab(theme.WallPrefabs, decorRandom), boundaryWallsRoot, center + (Vector3.back * halfTile), alongX, $"Wall_S_{x}_{y}");
                        wallCorners.Add(new Vector2Int(x, y));
                        wallCorners.Add(new Vector2Int(x + 1, y));
                    }
                }
            }

            BuildPillars(boundaryWallsRoot, innerWallsRoot, theme.PillarPrefabs, wallCorners, decorRandom);

            if (_settings.StartMarkerPrefab != null)
            {
                var markerPosition = new Vector3(entranceCell.x * _settings.TileSize, 0.0f, entranceCell.y * _settings.TileSize);
                PlaceObject(_settings.StartMarkerPrefab, mazeRoot, markerPosition, Quaternion.identity, "StartMarker");
            }
        }

        // 벽이 하나라도 닿는 꼭짓점마다 기둥을 세운다. 기둥 목록이 비어 있으면 세우지 않는다.
        private void BuildPillars(
            Transform boundaryWallsRoot,
            Transform innerWallsRoot,
            GameObject[] pillarPrefabs,
            HashSet<Vector2Int> wallCorners,
            System.Random decorRandom)
        {
            if ((pillarPrefabs == null) || (pillarPrefabs.Length == 0)) return;

            // 같은 시드에서 같은 기둥이 나오도록 HashSet 순서가 아닌 격자 순서로 돈다
            for (int i = 0; i <= _settings.Size; i++)
            {
                for (int j = 0; j <= _settings.Size; j++)
                {
                    if (!wallCorners.Contains(new Vector2Int(i, j))) continue;

                    var position = new Vector3((i - 0.5f) * _settings.TileSize, 0.0f, (j - 0.5f) * _settings.TileSize);
                    bool isBoundary = (i == 0) || (i == _settings.Size) || (j == 0) || (j == _settings.Size);
                    Transform parent = isBoundary ? boundaryWallsRoot : innerWallsRoot;
                    PlaceObject(PickPrefab(pillarPrefabs, decorRandom), parent, position, Quaternion.identity, $"Pillar_{i}_{j}");
                }
            }
        }

        private static GameObject PickPrefab(GameObject[] prefabs, System.Random random)
        {
            return prefabs[random.Next(prefabs.Length)];
        }

        // 미로 전체를 덮는 바닥 한 장
        private void BuildFloor(Transform mazeRoot)
        {
            float mazeLength = _settings.Size * _settings.TileSize;
            float centerOffset = (_settings.Size - 1) * _settings.TileSize * 0.5f;

            GameObject floor = Instantiate(_settings.FloorPrefab, mazeRoot);
            floor.name = "Floor";
            floor.transform.localPosition = new Vector3(centerOffset, -FLOOR_THICKNESS * 0.5f, centerOffset);
            floor.transform.localScale = new Vector3(mazeLength + _settings.WallThickness, FLOOR_THICKNESS, mazeLength + _settings.WallThickness);
        }

        // 출구 밖으로 이어지는 다리. 다음 미로의 입구(또는 도착 지점)까지 간다.
        private void BuildBridge(Transform mazeRoot, int exitColumn)
        {
            float bridgeStartZ = (_settings.Size - 0.5f) * _settings.TileSize;

            for (int i = 0; i < _settings.BridgeTileCount; i++)
            {
                var position = new Vector3(exitColumn * _settings.TileSize, 0.0f, bridgeStartZ + ((i + 0.5f) * _settings.TileSize));
                PlaceObject(_settings.BridgeSegmentPrefab, mazeRoot, position, Quaternion.identity, $"Bridge_{i}");
            }
        }

        // 입구·출구 칸에는 장애물이나 아이템을 놓지 않는다
        private List<Vector2Int> CollectFreeCells(Vector2Int entranceCell, Vector2Int exitCell)
        {
            var freeCells = new List<Vector2Int>();

            for (int x = 0; x < _settings.Size; x++)
            {
                for (int y = 0; y < _settings.Size; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if ((cell == entranceCell) || (cell == exitCell)) continue;

                    freeCells.Add(cell);
                }
            }

            return freeCells;
        }

        // 배치 목록의 순서대로, 규칙에 맞는 빈 칸에 +Z 를 통로 방향으로 돌려 놓는다. 차지한 칸은 freeCells 에서 빠진다.
        private void PlaceMazeObjects(
            Transform mazeRoot,
            MazeGenerator.SCell[,] grid,
            SMazePlacement[] placements,
            List<Vector2Int> solutionPath,
            List<Vector2Int> freeCells,
            System.Random random)
        {
            if (placements == null) return;

            foreach (SMazePlacement placement in placements)
            {
                if (placement.Prefab == null) continue;

                for (int i = 0; i < placement.Count; i++)
                {
                    if (!TryTakeCell(grid, placement, solutionPath, freeCells, random, out Vector2Int cell, out Vector2Int forward)) break;

                    Quaternion rotation = mazeRoot.rotation * Quaternion.LookRotation(new Vector3(forward.x, 0.0f, forward.y));
                    Vector3 position = GetCellCenter(mazeRoot, cell) + placement.Offset;
                    Instantiate(placement.Prefab, position, rotation, mazeRoot);
                }
            }
        }

        // 규칙에 맞는 칸을 무작위로 하나 고르고, 그 칸(과 함께 차지하는 칸)을 freeCells 에서 뺀다. forward 는 프리팹 +Z 가 향할 칸 방향이다.
        private static bool TryTakeCell(
            MazeGenerator.SCell[,] grid,
            SMazePlacement placement,
            List<Vector2Int> solutionPath,
            List<Vector2Int> freeCells,
            System.Random random,
            out Vector2Int cell,
            out Vector2Int forward)
        {
            cell = default;
            forward = default;

            if (placement.Rule == EMazeCellRule.SolutionPath)
            {
                if (solutionPath == null) return false;

                var candidates = new List<int>();
                for (int k = 0; k < solutionPath.Count; k++)
                {
                    if (IsPathRunAvailable(grid, solutionPath, k, placement.CellsBehind, placement.CellsAhead, freeCells))
                    {
                        candidates.Add(k);
                    }
                }

                if (candidates.Count == 0) return false;

                int picked = candidates[random.Next(candidates.Count)];
                cell = solutionPath[picked];
                forward = solutionPath[picked + 1] - solutionPath[picked];

                for (int k = picked - placement.CellsBehind; k <= picked + placement.CellsAhead; k++)
                {
                    freeCells.Remove(solutionPath[k]);
                }

                return true;
            }

            var candidateCells = new List<Vector2Int>();
            foreach (Vector2Int freeCell in freeCells)
            {
                MazeGenerator.SCell mazeCell = grid[freeCell.x, freeCell.y];
                bool isMatched = (placement.Rule == EMazeCellRule.Straight)
                    ? IsStraight(mazeCell)
                    : (GetOpenDirections(mazeCell).Count >= 2);   // 막다른 칸 제외

                if (isMatched)
                {
                    candidateCells.Add(freeCell);
                }
            }

            if (candidateCells.Count == 0) return false;

            cell = candidateCells[random.Next(candidateCells.Count)];
            List<Vector2Int> openDirections = GetOpenDirections(grid[cell.x, cell.y]);
            forward = openDirections[random.Next(openDirections.Count)];
            freeCells.Remove(cell);
            return true;
        }

        // 경로의 k 번째 칸이 앞뒤로 같은 직선 위에 있고, 차지할 칸(뒤 behind 칸 ~ 앞 ahead 칸)이 모두 비어 있는지 확인한다.
        // 놓인 칸 자체는 양쪽만 뚫린 직선 칸이어야 문이 통로를 다 막는다.
        private static bool IsPathRunAvailable(
            MazeGenerator.SCell[,] grid,
            List<Vector2Int> path,
            int k,
            int behind,
            int ahead,
            List<Vector2Int> freeCells)
        {
            int runStart = k - Mathf.Max(behind, 1);
            int runEnd = k + Mathf.Max(ahead, 1);
            if ((runStart < 0) || (runEnd >= path.Count)) return false;

            Vector2Int direction = path[k + 1] - path[k];
            for (int j = runStart; j < runEnd; j++)
            {
                if ((path[j + 1] - path[j]) != direction) return false;
            }

            if (!IsStraight(grid[path[k].x, path[k].y])) return false;

            for (int j = k - behind; j <= k + ahead; j++)
            {
                if (!freeCells.Contains(path[j])) return false;
            }

            // 브레이드로 순환이 생긴 미로에서는 돌아갈 길이 없는 칸이어야 문을 꼭 지나간다
            return !CanBypass(grid, path[0], path[path.Count - 1], path[k]);
        }

        // blockedCell 을 지나지 않고 start 에서 end 까지 갈 수 있는지
        private static bool CanBypass(MazeGenerator.SCell[,] grid, Vector2Int start, Vector2Int end, Vector2Int blockedCell)
        {
            var visited = new HashSet<Vector2Int> { start, blockedCell };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                if (cell == end) return true;

                foreach (Vector2Int direction in GetOpenDirections(grid[cell.x, cell.y]))
                {
                    Vector2Int next = cell + direction;
                    if (visited.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return false;
        }

        // 양쪽(남북 또는 동서)만 뚫린 직선 통로 칸인지
        private static bool IsStraight(MazeGenerator.SCell cell)
        {
            bool isNorthSouth = cell.OpenNorth && cell.OpenSouth && !cell.OpenEast && !cell.OpenWest;
            bool isEastWest = cell.OpenEast && cell.OpenWest && !cell.OpenNorth && !cell.OpenSouth;
            return isNorthSouth || isEastWest;
        }

        // 칸에서 뚫린 방향들 (북쪽이 +y = 월드 +Z)
        private static List<Vector2Int> GetOpenDirections(MazeGenerator.SCell cell)
        {
            var directions = new List<Vector2Int>(4);
            if (cell.OpenNorth) directions.Add(Vector2Int.up);
            if (cell.OpenEast) directions.Add(Vector2Int.right);
            if (cell.OpenSouth) directions.Add(Vector2Int.down);
            if (cell.OpenWest) directions.Add(Vector2Int.left);
            return directions;
        }

        // 칸의 바닥 중앙 (월드 좌표)
        private Vector3 GetCellCenter(Transform mazeRoot, Vector2Int cell)
        {
            return mazeRoot.TransformPoint(new Vector3(cell.x * _settings.TileSize, 0.0f, cell.y * _settings.TileSize));
        }

        // index 번째 체크포인트를 position 으로 옮긴다. 연결된 체크포인트가 모자라면 아무것도 하지 않는다.
        private void MoveCheckpointAnchor(int index, Vector3 position)
        {
            if ((_checkpointAnchors == null) || (index >= _checkpointAnchors.Length)) return;
            if (_checkpointAnchors[index] == null) return;

            _checkpointAnchors[index].position = position;
        }

        // mazeIndex 번 미로의 웨이포인트를 남은 칸 가운데 무작위로 골라 그 칸 중앙으로 옮긴다. 옮긴 칸은 cells 에서 빠진다.
        private void MoveWaypointAnchors(int mazeIndex, Transform mazeRoot, List<Vector2Int> cells, System.Random random)
        {
            if (_waypointGroups == null) return;

            foreach (SMazeWaypointGroup group in _waypointGroups)
            {
                if ((group.MazeIndex != mazeIndex) || (group.Anchors == null)) continue;

                foreach (Transform anchor in group.Anchors)
                {
                    if (anchor == null) continue;
                    if (cells.Count == 0) return;

                    int index = random.Next(cells.Count);
                    anchor.position = GetCellCenter(mazeRoot, cells[index]);
                    cells.RemoveAt(index);
                }
            }
        }

        private static Transform CreateContainer(Transform parent, string objectName)
        {
            var container = new GameObject(objectName).transform;
            container.SetParent(parent, false);
            return container;
        }

        // 프리팹을 parent 아래 지정한 로컬 위치·회전으로 놓는다.
        private static GameObject PlaceObject(
            GameObject prefab,
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            string objectName)
        {
            GameObject instance = Instantiate(prefab, parent);
            instance.name = objectName;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            return instance;
        }
        #endregion
    }
}
