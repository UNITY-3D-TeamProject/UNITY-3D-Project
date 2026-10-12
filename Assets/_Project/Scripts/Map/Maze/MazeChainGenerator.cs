using System.Collections.Generic;
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
        #endregion

        #region Serialized Fields
        [Header("Settings")]
        [Tooltip("미로 크기, 미로별 겉모습, 바닥·다리 프리팹을 담은 설정 에셋")]
        [SerializeField] private SOMazeChainSettings _settings;

        [Header("Checkpoints")]
        [Tooltip("씬에 미리 놓아 둔 체크포인트 (선택). 0번은 첫 미로의 입구 칸으로, 그 뒤는 차례대로 각 미로의 출구 밖 다리 가운데로 옮긴다.")]
        [SerializeField] private Transform[] _checkpointAnchors;
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

                var entranceCell = new Vector2Int(entranceColumn, 0);
                MazeGenerator.SCell[,] grid = generator.GenerateMaze(_settings.Size, _settings.Size, entranceCell, random);
                Vector2Int exitCell = FindFarthestExitCell(generator.ComputeDistances(grid, entranceCell));

                bool isFirstMaze = i == 0;
                bool isLastMaze = i == _settings.Themes.Count - 1;
                BuildMaze(mazeRoot, grid, _settings.Themes[i], entranceCell, exitCell, isFirstMaze, isLastMaze);
                BuildBridge(mazeRoot, exitCell.x);
                CollectPlacementPoints(mazeRoot, entranceCell, exitCell, placementPoints);

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
                if ((theme.WallPrefab == null) || (theme.GatePrefab == null)) return false;
            }

            return true;
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

        // 미로 하나의 바닥·벽·출구·입구 표시를 mazeRoot 아래에 놓는다. 칸 (x, y) 의 중앙은 (x * 칸 크기, 0, y * 칸 크기) 이다.
        private void BuildMaze(
            Transform mazeRoot,
            MazeGenerator.SCell[,] grid,
            SMazeTheme theme,
            Vector2Int entranceCell,
            Vector2Int exitCell,
            bool isFirstMaze,
            bool isLastMaze)
        {
            BuildFloor(mazeRoot);

            float halfTile = _settings.TileSize * 0.5f;
            Quaternion alongX = Quaternion.identity;
            Quaternion alongZ = Quaternion.Euler(0.0f, 90.0f, 0.0f);

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
                            PlaceObject(theme.WallPrefab, mazeRoot, center + (Vector3.forward * halfTile), alongX, $"Wall_N_{x}_{y}");
                        }
                    }

                    if (!cell.OpenEast)
                    {
                        PlaceObject(theme.WallPrefab, mazeRoot, center + (Vector3.right * halfTile), alongZ, $"Wall_E_{x}_{y}");
                    }

                    // 바깥 테두리의 서쪽·남쪽 벽
                    if (x == 0)
                    {
                        PlaceObject(theme.WallPrefab, mazeRoot, center + (Vector3.left * halfTile), alongZ, $"Wall_W_{x}_{y}");
                    }

                    // 첫 미로는 입구도 막혀 있고(안에서 시작), 나머지는 다리와 이어지도록 입구를 비운다.
                    bool isOpenEntrance = !isFirstMaze && (cellPosition == entranceCell);
                    if ((y == 0) && !isOpenEntrance)
                    {
                        PlaceObject(theme.WallPrefab, mazeRoot, center + (Vector3.back * halfTile), alongX, $"Wall_S_{x}_{y}");
                    }
                }
            }

            if (_settings.StartMarkerPrefab != null)
            {
                var markerPosition = new Vector3(entranceCell.x * _settings.TileSize, 0.0f, entranceCell.y * _settings.TileSize);
                PlaceObject(_settings.StartMarkerPrefab, mazeRoot, markerPosition, Quaternion.identity, "StartMarker");
            }
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
        private void CollectPlacementPoints(
            Transform mazeRoot,
            Vector2Int entranceCell,
            Vector2Int exitCell,
            List<Vector3> placementPoints)
        {
            for (int x = 0; x < _settings.Size; x++)
            {
                for (int y = 0; y < _settings.Size; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if ((cell == entranceCell) || (cell == exitCell)) continue;

                    placementPoints.Add(GetCellCenter(mazeRoot, cell));
                }
            }
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
